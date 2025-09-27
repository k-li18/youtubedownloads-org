using Microsoft.AspNetCore.Mvc;
using YoutubeDownloader.WebApi.Models;

namespace YoutubeDownloader.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet]
    public ActionResult<ApiResponse<object>> GetHealth()
    {
        _logger.LogInformation("Health check requested");

        var healthInfo = new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        };

        return Ok(ApiResponse<object>.SuccessResult(
            healthInfo,
            "Service is healthy"));
    }
}