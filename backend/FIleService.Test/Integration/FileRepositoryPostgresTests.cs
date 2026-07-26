using FileService.Api.Data;
using FileService.Api.Entities;
using FileService.Api.Repository;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace FIleService.Test.Integration;

/// <summary>
/// PostgreSQL integration tests using Testcontainers.
/// Requires Docker running OR set TEST_POSTGRES_CONNECTION_STRING env var.
/// </summary>
public class FileRepositoryPostgresTests : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private ApplicationDbContext _dbContext = null!;
    private FileRepository _sut = null!;
    private bool _skipTests;
    private string? _connectionString;

    public FileRepositoryPostgresTests()
    {
        _connectionString = Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION_STRING");

        if (!string.IsNullOrWhiteSpace(_connectionString))
            return;

        try
        {
            _postgres = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("testdb")
                .WithUsername("testuser")
                .WithPassword("testpass")
                .Build();
        }
        catch { _skipTests = true; }
    }

    public async Task InitializeAsync()
    {
        if (_skipTests) return;

        string connStr;
        if (_connectionString is not null)
        {
            connStr = _connectionString;
        }
        else
        {
            try
            {
                await _postgres!.StartAsync();
                connStr = _postgres.GetConnectionString();
            }
            catch { _skipTests = true; return; }
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connStr)
            .Options;

        _dbContext = new ApplicationDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();
        _sut = new FileRepository(_dbContext);
    }

    public async Task DisposeAsync()
    {
        if (_dbContext is not null) await _dbContext.DisposeAsync();
        if (_postgres is not null) await _postgres.DisposeAsync();
    }

    private string? GetConnStr()
        => _postgres?.GetConnectionString() ?? _connectionString;

    [SkippableFact]
    public async Task AddAsync_ThenGetByCodeAsync_PersistsMetadata()
    {
        Skip.If(_skipTests, "Docker unavailable and TEST_POSTGRES_CONNECTION_STRING not set.");

        var now = DateTime.UtcNow;
        var metadata = new FileMetadata
        {
            Id = Guid.NewGuid(),
            Code = "pg-int-02",
            StorageKey = "uploads/pg-test.pdf",
            OriginalFilename = "pg-test.pdf",
            MimeType = "application/pdf",
            SizeBytes = 4096,
            DownloadCount = 0,
            MaxDownloads = 5,
            ExpiresAt = now.AddDays(7),
            CreatedAt = now,
            PasswordHash = null
        };

        await _sut.AddAsync(metadata);
        await _sut.SaveChangesAsync();

        var freshOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(GetConnStr()!)
            .Options;
        await using var freshDb = new ApplicationDbContext(freshOptions);
        var freshRepo = new FileRepository(freshDb);

        var result = await freshRepo.GetByCodeAsync("pg-int-02");

        Assert.NotNull(result);
        Assert.Equal("pg-int-02", result.Code);
        Assert.Equal("uploads/pg-test.pdf", result.StorageKey);
        Assert.Equal("pg-test.pdf", result.OriginalFilename);
        Assert.Equal("application/pdf", result.MimeType);
        Assert.Equal(4096, result.SizeBytes);
        Assert.Equal(0, result.DownloadCount);
        Assert.Equal(5, result.MaxDownloads);
    }

    [SkippableFact]
    public async Task GetExpiredFilesAsync_ReturnsOnlyExpiredFiles()
    {
        Skip.If(_skipTests, "Docker unavailable and TEST_POSTGRES_CONNECTION_STRING not set.");

        var now = DateTime.UtcNow;

        var expired = new FileMetadata
        {
            Id = Guid.NewGuid(), Code = "expX2",
            StorageKey = "s3/expX2", OriginalFilename = "x.pdf",
            MimeType = "text/plain", SizeBytes = 1, DownloadCount = 0,
            ExpiresAt = now.AddHours(-1), CreatedAt = now.AddDays(-1)
        };
        var active = new FileMetadata
        {
            Id = Guid.NewGuid(), Code = "actY2",
            StorageKey = "s3/actY2", OriginalFilename = "y.pdf",
            MimeType = "text/plain", SizeBytes = 1, DownloadCount = 0,
            ExpiresAt = now.AddHours(1), CreatedAt = now
        };
        var never = new FileMetadata
        {
            Id = Guid.NewGuid(), Code = "noExpZ2",
            StorageKey = "s3/z2", OriginalFilename = "z.pdf",
            MimeType = "text/plain", SizeBytes = 1, DownloadCount = 0,
            ExpiresAt = null, CreatedAt = now
        };

        await _dbContext.Files.AddRangeAsync(expired, active, never);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetExpiredFilesAsync();

        Assert.Single(result);
        Assert.Equal("expX2", result[0].Code);
    }

    [SkippableFact]
    public async Task DeleteAsync_RemovesMetadata()
    {
        Skip.If(_skipTests, "Docker unavailable and TEST_POSTGRES_CONNECTION_STRING not set.");

        var meta = new FileMetadata
        {
            Id = Guid.NewGuid(), Code = "todel-pg2",
            StorageKey = "s3/todel2", OriginalFilename = "d.pdf",
            MimeType = "text/plain", SizeBytes = 1, DownloadCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.Files.AddAsync(meta);
        await _dbContext.SaveChangesAsync();

        await _sut.DeleteAsync(meta);
        await _sut.SaveChangesAsync();

        var freshOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(GetConnStr()!)
            .Options;
        await using var freshDb = new ApplicationDbContext(freshOptions);
        var freshRepo = new FileRepository(freshDb);

        var result = await freshRepo.GetByCodeAsync("todel-pg2");
        Assert.Null(result);
    }

    [SkippableFact]
    public async Task GetByCodeAsync_WhenCodeDoesNotExist_ReturnsNull()
    {
        Skip.If(_skipTests, "Docker unavailable and TEST_POSTGRES_CONNECTION_STRING not set.");

        var result = await _sut.GetByCodeAsync("no-such-code");
        Assert.Null(result);
    }
}
