namespace FileService.Api.Services;

public interface IStorageClient
{
    Task<(string Key, string BucketName)> UploadFileAsync(IFormFile file);
    Task<byte[]> DownloadFileAsync(string key);
    Task DeleteFileAsync(string key);
}
