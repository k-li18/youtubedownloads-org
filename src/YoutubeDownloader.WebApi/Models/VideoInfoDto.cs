namespace YoutubeDownloader.WebApi.Models;

public class VideoInfoDto
{
    public required string VideoId { get; set; }
    public required string Title { get; set; }
    public string? Author { get; set; }
    public string? ChannelId { get; set; }
    public string? Description { get; set; }
    public TimeSpan Duration { get; set; }
    public long? ViewCount { get; set; }
    public string? ThumbnailUrl { get; set; }
    public DateTime? UploadDate { get; set; }
    public List<DownloadOptionDto> DownloadOptions { get; set; } = new();
}

public class DownloadOptionDto
{
    public required string Container { get; set; }
    public bool IsAudioOnly { get; set; }
    public string? VideoQuality { get; set; }
    public string? AudioBitrate { get; set; }
    public long? FileSize { get; set; }
}