namespace YoutubeDownloader.WebApi.Models;

public class DownloadSettings
{
    public string DefaultOutputPath { get; set; } = "./downloads";
    public int MaxConcurrentDownloads { get; set; } = 3;
    public string[] AllowedFormats { get; set; } = ["mp4", "webm", "mp3", "ogg"];
    public int MaxFileSizeMB { get; set; } = 2048;

    // Minutes to retain files on server after completion
    public int RetentionMinutes { get; set; } = 120;
    
    // Minutes between data cleanup operations (for testing use shorter intervals)
    public int DataCleanupIntervalMinutes { get; set; } = 30;
    
    // Days to retain task and video data in memory
    public int DataRetentionDays { get; set; } = 7;
}

