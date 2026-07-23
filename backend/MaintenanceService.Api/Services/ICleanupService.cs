namespace MaintenanceService.Api.Services
{
    public interface ICleanupService
    {
        Task<int> CleanupExpiredFilesAsync(CancellationToken cancellationToken = default);
    }
}
