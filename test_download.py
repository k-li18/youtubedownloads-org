#!/usr/bin/env python3
import requests
import json
import time
import sys

# 定义测试配置
API_BASE_URL = "http://localhost:5261"
TEST_YOUTUBE_URL = "https://www.youtube.com/watch?v=dQw4w9WgXcQ"  # Rick Astley - Never Gonna Give You Up (popular test video)

class YouTubeDownloadTester:
    def __init__(self, base_url):
        self.base_url = base_url
        self.session = requests.Session()
        self.test_results = {
            "environment_check": None,
            "url_resolution": None,
            "download_start": None,
            "progress_monitoring": None,
            "download_completion": None,
            "errors": []
        }
    
    def log(self, message, level="INFO"):
        timestamp = time.strftime("%Y-%m-%d %H:%M:%S")
        print(f"[{timestamp}] [{level}] {message}")
    
    def test_api_health(self):
        """测试API健康状态"""
        self.log("=== 步骤1: 测试API健康状态 ===")
        try:
            response = self.session.get(f"{self.base_url}/api/health", timeout=10)
            if response.status_code == 200:
                data = response.json()
                self.log(f"✅ API健康检查通过: {data['data']['status']}")
                self.test_results["environment_check"] = {"success": True, "data": data}
                return True
            else:
                self.log(f"❌ API健康检查失败: HTTP {response.status_code}")
                self.test_results["environment_check"] = {"success": False, "error": f"HTTP {response.status_code}"}
                return False
        except Exception as e:
            self.log(f"❌ API健康检查异常: {str(e)}", "ERROR")
            self.test_results["environment_check"] = {"success": False, "error": str(e)}
            return False
    
    def test_video_resolution(self, video_url):
        """测试视频URL解析功能"""
        self.log("=== 步骤2: 测试视频URL解析功能 ===")
        try:
            payload = {"url": video_url}
            response = self.session.post(
                f"{self.base_url}/api/video/resolve",
                json=payload,
                headers={"Content-Type": "application/json"},
                timeout=30
            )
            
            self.log(f"解析请求状态码: {response.status_code}")
            
            if response.status_code == 200:
                data = response.json()
                if data.get("success"):
                    video_info = data.get("data", {})
                    self.log(f"✅ 视频解析成功")
                    self.log(f"   - 标题: {video_info.get('title', 'N/A')}")
                    self.log(f"   - 作者: {video_info.get('author', 'N/A')}")
                    self.log(f"   - 时长: {video_info.get('duration', 'N/A')}")
                    self.test_results["url_resolution"] = {"success": True, "data": video_info}
                    return True
                else:
                    error_msg = data.get("message", "未知错误")
                    self.log(f"❌ 视频解析失败: {error_msg}")
                    self.test_results["url_resolution"] = {"success": False, "error": error_msg}
                    return False
            else:
                error_text = response.text
                self.log(f"❌ 视频解析请求失败: HTTP {response.status_code} - {error_text}")
                self.test_results["url_resolution"] = {"success": False, "error": f"HTTP {response.status_code}"}
                return False
                
        except Exception as e:
            self.log(f"❌ 视频解析异常: {str(e)}", "ERROR")
            self.test_results["url_resolution"] = {"success": False, "error": str(e)}
            return False
    
    def test_download_start(self, video_url, format_type="mp4", quality="720p"):
        """测试下载启动"""
        self.log("=== 步骤3: 测试下载启动 ===")
        try:
            payload = {
                "videoUrl": video_url,
                "format": format_type,
                "quality": quality
            }
            
            response = self.session.post(
                f"{self.base_url}/api/download/start",
                json=payload,
                headers={"Content-Type": "application/json"},
                timeout=30
            )
            
            self.log(f"下载启动请求状态码: {response.status_code}")
            
            if response.status_code == 200:
                data = response.json()
                if data.get("success"):
                    task_info = data.get("data", {})
                    task_id = task_info.get("id")
                    self.log(f"✅ 下载任务启动成功, 任务ID: {task_id}")
                    self.test_results["download_start"] = {"success": True, "task_id": task_id, "data": task_info}
                    return task_id
                else:
                    error_msg = data.get("message", "未知错误")
                    self.log(f"❌ 下载启动失败: {error_msg}")
                    self.test_results["download_start"] = {"success": False, "error": error_msg}
                    return None
            else:
                error_text = response.text
                self.log(f"❌ 下载启动请求失败: HTTP {response.status_code} - {error_text}")
                self.test_results["download_start"] = {"success": False, "error": f"HTTP {response.status_code}"}
                return None
                
        except Exception as e:
            self.log(f"❌ 下载启动异常: {str(e)}", "ERROR")
            self.test_results["download_start"] = {"success": False, "error": str(e)}
            return None
    
    def monitor_download_progress(self, task_id, max_duration=120):
        """监控下载进度"""
        self.log("=== 步骤4: 监控下载进度 ===")
        if not task_id:
            self.log("❌ 无有效任务ID，跳过进度监控")
            return False
            
        start_time = time.time()
        progress_history = []
        
        try:
            while time.time() - start_time < max_duration:
                response = self.session.get(f"{self.base_url}/api/download/{task_id}/progress", timeout=10)
                
                if response.status_code == 200:
                    data = response.json()
                    if data.get("success"):
                        progress_data = data.get("data", {})
                        progress = progress_data.get("progress", 0)
                        status = progress_data.get("status", "Unknown")
                        
                        progress_history.append({
                            "timestamp": time.time(),
                            "progress": progress,
                            "status": status
                        })
                        
                        self.log(f"⏳ 下载进度: {progress:.1f}% - 状态: {status}")
                        
                        if status == "Completed":
                            self.log("✅ 下载完成！")
                            self.test_results["progress_monitoring"] = {"success": True, "final_status": status, "history": progress_history}
                            self.test_results["download_completion"] = {"success": True, "final_progress": progress}
                            return True
                        elif status == "Failed":
                            self.log("❌ 下载失败！")
                            self.test_results["progress_monitoring"] = {"success": False, "final_status": status, "history": progress_history}
                            self.test_results["download_completion"] = {"success": False, "error": "下载任务失败"}
                            return False
                        elif status == "Downloading":
                            time.sleep(2)  # 等待2秒后再次检查
                        else:
                            time.sleep(1)
                    else:
                        error_msg = data.get("message", "未知错误")
                        self.log(f"❌ 进度查询失败: {error_msg}")
                        break
                else:
                    self.log(f"❌ 进度查询请求失败: HTTP {response.status_code}")
                    break
            
            # 超时
            self.log("⚠️  下载监控超时")
            self.test_results["progress_monitoring"] = {"success": False, "error": "监控超时", "history": progress_history}
            self.test_results["download_completion"] = {"success": False, "error": "下载超时"}
            return False
            
        except Exception as e:
            self.log(f"❌ 进度监控异常: {str(e)}", "ERROR")
            self.test_results["progress_monitoring"] = {"success": False, "error": str(e)}
            return False
    
    def run_full_test(self, video_url):
        """执行完整的下载测试流程"""
        self.log("🚀 开始YouTube视频下载系统功能测试")
        self.log(f"测试视频URL: {video_url}")
        
        # 步骤1: API健康检查
        if not self.test_api_health():
            self.log("❌ 环境检查失败，测试终止")
            return self.test_results
        
        # 步骤2: 视频URL解析
        if not self.test_video_resolution(video_url):
            self.log("❌ 视频解析失败，测试终止")
            return self.test_results
        
        # 步骤3: 启动下载
        task_id = self.test_download_start(video_url)
        if not task_id:
            self.log("❌ 下载启动失败，测试终止")
            return self.test_results
        
        # 步骤4: 监控下载进度
        self.monitor_download_progress(task_id)
        
        return self.test_results

    def generate_report(self):
        """生成测试报告"""
        print("\n" + "="*60)
        print("📊 测试结果汇总:")
        print("="*60)

        step_names = {
            "environment_check": "环境检查",
            "url_resolution": "视频URL解析",
            "download_start": "下载启动",
            "progress_monitoring": "进度监控", 
            "download_completion": "下载完成"
        }

        all_success = True
        for step, result in self.test_results.items():
            if result and step in step_names:
                status = "✅ 通过" if result.get("success") else "❌ 失败"
                print(f"{step_names[step]}: {status}")
                if not result.get("success"):
                    all_success = False
                    if result.get("error"):
                        print(f"   错误: {result['error']}")

        print("\n" + "="*60)
        if all_success:
            print("🎉 所有测试通过！YouTube下载功能运行正常。")
        else:
            print("⚠️  部分测试失败，需要进一步诊断。")
        
        return self.test_results

# 主函数
if __name__ == "__main__":
    print("正在启动YouTube下载器系统功能测试...")
    tester = YouTubeDownloadTester(API_BASE_URL)
    results = tester.run_full_test(TEST_YOUTUBE_URL)
    tester.generate_report()