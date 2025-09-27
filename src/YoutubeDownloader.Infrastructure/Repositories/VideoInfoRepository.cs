using System.Collections.Concurrent;
using YoutubeDownloader.Infrastructure.Models;

namespace YoutubeDownloader.Infrastructure.Repositories;

public class VideoInfoRepository : IVideoInfoRepository
{
    private readonly ConcurrentDictionary<Guid, VideoInfo> _videoInfos = new();
    private const int MAX_VIDEO_INFOS = 3000; // 最大视频信息数限制
    private const int CLEANUP_BATCH_SIZE = 300; // 每次清理的批次大小

    public Task<VideoInfo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _videoInfos.TryGetValue(id, out var videoInfo);
        return Task.FromResult(videoInfo);
    }

    public Task<VideoInfo?> GetByVideoIdAsync(string videoId, CancellationToken cancellationToken = default)
    {
        var videoInfo = _videoInfos.Values.FirstOrDefault(x => x.VideoId == videoId);
        return Task.FromResult(videoInfo);
    }

    public Task<IEnumerable<VideoInfo>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var videoInfos = _videoInfos.Values
            .OrderByDescending(x => x.CreatedAt)
            .AsEnumerable();
        return Task.FromResult(videoInfos);
    }

    public Task<IEnumerable<VideoInfo>> SearchByTitleAsync(string title, CancellationToken cancellationToken = default)
    {
        var videoInfos = _videoInfos.Values
            .Where(x => x.Title.Contains(title, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.CreatedAt)
            .AsEnumerable();
        return Task.FromResult(videoInfos);
    }

    public Task<VideoInfo> CreateAsync(VideoInfo videoInfo, CancellationToken cancellationToken = default)
    {
        // 检查容量限制，如果超过则清理旧数据
        if (_videoInfos.Count >= MAX_VIDEO_INFOS)
        {
            CleanupOldVideoInfos();
        }
        
        _videoInfos.TryAdd(videoInfo.Id, videoInfo);
        return Task.FromResult(videoInfo);
    }

    public Task UpdateAsync(VideoInfo videoInfo, CancellationToken cancellationToken = default)
    {
        _videoInfos.TryUpdate(videoInfo.Id, videoInfo, _videoInfos[videoInfo.Id]);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _videoInfos.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string videoId, CancellationToken cancellationToken = default)
    {
        var exists = _videoInfos.Values.Any(x => x.VideoId == videoId);
        return Task.FromResult(exists);
    }

    /// <summary>
    /// 清理旧的视频信息，防止内存无限增长
    /// </summary>
    private void CleanupOldVideoInfos()
    {
        try
        {
            // 按创建时间排序，删除最老的记录
            var oldVideoInfos = _videoInfos.Values
                .OrderBy(v => v.CreatedAt)
                .Take(CLEANUP_BATCH_SIZE)
                .ToList();

            // 删除选中的视频信息
            foreach (var videoInfo in oldVideoInfos)
            {
                _videoInfos.TryRemove(videoInfo.Id, out _);
            }

            // 简单日志记录
            if (oldVideoInfos.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"VideoInfoRepository: Cleaned up {oldVideoInfos.Count} old video infos. Current count: {_videoInfos.Count}");
            }
        }
        catch (Exception ex)
        {
            // 静默处理异常，确保不影响主要功能
            System.Diagnostics.Debug.WriteLine($"VideoInfoRepository cleanup error: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取当前存储的视频信息数量（用于监控）
    /// </summary>
    public int GetVideoInfoCount() => _videoInfos.Count;
}