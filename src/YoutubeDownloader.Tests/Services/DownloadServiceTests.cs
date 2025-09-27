using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using YoutubeDownloader.Infrastructure.Models;
using YoutubeDownloader.Infrastructure.Repositories;
using YoutubeDownloader.WebApi.Mapping;
using YoutubeDownloader.WebApi.Models;
using YoutubeDownloader.WebApi.Services;

namespace YoutubeDownloader.Tests.Services;

public class DownloadServiceTests
{
    private readonly Mock<IDownloadTaskRepository> _mockDownloadTaskRepository;
    private readonly Mock<IVideoInfoRepository> _mockVideoInfoRepository;
    private readonly Mock<IVideoService> _mockVideoService;
    private readonly IMapper _mapper;
    private readonly Mock<ILogger<DownloadService>> _mockLogger;
    private readonly DownloadService _downloadService;

    public DownloadServiceTests()
    {
        _mockDownloadTaskRepository = new Mock<IDownloadTaskRepository>();
        _mockVideoInfoRepository = new Mock<IVideoInfoRepository>();
        _mockVideoService = new Mock<IVideoService>();
        
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
        
        _mockLogger = new Mock<ILogger<DownloadService>>();
        
        _downloadService = new DownloadService(
            _mockDownloadTaskRepository.Object,
            _mockVideoInfoRepository.Object,
            _mockVideoService.Object,
            _mapper,
            _mockLogger.Object);
    }

