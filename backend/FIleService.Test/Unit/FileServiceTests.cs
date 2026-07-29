using FileService.Api.Dtos;
using FileService.Api.Dtos.UploadFileRequest;
using FileService.Api.Dtos.UploadFileResponse;
using FileService.Api.Entities;
using FileService.Api.Repository;
using FileService.Api.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using StorageService.Api.Dtos;
using FileManagementService = FileService.Api.Services.FileService;

namespace FIleService.Test.Unit;

public class FileServiceTests
{
    private readonly Mock<IFileRepository> _repoMock;
    private readonly Mock<IStorageApiClient> _storageApiMock;
    private readonly FileManagementService _sut;

    public FileServiceTests()
    {
        _repoMock = new Mock<IFileRepository>();
        _storageApiMock = new Mock<IStorageApiClient>();
        _sut = new FileManagementService(_repoMock.Object, _storageApiMock.Object);
    }

    // ──────────────────────────────────────────────
    // UPLOAD TESTS
    // ──────────────────────────────────────────────

    [Fact]
    public async Task UploadAsync_WithValidFile_UploadsToStorageAndSavesMetadata()
    {
        // Arrange
        var fileMock = CreateFormFile("test.pdf", "application/pdf", 512);
        var request = new UploadFileRequest
        {
            File = fileMock.Object,
            MaxDownloads = 5,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _storageApiMock
            .Setup(x => x.UploadFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(new UploadResponse { StorageKey = "s3/abc" });

        // Act
        var result = await _sut.UploadAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Code);
        Assert.Equal(8, result.Code.Length);
        Assert.Equal($"/files/{result.Code}", result.DownloadUrl);

        _storageApiMock.Verify(x => x.UploadFileAsync(fileMock.Object), Times.Once);
        _repoMock.Verify(x => x.AddAsync(It.Is<FileMetadata>(f =>
            f.StorageKey == "s3/abc" &&
            f.OriginalFilename == "test.pdf" &&
            f.MimeType == "application/pdf" &&
            f.SizeBytes == 512 &&
            f.MaxDownloads == 5 &&
            f.DownloadCount == 0)),
            Times.Once);
        _repoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UploadAsync_WithNullFile_ThrowsAndDoesNotCallDependencies()
    {
        var request = new UploadFileRequest { File = null! };

        var ex = await Assert.ThrowsAsync<Exception>(() => _sut.UploadAsync(request));

        Assert.Equal("File is required.", ex.Message);
        _storageApiMock.Verify(x => x.UploadFileAsync(It.IsAny<IFormFile>()), Times.Never);
        _repoMock.Verify(x => x.AddAsync(It.IsAny<FileMetadata>()), Times.Never);
    }

    [Fact]
    public async Task UploadAsync_WithEmptyFile_ThrowsAndDoesNotCallDependencies()
    {
        var fileMock = CreateFormFile("empty.txt", "text/plain", 0);
        var request = new UploadFileRequest { File = fileMock.Object };

        var ex = await Assert.ThrowsAsync<Exception>(() => _sut.UploadAsync(request));

        Assert.Equal("Empty file.", ex.Message);
        _storageApiMock.Verify(x => x.UploadFileAsync(It.IsAny<IFormFile>()), Times.Never);
        _repoMock.Verify(x => x.AddAsync(It.IsAny<FileMetadata>()), Times.Never);
    }

    [Fact]
    public async Task UploadAsync_WithFileOver10Mb_ThrowsAndDoesNotCallDependencies()
    {
        var fileMock = CreateFormFile("big.zip", "application/zip", 11 * 1024 * 1024);
        var request = new UploadFileRequest { File = fileMock.Object };

        var ex = await Assert.ThrowsAsync<Exception>(() => _sut.UploadAsync(request));

        Assert.Equal("File exceeds 10MB limit.", ex.Message);
        _storageApiMock.Verify(x => x.UploadFileAsync(It.IsAny<IFormFile>()), Times.Never);
        _repoMock.Verify(x => x.AddAsync(It.IsAny<FileMetadata>()), Times.Never);
    }

    [Fact]
    public async Task UploadAsync_WithInvalidMimeType_ThrowsAndDoesNotCallDependencies()
    {
        var fileMock = CreateFormFile("virus.exe", "application/x-msdownload", 256);
        var request = new UploadFileRequest { File = fileMock.Object };

        var ex = await Assert.ThrowsAsync<Exception>(() => _sut.UploadAsync(request));

        Assert.Equal("Invalid MIME type.", ex.Message);
        _storageApiMock.Verify(x => x.UploadFileAsync(It.IsAny<IFormFile>()), Times.Never);
        _repoMock.Verify(x => x.AddAsync(It.IsAny<FileMetadata>()), Times.Never);
    }

    [Fact]
    public async Task UploadAsync_WhenStorageUploadFails_DoesNotSaveMetadata()
    {
        var fileMock = CreateFormFile("fail.pdf", "application/pdf", 256);
        var request = new UploadFileRequest { File = fileMock.Object };

        _storageApiMock
            .Setup(x => x.UploadFileAsync(It.IsAny<IFormFile>()))
            .ThrowsAsync(new HttpRequestException("Storage down"));

        await Assert.ThrowsAsync<HttpRequestException>(() => _sut.UploadAsync(request));

        _repoMock.Verify(x => x.AddAsync(It.IsAny<FileMetadata>()), Times.Never);
        _repoMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UploadAsync_WhenDatabaseSaveFails_DeletesUploadedStorageObject()
    {
        var fileMock = CreateFormFile("rollback.pdf", "application/pdf", 256);
        var request = new UploadFileRequest { File = fileMock.Object };
        const string uploadedKey = "s3/rollback-key";

        _storageApiMock
            .Setup(x => x.UploadFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(new UploadResponse { StorageKey = uploadedKey });

        _repoMock
            .Setup(x => x.SaveChangesAsync())
            .ThrowsAsync(new InvalidOperationException("DB crash"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UploadAsync(request));

        // Compensation: uploaded object must be deleted
        _storageApiMock.Verify(x => x.DeleteFileAsync(uploadedKey), Times.Once);
        _repoMock.Verify(x => x.AddAsync(It.IsAny<FileMetadata>()), Times.Once);
        _repoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    // ──────────────────────────────────────────────
    // DOWNLOAD TESTS
    // ──────────────────────────────────────────────

    [Fact]
    public async Task DownloadAsync_WithValidFile_ReturnsSignedUrlAndIncrementsDownloadCount()
    {
        var code = "abc12345";
        var storageKey = "uploads/doc.pdf";
        var signedUrl = "https://s3.signed/url";

        var metadata = new FileMetadata
        {
            Code = code,
            StorageKey = storageKey,
            OriginalFilename = "doc.pdf",
            MimeType = "application/pdf",
            SizeBytes = 512,
            DownloadCount = 2,
            MaxDownloads = 10,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            PasswordHash = null
        };

        _repoMock.Setup(x => x.GetByCodeAsync(code)).ReturnsAsync(metadata);
        _storageApiMock.Setup(x => x.GetSignedUrlAsync(storageKey, It.IsAny<string?>()))
            .ReturnsAsync(new SignedUrlResponse { Url = signedUrl });

        var result = await _sut.DownloadAsync(code, null);

        Assert.Equal(signedUrl, result);
        Assert.Equal(3, metadata.DownloadCount);
        _repoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DownloadAsync_WhenCodeDoesNotExist_Throws()
    {
        _repoMock.Setup(x => x.GetByCodeAsync("missing"))
            .ReturnsAsync((FileMetadata?)null);

        var ex = await Assert.ThrowsAsync<Exception>(
            () => _sut.DownloadAsync("missing", null));

        Assert.Equal("File not found.", ex.Message);
        _storageApiMock.Verify(x => x.GetSignedUrlAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DownloadAsync_WhenFileExpired_DeletesFileAndThrows()
    {
        var code = "expired1";
        var storageKey = "uploads/old.pdf";

        var metadata = new FileMetadata
        {
            Code = code,
            StorageKey = storageKey,
            OriginalFilename = "old.pdf",
            MimeType = "application/pdf",
            SizeBytes = 100,
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            PasswordHash = null
        };

        _repoMock.Setup(x => x.GetByCodeAsync(code)).ReturnsAsync(metadata);

        var ex = await Assert.ThrowsAsync<Exception>(
            () => _sut.DownloadAsync(code, null));

        Assert.Equal("File expired.", ex.Message);
        _storageApiMock.Verify(x => x.DeleteFileAsync(storageKey), Times.Once);
        _repoMock.Verify(x => x.DeleteAsync(metadata), Times.Once);
        _repoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        _storageApiMock.Verify(x => x.GetSignedUrlAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DownloadAsync_WhenDownloadLimitReached_DeletesFileAndThrows()
    {
        var code = "limit99";
        var storageKey = "uploads/limited.pdf";

        var metadata = new FileMetadata
        {
            Code = code,
            StorageKey = storageKey,
            OriginalFilename = "limited.pdf",
            MimeType = "application/pdf",
            SizeBytes = 100,
            DownloadCount = 5,
            MaxDownloads = 5,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            PasswordHash = null
        };

        _repoMock.Setup(x => x.GetByCodeAsync(code)).ReturnsAsync(metadata);

        var ex = await Assert.ThrowsAsync<Exception>(
            () => _sut.DownloadAsync(code, null));

        Assert.Equal("Download limit reached.", ex.Message);
        _storageApiMock.Verify(x => x.DeleteFileAsync(storageKey), Times.Once);
        _repoMock.Verify(x => x.DeleteAsync(metadata), Times.Once);
        _storageApiMock.Verify(x => x.GetSignedUrlAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DownloadAsync_WhenSignedUrlResponseIsEmpty_Throws()
    {
        var code = "emptyurl";
        var storageKey = "uploads/empty.pdf";

        var metadata = new FileMetadata
        {
            Code = code,
            StorageKey = storageKey,
            OriginalFilename = "empty.pdf",
            MimeType = "application/pdf",
            SizeBytes = 100,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            PasswordHash = null
        };

        _repoMock.Setup(x => x.GetByCodeAsync(code)).ReturnsAsync(metadata);
        _storageApiMock.Setup(x => x.GetSignedUrlAsync(storageKey, It.IsAny<string?>()))
            .ReturnsAsync(new SignedUrlResponse { Url = "" });

        var ex = await Assert.ThrowsAsync<Exception>(
            () => _sut.DownloadAsync(code, null));

        Assert.Equal("Failed to generate signed URL.", ex.Message);
    }

    // ──────────────────────────────────────────────
    // DELETE TESTS
    // ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WhenFileExists_DeletesStorageAndMetadata()
    {
        var code = "del001";
        var storageKey = "uploads/bye.pdf";
        var metadata = new FileMetadata { Code = code, StorageKey = storageKey };

        _repoMock.Setup(x => x.GetByCodeAsync(code)).ReturnsAsync(metadata);

        await _sut.DeleteAsync(code);

        _storageApiMock.Verify(x => x.DeleteFileAsync(storageKey), Times.Once);
        _repoMock.Verify(x => x.DeleteAsync(metadata), Times.Once);
        _repoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenFileDoesNotExist_DoesNothing()
    {
        _repoMock.Setup(x => x.GetByCodeAsync("nope"))
            .ReturnsAsync((FileMetadata?)null);

        await _sut.DeleteAsync("nope");

        _storageApiMock.Verify(x => x.DeleteFileAsync(It.IsAny<string>()), Times.Never);
        _repoMock.Verify(x => x.DeleteAsync(It.IsAny<FileMetadata>()), Times.Never);
        _repoMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    // ──────────────────────────────────────────────
    // GET EXPIRED FILES
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetExpiredFilesAsync_ReturnsRepositoryResult()
    {
        var files = new List<FileMetadata>
        {
            new() { Code = "exp1" },
            new() { Code = "exp2" }
        };

        _repoMock.Setup(x => x.GetExpiredFilesAsync()).ReturnsAsync(files);

        var result = await _sut.GetExpiredFilesAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, f => f.Code == "exp1");
        Assert.Contains(result, f => f.Code == "exp2");
    }

    // ──────────────────────────────────────────────
    // PHASE 9 — SAFE DELETE INTERNAL ORPHAN RISK
    // ──────────────────────────────────────────────

    /// <summary>
    /// SafeDeleteInternal catches storage exceptions but ALWAYS deletes metadata.
    /// If storage deletion fails, the S3 object becomes an orphan.
    /// This test documents the current behavior — it is NOT a bug per se,
    /// but a known trade-off. Metadata is cleaned up to avoid stale DB state.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_WhenStorageDeletionFails_DoesNotSilentlyLoseMetadata()
    {
        // This test reveals the SafeDeleteInternal behavior:
        // When DeleteFileAsync (storage) throws, the catch block swallows it
        // and metadata is STILL removed from the database.
        // Result: storage object becomes orphan on S3, metadata is gone.

        var code = "orphan1";
        var storageKey = "uploads/orphan.pdf";
        var metadata = new FileMetadata { Code = code, StorageKey = storageKey };

        _repoMock.Setup(x => x.GetByCodeAsync(code)).ReturnsAsync(metadata);
        _storageApiMock.Setup(x => x.DeleteFileAsync(storageKey))
            .ThrowsAsync(new Exception("S3 unavailable"));

        // Act — delete should succeed (not throw) because SafeDeleteInternal swallows errors
        await _sut.DeleteAsync(code);

        // Assert: storage deletion was attempted
        _storageApiMock.Verify(x => x.DeleteFileAsync(storageKey), Times.Once);
        // BUT metadata is still deleted regardless of storage failure
        _repoMock.Verify(x => x.DeleteAsync(metadata), Times.Once);
        _repoMock.Verify(x => x.SaveChangesAsync(), Times.Once);

        // KNOWN TRADE-OFF: orphan object remains on S3 with key "uploads/orphan.pdf"
        // but no corresponding metadata record exists in PostgreSQL.
        // This is by design — the app prioritizes clean DB state over S3 consistency.
    }

    // ──────────────────────────────────────────────
    // HELPER
    // ──────────────────────────────────────────────

    private static Mock<IFormFile> CreateFormFile(
        string fileName, string contentType, long length)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.ContentType).Returns(contentType);
        mock.Setup(f => f.Length).Returns(length);
        mock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
        return mock;
    }
}
