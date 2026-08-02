using MaintenanceService.Api.Clients;
using MaintenanceService.Api.DTOs;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace MaintenanceService.Test.Unit;

/// <summary>
/// Additional unit tests for FileServiceClient covering edge cases
/// not covered by the existing FileServiceClientTests.
/// </summary>
public class FileServiceClientAdditionalTests
{
    private const string BaseAddress = "http://localhost:7001/";

    [Fact]
    public async Task GetExpiredFilesAsync_WhenBodyIsNull_ReturnsEmptyList()
    {
        var handler = new FakeHandler(_ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null")
            }));

        var client = new FileServiceClient(
            new HttpClient(handler) { BaseAddress = new Uri(BaseAddress) },
            Mock.Of<ILogger<FileServiceClient>>());

        var result = await client.GetExpiredFilesAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetExpiredFilesAsync_With404_ThrowsHttpRequestException()
    {
        var handler = new FakeHandler(_ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.NotFound)));

        var client = new FileServiceClient(
            new HttpClient(handler) { BaseAddress = new Uri(BaseAddress) },
            Mock.Of<ILogger<FileServiceClient>>());

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetExpiredFilesAsync());
    }

    [Fact]
    public async Task GetExpiredFilesAsync_With503_ThrowsHttpRequestException()
    {
        var handler = new FakeHandler(_ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        var client = new FileServiceClient(
            new HttpClient(handler) { BaseAddress = new Uri(BaseAddress) },
            Mock.Of<ILogger<FileServiceClient>>());

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetExpiredFilesAsync());
    }

    [Fact]
    public async Task DeleteFileAsync_WithSpecialCode_EncodesCorrectly()
    {
        string? capturedPath = null;

        var handler = new FakeHandler(request =>
        {
            capturedPath = request.RequestUri!.AbsolutePath;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });

        var client = new FileServiceClient(
            new HttpClient(handler) { BaseAddress = new Uri(BaseAddress) },
            Mock.Of<ILogger<FileServiceClient>>());

        await client.DeleteFileAsync("abc 123");

        Assert.Equal("/files/abc%20123", capturedPath);
    }

    [Fact]
    public async Task DeleteFileAsync_With500_ThrowsHttpRequestException()
    {
        var handler = new FakeHandler(_ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Server error")
            }));

        var client = new FileServiceClient(
            new HttpClient(handler) { BaseAddress = new Uri(BaseAddress) },
            Mock.Of<ILogger<FileServiceClient>>());

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.DeleteFileAsync("any-code"));
    }

    [Fact]
    public async Task GetExpiredFilesAsync_WithCancellationToken_CancelsRequest()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // No handler needed — HttpClient throws for canceled token before sending
        var httpClient = new HttpClient(new FakeHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<ExpiredFileDto> { new() { Code = "a" } })
            })))
        { BaseAddress = new Uri(BaseAddress) };

        var client = new FileServiceClient(httpClient,
            Mock.Of<ILogger<FileServiceClient>>());

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => client.GetExpiredFilesAsync(cts.Token));
    }

    [Fact]
    public async Task DeleteFileAsync_WithCancellationToken_CancelsRequest()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var httpClient = new HttpClient(new FakeHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent))))
        { BaseAddress = new Uri(BaseAddress) };

        var client = new FileServiceClient(httpClient,
            Mock.Of<ILogger<FileServiceClient>>());

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => client.DeleteFileAsync("code", cts.Token));
    }

    // ──────────────────────────────────────────────
    // FAKE HTTP HANDLER
    // ──────────────────────────────────────────────

    private class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public FakeHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request);
        }
    }
}
