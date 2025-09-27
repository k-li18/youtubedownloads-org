using YoutubeDownloader.Infrastructure.Models;

namespace YoutubeDownloader.Infrastructure.Repositories;

public interface IDownloadTaskRepository
{
    Task<DownloadTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DownloadTask?> GetByHangfireJobIdAsync(string jobId, CancellationToken cancellationToken = default);
    Task<IEnumerable<DownloadTask>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<DownloadTask>> GetByStatusAsync(DownloadStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<DownloadTask>> GetRecentAsync(int count = 50, CancellationToken cancellationToken = default);
    Task<DownloadTask> CreateAsync(DownloadTask downloadTask, CancellationToken cancellationToken = default);
    Task UpdateAsync(DownloadTask downloadTask, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}