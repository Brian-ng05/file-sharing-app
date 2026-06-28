using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using StorageService.Api;
using StorageService.Api.DTOs;
using StorageService.Api.Services;
using StorageService.Test.Helpers;
using System.Net;
using System.Net.Http.Json;

namespace StorageService.Test.Integration;

public class StorageApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public StorageApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IStorageService, FakeStorageService>();
            });
        });
    }

    // Upload tests
    [Fact]
    public async Task Upload_WithValidFile_ReturnsOkAndStorageKey()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("test file content"));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("multipart/form-data");
        content.Add(fileContent, "file", "test.pdf");

        // Act
        var response = await client.PostAsync("/api/objects", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<UploadResponse>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.StorageKey));
        Assert.StartsWith("uploads/", result.StorageKey);
    }

    [Fact]
    public async Task Upload_WithNoFile_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var content = new MultipartFormDataContent();

        // Act
        var response = await client.PostAsync("/api/objects", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Signed URL tests
    [Fact]
    public async Task GetSignedUrl_WithExistingObject_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        var fakeService = _factory.Services.GetRequiredService<IStorageService>() as FakeStorageService;
        var testKey = "uploads/2026/06/28/test.pdf";
        fakeService!.AddObject(testKey);

        // Act
        var response = await client.GetAsync($"/api/objects/signed-url/{testKey}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SignedUrlResponse>();
        Assert.NotNull(result);
        Assert.Contains(testKey, result.Url);
    }

    [Fact]
    public async Task GetSignedUrl_WithMissingObject_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        var testKey = "uploads/2026/06/28/missing.pdf";

        // Act
        var response = await client.GetAsync($"/api/objects/signed-url/{testKey}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSignedUrl_WithEmptyStorageKey_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/objects/signed-url/");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Delete tests
    [Fact]
    public async Task Delete_WithValidKey_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.CreateClient();
        var fakeService = _factory.Services.GetRequiredService<IStorageService>() as FakeStorageService;
        var testKey = "uploads/2026/06/28/to-delete.pdf";
        fakeService!.AddObject(testKey);

        // Act
        var response = await client.DeleteAsync($"/api/objects/{testKey}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteThenGetSignedUrl_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        var fakeService = _factory.Services.GetRequiredService<IStorageService>() as FakeStorageService;
        var testKey = "uploads/2026/06/28/delete-then-test.pdf";
        fakeService!.AddObject(testKey);

        // Act 1 - Delete
        var deleteResponse = await client.DeleteAsync($"/api/objects/{testKey}");
        deleteResponse.EnsureSuccessStatusCode();

        // Act 2 - Try to get signed URL
        var signedUrlResponse = await client.GetAsync($"/api/objects/signed-url/{testKey}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, signedUrlResponse.StatusCode);
    }
}
