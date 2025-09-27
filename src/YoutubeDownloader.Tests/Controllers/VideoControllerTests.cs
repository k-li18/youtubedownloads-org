using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using YoutubeDownloader.WebApi.Controllers;
using YoutubeDownloader.WebApi.Models;
using YoutubeDownloader.WebApi.Services;

namespace YoutubeDownloader.Tests.Controllers;

public class VideoControllerTests
{
    private readonly Mock<IVideoService> _mockVideoService;
    private readonly Mock<ILogger<VideoController>> _mockLogger;
    private readonly VideoController _controller;

    public VideoControllerTests()
    {
        _mockVideoService = new Mock<IVideoService>();
        _mockLogger = new Mock<ILogger<VideoController>>();
        _controller = new VideoController(_mockVideoService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ResolveVideo_ValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var request = new VideoResolveRequest { Url = "https://youtube.com/watch?v=test" };
        var expectedVideoInfo = new VideoInfoDto
        {
            VideoId = "test",
            Title = "Test Video",
            Author = "Test Author",
            Duration = TimeSpan.FromMinutes(5),
            DownloadOptions = new List<DownloadOptionDto>
            {
                new() { Container = "mp4", IsAudioOnly = false, VideoQuality = "720p" }
            }
        };

        _mockVideoService
            .Setup(s => s.ResolveAsync(request.Url, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedVideoInfo);

        // Act
        var result = await _controller.ResolveVideo(request);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<VideoInfoDto>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<VideoInfoDto>>().Subject;
        
        response.Success.Should().BeTrue();
        response.Data.Should().BeEquivalentTo(expectedVideoInfo);
        response.Message.Should().Be("Video resolved successfully");
    }

    [Fact]
    public async Task ResolveVideo_InvalidUrl_ReturnsBadRequest()
    {
        // Arrange
        var request = new VideoResolveRequest { Url = "invalid-url" };
        
        _mockVideoService
            .Setup(s => s.ResolveAsync(request.Url, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid URL"));

        // Act
        var result = await _controller.ResolveVideo(request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<ApiResponse<VideoInfoDto>>().Subject;
        
        response.Success.Should().BeFalse();
        response.Error?.Code.Should().Be("INVALID_URL");
    }

    [Fact]
    public async Task ResolveVideo_VideoNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new VideoResolveRequest { Url = "https://youtube.com/watch?v=notfound" };
        
        _mockVideoService
            .Setup(s => s.ResolveAsync(request.Url, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Video not found"));

        // Act
        var result = await _controller.ResolveVideo(request);

        // Assert
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFoundResult.Value.Should().BeOfType<ApiResponse<VideoInfoDto>>().Subject;
        
        response.Success.Should().BeFalse();
        response.Error?.Code.Should().Be("VIDEO_NOT_FOUND");
    }

    [Fact]
    public async Task ResolveVideo_ServiceException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new VideoResolveRequest { Url = "https://youtube.com/watch?v=test" };
        
        _mockVideoService
            .Setup(s => s.ResolveAsync(request.Url, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Internal error"));

        // Act
        var result = await _controller.ResolveVideo(request);

        // Assert
        var errorResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        errorResult.StatusCode.Should().Be(500);
        
        var response = errorResult.Value.Should().BeOfType<ApiResponse<VideoInfoDto>>().Subject;
        response.Success.Should().BeFalse();
        response.Error?.Code.Should().Be("INTERNAL_ERROR");
    }

    [Fact]
    public async Task GetDownloadOptions_ValidVideoId_ReturnsSuccessResponse()
    {
        // Arrange
        var videoId = "test-video-id";
        var expectedOptions = new List<DownloadOptionDto>
        {
            new() { Container = "mp4", IsAudioOnly = false, VideoQuality = "720p" },
            new() { Container = "mp3", IsAudioOnly = true }
        };

        _mockVideoService
            .Setup(s => s.GetDownloadOptionsAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOptions);

        // Act
        var result = await _controller.GetDownloadOptions(videoId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DownloadOptionDto>>>().Subject;
        
        response.Success.Should().BeTrue();
        response.Data.Should().BeEquivalentTo(expectedOptions);
    }

    [Fact]
    public async Task GetDownloadOptions_EmptyVideoId_ReturnsBadRequest()
    {
        // Arrange
        var videoId = "";

        // Act
        var result = await _controller.GetDownloadOptions(videoId);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DownloadOptionDto>>>().Subject;
        
        response.Success.Should().BeFalse();
        response.Error?.Code.Should().Be("INVALID_VIDEO_ID");
    }

    [Fact]
    public async Task GetDownloadOptions_InvalidVideoId_ReturnsBadRequest()
    {
        // Arrange
        var videoId = "invalid-id";
        
        _mockVideoService
            .Setup(s => s.GetDownloadOptionsAsync(videoId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid video ID"));

        // Act
        var result = await _controller.GetDownloadOptions(videoId);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DownloadOptionDto>>>().Subject;
        
        response.Success.Should().BeFalse();
        response.Error?.Code.Should().Be("INVALID_VIDEO_ID");
    }
}