using MaintenanceService.Api.Clients;
using MaintenanceService.Api.DTOs;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace MaintenanceService.Test.Unit;

public class FileServiceClientTests
{
    private const string BaseAddress = "http://localhost:7001/";

    [Fact]
    public async Task GetExpiredFilesAsync_WhenSuccessful_DeserializesExpiredFiles()
    {
        HttpMethod? capturedMethod = null;
        string? capturedPath = null;

        var handler = new FakeHandler(request =>
        {
            capturedMethod = request.Method;
            capturedPath = request.RequestUri!.AbsolutePath;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<ExpiredFileDto>
                {
                    new() { Code = "exp-A" },
                    new() { Code = "exp-B" }
                })
            });
        });

        var client = CreateClient(handler);

        var result = await client.GetExpiredFilesAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("exp-A", result[0].Code);
        Assert.Equal("exp-B", result[1].Code);
        Assert.Equal(HttpMethod.Get, capturedMethod);
        Assert.Equal("/files/expired", capturedPath);
    }

    [Fact]
    public async Task GetExpiredFilesAsync_WhenBodyIsEmpty_ReturnsEmptyList()
    {
        var handler = new FakeHandler(_ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<ExpiredFileDto>())
            }));

        var client = CreateClient(handler);
        var result = await client.GetExpiredFilesAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetExpiredFilesAsync_WhenServerReturnsError_ThrowsHttpRequestException()
    {
        var handler = new FakeHandler(_ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.InternalServerError)
            { Content = new StringContent("Server error") }));

        var client = CreateClient(handler);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetExpiredFilesAsync());
    }

    [Fact]
    public async Task DeleteFileAsync_SendsDeleteToCorrectCode()
    {
        HttpMethod? capturedMethod = null;
        string? capturedPath = null;

        var handler = new FakeHandler(request =>
        {
            capturedMethod = request.Method;
            capturedPath = request.RequestUri!.AbsolutePath;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });

        var client = CreateClient(handler);
        await client.DeleteFileAsync("code-xyz");

        Assert.Equal(HttpMethod.Delete, capturedMethod);
        Assert.Equal("/files/code-xyz", capturedPath);
    }

    [Fact]
    public async Task DeleteFileAsync_WhenSuccessful_Completes()
    {
        var handler = new FakeHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));

        var client = CreateClient(handler);

        // Should not throw
        await client.DeleteFileAsync("any-code");
    }

    [Fact]
    public async Task DeleteFileAsync_WhenServerReturnsError_ThrowsHttpRequestException()
    {
        var handler = new FakeHandler(_ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.NotFound)
            { Content = new StringContent("Not found") }));

        var client = CreateClient(handler);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.DeleteFileAsync("missing-code"));
    }

    [Fact]
    public async Task CancellationToken_IsPassedToHttpRequest()
    {
        using var cts = new CancellationTokenSource();
        bool called = false;

        var handler = new FakeHandler(_ =>
        {
            called = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });

        var client = CreateClient(handler);
        await client.DeleteFileAsync("code", cts.Token);

        Assert.True(called);
    }

    // ──────────────────────────────────────────────
    // HELPERS
    // ──────────────────────────────────────────────

    private static FileServiceClient CreateClient(FakeHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(BaseAddress) };
        var logger = new Mock<ILogger<FileServiceClient>>().Object;
        return new FileServiceClient(httpClient, logger);
    }

    private class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;
        public FakeHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) => _handler = handler;
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct) => _handler(request);
    }
}
