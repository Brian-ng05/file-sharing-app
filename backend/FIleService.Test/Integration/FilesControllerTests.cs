using FileService.Api;
using FileService.Api.Data;
using FileService.Api.Dtos.UploadFileResponse;
using FileService.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using StorageService.Api.Dtos;
using System.Net;
using System.Net.Http.Json;
using Testcontainers.PostgreSql;

namespace FIleService.Test.Integration;

/// <summary>
/// API integration tests using WebApplicationFactory with real PostgreSQL.
/// Requires Docker OR TEST_POSTGRES_CONNECTION_STRING.
/// Replaces IStorageApiClient with mock — no AWS calls.
/// </summary>
public class FilesControllerTests : IClassFixture<FilesControllerTests.Fixture>
{
    private readonly Fixture _fixture;

    public FilesControllerTests(Fixture fixture) => _fixture = fixture;

    public class Fixture : IAsyncDisposable
    {
        private PostgreSqlContainer? _postgres;
        private readonly string? _connectionString;
        public WebApplicationFactory<Program> Factory { get; }
        public Mock<IStorageApiClient> StorageApiMock { get; } = new();
        public bool SkipTests { get; }

        public Fixture()
        {
            _connectionString = Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION_STRING");

            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                try { _postgres = new PostgreSqlBuilder().WithImage("postgres:16-alpine")
                    .WithDatabase("apitestdb").WithUsername("testuser").WithPassword("testpass").Build(); }
                catch { SkipTests = true; }
            }

            string connStr;
            if (SkipTests)
                connStr = "Host=skip;Database=skip;Username=skip;Password=skip";
            else if (_connectionString is not null)
                connStr = _connectionString;
            else
            {
                try { _postgres!.StartAsync().GetAwaiter().GetResult(); connStr = _postgres.GetConnectionString(); }
                catch { SkipTests = true; connStr = "Host=skip;Database=skip;Username=skip;Password=skip"; }
            }

            Factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseSetting("ConnectionStrings:Default", connStr);
                    builder.ConfigureTestServices(services =>
                    {
                        services.RemoveAll<IStorageApiClient>();
                        services.AddSingleton(StorageApiMock.Object);
                    });
                });

            if (!SkipTests)
            {
                using var scope = Factory.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<FileService.Api.Data.ApplicationDbContext>();
                db.Database.EnsureCreated();
            }
        }

        public async ValueTask DisposeAsync()
        {
            Factory.Dispose();
            if (_postgres is not null) await _postgres.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task PostFiles_WithInvalidFile_ReturnsClientError()
    {
        Skip.If(_fixture.SkipTests, "Docker unavailable and TEST_POSTGRES_CONNECTION_STRING not set.");

        var client = _fixture.Factory.CreateClient();
        using var content = new MultipartFormDataContent();
        using var file = new ByteArrayContent("malware"u8.ToArray());
        file.Headers.ContentType = new("application/x-msdownload");
        content.Add(file, "File", "virus.exe");

        var response = await client.PostAsync("/files", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [SkippableFact]
    public async Task PostFiles_WithValidFile_ReturnsSuccessAndShortCode()
    {
        Skip.If(_fixture.SkipTests, "Docker unavailable and TEST_POSTGRES_CONNECTION_STRING not set.");

        _fixture.StorageApiMock
            .Setup(x => x.UploadFileAsync(It.IsAny<Microsoft.AspNetCore.Http.IFormFile>()))
            .ReturnsAsync(new UploadResponse { StorageKey = "s3/api-ok" });

        var client = _fixture.Factory.CreateClient();
        using var content = new MultipartFormDataContent();
        using var file = new ByteArrayContent("hello"u8.ToArray());
        file.Headers.ContentType = new("text/plain");
        content.Add(file, "File", "hello.txt");

        var response = await client.PostAsync("/files", content);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<UploadFileResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Code);
        Assert.Equal(8, result.Code.Length);
    }

    [SkippableFact]
    public async Task DeleteFiles_WithExistingCode_ReturnsExpectedStatusCode()
    {
        Skip.If(_fixture.SkipTests, "Docker unavailable and TEST_POSTGRES_CONNECTION_STRING not set.");

        using var scope = _fixture.Factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var file = new FileService.Api.Entities.FileMetadata
        {
            Id = Guid.NewGuid(), Code = "apitodel",
            StorageKey = "s3/apitodel", OriginalFilename = "d.txt",
            MimeType = "text/plain", SizeBytes = 10, DownloadCount = 0,
            CreatedAt = DateTime.UtcNow
        };
        db.Files.Add(file);
        await db.SaveChangesAsync();

        var client = _fixture.Factory.CreateClient();
        var response = await client.DeleteAsync("/files/apitodel");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [SkippableFact]
    public async Task GetExpiredFiles_ReturnsExpiredRecords()
    {
        Skip.If(_fixture.SkipTests, "Docker unavailable and TEST_POSTGRES_CONNECTION_STRING not set.");

        using var scope = _fixture.Factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var expired = new FileService.Api.Entities.FileMetadata
        {
            Id = Guid.NewGuid(), Code = "apiexp03",
            StorageKey = "s3/apiexp03", OriginalFilename = "e.pdf",
            MimeType = "application/pdf", SizeBytes = 1, DownloadCount = 0,
            ExpiresAt = DateTime.UtcNow.AddHours(-1), CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        db.Files.Add(expired);
        await db.SaveChangesAsync();

        var client = _fixture.Factory.CreateClient();
        var response = await client.GetAsync("/files/expired");

        response.EnsureSuccessStatusCode();
        var result = await response.Content
            .ReadFromJsonAsync<List<FileService.Api.Dtos.ExpiredFileDto>>();

        Assert.NotNull(result);
        Assert.Contains(result, f => f.Code == "apiexp03");
    }
}
