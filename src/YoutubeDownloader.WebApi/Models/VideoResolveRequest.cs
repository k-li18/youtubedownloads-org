using System.ComponentModel.DataAnnotations;

namespace YoutubeDownloader.WebApi.Models;

public class VideoResolveRequest
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public required string Url { get; set; }
}