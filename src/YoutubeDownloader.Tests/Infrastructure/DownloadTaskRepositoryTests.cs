using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using YoutubeDownloader.Infrastructure.Data;
using YoutubeDownloader.Infrastructure.Models;
using YoutubeDownloader.Infrastructure.Repositories;

namespace YoutubeDownloader.Tests.Infrastructure;

public class DownloadTaskRepositoryTests : IDisposable
{
    private readonly YoutubeDownloaderDbContext _context;
    private readonly DownloadTaskRepository _repository;

    public DownloadTaskRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<YoutubeDownloaderDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new YoutubeDownloaderDbContext(options);
        _repository = new DownloadTaskRepository(_context);
    }

    [Fact]
    public async Task CreateAsync_ValidTask_ReturnsCreatedTask()
    {
        // Arrange
        var downloadTask = new DownloadTask
        {
            VideoUrl = "https://youtube.com/watch?v=test",
            VideoId = "test",
            VideoTitle = "Test Video",
            OutputPath = "/downloads/test.mp4",
            Format = "mp4",
            Quality = "720p",
            Status = DownloadStatus.Pending
        };

        // Act
        var result = await _repository.CreateAsync(downloadTask);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.VideoUrl.Should().Be(downloadTask.VideoUrl);
        result.Status.Should().Be(DownloadStatus.Pending);

        var savedTask = await _context.DownloadTasks.FindAsync(result.Id);
        savedTask.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingTask_ReturnsTask()
    {
        // Arrange
        var downloadTask = new DownloadTask
        {
            VideoUrl = "https://youtube.com/watch?v=test",
            VideoId = "test",
            OutputPath = "/downloads/test.mp4",
            Format = "mp4",
            Quality = "720p"
        };

        _context.DownloadTasks.Add(downloadTask);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(downloadTask.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(downloadTask.Id);
        result.VideoUrl.Should().Be(downloadTask.VideoUrl);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentTask_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByHangfireJobIdAsync_ExistingJobId_ReturnsTask()
    {
        // Arrange
        var jobId = "test-job-123";
        var downloadTask = new DownloadTask
        {
            VideoUrl = "https://youtube.com/watch?v=test",
            VideoId = "test",
            OutputPath = "/downloads/test.mp4",
            Format = "mp4",
            Quality = "720p",
            HangfireJobId = jobId
        };

        _context.DownloadTasks.Add(downloadTask);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByHangfireJobIdAsync(jobId);

        // Assert
        result.Should().NotBeNull();
        result!.HangfireJobId.Should().Be(jobId);
    }

    [Fact]
    public async Task GetByStatusAsync_ReturnsTasksWithMatchingStatus()
    {
        // Arrange
        var tasks = new[]
        {
            new DownloadTask
            {
                VideoUrl = "https://youtube.com/watch?v=test1",
                VideoId = "test1",
                OutputPath = "/downloads/test1.mp4",
                Format = "mp4",
                Quality = "720p",
                Status = DownloadStatus.Pending
            },
            new DownloadTask
            {
                VideoUrl = "https://youtube.com/watch?v=test2",
                VideoId = "test2",
                OutputPath = "/downloads/test2.mp4",
                Format = "mp4",
                Quality = "720p",
                Status = DownloadStatus.InProgress
            },
            new DownloadTask
            {
                VideoUrl = "https://youtube.com/watch?v=test3",
                VideoId = "test3",
                OutputPath = "/downloads/test3.mp4",
                Format = "mp4",
                Quality = "720p",
                Status = DownloadStatus.Pending
            }
        };

        _context.DownloadTasks.AddRange(tasks);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStatusAsync(DownloadStatus.Pending);

        // Assert
        result.Should().HaveCount(2);
        result.All(t => t.Status == DownloadStatus.Pending).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ExistingTask_UpdatesTask()
    {
        // Arrange
        var downloadTask = new DownloadTask
        {
            VideoUrl = "https://youtube.com/watch?v=test",
            VideoId = "test",
            OutputPath = "/downloads/test.mp4",
            Format = "mp4",
            Quality = "720p",
            Status = DownloadStatus.Pending,
            Progress = 0.0
        };

        _context.DownloadTasks.Add(downloadTask);
        await _context.SaveChangesAsync();

        // Act
        downloadTask.Status = DownloadStatus.InProgress;
        downloadTask.Progress = 50.0;
        await _repository.UpdateAsync(downloadTask);

        // Assert
        var updatedTask = await _context.DownloadTasks.FindAsync(downloadTask.Id);
        updatedTask.Should().NotBeNull();
        updatedTask!.Status.Should().Be(DownloadStatus.InProgress);
        updatedTask.Progress.Should().Be(50.0);
    }

    [Fact]
    public async Task DeleteAsync_ExistingTask_RemovesTask()
    {
        // Arrange
        var downloadTask = new DownloadTask
        {
            VideoUrl = "https://youtube.com/watch?v=test",
            VideoId = "test",
            OutputPath = "/downloads/test.mp4",
            Format = "mp4",
            Quality = "720p"
        };

        _context.DownloadTasks.Add(downloadTask);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(downloadTask.Id);

        // Assert
        var deletedTask = await _context.DownloadTasks.FindAsync(downloadTask.Id);
        deletedTask.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_ExistingTask_ReturnsTrue()
    {
        // Arrange
        var downloadTask = new DownloadTask
        {
            VideoUrl = "https://youtube.com/watch?v=test",
            VideoId = "test",
            OutputPath = "/downloads/test.mp4",
            Format = "mp4",
            Quality = "720p"
        };

        _context.DownloadTasks.Add(downloadTask);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsAsync(downloadTask.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonExistentTask_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.ExistsAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsTasksInDescendingOrder()
    {
        // Arrange
        var tasks = new[]
        {
            new DownloadTask
            {
                VideoUrl = "https://youtube.com/watch?v=test1",
                VideoId = "test1",
                OutputPath = "/downloads/test1.mp4",
                Format = "mp4",
                Quality = "720p",
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            },
            new DownloadTask
            {
                VideoUrl = "https://youtube.com/watch?v=test2",
                VideoId = "test2",
                OutputPath = "/downloads/test2.mp4",
                Format = "mp4",
                Quality = "720p",
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            },
            new DownloadTask
            {
                VideoUrl = "https://youtube.com/watch?v=test3",
                VideoId = "test3",
                OutputPath = "/downloads/test3.mp4",
                Format = "mp4",
                Quality = "720p",
                CreatedAt = DateTime.UtcNow
            }
        };

        _context.DownloadTasks.AddRange(tasks);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetRecentAsync(2);

        // Assert
        result.Should().HaveCount(2);
        var resultList = result.ToList();
        resultList[0].VideoId.Should().Be("test3"); // Most recent
        resultList[1].VideoId.Should().Be("test2"); // Second most recent
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}