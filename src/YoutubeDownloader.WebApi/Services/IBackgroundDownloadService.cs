namespace YoutubeDownloader.WebApi.Services;

public interface IBackgroundDownloadService
{
    Task ExecuteDownloadAsync(Guid taskId, CancellationToken cancellationToken = default);
}