using YoutubeDownloader.Infrastructure.Models;

namespace YoutubeDownloader.WebApi.Models;

public class DownloadTaskDto
{
    public Guid Id { get; set; }
    public required string VideoUrl { get; set; }
    public required string VideoId { get; set; }
    public string? VideoTitle { get; set; }
    public string? Author { get; set; }
    public TimeSpan? Duration { get; set; }
    public required string OutputPath { get; set; }
    public required string Format { get; set; }
    public required string Quality { get; set; }
    public bool IncludeSubtitles { get; set; }
    public DownloadStatus Status { get; set; }
    public double Progress { get; set; }
    public string? ErrorMessage { get; set; }
    public long? FileSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class DownloadProgressDto
{
    public Guid TaskId { get; set; }
    public double Progress { get; set; }
    public DownloadStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public long? FileSizeBytes { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}