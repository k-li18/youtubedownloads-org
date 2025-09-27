using Microsoft.Extensions.Options;
using YoutubeDownloader.WebApi.Models;

namespace YoutubeDownloader.WebApi.Services;

/// <summary>
/// Periodically deletes completed download files after a retention window.
/// Uses task metadata to reconstruct file paths and remove them safely.
/// </summary>
public class FileCleanupService : BackgroundService
{
    private readonly ILogger<FileCleanupService> _logger;
    private readonly IOptions<DownloadSettings> _settings;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(15);

    public FileCleanupService(
        IOptions<DownloadSettings> settings,
        ILogger<FileCleanupService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FileCleanupService started. Interval: {Interval} minutes, Retention: {Retention} minutes",
            _interval.TotalMinutes, _settings.Value.RetentionMinutes);

        // First run shortly after startup
        try { await CleanupAsync(stoppingToken); } catch (Exception ex) { _logger.LogWarning(ex, "Initial cleanup failed"); }

        var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Periodic cleanup failed");
            }
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        var retention = TimeSpan.FromMinutes(Math.Max(0, _settings.Value.RetentionMinutes));
        var cutoff = DateTime.UtcNow - retention;
        
        var downloadPath = Path.GetFullPath(_settings.Value.DefaultOutputPath);
        
        if (!Directory.Exists(downloadPath))
        {
            _logger.LogDebug("Cleanup: download directory does not exist: {Path}", downloadPath);
            return;
        }

        _logger.LogDebug("Cleanup: scanning directory {Path} for files older than {Cutoff:o}", downloadPath, cutoff);

        var eligibleFiles = await Task.Run(() => ScanDownloadDirectory(downloadPath, cutoff), ct);
        
        if (eligibleFiles.Count == 0)
        {
            _logger.LogDebug("Cleanup: no files eligible for deletion (cutoff: {Cutoff:o})", cutoff);
            return;
        }

        _logger.LogInformation("Cleanup: found {Count} files eligible for deletion", eligibleFiles.Count);

        foreach (var filePath in eligibleFiles)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    // Check if file is currently in use
                    if (IsFileInUse(filePath))
                    {
                        _logger.LogDebug("Cleanup: skipping file in use: {File}", filePath);
                        continue;
                    }

                    File.Delete(filePath);
                    _logger.LogInformation("Cleanup: deleted file {File}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cleanup: failed to delete file {File}", filePath);
            }
        }
    }

    private List<string> ScanDownloadDirectory(string directoryPath, DateTime cutoff)
    {
        var eligibleFiles = new List<string>();
        var allowedFormats = _settings.Value.AllowedFormats.ToHashSet(StringComparer.OrdinalIgnoreCase);
        
        try
        {
            // Scan all files in directory and subdirectories
            var allFiles = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            
            foreach (var filePath in allFiles)
            {
                if (IsEligibleForCleanup(filePath, cutoff, allowedFormats))
                {
                    eligibleFiles.Add(filePath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cleanup: error scanning directory {Directory}", directoryPath);
        }
        
        return eligibleFiles;
    }

    private bool IsEligibleForCleanup(string filePath, DateTime cutoff, HashSet<string> allowedFormats)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            
            // Check if file is old enough (convert file times to UTC for proper comparison)
            var fileCreationUtc = fileInfo.CreationTime.ToUniversalTime();
            var fileWriteUtc = fileInfo.LastWriteTime.ToUniversalTime();
            
            if (fileCreationUtc > cutoff && fileWriteUtc > cutoff)
            {
                return false;
            }
            
            // Check if file extension is in allowed formats
            var extension = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();
            if (!allowedFormats.Contains(extension))
            {
                _logger.LogDebug("Cleanup: skipping file with non-allowed format: {File} (extension: {Extension})", 
                    filePath, extension);
                return false;
            }
            
            // Additional safety check: ensure we're in the download directory
            var downloadPath = Path.GetFullPath(_settings.Value.DefaultOutputPath);
            var fullFilePath = Path.GetFullPath(filePath);
            if (!fullFilePath.StartsWith(downloadPath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Cleanup: file outside download directory, skipping: {File}", filePath);
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cleanup: error checking file eligibility: {File}", filePath);
            return false;
        }
    }

    private bool IsFileInUse(string filePath)
    {
        try
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            // File is in use
            return true;
        }
        catch (Exception)
        {
            // For other exceptions, assume file is not in use but log the issue
            return false;
        }
    }
}

