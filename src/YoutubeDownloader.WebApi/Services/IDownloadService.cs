using YoutubeDownloader.WebApi.Models;

namespace YoutubeDownloader.WebApi.Services;

public interface IDownloadService
{
    Task<DownloadTaskDto> StartDownloadAsync(DownloadRequest request, CancellationToken cancellationToken = default);
    Task<DownloadTaskDto?> GetDownloadStatusAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<IEnumerable<DownloadTaskDto>> GetAllDownloadsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<DownloadTaskDto>> GetRecentDownloadsAsync(int count = 50, CancellationToken cancellationToken = default);
    Task<bool> CancelDownloadAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<bool> DeleteDownloadAsync(Guid taskId, CancellationToken cancellationToken = default);
}