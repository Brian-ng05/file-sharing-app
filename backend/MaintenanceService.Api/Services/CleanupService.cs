using MaintenanceService.Api.Clients;

namespace MaintenanceService.Api.Services;

public class CleanupService : ICleanupService
{
    private readonly IFileServiceClient _fileServiceClient;
    private readonly ILogger<CleanupService> _logger;

    public CleanupService(
        IFileServiceClient fileServiceClient,
        ILogger<CleanupService> logger)
    {
        _fileServiceClient = fileServiceClient;
        _logger = logger;
    }

    public async Task<int> CleanupExpiredFilesAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting expired file cleanup.");

        var expiredFiles =
            await _fileServiceClient.GetExpiredFilesAsync(cancellationToken);

        if (expiredFiles.Count == 0)
        {
            _logger.LogInformation("No expired files found.");

            return 0;
        }

        var deletedCount = 0;

        foreach (var file in expiredFiles)
        {
            try
            {
                await _fileServiceClient.DeleteFileAsync(
                    file.Code,
                    cancellationToken);

                deletedCount++;

                _logger.LogDebug(
                    "Deleted expired file {Code}.",
                    file.Code);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to delete expired file {Code}. Continuing with next file.",
                    file.Code);
            }
        }

        _logger.LogInformation(
            "Expired file cleanup finished. Deleted {DeletedCount} of {TotalFiles} file(s).",
            deletedCount,
            expiredFiles.Count);

        return deletedCount;
    }
}