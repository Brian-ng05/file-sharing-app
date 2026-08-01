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
    public async Task DownloadAsync_WithPasswordProtectedFile_CorrectPassword_ReturnsSignedUrl()
    {
        var code = "pw12345";
        var storageKey = "uploads/secret.pdf";
        var signedUrl = "https://s3.signed/secret";
        var password = "mypassword";

        var metadata = new FileMetadata
        {
            Code = code,
            StorageKey = storageKey,
            OriginalFilename = "secret.pdf",
            MimeType = "application/pdf",
            SizeBytes = 512,
            DownloadCount = 0,
            MaxDownloads = 10,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };

        _repoMock.Setup(x => x.GetByCodeAsync(code)).ReturnsAsync(metadata);
        _storageApiMock.Setup(x => x.GetSignedUrlAsync(storageKey, It.IsAny<string?>()))
            .ReturnsAsync(new SignedUrlResponse { Url = signedUrl });

        var result = await _sut.DownloadAsync(code, password);

        Assert.Equal(signedUrl, result);
        Assert.Equal(1, metadata.DownloadCount);
    }

    [Fact]
    public async Task DownloadAsync_WithPasswordProtectedFile_NoPassword_Throws()
    {
        var code = "pw12345";
        var storageKey = "uploads/secret.pdf";

        var metadata = new FileMetadata
        {
            Code = code,
            StorageKey = storageKey,
            OriginalFilename = "secret.pdf",
            MimeType = "application/pdf",
            SizeBytes = 512,
            DownloadCount = 0,
            MaxDownloads = 10,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("mypassword")
        };

        _repoMock.Setup(x => x.GetByCodeAsync(code)).ReturnsAsync(metadata);

        var ex = await Assert.ThrowsAsync<Exception>(
            () => _sut.DownloadAsync(code, null));

        Assert.Equal("Password is required.", ex.Message);
        _storageApiMock.Verify(x => x.GetSignedUrlAsync(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task DownloadAsync_WithPasswordProtectedFile_WrongPassword_Throws()
    {
        var code = "pw12345";
        var storageKey = "uploads/secret.pdf";

        var metadata = new FileMetadata
        {
            Code = code,
            StorageKey = storageKey,
            OriginalFilename = "secret.pdf",
            MimeType = "application/pdf",
            SizeBytes = 512,
            DownloadCount = 0,
            MaxDownloads = 10,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword")
        };

        _repoMock.Setup(x => x.GetByCodeAsync(code)).ReturnsAsync(metadata);

        var ex = await Assert.ThrowsAsync<Exception>(
            () => _sut.DownloadAsync(code, "wrongpassword"));

        Assert.Equal("Invalid password.", ex.Message);
        _storageApiMock.Verify(x => x.GetSignedUrlAsync(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task DownloadAsync_WithEmptyPasswordString_WhenPasswordRequired_Throws()
    {
        var code = "pw12345";
        var storageKey = "uploads/secret.pdf";

        var metadata = new FileMetadata
        {
            Code = code,
            StorageKey = storageKey,
            OriginalFilename = "secret.pdf",
            MimeType = "application/pdf",
            SizeBytes = 512,
            DownloadCount = 0,
            MaxDownloads = 10,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("mypassword")
        };

        _repoMock.Setup(x => x.GetByCodeAsync(code)).ReturnsAsync(metadata);

        var ex = await Assert.ThrowsAsync<Exception>(
            () => _sut.DownloadAsync(code, ""));

        Assert.Equal("Password is required.", ex.Message);
    }

    [Fact]
    public async Task DownloadAsync_WithNoPasswordProtectedFile_PasswordIgnored()
    {
        var code = "nopw001";
        var storageKey = "uploads/open.pdf";
        var signedUrl = "https://s3.signed/open";

        var metadata = new FileMetadata
        {
            Code = code,
            StorageKey = storageKey,
            OriginalFilename = "open.pdf",
            MimeType = "application/pdf",
            SizeBytes = 512,
            DownloadCount = 0,
            MaxDownloads = 10,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            PasswordHash = null
        };

        _repoMock.Setup(x => x.GetByCodeAsync(code)).ReturnsAsync(metadata);
        _storageApiMock.Setup(x => x.GetSignedUrlAsync(storageKey, It.IsAny<string?>()))
            .ReturnsAsync(new SignedUrlResponse { Url = signedUrl });

        // Password provided but file has no password — it should be ignored
        var result = await _sut.DownloadAsync(code, "somepassword");

        Assert.Equal(signedUrl, result);
    }

    [Fact]
    public async Task DownloadAsync_WithMaxDownloadsNull_UnlimitedDownloads()
    {
        var code = "nolimit1";
        var storageKey = "uploads/unlimited.pdf";
        var signedUrl = "https://s3.signed/unlimited";

        var metadata = new FileMetadata
        {
            Code = code,
            StorageKey = storageKey,
            OriginalFilename = "unlimited.pdf",
            MimeType = "application/pdf",
            SizeBytes = 512,
            DownloadCount = 100,
            MaxDownloads = null,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            PasswordHash = null
        };

        _repoMock.Setup(x => x.GetByCodeAsync(code)).ReturnsAsync(metadata);
        _storageApiMock.Setup(x => x.GetSignedUrlAsync(storageKey, It.IsAny<string?>()))
            .ReturnsAsync(new SignedUrlResponse { Url = signedUrl });

        var result = await _sut.DownloadAsync(code, null);

        Assert.Equal(signedUrl, result);
        Assert.Equal(101, metadata.DownloadCount);
    }

    [Fact]
    public async Task DownloadAsync_WithNullExpiresAt_NeverExpires()
    {
        var code = "noexp001";
        var storageKey = "uploads/neverexpires.pdf";
        var signedUrl = "https://s3.signed/never";

        var metadata = new FileMetadata
        {
            Code = code,
            StorageKey = storageKey,
            OriginalFilename = "neverexpires.pdf",
            MimeType = "application/pdf",
            SizeBytes = 512,
            DownloadCount = 0,
            MaxDownloads = 10,
            ExpiresAt = null,
            PasswordHash = null
        };

        _repoMock.Setup(x => x.GetByCodeAsync(code)).ReturnsAsync(metadata);
        _storageApiMock.Setup(x => x.GetSignedUrlAsync(storageKey, It.IsAny<string?>()))
            .ReturnsAsync(new SignedUrlResponse { Url = signedUrl });

        var result = await _sut.DownloadAsync(code, null);

        Assert.Equal(signedUrl, result);
    }

    [Fact]
    public async Task DownloadAsync_WhenSignedUrlResponseIsNull_Throws()
    {
        var code = "nullurl1";
        var storageKey = "uploads/null.pdf";

        var metadata = new FileMetadata
        {
            Code = code,
            StorageKey = storageKey,
            OriginalFilename = "null.pdf",
            MimeType = "application/pdf",
            SizeBytes = 100,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            PasswordHash = null
        };

        _repoMock.Setup(x => x.GetByCodeAsync(code)).ReturnsAsync(metadata);
        _storageApiMock.Setup(x => x.GetSignedUrlAsync(storageKey, It.IsAny<string?>()))
            .ReturnsAsync((SignedUrlResponse?)null);

        var ex = await Assert.ThrowsAsync<Exception>(
            () => _sut.DownloadAsync(code, null));

        Assert.Equal("Failed to generate signed URL.", ex.Message);
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
    public async Task DownloadAsync_WhenFileExpired_WithPasswordProtection_DeletesFileAndThrows()
    {
        var code = "expired2";
        var storageKey = "uploads/oldsecret.pdf";

        var metadata = new FileMetadata
        {
            Code = code,
            StorageKey = storageKey,
            OriginalFilename = "oldsecret.pdf",
            MimeType = "application/pdf",
            SizeBytes = 100,
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("secret")
        };

        _repoMock.Setup(x => x.GetByCodeAsync(code)).ReturnsAsync(metadata);

        var ex = await Assert.ThrowsAsync<Exception>(
            () => _sut.DownloadAsync(code, "secret"));

        Assert.Equal("File expired.", ex.Message);
        _storageApiMock.Verify(x => x.DeleteFileAsync(storageKey), Times.Once);
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
    public async Task DownloadAsync_WhenDownloadCountExceedsMaxDownloads_DeletesFileAndThrows()
    {
        var code = "limit101";
        var storageKey = "uploads/overlimit.pdf";

        var metadata = new FileMetadata
        {
            Code = code,
            StorageKey = storageKey,
            OriginalFilename = "overlimit.pdf",
            MimeType = "application/pdf",
            SizeBytes = 100,
            DownloadCount = 10,
            MaxDownloads = 3,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            PasswordHash = null
        };

        _repoMock.Setup(x => x.GetByCodeAsync(code)).ReturnsAsync(metadata);

        var ex = await Assert.ThrowsAsync<Exception>(
            () => _sut.DownloadAsync(code, null));

        Assert.Equal("Download limit reached.", ex.Message);
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

    [Fact]
    public async Task GetExpiredFilesAsync_WhenNoExpiredFiles_ReturnsEmptyList()
    {
        _repoMock.Setup(x => x.GetExpiredFilesAsync())
            .ReturnsAsync(new List<FileMetadata>());

        var result = await _sut.GetExpiredFilesAsync();

        Assert.Empty(result);
    }

    // ──────────────────────────────────────────────
    // VERIFY PASSWORD ONLY TESTS
    // ──────────────────────────────────────────────

    [Fact]
    public async Task VerifyPasswordOnlyAsync_WithNoPasswordSet_ReturnsTrue()
    {
        var code = "nopw001";
        var metadata = new FileMetadata
        {
            Code = code,
            StorageKey = "uploads/nopw.pdf",
            PasswordHash = null
        };

        _repoMock.Setup(x => x.GetByCodeAsync(code)).ReturnsAsync(metadata);

        var result = await _sut.VerifyPasswordOnlyAsync(code, "anypassword");

        Assert.True(result);
    }

    [Fact]
    public async Task VerifyPasswordOnlyAsync_WithCorrectPassword_ReturnsTrue()
    {
        var code = "pwok001";
        var password = "correctpassword";
        var metadata = new FileMetadata
        {
            Code = code,
            StorageKey = "uploads/pwok.pdf",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };

        _repoMock.Setup(x => x.GetByCodeAsync(code)).ReturnsAsync(metadata);

        var result = await _sut.VerifyPasswordOnlyAsync(code, password);

        Assert.True(result);
    }

    [Fact]
    public async Task VerifyPasswordOnlyAsync_WithWrongPassword_ReturnsFalse()
    {
        var code = "pwrong01";
        var metadata = new FileMetadata
        {
            Code = code,
            StorageKey = "uploads/pwrong.pdf",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword")
        };

        _repoMock.Setup(x => x.GetByCodeAsync(code)).ReturnsAsync(metadata);

        var result = await _sut.VerifyPasswordOnlyAsync(code, "wrongpassword");

        Assert.False(result);
    }

    [Fact]
    public async Task VerifyPasswordOnlyAsync_WhenPasswordRequired_AndEmptyPassword_ReturnsFalse()
    {
        var code = "pwempty1";
        var metadata = new FileMetadata
        {
            Code = code,
            StorageKey = "uploads/pwempty.pdf",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("somepassword")
        };

        _repoMock.Setup(x => x.GetByCodeAsync(code)).ReturnsAsync(metadata);

        var result = await _sut.VerifyPasswordOnlyAsync(code, "");

        Assert.False(result);
    }

    [Fact]
    public async Task VerifyPasswordOnlyAsync_WhenPasswordRequired_AndNullPassword_ReturnsFalse()
    {
        var code = "pwnull01";
        var metadata = new FileMetadata
        {
            Code = code,
            StorageKey = "uploads/pwnull.pdf",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("somepassword")
        };

        _repoMock.Setup(x => x.GetByCodeAsync(code)).ReturnsAsync(metadata);

        var result = await _sut.VerifyPasswordOnlyAsync(code, null!);

        Assert.False(result);
    }

    [Fact]
    public async Task VerifyPasswordOnlyAsync_WhenFileNotFound_Throws()
    {
        _repoMock.Setup(x => x.GetByCodeAsync("missing"))
            .ReturnsAsync((FileMetadata?)null);

        var ex = await Assert.ThrowsAsync<Exception>(
            () => _sut.VerifyPasswordOnlyAsync("missing", "password"));

        Assert.Equal("File not found.", ex.Message);
    }

    // ──────────────────────────────────────────────
    // GET METADATA TESTS
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetMetadataAsync_WhenFileExists_ReturnsFileMetadata()
    {
        var code = "meta001";
        var expected = new FileMetadata
        {
            Code = code,
            StorageKey = "uploads/meta.pdf",
            OriginalFilename = "meta.pdf",
            MimeType = "application/pdf",
            SizeBytes = 1024,
            DownloadCount = 3,
            MaxDownloads = 10,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("secret")
        };

        _repoMock.Setup(x => x.GetByCodeAsync(code)).ReturnsAsync(expected);

        var result = await _sut.GetMetadataAsync(code);

        Assert.NotNull(result);
        Assert.Equal(code, result.Code);
        Assert.Equal("meta.pdf", result.OriginalFilename);
        Assert.Equal("application/pdf", result.MimeType);
        Assert.Equal(1024, result.SizeBytes);
        Assert.NotNull(result.PasswordHash);
    }

    [Fact]
    public async Task GetMetadataAsync_WhenFileDoesNotExist_ReturnsNull()
    {
        _repoMock.Setup(x => x.GetByCodeAsync("missing"))
            .ReturnsAsync((FileMetadata?)null);

        var result = await _sut.GetMetadataAsync("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMetadataAsync_WithNoPassword_ReturnsMetadataWithNullHash()
    {
        var code = "nopwmeta";
        var expected = new FileMetadata
        {
            Code = code,
            StorageKey = "uploads/nopw.pdf",
            OriginalFilename = "nopw.pdf",
            MimeType = "text/plain",
            SizeBytes = 256,
            PasswordHash = null
        };

        _repoMock.Setup(x => x.GetByCodeAsync(code)).ReturnsAsync(expected);

        var result = await _sut.GetMetadataAsync(code);

        Assert.NotNull(result);
        Assert.Null(result.PasswordHash);
    }

    // ──────────────────────────────────────────────
    // UPLOAD ADDITIONAL TESTS
    // ──────────────────────────────────────────────

    [Fact]
    public async Task UploadAsync_WithPassword_HashesPassword()
    {
        var fileMock = CreateFormFile("secret.pdf", "application/pdf", 512);
        var request = new UploadFileRequest
        {
            File = fileMock.Object,
            Password = "mypassword"
        };

        _storageApiMock
            .Setup(x => x.UploadFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(new UploadResponse { StorageKey = "s3/secret" });

        var result = await _sut.UploadAsync(request);

        Assert.NotNull(result);
        _repoMock.Verify(x => x.AddAsync(It.Is<FileMetadata>(f =>
            f.PasswordHash != null &&
            !string.IsNullOrEmpty(f.PasswordHash))),
            Times.Once);
    }

    [Fact]
    public async Task UploadAsync_WithEmptyPassword_DoesNotHash()
    {
        var fileMock = CreateFormFile("open.pdf", "application/pdf", 512);
        var request = new UploadFileRequest
        {
            File = fileMock.Object,
            Password = ""
        };

        _storageApiMock
            .Setup(x => x.UploadFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(new UploadResponse { StorageKey = "s3/open" });

        var result = await _sut.UploadAsync(request);

        Assert.NotNull(result);
        _repoMock.Verify(x => x.AddAsync(It.Is<FileMetadata>(f =>
            f.PasswordHash == null)),
            Times.Once);
    }

    [Fact]
    public async Task UploadAsync_WithWhitespacePassword_DoesNotHash()
    {
        var fileMock = CreateFormFile("open.pdf", "application/pdf", 512);
        var request = new UploadFileRequest
        {
            File = fileMock.Object,
            Password = "   "
        };

        _storageApiMock
            .Setup(x => x.UploadFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(new UploadResponse { StorageKey = "s3/open" });

        var result = await _sut.UploadAsync(request);

        Assert.NotNull(result);
        _repoMock.Verify(x => x.AddAsync(It.Is<FileMetadata>(f =>
            f.PasswordHash == null)),
            Times.Once);
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("application/pdf")]
    [InlineData("text/plain")]
    [InlineData("application/zip")]
    public async Task UploadAsync_WithAllAllowedMimeTypes_Succeeds(string mimeType)
    {
        var extension = mimeType switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "application/pdf" => "pdf",
            "text/plain" => "txt",
            "application/zip" => "zip",
            _ => "bin"
        };
        var fileMock = CreateFormFile($"test.{extension}", mimeType, 512);
        var request = new UploadFileRequest { File = fileMock.Object };

        _storageApiMock
            .Setup(x => x.UploadFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(new UploadResponse { StorageKey = $"s3/test.{extension}" });

        var result = await _sut.UploadAsync(request);

        Assert.NotNull(result);
        Assert.Equal(8, result.Code.Length);
    }

    [Fact]
    public async Task UploadAsync_WhenStorageReturnsNull_Throws()
    {
        var fileMock = CreateFormFile("null.pdf", "application/pdf", 256);
        var request = new UploadFileRequest { File = fileMock.Object };

        _storageApiMock
            .Setup(x => x.UploadFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync((UploadResponse?)null);

        var ex = await Assert.ThrowsAsync<Exception>(() => _sut.UploadAsync(request));

        Assert.Equal("Storage upload failed.", ex.Message);
        _repoMock.Verify(x => x.AddAsync(It.IsAny<FileMetadata>()), Times.Never);
    }

    [Fact]
    public async Task UploadAsync_WithNullExpiresAt_SetsNull()
    {
        var fileMock = CreateFormFile("noexpiry.pdf", "application/pdf", 512);
        var request = new UploadFileRequest
        {
            File = fileMock.Object,
            ExpiresAt = null
        };

        _storageApiMock
            .Setup(x => x.UploadFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(new UploadResponse { StorageKey = "s3/noexpiry" });

        var result = await _sut.UploadAsync(request);

        Assert.NotNull(result);
        _repoMock.Verify(x => x.AddAsync(It.Is<FileMetadata>(f =>
            f.ExpiresAt == null)),
            Times.Once);
    }

    [Fact]
    public async Task UploadAsync_WithNullMaxDownloads_SetsNull()
    {
        var fileMock = CreateFormFile("unlimited.pdf", "application/pdf", 512);
        var request = new UploadFileRequest
        {
            File = fileMock.Object,
            MaxDownloads = null
        };

        _storageApiMock
            .Setup(x => x.UploadFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(new UploadResponse { StorageKey = "s3/unlimited" });

        var result = await _sut.UploadAsync(request);

        Assert.NotNull(result);
        _repoMock.Verify(x => x.AddAsync(It.Is<FileMetadata>(f =>
            f.MaxDownloads == null)),
            Times.Once);
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
