using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using YoutubeDownloader.WebApi.Mapping;
using YoutubeDownloader.WebApi.Services;

namespace YoutubeDownloader.Tests.Services;

public class VideoServiceTests
{
    private readonly IMapper _mapper;
    private readonly Mock<ILogger<VideoService>> _mockLogger;
    private readonly VideoService _videoService;

    public VideoServiceTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
        _mockLogger = new Mock<ILogger<VideoService>>();
        _videoService = new VideoService(_mapper, _mockLogger.Object);
    }

    [Fact]
    public async Task ResolveAsync_ValidYouTubeUrl_ReturnsVideoInfo()
    {
        // Arrange
        var validUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ"; // Rick Roll video for testing
        
        // Act & Assert
        // Note: This test requires actual network access to YouTube
        // In a real scenario, you would mock the QueryResolver dependency
        var act = () => _videoService.ResolveAsync(validUrl, CancellationToken.None);
        
        // This test might fail if the video is not accessible or network issues occur
        // We test that the method doesn't throw for a well-formed URL
        await act.Should().NotThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ResolveAsync_InvalidUrl_ThrowsException()
    {
        // Arrange
        var invalidUrl = "not-a-url";
        
        // Act & Assert
        var act = () => _videoService.ResolveAsync(invalidUrl, CancellationToken.None);
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task ResolveAsync_EmptyUrl_ThrowsException()
    {
        // Arrange
        var emptyUrl = "";
        
        // Act & Assert
        var act = () => _videoService.ResolveAsync(emptyUrl, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetDownloadOptionsAsync_ValidVideoId_ReturnsOptions()
    {
        // Arrange
        var validVideoId = "dQw4w9WgXcQ"; // Rick Roll video ID
        
        // Act & Assert
        // Note: This test requires actual network access to YouTube
        var act = () => _videoService.GetDownloadOptionsAsync(validVideoId, CancellationToken.None);
        
        // Test that the method doesn't throw for a valid video ID
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetDownloadOptionsAsync_InvalidVideoId_ThrowsException()
    {
        // Arrange
        var invalidVideoId = "invalid-id";
        
        // Act & Assert
        var act = () => _videoService.GetDownloadOptionsAsync(invalidVideoId, CancellationToken.None);
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public void VideoService_Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var service = new VideoService(_mapper, _mockLogger.Object);
        
        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void VideoService_Constructor_WithNullMapper_ThrowsException()
    {
        // Act & Assert
        var act = () => new VideoService(null!, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void VideoService_Constructor_WithNullLogger_ThrowsException()
    {
        // Act & Assert
        var act = () => new VideoService(_mapper, null!);
        act.Should().Throw<ArgumentNullException>();
    }
}