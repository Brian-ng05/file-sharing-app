using FileService.Api.Dtos;
using StorageService.Api.Dtos;

namespace FileService.Api.Services
{
    public interface IStorageApiClient
    {
        Task<UploadResponse> UploadFileAsync(IFormFile file);
        Task<SignedUrlResponse> GetSignedUrlAsync(string storageKey, string? fileName = null);
        Task DeleteFileAsync(string storageKey);
    }
}
