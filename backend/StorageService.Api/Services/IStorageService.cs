using Microsoft.AspNetCore.Http;

namespace StorageService.Api.Services;

public interface IStorageService
{
    Task<string> UploadAsync(IFormFile file);

    Task DeleteAsync(string storageKey);

    Task<string> GenerateSignedUrlAsync(string storageKey);
}