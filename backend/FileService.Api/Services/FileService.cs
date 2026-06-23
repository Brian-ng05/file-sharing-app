using FileService.Api.Dtos.UploadFileRequest;
using FileService.Api.Dtos.UploadFileResponse;
using FileService.Api.Entities;
using FileService.Api.Models;
using FileService.Api.Repository;
using Microsoft.Extensions.Options;

namespace FileService.Api.Services
{
    public class FileService : IFileService
    {
        private readonly IFileRepository _repo;
        private readonly IStorageService _storageService;
        private readonly AwsSettings _awsSettings;

        private const long MAX_SIZE = 10 * 1024 * 1024;

        public FileService(
            IFileRepository repo,
            IStorageService storageService,
            IOptions<AwsSettings> awsSettings)
        {
            _repo = repo;
            _storageService = storageService;
            _awsSettings = awsSettings.Value;
        }

        public async Task<UploadFileResponse> UploadAsync(
            UploadFileRequest request)
        {
            if (request.File.Length == 0)
                throw new Exception("Empty file");

            if (request.File.Length > MAX_SIZE)
                throw new Exception("File exceeds 10 MB");

            var allowedMimeTypes = new[]
            {
                "image/jpeg",
                "image/png",
                "application/pdf",
                "text/plain",
                "application/zip"
            };

            if (!allowedMimeTypes.Contains(request.File.ContentType))
            {
                throw new Exception("Invalid MIME type");
            }

            var code = Guid.NewGuid()
                .ToString("N")
                .Substring(0, 8);

            var s3Key = await _storageService.UploadFileAsync(request.File);

            var entity = new FileMetadata
            {
                Code = code,
                OriginalFilename = request.File.FileName,
                MimeType = request.File.ContentType,
                SizeBytes = request.File.Length,
                S3Key = s3Key,
                S3BucketName = _awsSettings.BucketName,
                MaxDownloads = request.MaxDownloads,
                DownloadCount = 0,
                ExpiresAt = request.ExpiresAt,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(entity);
            await _repo.SaveChangesAsync();

            return new UploadFileResponse
            {
                Code = code,
                DownloadUrl = $"/files/{code}"
            };
        }

        public async Task<(byte[] Content,
            string FileName,
            string MimeType)> DownloadAsync(
            string code)
        {
            var file = await _repo.GetByCodeAsync(code);

            if (file == null)
                throw new Exception("File not found");

            if (file.ExpiresAt.HasValue &&
                file.ExpiresAt.Value < DateTime.UtcNow)
            {
                await DeleteAsync(code);
                throw new Exception("File expired");
            }

            if (file.MaxDownloads.HasValue &&
                file.DownloadCount >= file.MaxDownloads)
            {
                await DeleteAsync(code);
                throw new Exception("Download limit reached");
            }

            var bytes = await _storageService.DownloadFileAsync(file.S3Key);

            file.DownloadCount++;

            await _repo.SaveChangesAsync();

            return (
                bytes,
                file.OriginalFilename,
                file.MimeType
            );
        }

        public async Task DeleteAsync(string code)
        {
            var file = await _repo.GetByCodeAsync(code);

            if (file == null)
                return;

            await _storageService.DeleteFileAsync(file.S3Key);

            await _repo.DeleteAsync(file);
            await _repo.SaveChangesAsync();
        }
    }
}
