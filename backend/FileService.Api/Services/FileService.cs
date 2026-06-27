using FileService.Api.Dtos.UploadFileRequest;
using FileService.Api.Dtos.UploadFileResponse;
using FileService.Api.Entities;
using FileService.Api.Repository;

namespace FileService.Api.Services
{
    public class FileService : IFileService
    {
        private readonly IFileRepository _repo;
        private readonly StorageApiClient _storageApiClient;

        private const long MAX_SIZE = 10 * 1024 * 1024;

        private static readonly string[] AllowedMimeTypes =
        {
            "image/jpeg",
            "image/png",
            "application/pdf",
            "text/plain",
            "application/zip"
        };

        public FileService(
            IFileRepository repo,
            StorageApiClient storageApiClient)
        {
            _repo = repo;
            _storageApiClient = storageApiClient;
        }

        public async Task<UploadFileResponse> UploadAsync(UploadFileRequest request)
        {
            ValidateUploadRequest(request);

            var uploadResult =
                await _storageApiClient.UploadFileAsync(request.File);

            if (uploadResult is null)
                throw new Exception("Storage upload failed.");

            var code = GenerateCode();

            var entity = new FileMetadata
            {
                Code = code,
                StorageKey = uploadResult.StorageKey,
                OriginalFilename = request.File.FileName,
                MimeType = request.File.ContentType,
                SizeBytes = request.File.Length,
                MaxDownloads = request.MaxDownloads,
                DownloadCount = 0,
                ExpiresAt = request.ExpiresAt,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _repo.AddAsync(entity);
                await _repo.SaveChangesAsync();
            }
            catch
            {
                await _storageApiClient.DeleteFileAsync(uploadResult.StorageKey);
                throw;
            }

            return new UploadFileResponse
            {
                Code = code,
                DownloadUrl = $"/files/{code}"
            };
        }

        public async Task<string> DownloadAsync(string code)
        {
            var file = await _repo.GetByCodeAsync(code);

            if (file is null)
                throw new Exception("File not found.");

            if (IsExpired(file))
            {
                await SafeDeleteInternal(file);
                throw new Exception("File expired.");
            }

            if (IsDownloadLimitReached(file))
            {
                await SafeDeleteInternal(file);
                throw new Exception("Download limit reached.");
            }

            var signedUrl =
                await _storageApiClient.GetSignedUrlAsync(file.StorageKey);

            if (signedUrl is null || string.IsNullOrEmpty(signedUrl.Url))
                throw new Exception("Failed to generate signed URL.");

            file.DownloadCount++;

            await _repo.SaveChangesAsync();

            return signedUrl.Url;
        }

        public async Task DeleteAsync(string code)
        {
            var file = await _repo.GetByCodeAsync(code);

            if (file is null)
                return;

            await SafeDeleteInternal(file);
        }

        // =========================
        // PRIVATE HELPERS
        // =========================

        private void ValidateUploadRequest(UploadFileRequest request)
        {
            if (request.File is null)
                throw new Exception("File is required.");

            if (request.File.Length == 0)
                throw new Exception("Empty file.");

            if (request.File.Length > MAX_SIZE)
                throw new Exception("File exceeds 10MB limit.");

            if (!AllowedMimeTypes.Contains(request.File.ContentType))
                throw new Exception("Invalid MIME type.");
        }

        private static string GenerateCode()
        {
            return Guid.NewGuid()
                .ToString("N")
                .Substring(0, 8);
        }

        private static bool IsExpired(FileMetadata file)
        {
            return file.ExpiresAt.HasValue &&
                   file.ExpiresAt.Value <= DateTime.UtcNow;
        }

        private static bool IsDownloadLimitReached(FileMetadata file)
        {
            return file.MaxDownloads.HasValue &&
                   file.DownloadCount >= file.MaxDownloads.Value;
        }

        private async Task SafeDeleteInternal(FileMetadata file)
        {
            try
            {
                await _storageApiClient.DeleteFileAsync(file.StorageKey);
            }
            catch
            {
                // optional: log
            }

            await _repo.DeleteAsync(file);
            await _repo.SaveChangesAsync();
        }
    }
}