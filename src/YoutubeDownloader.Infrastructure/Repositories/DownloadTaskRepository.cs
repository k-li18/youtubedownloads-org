using System.Collections.Concurrent;
using YoutubeDownloader.Infrastructure.Models;

namespace YoutubeDownloader.Infrastructure.Repositories;

public class DownloadTaskRepository : IDownloadTaskRepository
{
    private readonly ConcurrentDictionary<Guid, DownloadTask> _tasks = new();
    private const int MAX_TASKS = 5000; // 最大任务数限制，防止内存无限增长
    private const int CLEANUP_BATCH_SIZE = 500; // 每次清理的批次大小

    public Task<DownloadTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _tasks.TryGetValue(id, out var task);
        return Task.FromResult(task);
    }

    public Task<DownloadTask?> GetByHangfireJobIdAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var task = _tasks.Values.FirstOrDefault(x => x.HangfireJobId == jobId);
        return Task.FromResult(task);
    }

    public Task<IEnumerable<DownloadTask>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var tasks = _tasks.Values
            .OrderByDescending(x => x.CreatedAt)
            .AsEnumerable();
        return Task.FromResult(tasks);
    }

    public Task<IEnumerable<DownloadTask>> GetByStatusAsync(DownloadStatus status, CancellationToken cancellationToken = default)
    {
        var tasks = _tasks.Values
            .Where(x => x.Status == status)
            .OrderByDescending(x => x.CreatedAt)
            .AsEnumerable();
        return Task.FromResult(tasks);
    }

    public Task<IEnumerable<DownloadTask>> GetRecentAsync(int count = 50, CancellationToken cancellationToken = default)
    {
        var tasks = _tasks.Values
            .OrderByDescending(x => x.CreatedAt)
            .Take(count)
            .AsEnumerable();
        return Task.FromResult(tasks);
    }

    public Task<DownloadTask> CreateAsync(DownloadTask downloadTask, CancellationToken cancellationToken = default)
    {
        // 检查容量限制，如果超过则清理旧任务
        if (_tasks.Count >= MAX_TASKS)
        {
            CleanupOldCompletedTasks();
        }
        
        _tasks.TryAdd(downloadTask.Id, downloadTask);
        return Task.FromResult(downloadTask);
    }

    public Task UpdateAsync(DownloadTask downloadTask, CancellationToken cancellationToken = default)
    {
        _tasks.TryUpdate(downloadTask.Id, downloadTask, _tasks[downloadTask.Id]);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _tasks.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exists = _tasks.ContainsKey(id);
        return Task.FromResult(exists);
    }

    /// <summary>
    /// 清理旧的已完成/失败/取消的任务，防止内存无限增长
    /// </summary>
    private void CleanupOldCompletedTasks()
    {
        try
        {
            // 获取所有已完成、失败或取消的任务，按创建时间排序
            var completedTasks = _tasks.Values
                .Where(t => t.Status == DownloadStatus.Completed || 
                           t.Status == DownloadStatus.Failed || 
                           t.Status == DownloadStatus.Cancelled)
                .OrderBy(t => t.CreatedAt)
                .Take(CLEANUP_BATCH_SIZE)
                .ToList();

            // 如果没有已完成的任务可清理，则清理最老的任务（除了正在进行的）
            if (completedTasks.Count == 0)
            {
                var oldTasks = _tasks.Values
                    .Where(t => t.Status != DownloadStatus.InProgress)
                    .OrderBy(t => t.CreatedAt)
                    .Take(CLEANUP_BATCH_SIZE / 2) // 清理较少的任务
                    .ToList();
                
                completedTasks = oldTasks;
            }

            // 删除选中的任务
            foreach (var task in completedTasks)
            {
                _tasks.TryRemove(task.Id, out _);
            }

            // 简单日志记录（由于这是基础设施层，不直接使用ILogger）
            if (completedTasks.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"DownloadTaskRepository: Cleaned up {completedTasks.Count} old tasks. Current count: {_tasks.Count}");
            }
        }
        catch (Exception ex)
        {
            // 静默处理异常，确保不影响主要功能
            System.Diagnostics.Debug.WriteLine($"DownloadTaskRepository cleanup error: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取当前存储的任务数量（用于监控）
    /// </summary>
    public int GetTaskCount() => _tasks.Count;
}