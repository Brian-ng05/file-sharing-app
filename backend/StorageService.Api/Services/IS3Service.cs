namespace StorageService.Api.Services
{
    public interface IS3Service
    {
        Task<string> UploadFileAsync(IFormFile file);
        Task<string> GetPresignedUrlAsync(string key);
    }
}
