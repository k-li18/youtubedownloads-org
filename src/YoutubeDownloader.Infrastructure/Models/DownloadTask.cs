namespace YoutubeDownloader.Infrastructure.Models;

public class DownloadTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public required string VideoUrl { get; set; }
    
    public required string VideoId { get; set; }
    
    public string? VideoTitle { get; set; }
    
    public string? Author { get; set; }
    
    public TimeSpan? Duration { get; set; }
    
    public required string OutputPath { get; set; }
    
    public required string Format { get; set; }
    
    public required string Quality { get; set; }
    
    public bool IncludeSubtitles { get; set; } = true;
    
    public DownloadStatus Status { get; set; } = DownloadStatus.Pending;
    
    public double Progress { get; set; } = 0.0;
    
    public string? ErrorMessage { get; set; }
    
    public long? FileSizeBytes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? StartedAt { get; set; }
    
    public DateTime? CompletedAt { get; set; }
    
    public string? HangfireJobId { get; set; }
}

public enum DownloadStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}