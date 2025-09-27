using YoutubeDownloader.Infrastructure.Models;

namespace YoutubeDownloader.Infrastructure.Repositories;

public interface IVideoInfoRepository
{
    Task<VideoInfo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<VideoInfo?> GetByVideoIdAsync(string videoId, CancellationToken cancellationToken = default);
    Task<IEnumerable<VideoInfo>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<VideoInfo>> SearchByTitleAsync(string title, CancellationToken cancellationToken = default);
    Task<VideoInfo> CreateAsync(VideoInfo videoInfo, CancellationToken cancellationToken = default);
    Task UpdateAsync(VideoInfo videoInfo, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string videoId, CancellationToken cancellationToken = default);
}