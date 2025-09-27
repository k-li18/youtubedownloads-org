using YoutubeDownloader.WebApi.Models;

namespace YoutubeDownloader.WebApi.Services;

public interface IVideoService
{
    Task<VideoInfoDto> ResolveAsync(string url, CancellationToken cancellationToken = default);
    Task<IEnumerable<DownloadOptionDto>> GetDownloadOptionsAsync(string videoId, CancellationToken cancellationToken = default);
}