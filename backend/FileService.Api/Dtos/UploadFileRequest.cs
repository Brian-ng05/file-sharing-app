namespace FileService.Api.Dtos.UploadFileRequest;

public class UploadFileRequest
{
    public IFormFile File { get; set; } = null!;

    public int? MaxDownloads { get; set; }

    public DateTime? ExpiresAt { get; set; }
}