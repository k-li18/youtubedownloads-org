using Microsoft.AspNetCore.SignalR;
using YoutubeDownloader.Core.Downloading;
using YoutubeDownloader.Infrastructure.Models;
using YoutubeDownloader.Infrastructure.Repositories;
using YoutubeDownloader.WebApi.Hubs;
using YoutubeDownloader.WebApi.Models;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using Gress;

namespace YoutubeDownloader.WebApi.Services;

public class BackgroundDownloadService : IBackgroundDownloadService
{
    private readonly IDownloadTaskRepository _downloadTaskRepository;
    private readonly IHubContext<DownloadHub> _hubContext;
    private readonly ILogger<BackgroundDownloadService> _logger;

    public BackgroundDownloadService(
        IDownloadTaskRepository downloadTaskRepository,
        IHubContext<DownloadHub> hubContext,
        ILogger<BackgroundDownloadService> logger)
    {
        _downloadTaskRepository = downloadTaskRepository;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task ExecuteDownloadAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        DownloadTask? downloadTask = null;
        
        try
        {
            _logger.LogInformation("Starting background download for task: {TaskId}", taskId);

            downloadTask = await _downloadTaskRepository.GetByIdAsync(taskId, cancellationToken);
            if (downloadTask == null)
            {
                _logger.LogError("Download task not found: {TaskId}", taskId);
                return;
            }

            // Update status to in progress
            downloadTask.Status = DownloadStatus.InProgress;
            downloadTask.StartedAt = DateTime.UtcNow;
            await _downloadTaskRepository.UpdateAsync(downloadTask, cancellationToken);

            // Notify clients
            await NotifyProgress(downloadTask, "Download started");

            // Create video downloader
            var videoDownloader = new VideoDownloader();
            var videoId = VideoId.Parse(downloadTask.VideoId);

            // Get all available download options and find exact match
            var allOptions = await videoDownloader.GetDownloadOptionsAsync(
                videoId, 
                true, 
                cancellationToken);
            
            var downloadOption = FindExactMatchingOption(allOptions, downloadTask.Format, downloadTask.Quality);
            
            if (downloadOption == null)
            {
                throw new InvalidOperationException(
                    $"No download option found for format '{downloadTask.Format}' and quality '{downloadTask.Quality}'");
            }

            // Create progress reporter
            var progress = new Progress<Percentage>(p =>
            {
                downloadTask.Progress = p.Fraction * 100;
                _ = Task.Run(async () =>
                {
                    await _downloadTaskRepository.UpdateAsync(downloadTask, CancellationToken.None);
                    await NotifyProgress(downloadTask, $"Downloading... {p.Fraction:P1}");
                });
            });

            // Ensure output directory exists
            var outputDir = downloadTask.OutputPath;
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Download the video
            var video = await new YoutubeDownloader.Core.Resolving.QueryResolver()
                .ResolveAsync(downloadTask.VideoUrl, cancellationToken);
            
            var videoInfo = video.Videos.First();

            // Create file name
            var safeTitle = string.Join("_", downloadTask.VideoTitle?.Split(Path.GetInvalidFileNameChars()) ?? new[] { downloadTask.VideoId });
            var fileName = $"{safeTitle}_{downloadTask.Quality}.{downloadTask.Format}";
            var fullFilePath = Path.Combine(outputDir, fileName);

            await videoDownloader.DownloadVideoAsync(
                fullFilePath,
                videoInfo,
                downloadOption,
                downloadTask.IncludeSubtitles,
                progress,
                cancellationToken);

            // Update task as completed
            downloadTask.Status = DownloadStatus.Completed;
            downloadTask.CompletedAt = DateTime.UtcNow;
            downloadTask.Progress = 100;

            // Get file size
            if (File.Exists(downloadTask.OutputPath))
            {
                downloadTask.FileSizeBytes = new FileInfo(downloadTask.OutputPath).Length;
            }

            await _downloadTaskRepository.UpdateAsync(downloadTask, cancellationToken);
            await NotifyProgress(downloadTask, "Download completed successfully");

            _logger.LogInformation("Download completed successfully for task: {TaskId}", taskId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Download cancelled for task: {TaskId}", taskId);
            
            if (downloadTask != null)
            {
                downloadTask.Status = DownloadStatus.Cancelled;
                downloadTask.ErrorMessage = "Download was cancelled";
                await _downloadTaskRepository.UpdateAsync(downloadTask, CancellationToken.None);
                await NotifyProgress(downloadTask, "Download cancelled");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download failed for task: {TaskId}", taskId);

            if (downloadTask != null)
            {
                downloadTask.Status = DownloadStatus.Failed;
                downloadTask.ErrorMessage = ex.Message;
                await _downloadTaskRepository.UpdateAsync(downloadTask, CancellationToken.None);
                await NotifyProgress(downloadTask, $"Download failed: {ex.Message}");
            }
        }
    }

    private async Task NotifyProgress(DownloadTask downloadTask, string message)
    {
        try
        {
            var progressDto = new DownloadProgressDto
            {
                TaskId = downloadTask.Id,
                Progress = downloadTask.Progress,
                Status = downloadTask.Status,
                ErrorMessage = downloadTask.ErrorMessage,
                FileSizeBytes = downloadTask.FileSizeBytes
            };

            await _hubContext.Clients.All.SendAsync("DownloadProgress", progressDto);
            _logger.LogDebug("Progress notification sent for task {TaskId}: {Message}", downloadTask.Id, message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send progress notification for task: {TaskId}", downloadTask.Id);
        }
    }

    private static VideoDownloadOption? FindExactMatchingOption(
        IReadOnlyList<VideoDownloadOption> allOptions, 
        string userFormat, 
        string userQuality)
    {
        // Extract user's target resolution
        var targetResolution = ExtractResolutionFromQuality(userQuality);
        var targetContainer = userFormat.ToLowerInvariant();
        
        // Try exact match: container + resolution
        var exactMatch = allOptions.FirstOrDefault(option =>
            string.Equals(option.Container.Name, targetContainer, StringComparison.OrdinalIgnoreCase) &&
            option.VideoQuality?.MaxHeight == targetResolution);
            
        if (exactMatch != null)
            return exactMatch;
            
        // If no exact match, find closest resolution in same container
        var sameContainerOptions = allOptions
            .Where(option => string.Equals(option.Container.Name, targetContainer, StringComparison.OrdinalIgnoreCase))
            .OrderBy(option => Math.Abs((option.VideoQuality?.MaxHeight ?? 0) - targetResolution))
            .ToList();
            
        return sameContainerOptions.FirstOrDefault();
    }

    private static int ExtractResolutionFromQuality(string quality)
    {
        if (string.IsNullOrEmpty(quality))
            return 720; // Default value
            
        // Use regex to extract numbers: supports "144p", "720p (HD)", "1080p (Full HD)", etc.
        var match = System.Text.RegularExpressions.Regex.Match(quality, @"(\d+)p?");
        
        if (match.Success && int.TryParse(match.Groups[1].Value, out int resolution))
            return resolution;
            
        // Fallback for special cases
        var lowerQuality = quality.ToLowerInvariant();
        if (lowerQuality.Contains("144")) return 144;
        if (lowerQuality.Contains("240")) return 240;
        if (lowerQuality.Contains("360")) return 360;
        if (lowerQuality.Contains("480")) return 480;
        if (lowerQuality.Contains("720")) return 720;
        if (lowerQuality.Contains("1080")) return 1080;
        if (lowerQuality.Contains("1440")) return 1440;
        if (lowerQuality.Contains("2160")) return 2160; // 4K
        
        return 720; // Default to 720p
    }

}
