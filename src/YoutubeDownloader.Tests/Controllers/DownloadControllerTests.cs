using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using YoutubeDownloader.Infrastructure.Models;
using YoutubeDownloader.WebApi.Controllers;
using YoutubeDownloader.WebApi.Models;
using YoutubeDownloader.WebApi.Services;

namespace YoutubeDownloader.Tests.Controllers;

public class DownloadControllerTests
{
    private readonly Mock<IDownloadService> _mockDownloadService;
    private readonly Mock<ILogger<DownloadController>> _mockLogger;
    private readonly DownloadController _controller;

    public DownloadControllerTests()
    {
        _mockDownloadService = new Mock<IDownloadService>();
        _mockLogger = new Mock<ILogger<DownloadController>>();
        _controller = new DownloadController(_mockDownloadService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task StartDownload_ValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var request = new DownloadRequest
        {
            VideoUrl = "https://youtube.com/watch?v=test",
            Format = "mp4",
            Quality = "720p",
            IncludeSubtitles = true
        };

        var expectedTask = new DownloadTaskDto
        {
            Id = Guid.NewGuid(),
            VideoUrl = request.VideoUrl,
            VideoId = "test",
            VideoTitle = "Test Video",
            OutputPath = "/downloads/test.mp4",
            Format = request.Format,
            Quality = request.Quality,
            Status = DownloadStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _mockDownloadService
            .Setup(s => s.StartDownloadAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTask);

        // Act
        var result = await _controller.StartDownload(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<DownloadTaskDto>>().Subject;
        
        response.Success.Should().BeTrue();
        response.Data.Should().BeEquivalentTo(expectedTask);
        response.Message.Should().Be("Download started successfully");
    }

    [Fact]
    public async Task StartDownload_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new DownloadRequest
        {
            VideoUrl = "invalid-url",
            Format = "mp4",
            Quality = "720p"
        };

        _mockDownloadService
            .Setup(s => s.StartDownloadAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid URL"));

        // Act
        var result = await _controller.StartDownload(request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<ApiResponse<DownloadTaskDto>>().Subject;
        
        response.Success.Should().BeFalse();
        response.Error?.Code.Should().Be("INVALID_REQUEST");
    }

    [Fact]
    public async Task GetDownloadStatus_ExistingTask_ReturnsSuccessResponse()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var expectedTask = new DownloadTaskDto
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

        _mockDownloadService
            .Setup(s => s.GetDownloadStatusAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTask);

        // Act
        var result = await _controller.GetDownloadStatus(taskId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<DownloadTaskDto>>().Subject;
        
        response.Success.Should().BeTrue();
        response.Data.Should().BeEquivalentTo(expectedTask);
    }

    [Fact]
    public async Task GetDownloadStatus_NonExistentTask_ReturnsNotFound()
    {
        // Arrange
        var taskId = Guid.NewGuid();

        _mockDownloadService
            .Setup(s => s.GetDownloadStatusAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DownloadTaskDto?)null);

        // Act
        var result = await _controller.GetDownloadStatus(taskId);

        // Assert
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFoundResult.Value.Should().BeOfType<ApiResponse<DownloadTaskDto>>().Subject;
        
        response.Success.Should().BeFalse();
        response.Error?.Code.Should().Be("TASK_NOT_FOUND");
    }

    [Fact]
    public async Task GetDownloadProgress_ExistingTask_ReturnsSuccessResponse()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var taskDto = new DownloadTaskDto
        {
            Id = taskId,
            VideoUrl = "https://youtube.com/watch?v=test",
            VideoId = "test",
            OutputPath = "/downloads/test.mp4",
            Format = "mp4",
            Quality = "720p",
            Status = DownloadStatus.InProgress,
            Progress = 75.0,
            FileSizeBytes = 1024000
        };

        _mockDownloadService
            .Setup(s => s.GetDownloadStatusAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(taskDto);

        // Act
        var result = await _controller.GetDownloadProgress(taskId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<DownloadProgressDto>>().Subject;
        
        response.Success.Should().BeTrue();
        response.Data!.TaskId.Should().Be(taskId);
        response.Data.Progress.Should().Be(75.0);
        response.Data.Status.Should().Be(DownloadStatus.InProgress);
        response.Data.FileSizeBytes.Should().Be(1024000);
    }

    [Fact]
    public async Task CancelDownload_ExistingTask_ReturnsSuccessResponse()
    {
        // Arrange
        var taskId = Guid.NewGuid();

        _mockDownloadService
            .Setup(s => s.CancelDownloadAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CancelDownload(taskId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
        
        response.Success.Should().BeTrue();
        response.Data.Should().BeTrue();
        response.Message.Should().Be("Download cancelled successfully");
    }

    [Fact]
    public async Task CancelDownload_NonExistentTask_ReturnsNotFound()
    {
        // Arrange
        var taskId = Guid.NewGuid();

        _mockDownloadService
            .Setup(s => s.CancelDownloadAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CancelDownload(taskId);

        // Assert
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFoundResult.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
        
        response.Success.Should().BeFalse();
        response.Error?.Code.Should().Be("TASK_NOT_FOUND");
    }

    [Fact]
    public async Task GetAllDownloads_ReturnsSuccessResponse()
    {
        // Arrange
        var downloads = new List<DownloadTaskDto>
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

        _mockDownloadService
            .Setup(s => s.GetAllDownloadsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(downloads);

        // Act
        var result = await _controller.GetAllDownloads();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DownloadTaskDto>>>().Subject;
        
        response.Success.Should().BeTrue();
        response.Data.Should().BeEquivalentTo(downloads);
    }

    [Fact]
    public async Task GetRecentDownloads_ValidCount_ReturnsSuccessResponse()
    {
        // Arrange
        var count = 10;
        var downloads = Enumerable.Range(1, count)
            .Select(i => new DownloadTaskDto { 
                Id = Guid.NewGuid(), 
                VideoUrl = $"https://youtube.com/watch?v=test{i}",
                VideoId = $"test{i}",
                OutputPath = $"/downloads/test{i}.mp4",
                Format = "mp4",
                Quality = "720p"
            })
            .ToList();

        _mockDownloadService
            .Setup(s => s.GetRecentDownloadsAsync(count, It.IsAny<CancellationToken>()))
            .ReturnsAsync(downloads);

        // Act
        var result = await _controller.GetRecentDownloads(count);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DownloadTaskDto>>>().Subject;
        
        response.Success.Should().BeTrue();
        response.Data.Should().HaveCount(count);
    }

    [Fact]
    public async Task GetRecentDownloads_InvalidCount_ReturnsBadRequest()
    {
        // Arrange
        var invalidCount = 0;

        // Act
        var result = await _controller.GetRecentDownloads(invalidCount);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DownloadTaskDto>>>().Subject;
        
        response.Success.Should().BeFalse();
        response.Error?.Code.Should().Be("INVALID_COUNT");
    }
}