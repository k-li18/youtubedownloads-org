using AutoMapper;
using YoutubeDownloader.Core.Downloading;
using YoutubeDownloader.Core.Resolving;
using YoutubeDownloader.WebApi.Models;
using YoutubeDownloader.WebApi.Services;
using YoutubeExplode.Videos;

namespace YoutubeDownloader.WebApi.Services;

public class VideoService : IVideoService
{
    private readonly QueryResolver _queryResolver;
    private readonly VideoDownloader _videoDownloader;
    private readonly IMapper _mapper;
    private readonly ILogger<VideoService> _logger;

    public VideoService(IMapper mapper, ILogger<VideoService> logger)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _queryResolver = new QueryResolver();
        _videoDownloader = new VideoDownloader();
    }

    public async Task<VideoInfoDto> ResolveAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be null or empty", nameof(url));
            
        try
        {
            _logger.LogInformation("Resolving video from URL: {Url}", url);
            
            var queryResult = await _queryResolver.ResolveAsync(url, cancellationToken);
            
            if (queryResult.Videos.Count == 0)
            {
                throw new InvalidOperationException("No videos found for the provided URL");
            }

            var video = queryResult.Videos.First();
            var videoDto = _mapper.Map<VideoInfoDto>(video);

            // Get download options
            videoDto.DownloadOptions = (await GetDownloadOptionsAsync(video.Id, cancellationToken)).ToList();

            _logger.LogInformation("Successfully resolved video: {Title}", video.Title);
            return videoDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving video from URL: {Url}", url);
            throw;
        }
    }

    public async Task<IEnumerable<DownloadOptionDto>> GetDownloadOptionsAsync(string videoId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting download options for video: {VideoId}", videoId);
            
            var options = await _videoDownloader.GetDownloadOptionsAsync(VideoId.Parse(videoId), true, cancellationToken);
            
            var optionDtos = options.Select(option => new DownloadOptionDto
            {
                Container = option.Container.Name,
                IsAudioOnly = option.IsAudioOnly,
                VideoQuality = option.VideoQuality?.ToString(),
                AudioBitrate = option.StreamInfos
                    .OfType<YoutubeExplode.Videos.Streams.IAudioStreamInfo>()
                    .FirstOrDefault()?.Bitrate.ToString(),
                FileSize = option.StreamInfos.Sum(s => s.Size.Bytes)
            });

            _logger.LogInformation("Found {Count} download options for video: {VideoId}", optionDtos.Count(), videoId);
            return optionDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting download options for video: {VideoId}", videoId);
            throw;
        }
    }
}