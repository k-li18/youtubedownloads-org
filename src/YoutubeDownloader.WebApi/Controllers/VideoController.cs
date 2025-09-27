using Microsoft.AspNetCore.Mvc;
using YoutubeDownloader.WebApi.Models;
using YoutubeDownloader.WebApi.Services;

namespace YoutubeDownloader.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VideoController : ControllerBase
{
    private readonly IVideoService _videoService;
    private readonly ILogger<VideoController> _logger;

    public VideoController(IVideoService videoService, ILogger<VideoController> logger)
    {
        _videoService = videoService;
        _logger = logger;
    }

    /// <summary>
    /// Resolve video information from a URL
    /// </summary>
    /// <param name="request">Video resolve request containing the URL</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Video information with available download options</returns>
    [HttpPost("resolve")]
    public async Task<ActionResult<ApiResponse<VideoInfoDto>>> ResolveVideo(
        [FromBody] VideoResolveRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<VideoInfoDto>.ErrorResult(
                    "VALIDATION_ERROR",
                    "Invalid request data",
                    ModelState));
            }

            _logger.LogInformation("Resolving video from URL: {Url}", request.Url);

            var videoInfo = await _videoService.ResolveAsync(request.Url, cancellationToken);

            return Ok(ApiResponse<VideoInfoDto>.SuccessResult(
                videoInfo, 
                "Video resolved successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid URL provided: {Url}", request.Url);
            return BadRequest(ApiResponse<VideoInfoDto>.ErrorResult(
                "INVALID_URL",
                "The provided URL is not valid",
                new { url = request.Url }));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Video not found or not accessible: {Url}", request.Url);
            return NotFound(ApiResponse<VideoInfoDto>.ErrorResult(
                "VIDEO_NOT_FOUND",
                "Video not found or not accessible",
                new { url = request.Url }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving video from URL: {Url}", request.Url);
            return StatusCode(500, ApiResponse<VideoInfoDto>.ErrorResult(
                "INTERNAL_ERROR",
                "An internal error occurred while resolving the video"));
        }
    }

    /// <summary>
    /// Get available download options for a video
    /// </summary>
    /// <param name="videoId">The video ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available download options</returns>
    [HttpGet("{videoId}/options")]
    public async Task<ActionResult<ApiResponse<IEnumerable<DownloadOptionDto>>>> GetDownloadOptions(
        string videoId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(videoId))
            {
                return BadRequest(ApiResponse<IEnumerable<DownloadOptionDto>>.ErrorResult(
                    "INVALID_VIDEO_ID",
                    "Video ID cannot be empty"));
            }

            _logger.LogInformation("Getting download options for video: {VideoId}", videoId);

            var options = await _videoService.GetDownloadOptionsAsync(videoId, cancellationToken);

            return Ok(ApiResponse<IEnumerable<DownloadOptionDto>>.SuccessResult(
                options,
                "Download options retrieved successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid video ID: {VideoId}", videoId);
            return BadRequest(ApiResponse<IEnumerable<DownloadOptionDto>>.ErrorResult(
                "INVALID_VIDEO_ID",
                "The provided video ID is not valid",
                new { videoId }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting download options for video: {VideoId}", videoId);
            return StatusCode(500, ApiResponse<IEnumerable<DownloadOptionDto>>.ErrorResult(
                "INTERNAL_ERROR",
                "An internal error occurred while retrieving download options"));
        }
    }
}