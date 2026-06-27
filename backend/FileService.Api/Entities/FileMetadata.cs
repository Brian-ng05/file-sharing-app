using System.ComponentModel.DataAnnotations;

namespace FileService.Api.Entities;

public class FileMetadata
{
    public Guid Id { get; set; }

    public string Code { get; set; } = default!;

    public string StorageKey { get; set; } = default!;

    public string OriginalFilename { get; set; } = default!;

    public string MimeType { get; set; } = default!;

    public long SizeBytes { get; set; }

    public int DownloadCount { get; set; }

    public int? MaxDownloads { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }
}