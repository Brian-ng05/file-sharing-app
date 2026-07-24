namespace FileService.Api.Dtos;

public class FileMetadataResponse
{
    public string Code { get; set; } = null!;

    public string OriginalFilename { get; set; } = null!;

    public string MimeType { get; set; } = null!;

    public long SizeBytes { get; set; }

    public bool RequiresPassword { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
