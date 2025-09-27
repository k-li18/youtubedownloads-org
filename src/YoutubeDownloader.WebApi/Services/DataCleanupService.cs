using Microsoft.Extensions.Options;
using YoutubeDownloader.Infrastructure.Models;
using YoutubeDownloader.Infrastructure.Repositories;
using YoutubeDownloader.WebApi.Models;

namespace YoutubeDownloader.WebApi.Services;

/// <summary>
/// 定期清理过期的下载任务和视频信息数据，防止内存累积
/// </summary>
public class DataCleanupService : BackgroundService
{
    private readonly ILogger<DataCleanupService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<DownloadSettings> _settings;
    private readonly TimeSpan _interval;

    public DataCleanupService(
        IServiceProvider serviceProvider,
        IOptions<DownloadSettings> settings,
        ILogger<DataCleanupService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // 测试时使用短间隔，生产时使用较长间隔
        _interval = TimeSpan.FromMinutes(_settings.Value.DataCleanupIntervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DataCleanupService started. Cleanup interval: {Interval} minutes, Data retention: {Retention} days",
            _interval.TotalMinutes, _settings.Value.DataRetentionDays);

        // 启动后稍等片刻再执行第一次清理
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        
        try 
        { 
            await CleanupExpiredDataAsync(stoppingToken); 
        } 
        catch (Exception ex) 
        { 
            _logger.LogWarning(ex, "Initial data cleanup failed"); 
        }

        var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CleanupExpiredDataAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Periodic data cleanup failed");
            }
        }
    }

    private async Task CleanupExpiredDataAsync(CancellationToken cancellationToken)
    {
        var retentionDays = Math.Max(1, _settings.Value.DataRetentionDays);
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        _logger.LogDebug("Data cleanup: scanning for data older than {CutoffDate:yyyy-MM-dd HH:mm:ss} UTC", cutoffDate);

        using var scope = _serviceProvider.CreateScope();
        var downloadTaskRepo = scope.ServiceProvider.GetRequiredService<IDownloadTaskRepository>();
        var videoInfoRepo = scope.ServiceProvider.GetRequiredService<IVideoInfoRepository>();

        var cleanupTasks = new List<Task>
        {
            CleanupExpiredDownloadTasksAsync(downloadTaskRepo, cutoffDate, cancellationToken),
            CleanupExpiredVideoInfoAsync(videoInfoRepo, cutoffDate, cancellationToken)
        };

        await Task.WhenAll(cleanupTasks);
    }

    private async Task CleanupExpiredDownloadTasksAsync(IDownloadTaskRepository repository, DateTime cutoffDate, CancellationToken cancellationToken)
    {
        try
        {
            var allTasks = await repository.GetAllAsync(cancellationToken);
            var expiredTasks = allTasks.Where(task => IsTaskExpired(task, cutoffDate)).ToList();

            if (expiredTasks.Count == 0)
            {
                _logger.LogDebug("Data cleanup: no expired download tasks found");
                return;
            }

            _logger.LogInformation("Data cleanup: found {Count} expired download tasks", expiredTasks.Count);

            foreach (var task in expiredTasks)
            {
                try
                {
                    await repository.DeleteAsync(task.Id, cancellationToken);
                    _logger.LogDebug("Data cleanup: deleted download task {TaskId} (status: {Status}, created: {Created:yyyy-MM-dd HH:mm:ss})", 
                        task.Id, task.Status, task.CreatedAt);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Data cleanup: failed to delete download task {TaskId}", task.Id);
                }
            }

            _logger.LogInformation("Data cleanup: successfully deleted {Count} expired download tasks", expiredTasks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data cleanup: error during download tasks cleanup");
        }
    }

    private async Task CleanupExpiredVideoInfoAsync(IVideoInfoRepository repository, DateTime cutoffDate, CancellationToken cancellationToken)
    {
        try
        {
            var allVideos = await repository.GetAllAsync(cancellationToken);
            var expiredVideos = allVideos.Where(video => video.CreatedAt < cutoffDate).ToList();

            if (expiredVideos.Count == 0)
            {
                _logger.LogDebug("Data cleanup: no expired video info found");
                return;
            }

            _logger.LogInformation("Data cleanup: found {Count} expired video info records", expiredVideos.Count);

            foreach (var video in expiredVideos)
            {
                try
                {
                    await repository.DeleteAsync(video.Id, cancellationToken);
                    _logger.LogDebug("Data cleanup: deleted video info {VideoId} (title: {Title}, created: {Created:yyyy-MM-dd HH:mm:ss})", 
                        video.Id, video.Title, video.CreatedAt);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Data cleanup: failed to delete video info {VideoId}", video.Id);
                }
            }

            _logger.LogInformation("Data cleanup: successfully deleted {Count} expired video info records", expiredVideos.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data cleanup: error during video info cleanup");
        }
    }

    private static bool IsTaskExpired(DownloadTask task, DateTime cutoffDate)
    {
        // 只清理已完成、失败或取消的任务
        if (task.Status == DownloadStatus.Pending || task.Status == DownloadStatus.InProgress)
        {
            return false;
        }

        // 基于任务完成时间或创建时间判断是否过期
        var referenceDate = task.CompletedAt ?? task.CreatedAt;
        return referenceDate < cutoffDate;
    }
}