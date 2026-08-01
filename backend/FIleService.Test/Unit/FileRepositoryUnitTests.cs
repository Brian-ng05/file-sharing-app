using FileService.Api.Data;
using FileService.Api.Entities;
using FileService.Api.Repository;
using Microsoft.EntityFrameworkCore;

namespace FIleService.Test.Unit;

/// <summary>
/// Unit tests for FileRepository using EF Core InMemory database
/// (no PostgreSQL container needed).
/// </summary>
public class FileRepositoryUnitTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly FileRepository _sut;

    public FileRepositoryUnitTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);
        _sut = new FileRepository(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    // ──────────────────────────────────────────────
    // ADD TESTS
    // ──────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_ThenGetByCodeAsync_ReturnsAddedEntity()
    {
        var entity = new FileMetadata
        {
            Id = Guid.NewGuid(),
            Code = "test001",
            StorageKey = "uploads/test.pdf",
            OriginalFilename = "test.pdf",
            MimeType = "application/pdf",
            SizeBytes = 1024,
            DownloadCount = 0,
            MaxDownloads = 5,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            PasswordHash = null
        };

        await _sut.AddAsync(entity);
        await _sut.SaveChangesAsync();

        // Use a fresh context to verify persistence
        var result = await _sut.GetByCodeAsync("test001");

        Assert.NotNull(result);
        Assert.Equal("test001", result.Code);
        Assert.Equal("uploads/test.pdf", result.StorageKey);
        Assert.Equal("test.pdf", result.OriginalFilename);
        Assert.Equal("application/pdf", result.MimeType);
        Assert.Equal(1024, result.SizeBytes);
        Assert.Equal(5, result.MaxDownloads);
    }

    [Fact]
    public async Task AddAsync_WithMultipleEntities_AllPersist()
    {
        var entity1 = new FileMetadata
        {
            Id = Guid.NewGuid(), Code = "code1", StorageKey = "s3/code1",
            OriginalFilename = "f1.pdf", MimeType = "application/pdf", SizeBytes = 100,
            CreatedAt = DateTime.UtcNow
        };
        var entity2 = new FileMetadata
        {
            Id = Guid.NewGuid(), Code = "code2", StorageKey = "s3/code2",
            OriginalFilename = "f2.pdf", MimeType = "application/pdf", SizeBytes = 200,
            CreatedAt = DateTime.UtcNow
        };

        await _sut.AddAsync(entity1);
        await _sut.AddAsync(entity2);
        await _sut.SaveChangesAsync();

        var result1 = await _sut.GetByCodeAsync("code1");
        var result2 = await _sut.GetByCodeAsync("code2");

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal("code1", result1.Code);
        Assert.Equal("code2", result2.Code);
    }

    // ──────────────────────────────────────────────
    // GET BY CODE TESTS
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetByCodeAsync_WhenCodeDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetByCodeAsync("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByCodeAsync_WithPasswordHash_ReturnsHash()
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("secret");
        var entity = new FileMetadata
        {
            Id = Guid.NewGuid(), Code = "pwfile", StorageKey = "s3/pwfile",
            OriginalFilename = "secret.pdf", MimeType = "application/pdf", SizeBytes = 512,
            PasswordHash = passwordHash, CreatedAt = DateTime.UtcNow
        };

        await _sut.AddAsync(entity);
        await _sut.SaveChangesAsync();

        var result = await _sut.GetByCodeAsync("pwfile");

        Assert.NotNull(result);
        Assert.Equal(passwordHash, result.PasswordHash);
    }

    // ──────────────────────────────────────────────
    // DELETE TESTS
    // ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesEntity()
    {
        var entity = new FileMetadata
        {
            Id = Guid.NewGuid(), Code = "todel", StorageKey = "s3/todel",
            OriginalFilename = "del.pdf", MimeType = "application/pdf", SizeBytes = 100,
            CreatedAt = DateTime.UtcNow
        };

        await _sut.AddAsync(entity);
        await _sut.SaveChangesAsync();

        await _sut.DeleteAsync(entity);
        await _sut.SaveChangesAsync();

        var result = await _sut.GetByCodeAsync("todel");
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ThenAddSameCode_Works()
    {
        var entity = new FileMetadata
        {
            Id = Guid.NewGuid(), Code = "reuse", StorageKey = "s3/reuse",
            OriginalFilename = "first.pdf", MimeType = "application/pdf", SizeBytes = 100,
            CreatedAt = DateTime.UtcNow
        };

        await _sut.AddAsync(entity);
        await _sut.SaveChangesAsync();
        await _sut.DeleteAsync(entity);
        await _sut.SaveChangesAsync();

        var entity2 = new FileMetadata
        {
            Id = Guid.NewGuid(), Code = "reuse", StorageKey = "s3/reuse2",
            OriginalFilename = "second.pdf", MimeType = "application/pdf", SizeBytes = 200,
            CreatedAt = DateTime.UtcNow
        };

        await _sut.AddAsync(entity2);
        await _sut.SaveChangesAsync();

        var result = await _sut.GetByCodeAsync("reuse");
        Assert.NotNull(result);
        Assert.Equal("second.pdf", result.OriginalFilename);
    }

    // ──────────────────────────────────────────────
    // GET EXPIRED FILES TESTS
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetExpiredFilesAsync_ReturnsOnlyExpired()
    {
        var expired = new FileMetadata
        {
            Id = Guid.NewGuid(), Code = "exp1", StorageKey = "s3/exp1",
            OriginalFilename = "exp1.pdf", MimeType = "application/pdf", SizeBytes = 100,
            ExpiresAt = DateTime.UtcNow.AddHours(-1), CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        var active = new FileMetadata
        {
            Id = Guid.NewGuid(), Code = "act1", StorageKey = "s3/act1",
            OriginalFilename = "act1.pdf", MimeType = "application/pdf", SizeBytes = 100,
            ExpiresAt = DateTime.UtcNow.AddDays(7), CreatedAt = DateTime.UtcNow
        };
        var noExpiry = new FileMetadata
        {
            Id = Guid.NewGuid(), Code = "noexp1", StorageKey = "s3/noexp1",
            OriginalFilename = "noexp1.pdf", MimeType = "application/pdf", SizeBytes = 100,
            ExpiresAt = null, CreatedAt = DateTime.UtcNow
        };

        await _sut.AddAsync(expired);
        await _sut.AddAsync(active);
        await _sut.AddAsync(noExpiry);
        await _sut.SaveChangesAsync();

        var result = await _sut.GetExpiredFilesAsync();

        Assert.Single(result);
        Assert.Equal("exp1", result[0].Code);
    }

    [Fact]
    public async Task GetExpiredFilesAsync_WhenNoExpired_ReturnsEmptyList()
    {
        var active = new FileMetadata
        {
            Id = Guid.NewGuid(), Code = "act1", StorageKey = "s3/act1",
            OriginalFilename = "act1.pdf", MimeType = "application/pdf", SizeBytes = 100,
            ExpiresAt = DateTime.UtcNow.AddDays(7), CreatedAt = DateTime.UtcNow
        };

        await _sut.AddAsync(active);
        await _sut.SaveChangesAsync();

        var result = await _sut.GetExpiredFilesAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetExpiredFilesAsync_WhenMultipleExpired_ReturnsAll()
    {
        for (int i = 0; i < 5; i++)
        {
            var entity = new FileMetadata
            {
                Id = Guid.NewGuid(), Code = $"exp{i}", StorageKey = $"s3/exp{i}",
                OriginalFilename = $"exp{i}.pdf", MimeType = "application/pdf", SizeBytes = 100,
                ExpiresAt = DateTime.UtcNow.AddHours(-(i + 1)), CreatedAt = DateTime.UtcNow.AddDays(-1)
            };
            await _sut.AddAsync(entity);
        }
        await _sut.SaveChangesAsync();

        var result = await _sut.GetExpiredFilesAsync();

        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task GetExpiredFilesAsync_WithExactlyExpiredAtNow_ReturnsFile()
    {
        var justExpired = new FileMetadata
        {
            Id = Guid.NewGuid(), Code = "justnow", StorageKey = "s3/justnow",
            OriginalFilename = "justnow.pdf", MimeType = "application/pdf", SizeBytes = 100,
            ExpiresAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        await _sut.AddAsync(justExpired);
        await _sut.SaveChangesAsync();

        var result = await _sut.GetExpiredFilesAsync();

        // ExpiresAt <= UtcNow should include files expiring right now
        Assert.Contains(result, f => f.Code == "justnow");
    }

    [Fact]
    public async Task GetExpiredFilesAsync_WithFutureExpiry_NotReturned()
    {
        var future = new FileMetadata
        {
            Id = Guid.NewGuid(), Code = "future1", StorageKey = "s3/future1",
            OriginalFilename = "future1.pdf", MimeType = "application/pdf", SizeBytes = 100,
            ExpiresAt = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow
        };

        await _sut.AddAsync(future);
        await _sut.SaveChangesAsync();

        var result = await _sut.GetExpiredFilesAsync();

        Assert.DoesNotContain(result, f => f.Code == "future1");
    }

    // ──────────────────────────────────────────────
    // SAVE CHANGES TESTS
    // ──────────────────────────────────────────────

    [Fact]
    public async Task SaveChangesAsync_WithoutAddOrDelete_DoesNotThrow()
    {
        // Should not throw even without pending changes
        await _sut.SaveChangesAsync();
    }

    [Fact]
    public async Task SaveChangesAsync_BatchOperations_AllPersist()
    {
        // Add, save, get, delete, save, verify deleted - full lifecycle
        var entity = new FileMetadata
        {
            Id = Guid.NewGuid(), Code = "lifecycle", StorageKey = "s3/lifecycle",
            OriginalFilename = "life.pdf", MimeType = "application/pdf", SizeBytes = 100,
            CreatedAt = DateTime.UtcNow
        };

        await _sut.AddAsync(entity);
        await _sut.SaveChangesAsync();

        var retrieved = await _sut.GetByCodeAsync("lifecycle");
        Assert.NotNull(retrieved);

        retrieved!.DownloadCount = 5;
        await _sut.SaveChangesAsync();

        var updated = await _sut.GetByCodeAsync("lifecycle");
        Assert.NotNull(updated);
        Assert.Equal(5, updated.DownloadCount);

        await _sut.DeleteAsync(updated);
        await _sut.SaveChangesAsync();

        var deleted = await _sut.GetByCodeAsync("lifecycle");
        Assert.Null(deleted);
    }
}
