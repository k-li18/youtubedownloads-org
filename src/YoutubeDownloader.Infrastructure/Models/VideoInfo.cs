namespace YoutubeDownloader.Infrastructure.Models;

public class VideoInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public required string VideoUrl { get; set; }
    
    public required string VideoId { get; set; }
    
    public required string Title { get; set; }
    
    public string? Author { get; set; }
    
    public string? ChannelId { get; set; }
    
    public string? Description { get; set; }
    
    public TimeSpan Duration { get; set; }
    
    public long? ViewCount { get; set; }
    
    public string? ThumbnailUrl { get; set; }
    
    public DateTime? UploadDate { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}