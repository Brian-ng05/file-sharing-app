using FileService.Api.Dtos.UploadFileRequest;
using FileService.Api.Dtos.UploadFileResponse;
using FileService.Api.Entities;

namespace FileService.Api.Services
{

    public interface IFileService
    {
        Task<UploadFileResponse> UploadAsync(UploadFileRequest request);

        Task<string> DownloadAsync(string code, string? password);

        Task<bool> VerifyPasswordOnlyAsync(string code, string password);

        Task<FileMetadata?> GetMetadataAsync(string code);

        Task DeleteAsync(string code);

        Task<List<FileMetadata>> GetExpiredFilesAsync();
    }
}
