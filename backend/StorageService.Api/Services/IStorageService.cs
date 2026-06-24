namespace StorageService.Api.Services;

public interface IStorageService
{
    Task<string> UploadFileAsync(IFormFile file);
    Task<byte[]> DownloadFileAsync(string key);
    Task DeleteFileAsync(string key);
}
