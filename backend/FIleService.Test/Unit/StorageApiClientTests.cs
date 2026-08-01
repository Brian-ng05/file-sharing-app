using FileService.Api.Dtos;
using FileService.Api.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using StorageService.Api.Dtos;
using System.Net;
using System.Net.Http.Json;
using FIleService.Test.Helpers;

namespace FIleService.Test.Unit;

public class StorageApiClientTests
{
    private const string BaseAddress = "http://localhost:5282/";

    [Fact]
    public async Task UploadFileAsync_SendsMultipartPostToApiObjects()
    {
        // Arrange
        HttpMethod? capturedMethod = null;
        string? capturedPath = null;
        bool isMultipart = false;

        var handler = new FakeHttpMessageHandler(async request =>
        {
            capturedMethod = request.Method;
            capturedPath = request.RequestUri!.AbsolutePath;
            isMultipart = request.Content is MultipartFormDataContent;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new UploadResponse
                { StorageKey = "s3/test-key" })
            };
        });

        var client = new StorageApiClient(new HttpClient(handler)
        { BaseAddress = new Uri(BaseAddress) });

        var formFile = CreateMockFormFile("test.png", "image/png", 256);

        // Act
        var result = await client.UploadFileAsync(formFile.Object);

        // Assert
        Assert.Equal(HttpMethod.Post, capturedMethod);
        Assert.Equal("/api/objects", capturedPath);
        Assert.True(isMultipart);
        Assert.NotNull(result);
        Assert.Equal("s3/test-key", result.StorageKey);
    }

    [Fact]
    public async Task UploadFileAsync_WhenResponseIsSuccessful_ReturnsUploadResponse()
    {
        var handler = new FakeHttpMessageHandler(_ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new UploadResponse
                { StorageKey = "s3/happy-path" })
            }));

        var client = new StorageApiClient(new HttpClient(handler)
        { BaseAddress = new Uri(BaseAddress) });

        var formFile = CreateMockFormFile("doc.pdf", "application/pdf", 100);

        var result = await client.UploadFileAsync(formFile.Object);

        Assert.NotNull(result);
        Assert.Equal("s3/happy-path", result.StorageKey);
    }

    [Fact]
    public async Task UploadFileAsync_WhenServerReturnsError_Throws()
    {
        var handler = new FakeHttpMessageHandler(_ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        var client = new StorageApiClient(new HttpClient(handler)
        { BaseAddress = new Uri(BaseAddress) });

        var formFile = CreateMockFormFile("fail.pdf", "application/pdf", 50);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.UploadFileAsync(formFile.Object));
    }

    [Fact]
    public async Task GetSignedUrlAsync_EncodesStorageKeyInQueryString()
    {
        // Arrange
        const string storageKey = "uploads/2026/06/30/test file.png";
        string? capturedQuery = null;

        var handler = new FakeHttpMessageHandler(request =>
        {
            capturedQuery = request.RequestUri!.Query;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new SignedUrlResponse
                { Url = "https://signed.url" })
            });
        });

        var client = new StorageApiClient(new HttpClient(handler)
        { BaseAddress = new Uri(BaseAddress) });

        // Act
        var result = await client.GetSignedUrlAsync(storageKey);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("storageKey=", capturedQuery);
        // Space in filename should be encoded
        Assert.Contains("test%20file.png", capturedQuery);
    }

    [Fact]
    public async Task GetSignedUrlAsync_WithFileName_AppendsFileNameParam()
    {
        const string storageKey = "uploads/doc.pdf";
        const string fileName = "my-renamed-file.pdf";
        string? capturedQuery = null;

        var handler = new FakeHttpMessageHandler(request =>
        {
            capturedQuery = request.RequestUri!.Query;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new SignedUrlResponse
                { Url = "https://signed.url" })
            });
        });

        var client = new StorageApiClient(new HttpClient(handler)
        { BaseAddress = new Uri(BaseAddress) });

        var result = await client.GetSignedUrlAsync(storageKey, fileName);

        Assert.NotNull(result);
        Assert.Contains("fileName=my-renamed-file.pdf", capturedQuery);
    }

    [Fact]
    public async Task GetSignedUrlAsync_WhenResponseBodyIsEmpty_Throws()
    {
        var handler = new FakeHttpMessageHandler(_ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null")
            }));

        var client = new StorageApiClient(new HttpClient(handler)
        { BaseAddress = new Uri(BaseAddress) });

        var ex = await Assert.ThrowsAsync<Exception>(
            () => client.GetSignedUrlAsync("some-key"));

        Assert.Contains("empty signed url response", ex.Message);
    }

    [Fact]
    public async Task GetSignedUrlAsync_WhenServerReturnsError_Throws()
    {
        var handler = new FakeHttpMessageHandler(_ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        var client = new StorageApiClient(new HttpClient(handler)
        { BaseAddress = new Uri(BaseAddress) });

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetSignedUrlAsync("some-key"));
    }

    [Fact]
    public async Task DeleteFileAsync_WithNestedStorageKey_PreservesSlashSegments()
    {
        // Arrange
        const string storageKey = "uploads/2026/06/30/abc.png";
        string? capturedPath = null;

        var handler = new FakeHttpMessageHandler(request =>
        {
            capturedPath = request.RequestUri!.AbsolutePath;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });

        var client = new StorageApiClient(new HttpClient(handler)
        { BaseAddress = new Uri(BaseAddress) });

        // Act
        await client.DeleteFileAsync(storageKey);

        // Assert: segments separated by slashes, not one encoded giant segment
        Assert.Equal("/api/objects/uploads/2026/06/30/abc.png", capturedPath);
    }

    [Fact]
    public async Task DeleteFileAsync_WithSpaceInFilename_EncodesFilenameSegment()
    {
        // Arrange
        const string storageKey = "uploads/2026/report final.pdf";
        string? capturedPath = null;

        var handler = new FakeHttpMessageHandler(request =>
        {
            capturedPath = request.RequestUri!.AbsolutePath;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });

        var client = new StorageApiClient(new HttpClient(handler)
        { BaseAddress = new Uri(BaseAddress) });

        // Act
        await client.DeleteFileAsync(storageKey);

        // Assert: filename segment is encoded, slashes preserved
        Assert.Equal("/api/objects/uploads/2026/report%20final.pdf", capturedPath);
    }

    [Fact]
    public async Task DeleteFileAsync_WhenStorageReturns404_ThrowsWithUsefulInformation()
    {
        var handler = new FakeHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Delete, request.Method);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("Object not found")
            });
        });

        var client = new StorageApiClient(new HttpClient(handler)
        { BaseAddress = new Uri(BaseAddress) });

        var ex = await Assert.ThrowsAsync<Exception>(
            () => client.DeleteFileAsync("uploads/nonexistent.pdf"));

        Assert.Contains("Failed to delete storage object", ex.Message);
        Assert.Contains("NotFound", ex.Message);
        Assert.Contains("Object not found", ex.Message);
    }

    [Fact]
    public async Task DeleteFileAsync_WhenStorageReturns204_CompletesSuccessfully()
    {
        var handler = new FakeHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Delete, request.Method);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });

        var client = new StorageApiClient(new HttpClient(handler)
        { BaseAddress = new Uri(BaseAddress) });

        // Should not throw
        await client.DeleteFileAsync("uploads/good.pdf");
    }

    [Fact]
    public async Task DeleteFileAsync_WhenStorageReturnsError_ThrowsWithUsefulInformation()
    {
        var handler = new FakeHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Delete, request.Method);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("S3 bucket not accessible")
            });
        });

        var client = new StorageApiClient(new HttpClient(handler)
        { BaseAddress = new Uri(BaseAddress) });

        var ex = await Assert.ThrowsAsync<Exception>(
            () => client.DeleteFileAsync("uploads/bad.pdf"));

        Assert.Contains("Failed to delete storage object", ex.Message);
        Assert.Contains("InternalServerError", ex.Message);
        Assert.Contains("S3 bucket not accessible", ex.Message);
    }

    // ──────────────────────────────────────────────
    // HELPER
    // ──────────────────────────────────────────────

    private static Mock<IFormFile> CreateMockFormFile(
        string fileName, string contentType, long length)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.ContentType).Returns(contentType);
        mock.Setup(f => f.Length).Returns(length);
        mock.Setup(f => f.OpenReadStream())
            .Returns(new MemoryStream(new byte[length]));
        return mock;
    }
}
