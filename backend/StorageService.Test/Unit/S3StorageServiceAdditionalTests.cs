using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Moq;
using StorageService.Api.Models;
using StorageService.Api.Services;
using System.Net;

namespace StorageService.Test.Unit;

/// <summary>
/// Additional unit tests for S3StorageService covering edge cases
/// not covered by the existing S3StorageServiceTests.
/// </summary>
public class S3StorageServiceAdditionalTests
{
    private readonly Mock<IAmazonS3> _mockS3;
    private readonly AwsSettings _awsSettings;
    private readonly S3StorageService _sut;

    public S3StorageServiceAdditionalTests()
    {
        _mockS3 = new Mock<IAmazonS3>();
        _awsSettings = new AwsSettings
        {
            BucketName = "test-bucket",
            Region = "ap-southeast-1",
            AccessKey = "fake-key",
            SecretKey = "fake-secret"
        };
        _sut = new S3StorageService(_mockS3.Object,
            Microsoft.Extensions.Options.Options.Create(_awsSettings));
    }

    // ──────────────────────────────────────────────
    // UPLOAD ADDITIONAL TESTS
    // ──────────────────────────────────────────────

    [Fact]
    public async Task UploadAsync_WithPngFile_UploadsAndReturnsStorageKey()
    {
        var formFile = CreateFormFile("screenshot.png", "fake png data", "image/png");
        _mockS3
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), default))
            .ReturnsAsync(new PutObjectResponse());

        var result = await _sut.UploadAsync(formFile);

        Assert.NotNull(result);
        Assert.StartsWith("uploads/", result.StorageKey);
        Assert.EndsWith(".png", result.StorageKey);
    }

    [Fact]
    public async Task UploadAsync_WithJpegFile_UploadsAndReturnsStorageKey()
    {
        var formFile = CreateFormFile("photo.jpg", "fake jpg data", "image/jpeg");
        _mockS3
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), default))
            .ReturnsAsync(new PutObjectResponse());

        var result = await _sut.UploadAsync(formFile);

        Assert.NotNull(result);
        Assert.EndsWith(".jpg", result.StorageKey);
    }

    [Fact]
    public async Task UploadAsync_WithNoExtension_UploadsWithoutExtension()
    {
        var formFile = CreateFormFile("noextension", "data", "application/octet-stream");
        _mockS3
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), default))
            .ReturnsAsync(new PutObjectResponse());

        var result = await _sut.UploadAsync(formFile);

        Assert.NotNull(result);
        Assert.StartsWith("uploads/", result.StorageKey);
        // File with no extension: storage key ends with GUID (no extension appended)
        Assert.DoesNotContain(".", result.StorageKey.Split('/').Last());
    }

    [Fact]
    public async Task UploadAsync_WithTransientS3Error_ThrowsStorageOperationExceptionWith503()
    {
        var formFile = CreateFormFile("transient.pdf", "data", "application/pdf");
        _mockS3
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), default))
            .ThrowsAsync(new AmazonS3Exception("Service unavailable")
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                ErrorCode = "ServiceUnavailable"
            });

        var exception = await Assert.ThrowsAsync<StorageOperationException>(
            () => _sut.UploadAsync(formFile));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
        Assert.Contains("Storage provider is temporarily unavailable", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_WithSlowDownError_Throws503()
    {
        var formFile = CreateFormFile("slow.pdf", "data", "application/pdf");
        _mockS3
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), default))
            .ThrowsAsync(new AmazonS3Exception("Slow down")
            {
                StatusCode = HttpStatusCode.OK, // Not a standard status, error code decides
                ErrorCode = "SlowDown"
            });

        var exception = await Assert.ThrowsAsync<StorageOperationException>(
            () => _sut.UploadAsync(formFile));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
    }

    [Fact]
    public async Task UploadAsync_WithRequestTimeoutError_Throws503()
    {
        var formFile = CreateFormFile("timeout.pdf", "data", "application/pdf");
        _mockS3
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), default))
            .ThrowsAsync(new AmazonS3Exception("Request timeout")
            {
                StatusCode = HttpStatusCode.RequestTimeout,
                ErrorCode = "RequestTimeout"
            });

        var exception = await Assert.ThrowsAsync<StorageOperationException>(
            () => _sut.UploadAsync(formFile));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
    }

    [Fact]
    public async Task UploadAsync_WithGatewayTimeoutError_Throws503()
    {
        var formFile = CreateFormFile("gtimeout.pdf", "data", "application/pdf");
        _mockS3
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), default))
            .ThrowsAsync(new AmazonS3Exception("Gateway timeout")
            {
                StatusCode = HttpStatusCode.GatewayTimeout,
                ErrorCode = "GatewayTimeout"
            });

        var exception = await Assert.ThrowsAsync<StorageOperationException>(
            () => _sut.UploadAsync(formFile));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
    }

    [Fact]
    public async Task UploadAsync_WithBadGatewayError_Throws503()
    {
        var formFile = CreateFormFile("bg.pdf", "data", "application/pdf");
        _mockS3
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), default))
            .ThrowsAsync(new AmazonS3Exception("Bad gateway")
            {
                StatusCode = HttpStatusCode.BadGateway,
                ErrorCode = "BadGateway"
            });

        var exception = await Assert.ThrowsAsync<StorageOperationException>(
            () => _sut.UploadAsync(formFile));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
    }

    // ──────────────────────────────────────────────
    // GENERATE SIGNED URL ADDITIONAL TESTS
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GenerateSignedUrlAsync_WithFileName_SetsContentDisposition()
    {
        var testKey = "uploads/2026/06/30/doc.pdf";
        GetPreSignedUrlRequest? capturedRequest = null;

        _mockS3
            .Setup(x => x.GetObjectMetadataAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(new GetObjectMetadataResponse());
        _mockS3
            .Setup(x => x.GetPreSignedURL(It.IsAny<GetPreSignedUrlRequest>()))
            .Callback<GetPreSignedUrlRequest>(r => capturedRequest = r)
            .Returns("https://signed.url/with-filename");

        var result = await _sut.GenerateSignedUrlAsync(testKey, "my-document.pdf");

        Assert.Equal("https://signed.url/with-filename", result);
        Assert.NotNull(capturedRequest);
        Assert.NotNull(capturedRequest.ResponseHeaderOverrides);
        Assert.Contains("my-document.pdf",
            capturedRequest.ResponseHeaderOverrides.ContentDisposition);
    }

    [Fact]
    public async Task GenerateSignedUrlAsync_WithoutFileName_SetsAttachmentDisposition()
    {
        var testKey = "uploads/2026/06/30/doc.pdf";
        GetPreSignedUrlRequest? capturedRequest = null;

        _mockS3
            .Setup(x => x.GetObjectMetadataAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(new GetObjectMetadataResponse());
        _mockS3
            .Setup(x => x.GetPreSignedURL(It.IsAny<GetPreSignedUrlRequest>()))
            .Callback<GetPreSignedUrlRequest>(r => capturedRequest = r)
            .Returns("https://signed.url/no-filename");

        var result = await _sut.GenerateSignedUrlAsync(testKey);

        Assert.Equal("https://signed.url/no-filename", result);
        Assert.NotNull(capturedRequest);
        Assert.NotNull(capturedRequest.ResponseHeaderOverrides);
        Assert.Equal("attachment", capturedRequest.ResponseHeaderOverrides.ContentDisposition);
    }

    [Fact]
    public async Task GenerateSignedUrlAsync_WithEmptyStorageKey_ThrowsArgumentException()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.GenerateSignedUrlAsync(""));

        Assert.Contains("StorageKey cannot be empty", exception.Message);
    }

    [Fact]
    public async Task GenerateSignedUrlAsync_WithWhitespaceStorageKey_ThrowsArgumentException()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.GenerateSignedUrlAsync("   "));

        Assert.Contains("StorageKey cannot be empty", exception.Message);
    }

    [Fact]
    public async Task GenerateSignedUrlAsync_WithNoSuchKeyError_ThrowsFileNotFoundException()
    {
        var testKey = "uploads/missing.pdf";
        _mockS3
            .Setup(x => x.GetObjectMetadataAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ThrowsAsync(new AmazonS3Exception("No such key")
            {
                StatusCode = HttpStatusCode.NotFound,
                ErrorCode = "NoSuchKey"
            });

        var exception = await Assert.ThrowsAsync<FileNotFoundException>(
            () => _sut.GenerateSignedUrlAsync(testKey));

        Assert.Contains(testKey, exception.Message);
    }

    [Fact]
    public async Task GenerateSignedUrlAsync_WithNotFoundErrorCode_ThrowsFileNotFoundException()
    {
        var testKey = "uploads/missing2.pdf";
        _mockS3
            .Setup(x => x.GetObjectMetadataAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ThrowsAsync(new AmazonS3Exception("Not found")
            {
                ErrorCode = "NotFound"
            });

        var exception = await Assert.ThrowsAsync<FileNotFoundException>(
            () => _sut.GenerateSignedUrlAsync(testKey));

        Assert.Contains(testKey, exception.Message);
    }

    // ──────────────────────────────────────────────
    // DELETE ADDITIONAL TESTS
    // ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WithMissingObject_SilentlySucceeds()
    {
        var testKey = "uploads/missing.pdf";
        _mockS3
            .Setup(x => x.GetObjectMetadataAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ThrowsAsync(new AmazonS3Exception("Not found")
            {
                StatusCode = HttpStatusCode.NotFound,
                ErrorCode = "NoSuchKey"
            });

        // Should not throw - delete silently returns when file not found
        await _sut.DeleteAsync(testKey);

        _mockS3.Verify(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithMissingObjectNoSuchKey_SilentlySucceeds()
    {
        var testKey = "uploads/nosuchkey.pdf";
        _mockS3
            .Setup(x => x.GetObjectMetadataAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ThrowsAsync(new AmazonS3Exception("No such key")
            {
                ErrorCode = "NoSuchKey"
            });

        await _sut.DeleteAsync(testKey);

        _mockS3.Verify(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenGetMetadataFailsWithTransient_ThrowsStorageOperationException()
    {
        var testKey = "uploads/transient-del.pdf";
        _mockS3
            .Setup(x => x.GetObjectMetadataAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ThrowsAsync(new AmazonS3Exception("Service unavailable")
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                ErrorCode = "ServiceUnavailable"
            });

        var exception = await Assert.ThrowsAsync<StorageOperationException>(
            () => _sut.DeleteAsync(testKey));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
        Assert.Contains("Failed to delete file", exception.Message);
    }

    // ──────────────────────────────────────────────
    // EXISTS ADDITIONAL TESTS
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ExistsAsync_WithNoSuchKeyErrorCode_ReturnsFalse()
    {
        var testKey = "uploads/nosuchkey.pdf";
        _mockS3
            .Setup(x => x.GetObjectMetadataAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ThrowsAsync(new AmazonS3Exception("No such key")
            {
                ErrorCode = "NoSuchKey"
            });

        var result = await _sut.ExistsAsync(testKey);

        Assert.False(result);
    }

    [Fact]
    public async Task ExistsAsync_WithNotFoundErrorCode_ReturnsFalse()
    {
        var testKey = "uploads/notfound.pdf";
        _mockS3
            .Setup(x => x.GetObjectMetadataAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ThrowsAsync(new AmazonS3Exception("Not Found")
            {
                ErrorCode = "NotFound"
            });

        var result = await _sut.ExistsAsync(testKey);

        Assert.False(result);
    }

    [Fact]
    public async Task ExistsAsync_WithOtherS3Error_Throws()
    {
        var testKey = "uploads/error.pdf";
        _mockS3
            .Setup(x => x.GetObjectMetadataAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ThrowsAsync(new AmazonS3Exception("Internal error")
            {
                StatusCode = HttpStatusCode.InternalServerError,
                ErrorCode = "InternalError"
            });

        // ExistsAsync does NOT catch non-NotFound errors — they propagate
        await Assert.ThrowsAsync<AmazonS3Exception>(
            () => _sut.ExistsAsync(testKey));
    }

    // ──────────────────────────────────────────────
    // STORAGE OPERATION EXCEPTION TESTS
    // ──────────────────────────────────────────────

    [Fact]
    public void StorageOperationException_Constructor_SetsProperties()
    {
        var inner = new Exception("inner");
        var ex = new StorageOperationException(
            HttpStatusCode.BadGateway, "test message", inner);

        Assert.Equal(HttpStatusCode.BadGateway, ex.StatusCode);
        Assert.Equal("test message", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void StorageOperationException_WithoutInnerException_Works()
    {
        var ex = new StorageOperationException(
            HttpStatusCode.ServiceUnavailable, "service down");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, ex.StatusCode);
        Assert.Equal("service down", ex.Message);
        Assert.Null(ex.InnerException);
    }

    // ──────────────────────────────────────────────
    // HELPER
    // ──────────────────────────────────────────────

    private static IFormFile CreateFormFile(
        string fileName, string content, string contentType)
    {
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        return new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}
