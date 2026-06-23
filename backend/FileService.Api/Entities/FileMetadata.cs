using System.ComponentModel.DataAnnotations;

namespace FileService.Api.Entities;

public class FileMetadata
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Code { get; set; } = null!;

    [Required]
    public string OriginalFilename { get; set; } = null!;

    [Required]
    public string MimeType { get; set; } = null!;

    public long SizeBytes { get; set; }

    [Required]
    public string S3Key { get; set; } = null!;

    [Required]
    public string S3BucketName { get; set; } = null!;

    public int? MaxDownloads { get; set; }

    public int DownloadCount { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }
}