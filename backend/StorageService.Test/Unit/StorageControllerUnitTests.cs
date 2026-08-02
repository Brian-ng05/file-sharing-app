using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using StorageService.Api.Controllers;
using StorageService.Api.DTOs;
using StorageService.Api.Services;
using System.Net;

namespace StorageService.Test.Unit;

/// <summary>
/// Unit tests for StorageController with mocked IStorageService.
/// </summary>
public class StorageControllerUnitTests
{
    private readonly Mock<IStorageService> _storageMock;
    private readonly Mock<ILogger<StorageController>> _loggerMock;
    private readonly StorageController _sut;

    public StorageControllerUnitTests()
    {
        _storageMock = new Mock<IStorageService>();
        _loggerMock = new Mock<ILogger<StorageController>>();
        _sut = new StorageController(_storageMock.Object, _loggerMock.Object);

        // Set up ControllerContext so Request is available for logging
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    // ──────────────────────────────────────────────
    // UPLOAD TESTS
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Upload_WithValidFile_ReturnsOkWithStorageKey()
    {
        var request = new UploadRequest
        {
            File = CreateFormFile("test.pdf", "hello", "application/pdf")
        };
        var expectedKey = "uploads/2026/08/01/test-guid.pdf";

        _storageMock
            .Setup(x => x.UploadAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(new UploadResponse { StorageKey = expectedKey });

        var result = await _sut.Upload(request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<UploadResponse>(okResult.Value);
        Assert.Equal(expectedKey, response.StorageKey);
    }

    [Fact]
    public async Task Upload_WithNoFile_ReturnsBadRequest()
    {
        var request = new UploadRequest { File = null! };

        var result = await _sut.Upload(request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("No file uploaded", badRequestResult.Value);
    }

    [Fact]
    public async Task Upload_WithEmptyFile_ReturnsBadRequest()
    {
        var request = new UploadRequest
        {
            File = CreateFormFile("empty.txt", "", "text/plain", 0)
        };

        var result = await _sut.Upload(request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("No file uploaded", badRequestResult.Value);
    }

    [Fact]
    public async Task Upload_WithFileOver10Mb_Returns413()
    {
        var request = new UploadRequest
        {
            File = CreateFormFile("big.zip", new string('x', 11 * 1024 * 1024), "application/zip")
        };

        var result = await _sut.Upload(request);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(413, statusResult.StatusCode);
        Assert.Contains("File size exceeds 10 MB limit", statusResult.Value!.ToString());
    }

    [Fact]
    public async Task Upload_WhenStorageOperationException_ReturnsStorageProblem()
    {
        var request = new UploadRequest
        {
            File = CreateFormFile("fail.pdf", "data", "application/pdf")
        };

        _storageMock
            .Setup(x => x.UploadAsync(It.IsAny<IFormFile>()))
            .ThrowsAsync(new StorageOperationException(
                HttpStatusCode.BadGateway,
                "Storage provider rejected the request."));

        var result = await _sut.Upload(request);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(502, objectResult.StatusCode);
    }

    [Fact]
    public async Task Upload_WhenStorageUnavailable_Returns503()
    {
        var request = new UploadRequest
        {
            File = CreateFormFile("fail.pdf", "data", "application/pdf")
        };

        _storageMock
            .Setup(x => x.UploadAsync(It.IsAny<IFormFile>()))
            .ThrowsAsync(new StorageOperationException(
                HttpStatusCode.ServiceUnavailable,
                "Storage provider is temporarily unavailable."));

        var result = await _sut.Upload(request);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(503, objectResult.StatusCode);
    }

    // ──────────────────────────────────────────────
    // GET SIGNED URL TESTS
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetSignedUrl_WithValidKey_ReturnsOkWithUrl()
    {
        var signedUrl = "https://s3.signed/url";
        _storageMock
            .Setup(x => x.GenerateSignedUrlAsync("uploads/doc.pdf", null))
            .ReturnsAsync(signedUrl);

        var result = await _sut.GetSignedUrl("uploads/doc.pdf");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SignedUrlResponse>(okResult.Value);
        Assert.Equal(signedUrl, response.Url);
    }

    [Fact]
    public async Task GetSignedUrl_WithFileName_PassesFileName()
    {
        _storageMock
            .Setup(x => x.GenerateSignedUrlAsync("uploads/doc.pdf", "my-doc.pdf"))
            .ReturnsAsync("https://signed.url");

        var result = await _sut.GetSignedUrl("uploads/doc.pdf", "my-doc.pdf");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _storageMock.Verify(
            x => x.GenerateSignedUrlAsync("uploads/doc.pdf", "my-doc.pdf"),
            Times.Once);
    }

    [Fact]
    public async Task GetSignedUrl_WithEmptyStorageKey_ReturnsBadRequest()
    {
        var result = await _sut.GetSignedUrl("");

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("StorageKey is required", badRequestResult.Value);
    }

    [Fact]
    public async Task GetSignedUrl_WithWhitespaceStorageKey_ReturnsBadRequest()
    {
        var result = await _sut.GetSignedUrl("   ");

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("StorageKey is required", badRequestResult.Value);
    }

    [Fact]
    public async Task GetSignedUrl_WhenFileNotFound_Returns404()
    {
        _storageMock
            .Setup(x => x.GenerateSignedUrlAsync("missing.pdf", null))
            .ThrowsAsync(new FileNotFoundException("not found", "missing.pdf"));

        var result = await _sut.GetSignedUrl("missing.pdf");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetSignedUrl_WhenStorageOperationException_ReturnsStorageProblem()
    {
        _storageMock
            .Setup(x => x.GenerateSignedUrlAsync("error.pdf", null))
            .ThrowsAsync(new StorageOperationException(
                HttpStatusCode.BadGateway,
                "Storage provider rejected"));

        var result = await _sut.GetSignedUrl("error.pdf");

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(502, objectResult.StatusCode);
    }

    // ──────────────────────────────────────────────
    // DELETE TESTS
    // ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteObject_WithValidKey_Returns204()
    {
        _storageMock
            .Setup(x => x.DeleteAsync("uploads/doc.pdf"))
            .Returns(Task.CompletedTask);

        var result = await _sut.DeleteObject("uploads/doc.pdf");

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteObject_WithNestedKey_Returns204()
    {
        _storageMock
            .Setup(x => x.DeleteAsync("uploads/2026/08/01/nested.pdf"))
            .Returns(Task.CompletedTask);

        var result = await _sut.DeleteObject("uploads/2026/08/01/nested.pdf");

        Assert.IsType<NoContentResult>(result);
        _storageMock.Verify(
            x => x.DeleteAsync("uploads/2026/08/01/nested.pdf"),
            Times.Once);
    }

    [Fact]
    public async Task DeleteObject_WithEmptyStorageKey_ReturnsBadRequest()
    {
        var result = await _sut.DeleteObject("");

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("StorageKey is required", badRequestResult.Value!.ToString());
    }

    [Fact]
    public async Task DeleteObject_WhenStorageOperationException_ReturnsStorageProblem()
    {
        _storageMock
            .Setup(x => x.DeleteAsync("error.pdf"))
            .ThrowsAsync(new StorageOperationException(
                HttpStatusCode.BadGateway,
                "Storage provider rejected"));

        var result = await _sut.DeleteObject("error.pdf");

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(502, objectResult.StatusCode);
    }

    [Fact]
    public async Task DeleteObject_WhenStorageUnavailable_Returns503()
    {
        _storageMock
            .Setup(x => x.DeleteAsync("transient.pdf"))
            .ThrowsAsync(new StorageOperationException(
                HttpStatusCode.ServiceUnavailable,
                "Storage provider is temporarily unavailable."));

        var result = await _sut.DeleteObject("transient.pdf");

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, objectResult.StatusCode);
    }

    // ──────────────────────────────────────────────
    // HELPER
    // ──────────────────────────────────────────────

    private static IFormFile CreateFormFile(
        string fileName, string content, string contentType, int? lengthOverride = null)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0,
            lengthOverride ?? stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}
