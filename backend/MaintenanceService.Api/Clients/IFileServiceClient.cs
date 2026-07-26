using MaintenanceService.Api.DTOs;

namespace MaintenanceService.Api.Clients
{
    public interface IFileServiceClient
    {
        Task<List<ExpiredFileDto>> GetExpiredFilesAsync(CancellationToken cancellationToken = default);
        Task DeleteFileAsync(string code, CancellationToken cancellationToken = default);
    }
}
