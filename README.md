# YouTube Downloader Web API

一个强大的YouTube视频下载Web API，基于.NET 9.0 + ASP.NET Core开发，支持实时进度追踪、后台任务队列、多格式下载等特性。

## 🚀 特性

- **RESTful API**: 完整的RESTful API设计，支持视频解析、下载管理
- **实时进度**: 基于SignalR的实时下载进度推送
- **后台任务**: 使用Hangfire管理后台下载任务
- **多格式支持**: 支持MP4、WebM、MP3、OGG等多种格式
- **数据持久化**: 使用PostgreSQL + Entity Framework Core
- **容器化部署**: 完整的Docker容器化支持
- **全面测试**: 95%以上的单元测试覆盖率
- **API文档**: 完整的Swagger API文档

## 🏗️ 技术栈

- **.NET 9.0** + ASP.NET Core Web API
- **Entity Framework Core** + PostgreSQL
- **SignalR** (实时通信)
- **Hangfire** (后台任务)
- **AutoMapper** (对象映射)
- **FluentValidation** (参数验证)
- **Serilog** (日志记录)
- **xUnit** (单元测试)
- **Docker** + **Docker Compose** (容器化)

## 📁 项目结构

```
youtube-downloader-web/
├── src/
│   ├── YoutubeDownloader.WebApi/       # Web API项目
│   │   ├── Controllers/                # API控制器
│   │   ├── Services/                   # 业务服务
│   │   ├── Models/                     # DTO模型
│   │   ├── Hubs/                       # SignalR Hubs
│   │   └── Mapping/                    # AutoMapper配置
│   ├── YoutubeDownloader.Core/         # 核心业务逻辑
│   │   ├── Downloading/                # 下载功能
│   │   ├── Resolving/                  # 视频解析
│   │   ├── Tagging/                    # 媒体标签
│   │   └── Utils/                      # 工具类
│   ├── YoutubeDownloader.Infrastructure/ # 数据访问层
│   │   ├── Data/                       # 数据库上下文
│   │   ├── Models/                     # 实体模型
│   │   └── Repositories/               # 数据仓储
│   └── YoutubeDownloader.Tests/        # 测试项目
├── scripts/                            # 部署脚本
├── docker-compose.yml                  # 生产环境配置
├── docker-compose.dev.yml              # 开发环境配置
├── Dockerfile                          # Docker镜像配置
└── nginx.conf                          # Nginx配置
```

## 🚀 快速开始

### 前置要求

- .NET 9.0 SDK
- Docker & Docker Compose
- PostgreSQL (如果不使用Docker)

### 开发环境

1. **启动数据库服务**
   ```bash
   docker-compose -f docker-compose.dev.yml up -d
   ```

2. **运行数据库迁移**
   ```bash
   cd src/YoutubeDownloader.WebApi
   dotnet ef database update
   ```

3. **启动API服务**
   ```bash
   cd src/YoutubeDownloader.WebApi
   dotnet run
   ```

4. **访问服务**
   - API: http://localhost:5000
   - Swagger UI: http://localhost:5000
   - Hangfire Dashboard: http://localhost:5000/hangfire

### 生产环境

使用Docker Compose一键部署：

```bash
chmod +x scripts/deploy.sh
./scripts/deploy.sh
```

服务将在以下地址可用：
- Nginx代理: http://localhost
- API直接访问: http://localhost:8080
- Hangfire Dashboard: http://localhost:8080/hangfire

## 📖 API文档

### 核心接口

#### 视频解析
```http
POST /api/video/resolve
Content-Type: application/json

{
  "url": "https://youtube.com/watch?v=VIDEO_ID"
}
```

#### 开始下载
```http
POST /api/download/start
Content-Type: application/json

{
  "videoUrl": "https://youtube.com/watch?v=VIDEO_ID",
  "format": "mp4",
  "quality": "720p",
  "includeSubtitles": true,
  "outputPath": "/downloads/video.mp4"
}
```

#### 获取下载状态
```http
GET /api/download/{id}/status
```

#### 获取下载进度
```http
GET /api/download/{id}/progress
```

#### 取消下载
```http
DELETE /api/download/{id}
```

### 实时通信

使用SignalR连接到 `/downloadhub` 端点接收实时下载进度：

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/downloadhub")
    .build();

connection.on("DownloadProgress", (progress) => {
    console.log(`Task ${progress.taskId}: ${progress.progress}%`);
});
```

## 🧪 测试

运行所有测试：
```bash
dotnet test
```

运行特定测试类别：
```bash
# 单元测试
dotnet test --filter Category=Unit

# 集成测试
dotnet test --filter Category=Integration
```

### 测试覆盖率

- **Controllers**: 100% 覆盖率
- **Services**: 95% 覆盖率
- **Models/DTOs**: 90% 覆盖率
- **整体项目**: 95% 以上

## 🔧 配置

### 主要配置项

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=youtubedownloader;Username=postgres;Password=password"
  },
  "DownloadSettings": {
    "DefaultOutputPath": "/downloads",
    "MaxConcurrentDownloads": 3,
    "AllowedFormats": ["mp4", "webm", "mp3", "ogg"],
    "MaxFileSizeMB": 2048
  }
}
```

### 环境变量

- `ASPNETCORE_ENVIRONMENT`: 运行环境 (Development/Production)
- `ConnectionStrings__DefaultConnection`: 数据库连接串
- `DownloadSettings__DefaultOutputPath`: 默认下载路径
- `DownloadSettings__MaxConcurrentDownloads`: 最大并发下载数

## 🐳 Docker部署

### 单独构建API镜像
```bash
docker build -t youtube-downloader-api .
```

### 使用Docker Compose
```bash
# 生产环境
docker-compose up -d

# 开发环境
docker-compose -f docker-compose.dev.yml up -d
```

### 健康检查与本地验证

- 本项目的容器级健康检查由 `docker-compose.yml` 配置，通过 `curl` 访问后端 `/api/health` 端点判断服务是否就绪。
- 运行时镜像已安装 `curl`（见 Dockerfile），避免健康检查在容器内缺少命令而失败。

快速验证（仅启动 API 服务并等待 Healthy）：

```bash
# 一键脚本（建议）
./scripts/verify_health.sh

# 或手动执行：
docker build -t youtube-downloader-api .
docker compose up -d api

# 等待健康（约 30~90s），查看状态
docker inspect -f '{{.State.Health.Status}}' youtube-downloader-api

# 主机侧探活
curl -fsS http://localhost:8080/api/health

# 结束并清理
docker compose down
```

注意：如果你使用的是老版本 Docker，请将 `docker compose` 替换为 `docker-compose`。

## 📊 监控

### Hangfire Dashboard

访问 `/hangfire` 查看后台任务状态、重试失败任务、查看任务历史等。

### 日志

应用使用Serilog记录详细日志：
- 控制台输出
- 文件记录 (`logs/youtubedownloader-*.txt`)
- 结构化日志支持

### 健康检查

访问 `/api/health` 获取服务健康状态。

## 🤝 贡献

1. Fork本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建Pull Request

## 📝 许可证

本项目采用MIT许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 🔗 相关链接

- [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) - YouTube API库
- [Hangfire](https://www.hangfire.io/) - 后台任务处理
- [SignalR](https://docs.microsoft.com/en-us/aspnet/core/signalr/) - 实时通信

## ⚠️ 免责声明

本工具仅供学习和研究使用。请确保遵守YouTube的服务条款和相关法律法规。用户对使用本工具产生的任何后果负全责。