    [Fact]
    public async Task StartDownloadAsync_ValidRequest_ReturnsDownloadTask()
    {
        // Arrange
        var request = new DownloadRequest
        {
            VideoUrl = "https://youtube.com/watch?v=test",
            Format = "mp4",
            Quality = "720p",
            IncludeSubtitles = true,
            OutputPath = "/downloads/test.mp4"
        };

        var videoInfo = new VideoInfoDto
        {
            VideoId = "test",
            Title = "Test Video",
            Author = "Test Author",
            Duration = TimeSpan.FromMinutes(5)
        };

        var downloadTask = new DownloadTask
        {
            Id = Guid.NewGuid(),
            VideoUrl = request.VideoUrl,
            VideoId = "test",
            VideoTitle = videoInfo.Title,
            Author = videoInfo.Author,
            Duration = videoInfo.Duration,
            OutputPath = request.OutputPath,
            Format = request.Format,
            Quality = request.Quality,
            IncludeSubtitles = request.IncludeSubtitles,
            Status = DownloadStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _mockVideoService
            .Setup(s => s.ResolveAsync(request.VideoUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(videoInfo);

        _mockDownloadTaskRepository
            .Setup(r => r.CreateAsync(It.IsAny<DownloadTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(downloadTask);

        _mockDownloadTaskRepository
            .Setup(r => r.UpdateAsync(It.IsAny<DownloadTask>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _downloadService.StartDownloadAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.VideoUrl.Should().Be(request.VideoUrl);
        result.VideoId.Should().Be("test");
        result.VideoTitle.Should().Be(videoInfo.Title);
        result.Status.Should().Be(DownloadStatus.Pending);

        _mockDownloadTaskRepository.Verify(r => r.CreateAsync(It.IsAny<DownloadTask>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockDownloadTaskRepository.Verify(r => r.UpdateAsync(It.IsAny<DownloadTask>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDownloadStatusAsync_ExistingTask_ReturnsTask()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var downloadTask = new DownloadTask
        {
            Id = taskId,
            VideoUrl = "https://youtube.com/watch?v=test",
            VideoId = "test",
            OutputPath = "/downloads/test.mp4",
            Format = "mp4",
            Quality = "720p",
            Status = DownloadStatus.InProgress,
            Progress = 50.0
        };

        _mockDownloadTaskRepository
            .Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(downloadTask);

        // Act
        var result = await _downloadService.GetDownloadStatusAsync(taskId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(taskId);
        result.Status.Should().Be(DownloadStatus.InProgress);
        result.Progress.Should().Be(50.0);
    }

    [Fact]
    public async Task GetDownloadStatusAsync_NonExistentTask_ReturnsNull()
    {
        // Arrange
        var taskId = Guid.NewGuid();

        _mockDownloadTaskRepository
            .Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DownloadTask?)null);

        // Act
        var result = await _downloadService.GetDownloadStatusAsync(taskId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllDownloadsAsync_ReturnsAllTasks()
    {
        // Arrange
        var downloadTasks = new List<DownloadTask>
        {
            new() { 
                Id = Guid.NewGuid(), 
                VideoUrl = "https://youtube.com/watch?v=test1",
                VideoId = "test1", 
                OutputPath = "/downloads/test1.mp4",
                Format = "mp4",
                Quality = "720p",
                Status = DownloadStatus.Completed 
            },
            new() { 
                Id = Guid.NewGuid(), 
                VideoUrl = "https://youtube.com/watch?v=test2",
                VideoId = "test2", 
                OutputPath = "/downloads/test2.mp4",
                Format = "mp4",
                Quality = "720p",
                Status = DownloadStatus.InProgress 
            }
        };

        _mockDownloadTaskRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(downloadTasks);

        // Act
        var result = await _downloadService.GetAllDownloadsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Select(t => t.VideoId).Should().Contain("test1", "test2");
    }

    [Fact]
    public async Task GetRecentDownloadsAsync_ReturnsRecentTasks()
    {
        // Arrange
        var count = 10;
        var downloadTasks = Enumerable.Range(1, count)
            .Select(i => new DownloadTask { 
                Id = Guid.NewGuid(), 
                VideoUrl = $"https://youtube.com/watch?v=test{i}",
                VideoId = $"test{i}",
                OutputPath = $"/downloads/test{i}.mp4",
                Format = "mp4",
                Quality = "720p"
            })
            .ToList();

        _mockDownloadTaskRepository
            .Setup(r => r.GetRecentAsync(count, It.IsAny<CancellationToken>()))
            .ReturnsAsync(downloadTasks);

        // Act
        var result = await _downloadService.GetRecentDownloadsAsync(count);

        // Assert
        result.Should().HaveCount(count);
    }

    [Fact]
    public async Task CancelDownloadAsync_ExistingPendingTask_ReturnsTrue()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var downloadTask = new DownloadTask
        {
            Id = taskId,
            VideoUrl = "https://youtube.com/watch?v=test",
            VideoId = "test",
            OutputPath = "/downloads/test.mp4",
            Format = "mp4",
            Quality = "720p",
            Status = DownloadStatus.Pending,
            HangfireJobId = "job-123"
        };

        _mockDownloadTaskRepository
            .Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(downloadTask);

        _mockDownloadTaskRepository
            .Setup(r => r.UpdateAsync(It.IsAny<DownloadTask>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _downloadService.CancelDownloadAsync(taskId);

        // Assert
        result.Should().BeTrue();
        _mockDownloadTaskRepository.Verify(r => r.UpdateAsync(It.Is<DownloadTask>(t => t.Status == DownloadStatus.Cancelled), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelDownloadAsync_NonExistentTask_ReturnsFalse()
    {
        // Arrange
        var taskId = Guid.NewGuid();

        _mockDownloadTaskRepository
            .Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DownloadTask?)null);

        // Act
        var result = await _downloadService.CancelDownloadAsync(taskId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteDownloadAsync_ExistingTask_ReturnsTrue()
    {
        // Arrange
        var taskId = Guid.NewGuid();

        _mockDownloadTaskRepository
            .Setup(r => r.DeleteAsync(taskId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _downloadService.DeleteDownloadAsync(taskId);

        // Assert
        result.Should().BeTrue();
        _mockDownloadTaskRepository.Verify(r => r.DeleteAsync(taskId, It.IsAny<CancellationToken>()), Times.Once);
    }
}