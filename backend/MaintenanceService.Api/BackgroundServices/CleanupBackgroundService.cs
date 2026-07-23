using MaintenanceService.Api.Services;

namespace MaintenanceService.Api.BackgroundServices;

public class CleanupBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CleanupBackgroundService> _logger;

    public CleanupBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<CleanupBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cleanup Background Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var cleanupService =
                    scope.ServiceProvider.GetRequiredService<ICleanupService>();

                var deletedCount =
                    await cleanupService.CleanupExpiredFilesAsync(stoppingToken);

                _logger.LogInformation(
                    "Cleanup completed successfully. Deleted {DeletedCount} expired file(s) at {Time}.",
                    deletedCount,
                    DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while cleaning expired files.");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromMinutes(15),
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Cleanup Background Service stopped.");
    }
}