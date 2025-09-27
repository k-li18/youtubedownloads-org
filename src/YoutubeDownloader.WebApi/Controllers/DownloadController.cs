using Microsoft.AspNetCore.Mvc;
using YoutubeDownloader.WebApi.Models;
using YoutubeDownloader.WebApi.Services;

namespace YoutubeDownloader.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DownloadController : ControllerBase
{
    private readonly IDownloadService _downloadService;
    private readonly ILogger<DownloadController> _logger;

    public DownloadController(IDownloadService downloadService, ILogger<DownloadController> logger)
    {
        _downloadService = downloadService;
        _logger = logger;
    }

    /// <summary>
    /// Start a new download task
    /// </summary>
    /// <param name="request">Download request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Download task information</returns>
    [HttpPost("start")]
    public async Task<ActionResult<ApiResponse<DownloadTaskDto>>> StartDownload(
        [FromBody] DownloadRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<DownloadTaskDto>.ErrorResult(
                    "VALIDATION_ERROR",
                    "Invalid request data",
                    ModelState));
            }

            _logger.LogInformation("Starting download for URL: {Url}", request.VideoUrl);

            var downloadTask = await _downloadService.StartDownloadAsync(request, cancellationToken);

            return Ok(ApiResponse<DownloadTaskDto>.SuccessResult(
                downloadTask,
                "Download started successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid download request: {Url}", request.VideoUrl);
            return BadRequest(ApiResponse<DownloadTaskDto>.ErrorResult(
                "INVALID_REQUEST",
                ex.Message,
                request));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot start download: {Url}", request.VideoUrl);
            return BadRequest(ApiResponse<DownloadTaskDto>.ErrorResult(
                "DOWNLOAD_ERROR",
                ex.Message,
                request));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting download for URL: {Url}", request.VideoUrl);
            return StatusCode(500, ApiResponse<DownloadTaskDto>.ErrorResult(
                "INTERNAL_ERROR",
                "An internal error occurred while starting the download"));
        }
    }

    /// <summary>
    /// Get download status by task ID
    /// </summary>
    /// <param name="id">Download task ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Download task status</returns>
    [HttpGet("{id}/status")]
    public async Task<ActionResult<ApiResponse<DownloadTaskDto>>> GetDownloadStatus(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting download status for task: {TaskId}", id);

            var downloadTask = await _downloadService.GetDownloadStatusAsync(id, cancellationToken);
            
            if (downloadTask == null)
            {
                return NotFound(ApiResponse<DownloadTaskDto>.ErrorResult(
                    "TASK_NOT_FOUND",
                    "Download task not found",
                    new { taskId = id }));
            }

            return Ok(ApiResponse<DownloadTaskDto>.SuccessResult(
                downloadTask,
                "Download status retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting download status for task: {TaskId}", id);
            return StatusCode(500, ApiResponse<DownloadTaskDto>.ErrorResult(
                "INTERNAL_ERROR",
                "An internal error occurred while retrieving download status"));
        }
    }

    /// <summary>
    /// Get download progress by task ID
    /// </summary>
    /// <param name="id">Download task ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Download progress information</returns>
    [HttpGet("{id}/progress")]
    public async Task<ActionResult<ApiResponse<DownloadProgressDto>>> GetDownloadProgress(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting download progress for task: {TaskId}", id);

            var downloadTask = await _downloadService.GetDownloadStatusAsync(id, cancellationToken);
            
            if (downloadTask == null)
            {
                return NotFound(ApiResponse<DownloadProgressDto>.ErrorResult(
                    "TASK_NOT_FOUND",
                    "Download task not found",
                    new { taskId = id }));
            }

            var progress = new DownloadProgressDto
            {
                TaskId = downloadTask.Id,
                Progress = downloadTask.Progress,
                Status = downloadTask.Status,
                ErrorMessage = downloadTask.ErrorMessage,
                FileSizeBytes = downloadTask.FileSizeBytes
            };

            return Ok(ApiResponse<DownloadProgressDto>.SuccessResult(
                progress,
                "Download progress retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting download progress for task: {TaskId}", id);
            return StatusCode(500, ApiResponse<DownloadProgressDto>.ErrorResult(
                "INTERNAL_ERROR",
                "An internal error occurred while retrieving download progress"));
        }
    }

    /// <summary>
    /// Cancel a download task
    /// </summary>
    /// <param name="id">Download task ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> CancelDownload(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Cancelling download for task: {TaskId}", id);

            var result = await _downloadService.CancelDownloadAsync(id, cancellationToken);
            
            if (!result)
            {
                return NotFound(ApiResponse<bool>.ErrorResult(
                    "TASK_NOT_FOUND",
                    "Download task not found or cannot be cancelled",
                    new { taskId = id }));
            }

            return Ok(ApiResponse<bool>.SuccessResult(
                true,
                "Download cancelled successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling download for task: {TaskId}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResult(
                "INTERNAL_ERROR",
                "An internal error occurred while cancelling the download"));
        }
    }

    /// <summary>
    /// Get all download tasks
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all download tasks</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<DownloadTaskDto>>>> GetAllDownloads(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting all download tasks");

            var downloads = await _downloadService.GetAllDownloadsAsync(cancellationToken);

            return Ok(ApiResponse<IEnumerable<DownloadTaskDto>>.SuccessResult(
                downloads,
                "Download tasks retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all download tasks");
            return StatusCode(500, ApiResponse<IEnumerable<DownloadTaskDto>>.ErrorResult(
                "INTERNAL_ERROR",
                "An internal error occurred while retrieving download tasks"));
        }
    }

    /// <summary>
    /// Get recent download tasks
    /// </summary>
    /// <param name="count">Number of recent tasks to retrieve (default: 50)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recent download tasks</returns>
    [HttpGet("recent")]
    public async Task<ActionResult<ApiResponse<IEnumerable<DownloadTaskDto>>>> GetRecentDownloads(
        [FromQuery] int count = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (count <= 0 || count > 1000)
            {
                return BadRequest(ApiResponse<IEnumerable<DownloadTaskDto>>.ErrorResult(
                    "INVALID_COUNT",
                    "Count must be between 1 and 1000",
                    new { count }));
            }

            _logger.LogInformation("Getting {Count} recent download tasks", count);

            var downloads = await _downloadService.GetRecentDownloadsAsync(count, cancellationToken);

            return Ok(ApiResponse<IEnumerable<DownloadTaskDto>>.SuccessResult(
                downloads,
                "Recent download tasks retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent download tasks");
            return StatusCode(500, ApiResponse<IEnumerable<DownloadTaskDto>>.ErrorResult(
                "INTERNAL_ERROR",
                "An internal error occurred while retrieving recent download tasks"));
        }
    }

    /// <summary>
    /// Download the completed file
    /// </summary>
    /// <param name="id">Download task ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File download result</returns>
    [HttpGet("{id}/file")]
    public async Task<ActionResult> DownloadFile(
        Guid id,
        [FromQuery] bool deleteOnComplete = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Downloading file for task: {TaskId}", id);

            var downloadTask = await _downloadService.GetDownloadStatusAsync(id, cancellationToken);
            
            if (downloadTask == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult(
                    "TASK_NOT_FOUND",
                    "Download task not found",
                    new { taskId = id }));
            }

            if (downloadTask.Status != YoutubeDownloader.Infrastructure.Models.DownloadStatus.Completed)
            {
                return BadRequest(ApiResponse<object>.ErrorResult(
                    "DOWNLOAD_NOT_COMPLETED",
                    "Download is not yet completed",
                    new { taskId = id, status = downloadTask.Status }));
            }

            // Construct file path
            var safeTitle = string.Join("_", downloadTask.VideoTitle?.Split(Path.GetInvalidFileNameChars()) ?? new[] { downloadTask.VideoId });
            var fileName = $"{safeTitle}_{downloadTask.Quality}.{downloadTask.Format}";
            var filePath = Path.Combine(downloadTask.OutputPath, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return NotFound(ApiResponse<object>.ErrorResult(
                    "FILE_NOT_FOUND",
                    "Downloaded file not found on server",
                    new { taskId = id, filePath }));
            }

            var contentType = downloadTask.Format?.ToLower() switch
            {
                "mp4" => "video/mp4",
                "webm" => "video/webm",
                "mp3" => "audio/mpeg",
                "ogg" => "audio/ogg",
                _ => "application/octet-stream"
            };

            // Stream the file directly from disk to the client with range support
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _logger.LogInformation("File download started for task: {TaskId}, file: {FileName}, size: {Size} bytes", id, fileName, fileStream.Length);

            if (deleteOnComplete)
            {
                // Delete the file on the server after the response has fully completed
                HttpContext.Response.OnCompleted(() =>
                {
                    try
                    {
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                            _logger.LogInformation("Deleted file after send: {FilePath}", filePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete file after send: {FilePath}", filePath);
                    }
                    return Task.CompletedTask;
                });
            }

            return File(fileStream, contentType, fileName, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file for task: {TaskId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "INTERNAL_ERROR",
                "An internal error occurred while downloading the file"));
        }
    }
}
