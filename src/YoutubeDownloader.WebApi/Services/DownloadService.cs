using AutoMapper;
using Hangfire;
using YoutubeDownloader.Infrastructure.Models;
using YoutubeDownloader.Infrastructure.Repositories;
using YoutubeDownloader.WebApi.Models;
using YoutubeDownloader.WebApi.Services;
using YoutubeExplode.Videos;

namespace YoutubeDownloader.WebApi.Services;

public class DownloadService : IDownloadService
{
    private readonly IDownloadTaskRepository _downloadTaskRepository;
    private readonly IVideoInfoRepository _videoInfoRepository;
    private readonly IVideoService _videoService;
    private readonly IMapper _mapper;
    private readonly ILogger<DownloadService> _logger;

    public DownloadService(
        IDownloadTaskRepository downloadTaskRepository,
        IVideoInfoRepository videoInfoRepository,
        IVideoService videoService,
        IMapper mapper,
        ILogger<DownloadService> logger)
    {
        _downloadTaskRepository = downloadTaskRepository ?? throw new ArgumentNullException(nameof(downloadTaskRepository));
        _videoInfoRepository = videoInfoRepository ?? throw new ArgumentNullException(nameof(videoInfoRepository));
        _videoService = videoService ?? throw new ArgumentNullException(nameof(videoService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DownloadTaskDto> StartDownloadAsync(DownloadRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting download for URL: {Url}", request.VideoUrl);

            // Resolve video information
            var videoInfo = await _videoService.ResolveAsync(request.VideoUrl, cancellationToken);
            
            // Extract video ID from URL
            var videoId = VideoId.Parse(request.VideoUrl);

            // Create download task
            var downloadTask = _mapper.Map<DownloadTask>(request);
            downloadTask.VideoId = videoId.ToString();
            downloadTask.VideoTitle = videoInfo.Title;
            downloadTask.Author = videoInfo.Author;
            downloadTask.Duration = videoInfo.Duration;

            // Save to database
            await _downloadTaskRepository.CreateAsync(downloadTask, cancellationToken);

            // Enqueue background job
            var jobId = BackgroundJob.Enqueue<IBackgroundDownloadService>(
                service => service.ExecuteDownloadAsync(downloadTask.Id, CancellationToken.None));

            // Update with job ID
            downloadTask.HangfireJobId = jobId;
            await _downloadTaskRepository.UpdateAsync(downloadTask, cancellationToken);

            _logger.LogInformation("Download task created with ID: {TaskId}, Job ID: {JobId}", downloadTask.Id, jobId);

            return _mapper.Map<DownloadTaskDto>(downloadTask);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting download for URL: {Url}", request.VideoUrl);
            throw;
        }
    }

    public async Task<DownloadTaskDto?> GetDownloadStatusAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var downloadTask = await _downloadTaskRepository.GetByIdAsync(taskId, cancellationToken);
        return downloadTask != null ? _mapper.Map<DownloadTaskDto>(downloadTask) : null;
    }

    public async Task<IEnumerable<DownloadTaskDto>> GetAllDownloadsAsync(CancellationToken cancellationToken = default)
    {
        var downloadTasks = await _downloadTaskRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<IEnumerable<DownloadTaskDto>>(downloadTasks);
    }

    public async Task<IEnumerable<DownloadTaskDto>> GetRecentDownloadsAsync(int count = 50, CancellationToken cancellationToken = default)
    {
        var downloadTasks = await _downloadTaskRepository.GetRecentAsync(count, cancellationToken);
        return _mapper.Map<IEnumerable<DownloadTaskDto>>(downloadTasks);
    }

    public async Task<bool> CancelDownloadAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        try
        {
            var downloadTask = await _downloadTaskRepository.GetByIdAsync(taskId, cancellationToken);
            if (downloadTask == null)
                return false;

            if (downloadTask.Status == DownloadStatus.InProgress && !string.IsNullOrEmpty(downloadTask.HangfireJobId))
            {
                BackgroundJob.Delete(downloadTask.HangfireJobId);
            }

            downloadTask.Status = DownloadStatus.Cancelled;
            await _downloadTaskRepository.UpdateAsync(downloadTask, cancellationToken);

            _logger.LogInformation("Download cancelled for task: {TaskId}", taskId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling download for task: {TaskId}", taskId);
            return false;
        }
    }

    public async Task<bool> DeleteDownloadAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _downloadTaskRepository.DeleteAsync(taskId, cancellationToken);
            _logger.LogInformation("Download task deleted: {TaskId}", taskId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting download task: {TaskId}", taskId);
            return false;
        }
    }
}