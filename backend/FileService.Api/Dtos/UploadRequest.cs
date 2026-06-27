using Microsoft.AspNetCore.Http;

namespace FileService.Api.Dtos;

public class UploadRequest
{
    public IFormFile File { get; set; } = null!;
}
