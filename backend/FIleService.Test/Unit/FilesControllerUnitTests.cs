using FileService.Api.Controllers;
using FileService.Api.Dtos;
using FileService.Api.Dtos.UploadFileRequest;
using FileService.Api.Dtos.UploadFileResponse;
using FileService.Api.Entities;
using FileService.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace FIleService.Test.Unit;

public class FilesControllerUnitTests
{
    private readonly Mock<IFileService> _serviceMock;
    private readonly Mock<ILogger<FilesController>> _loggerMock;
    private readonly FilesController _sut;

    public FilesControllerUnitTests()
    {
        _serviceMock = new Mock<IFileService>();
        _loggerMock = new Mock<ILogger<FilesController>>();
        _sut = new FilesController(_serviceMock.Object, _loggerMock.Object);
    }

    // ──────────────────────────────────────────────
    // UPLOAD TESTS
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Upload_WithValidFile_ReturnsOkWithCode()
    {
        var request = new UploadFileRequest
        {
            File = CreateMockFile("test.pdf", "application/pdf", 100).Object
        };
        var expectedResponse = new UploadFileResponse
        {
            Code = "abc12345",
            DownloadUrl = "/files/abc12345"
        };

        _serviceMock
            .Setup(x => x.UploadAsync(It.IsAny<UploadFileRequest>()))
            .ReturnsAsync(expectedResponse);

        var result = await _sut.Upload(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UploadFileResponse>(okResult.Value);
        Assert.Equal("abc12345", response.Code);
        Assert.Equal("/files/abc12345", response.DownloadUrl);
    }

    [Fact]
    public async Task Upload_WhenFileIsRequired_ReturnsBadRequest()
    {
        var request = new UploadFileRequest { File = null! };

        _serviceMock
            .Setup(x => x.UploadAsync(It.IsAny<UploadFileRequest>()))
            .ThrowsAsync(new Exception("File is required."));

        var result = await _sut.Upload(request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(400, problem.Status);
    }

    [Fact]
    public async Task Upload_WhenEmptyFile_ReturnsBadRequest()
    {
        var request = new UploadFileRequest
        {
            File = CreateMockFile("empty.txt", "text/plain", 0).Object
        };

        _serviceMock
            .Setup(x => x.UploadAsync(It.IsAny<UploadFileRequest>()))
            .ThrowsAsync(new Exception("Empty file."));

        var result = await _sut.Upload(request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(400, problem.Status);
    }

    [Fact]
    public async Task Upload_WhenInvalidMimeType_ReturnsBadRequest()
    {
        var request = new UploadFileRequest
        {
            File = CreateMockFile("virus.exe", "application/x-msdownload", 100).Object
        };

        _serviceMock
            .Setup(x => x.UploadAsync(It.IsAny<UploadFileRequest>()))
            .ThrowsAsync(new Exception("Invalid MIME type."));

        var result = await _sut.Upload(request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(400, problem.Status);
        Assert.Contains("Invalid MIME type.", problem.Detail);
    }

    [Fact]
    public async Task Upload_WhenFileExceeds10Mb_Returns413()
    {
        var request = new UploadFileRequest
        {
            File = CreateMockFile("big.zip", "application/zip", 11 * 1024 * 1024).Object
        };

        _serviceMock
            .Setup(x => x.UploadAsync(It.IsAny<UploadFileRequest>()))
            .ThrowsAsync(new Exception("File exceeds 10MB limit."));

        var result = await _sut.Upload(request);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(413, statusResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(statusResult.Value);
        Assert.Contains("File exceeds 10MB limit.", problem.Detail);
    }

    [Fact]
    public async Task Upload_WhenStorageFails_Returns502()
    {
        var request = new UploadFileRequest
        {
            File = CreateMockFile("fail.pdf", "application/pdf", 100).Object
        };

        _serviceMock
            .Setup(x => x.UploadAsync(It.IsAny<UploadFileRequest>()))
            .ThrowsAsync(new Exception("Storage upload failed."));

        var result = await _sut.Upload(request);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(502, objectResult.StatusCode);
    }

    [Fact]
    public async Task Upload_WhenUnhandledError_Returns500()
    {
        var request = new UploadFileRequest
        {
            File = CreateMockFile("boom.pdf", "application/pdf", 100).Object
        };

        _serviceMock
            .Setup(x => x.UploadAsync(It.IsAny<UploadFileRequest>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected database crash"));

        var result = await _sut.Upload(request);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }

    // ──────────────────────────────────────────────
    // DOWNLOAD TESTS
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Download_WithValidCode_ReturnsRedirect()
    {
        var signedUrl = "https://s3.signed/url";
        _serviceMock
            .Setup(x => x.DownloadAsync("abc12345", null))
            .ReturnsAsync(signedUrl);

        var result = await _sut.Download("abc12345", null);

        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal(signedUrl, redirectResult.Url);
    }

    [Fact]
    public async Task Download_WithPassword_ReturnsRedirect()
    {
        var signedUrl = "https://s3.signed/secret";
        _serviceMock
            .Setup(x => x.DownloadAsync("abc12345", "mypassword"))
            .ReturnsAsync(signedUrl);

        var result = await _sut.Download("abc12345", "mypassword");

        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal(signedUrl, redirectResult.Url);
    }

    [Fact]
    public async Task Download_WhenFileNotFound_Returns404()
    {
        _serviceMock
            .Setup(x => x.DownloadAsync("missing", null))
            .ThrowsAsync(new Exception("File not found."));

        var result = await _sut.Download("missing", null);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal(404, problem.Status);
    }

    [Fact]
    public async Task Download_WhenFileExpired_Returns410()
    {
        _serviceMock
            .Setup(x => x.DownloadAsync("expired", null))
            .ThrowsAsync(new Exception("File expired."));

        var result = await _sut.Download("expired", null);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(410, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Contains("File expired.", problem.Detail);
    }

    [Fact]
    public async Task Download_WhenDownloadLimitReached_Returns410()
    {
        _serviceMock
            .Setup(x => x.DownloadAsync("limit99", null))
            .ThrowsAsync(new Exception("Download limit reached."));

        var result = await _sut.Download("limit99", null);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(410, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Contains("Download limit reached.", problem.Detail);
    }

    [Fact]
    public async Task Download_WhenPasswordRequired_Returns401()
    {
        _serviceMock
            .Setup(x => x.DownloadAsync("secret", null))
            .ThrowsAsync(new Exception("Password is required."));

        var result = await _sut.Download("secret", null);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(unauthorizedResult.Value);
        Assert.Equal(401, problem.Status);
    }

    [Fact]
    public async Task Download_WhenInvalidPassword_Returns401()
    {
        _serviceMock
            .Setup(x => x.DownloadAsync("secret", "wrong"))
            .ThrowsAsync(new Exception("Invalid password."));

        var result = await _sut.Download("secret", "wrong");

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(unauthorizedResult.Value);
        Assert.Equal(401, problem.Status);
    }

    [Fact]
    public async Task Download_WhenSignedUrlFails_Returns502()
    {
        _serviceMock
            .Setup(x => x.DownloadAsync("emptyurl", null))
            .ThrowsAsync(new Exception("Failed to generate signed URL."));

        var result = await _sut.Download("emptyurl", null);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(502, objectResult.StatusCode);
    }

    [Fact]
    public async Task Download_WhenUnhandledError_Returns500()
    {
        _serviceMock
            .Setup(x => x.DownloadAsync("boom", null))
            .ThrowsAsync(new InvalidOperationException("Unexpected"));

        var result = await _sut.Download("boom", null);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }

    // ──────────────────────────────────────────────
    // DELETE TESTS
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Delete_WhenFileExists_Returns204()
    {
        _serviceMock
            .Setup(x => x.DeleteAsync("abc12345"))
            .Returns(Task.CompletedTask);

        var result = await _sut.Delete("abc12345");

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WhenFileNotFound_Returns404()
    {
        _serviceMock
            .Setup(x => x.DeleteAsync("missing"))
            .ThrowsAsync(new Exception("File not found."));

        var result = await _sut.Delete("missing");

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal(404, problem.Status);
    }

    [Fact]
    public async Task Delete_WhenStorageFails_Returns502()
    {
        _serviceMock
            .Setup(x => x.DeleteAsync("orphan1"))
            .ThrowsAsync(new Exception("Failed to delete storage object. StatusCode=InternalServerError, Body=..."));

        var result = await _sut.Delete("orphan1");

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(502, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Contains("Failed to delete storage object", problem.Detail);
    }

    [Fact]
    public async Task Delete_WhenUnhandledError_Returns500()
    {
        _serviceMock
            .Setup(x => x.DeleteAsync("boom"))
            .ThrowsAsync(new InvalidOperationException("Unexpected"));

        var result = await _sut.Delete("boom");

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }

    // ──────────────────────────────────────────────
    // GET EXPIRED FILES TESTS
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetExpiredFiles_ReturnsExpiredFiles()
    {
        var files = new List<FileMetadata>
        {
            new() { Code = "exp1", StorageKey = "s3/exp1", OriginalFilename = "e1.pdf",
                MimeType = "application/pdf", SizeBytes = 100, CreatedAt = DateTime.UtcNow },
            new() { Code = "exp2", StorageKey = "s3/exp2", OriginalFilename = "e2.pdf",
                MimeType = "application/pdf", SizeBytes = 200, CreatedAt = DateTime.UtcNow }
        };

        _serviceMock
            .Setup(x => x.GetExpiredFilesAsync())
            .ReturnsAsync(files);

        var result = await _sut.GetExpiredFiles();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IEnumerable<ExpiredFileDto>>(okResult.Value);
        var list = response.ToList();
        Assert.Equal(2, list.Count);
        Assert.Contains(list, f => f.Code == "exp1");
        Assert.Contains(list, f => f.Code == "exp2");
    }

    [Fact]
    public async Task GetExpiredFiles_WhenNoExpired_ReturnsEmptyList()
    {
        _serviceMock
            .Setup(x => x.GetExpiredFilesAsync())
            .ReturnsAsync(new List<FileMetadata>());

        var result = await _sut.GetExpiredFiles();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IEnumerable<ExpiredFileDto>>(okResult.Value);
        Assert.Empty(response);
    }

    [Fact]
    public async Task GetExpiredFiles_WhenUnhandledError_Returns500()
    {
        _serviceMock
            .Setup(x => x.GetExpiredFilesAsync())
            .ThrowsAsync(new InvalidOperationException("Database crash"));

        var result = await _sut.GetExpiredFiles();

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }

    // ──────────────────────────────────────────────
    // GET INFO TESTS
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetInfo_WhenFileExists_ReturnsMetadata()
    {
        var metadata = new FileMetadata
        {
            Code = "abc12345",
            OriginalFilename = "doc.pdf",
            MimeType = "application/pdf",
            SizeBytes = 1024,
            PasswordHash = null,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        _serviceMock
            .Setup(x => x.GetMetadataAsync("abc12345"))
            .ReturnsAsync(metadata);

        var result = await _sut.GetInfo("abc12345");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<FileMetadataResponse>(okResult.Value);
        Assert.Equal("abc12345", response.Code);
        Assert.Equal("doc.pdf", response.OriginalFilename);
        Assert.Equal("application/pdf", response.MimeType);
        Assert.Equal(1024, response.SizeBytes);
        Assert.False(response.RequiresPassword);
    }

    [Fact]
    public async Task GetInfo_WhenFileHasPassword_RequiresPasswordTrue()
    {
        var metadata = new FileMetadata
        {
            Code = "pw12345",
            OriginalFilename = "secret.pdf",
            MimeType = "application/pdf",
            SizeBytes = 512,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("secret"),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        _serviceMock
            .Setup(x => x.GetMetadataAsync("pw12345"))
            .ReturnsAsync(metadata);

        var result = await _sut.GetInfo("pw12345");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<FileMetadataResponse>(okResult.Value);
        Assert.True(response.RequiresPassword);
    }

    [Fact]
    public async Task GetInfo_WhenFileNotFound_Returns404()
    {
        _serviceMock
            .Setup(x => x.GetMetadataAsync("missing"))
            .ReturnsAsync((FileMetadata?)null);

        var result = await _sut.GetInfo("missing");

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal(404, problem.Status);
    }

    [Fact]
    public async Task GetInfo_WhenUnhandledError_Returns500()
    {
        _serviceMock
            .Setup(x => x.GetMetadataAsync("boom"))
            .ThrowsAsync(new InvalidOperationException("Unexpected"));

        var result = await _sut.GetInfo("boom");

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }

    // ──────────────────────────────────────────────
    // VERIFY PASSWORD TESTS
    // ──────────────────────────────────────────────

    [Fact]
    public async Task VerifyPassword_WithCorrectPassword_ReturnsValidTrue()
    {
        _serviceMock
            .Setup(x => x.VerifyPasswordOnlyAsync("pwfile", "correct"))
            .ReturnsAsync(true);

        var result = await _sut.VerifyPassword("pwfile",
            new VerifyPasswordRequest { Password = "correct" });

        var okResult = Assert.IsType<OkObjectResult>(result);
        // Anonymous object { valid = true } — check via dynamic or reflection
        var value = okResult.Value;
        var validProperty = value!.GetType().GetProperty("valid");
        Assert.NotNull(validProperty);
        Assert.True((bool)validProperty.GetValue(value)!);
    }

    [Fact]
    public async Task VerifyPassword_WithWrongPassword_ReturnsValidFalse()
    {
        _serviceMock
            .Setup(x => x.VerifyPasswordOnlyAsync("pwfile", "wrong"))
            .ReturnsAsync(false);

        var result = await _sut.VerifyPassword("pwfile",
            new VerifyPasswordRequest { Password = "wrong" });

        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        var validProperty = value!.GetType().GetProperty("valid");
        Assert.NotNull(validProperty);
        Assert.False((bool)validProperty.GetValue(value)!);
    }

    [Fact]
    public async Task VerifyPassword_WhenFileNotFound_Returns404()
    {
        _serviceMock
            .Setup(x => x.VerifyPasswordOnlyAsync("missing", It.IsAny<string>()))
            .ThrowsAsync(new Exception("File not found."));

        var result = await _sut.VerifyPassword("missing",
            new VerifyPasswordRequest { Password = "p" });

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal(404, problem.Status);
    }

    [Fact]
    public async Task VerifyPassword_WhenInvalidPassword_Returns401()
    {
        _serviceMock
            .Setup(x => x.VerifyPasswordOnlyAsync("pwfile", "bad"))
            .ThrowsAsync(new Exception("Invalid password."));

        var result = await _sut.VerifyPassword("pwfile",
            new VerifyPasswordRequest { Password = "bad" });

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(unauthorizedResult.Value);
        Assert.Equal(401, problem.Status);
    }

    [Fact]
    public async Task VerifyPassword_WhenPasswordRequired_Returns401()
    {
        _serviceMock
            .Setup(x => x.VerifyPasswordOnlyAsync("pwfile", ""))
            .ThrowsAsync(new Exception("Password is required."));

        var result = await _sut.VerifyPassword("pwfile",
            new VerifyPasswordRequest { Password = "" });

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(unauthorizedResult.Value);
        Assert.Equal(401, problem.Status);
    }

    [Fact]
    public async Task VerifyPassword_WhenUnhandledError_Returns500()
    {
        _serviceMock
            .Setup(x => x.VerifyPasswordOnlyAsync("boom", It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected"));

        var result = await _sut.VerifyPassword("boom",
            new VerifyPasswordRequest { Password = "p" });

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }

    // ──────────────────────────────────────────────
    // HELPER
    // ──────────────────────────────────────────────

    private static Mock<IFormFile> CreateMockFile(
        string fileName, string contentType, long length)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.ContentType).Returns(contentType);
        mock.Setup(f => f.Length).Returns(length);
        mock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
        return mock;
    }

    // ──────────────────────────────────────────────
    // REMAINING WELL-KNOWN MESSAGE COVERAGE
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Upload_WhenStorageReturnsEmptyResponse_Returns502()
    {
        var request = new UploadFileRequest
        {
            File = CreateMockFile("empty.pdf", "application/pdf", 100).Object
        };

        _serviceMock
            .Setup(x => x.UploadAsync(It.IsAny<UploadFileRequest>()))
            .ThrowsAsync(new Exception("Storage service returned an empty upload response."));

        var result = await _sut.Upload(request);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(502, objectResult.StatusCode);
    }

    [Fact]
    public async Task Download_WhenStorageReturnsEmptySignedUrl_Returns502()
    {
        _serviceMock
            .Setup(x => x.DownloadAsync("emptyurl", null))
            .ThrowsAsync(new Exception("Storage service returned an empty signed url response."));

        var result = await _sut.Download("emptyurl", null);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(502, objectResult.StatusCode);
    }
}
