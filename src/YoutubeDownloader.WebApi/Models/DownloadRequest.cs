using System.ComponentModel.DataAnnotations;

namespace YoutubeDownloader.WebApi.Models;

public class DownloadRequest
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public required string VideoUrl { get; set; }
    
    [Required]
    [StringLength(100)]
    public required string Format { get; set; }
    
    [Required]
    [StringLength(100)]
    public required string Quality { get; set; }
    
    public bool IncludeSubtitles { get; set; } = true;
    
    [StringLength(1000)]
    public string? OutputPath { get; set; }
}