using Microsoft.AspNetCore.Http;

namespace StorageService.Api.DTOs;

public class UploadRequest
{
    public IFormFile File { get; set; } = null!;
}
