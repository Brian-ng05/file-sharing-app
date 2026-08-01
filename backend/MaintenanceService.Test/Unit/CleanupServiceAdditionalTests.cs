using MaintenanceService.Api.Clients;
using MaintenanceService.Api.DTOs;
using MaintenanceService.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace MaintenanceService.Test.Unit;

/// <summary>
/// Additional unit tests for CleanupService covering edge cases
/// not covered by the existing CleanupServiceTests.
/// </summary>
public class CleanupServiceAdditionalTests
{
    private readonly Mock<IFileServiceClient> _clientMock;
    private readonly Mock<ILogger<CleanupService>> _loggerMock;
    private readonly CleanupService _sut;

    public CleanupServiceAdditionalTests()
    {
        _clientMock = new Mock<IFileServiceClient>();
        _loggerMock = new Mock<ILogger<CleanupService>>();
        _sut = new CleanupService(_clientMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CleanupExpiredFilesAsync_WhenAllDeletesFail_ReturnsZero()
    {
        var files = new List<ExpiredFileDto>
        {
            new() { Code = "A" }, new() { Code = "B" }, new() { Code = "C" }
        };

        _clientMock
            .Setup(x => x.GetExpiredFilesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(files);

        _clientMock
            .Setup(x => x.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Service unavailable"));

        var result = await _sut.CleanupExpiredFilesAsync(CancellationToken.None);

        Assert.Equal(0, result);
        // All three deletes were attempted
        _clientMock.Verify(x => x.DeleteFileAsync("A", It.IsAny<CancellationToken>()), Times.Once);
        _clientMock.Verify(x => x.DeleteFileAsync("B", It.IsAny<CancellationToken>()), Times.Once);
        _clientMock.Verify(x => x.DeleteFileAsync("C", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CleanupExpiredFilesAsync_WithSingleFile_Succeeds()
    {
        var files = new List<ExpiredFileDto>
        {
            new() { Code = "single" }
        };

        _clientMock
            .Setup(x => x.GetExpiredFilesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(files);

        var result = await _sut.CleanupExpiredFilesAsync(CancellationToken.None);

        Assert.Equal(1, result);
        _clientMock.Verify(x => x.DeleteFileAsync("single", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CleanupExpiredFilesAsync_PassesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _clientMock
            .Setup(x => x.GetExpiredFilesAsync(token))
            .ReturnsAsync(new List<ExpiredFileDto>());

        await _sut.CleanupExpiredFilesAsync(token);

        _clientMock.Verify(x => x.GetExpiredFilesAsync(token), Times.Once);
    }

    [Fact]
    public async Task CleanupExpiredFilesAsync_WithManyFiles_ProcessesAll()
    {
        var files = Enumerable.Range(0, 50)
            .Select(i => new ExpiredFileDto { Code = $"file-{i:D4}" })
            .ToList();

        _clientMock
            .Setup(x => x.GetExpiredFilesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(files);

        var result = await _sut.CleanupExpiredFilesAsync(CancellationToken.None);

        Assert.Equal(50, result);
        _clientMock.Verify(
            x => x.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(50));
    }

    [Fact]
    public async Task CleanupExpiredFilesAsync_WhenOperationCanceled_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _clientMock
            .Setup(x => x.GetExpiredFilesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.CleanupExpiredFilesAsync(cts.Token));
    }

    [Fact]
    public async Task CleanupExpiredFilesAsync_WhenGetExpiredFilesThrowsGeneric_Propagates()
    {
        _clientMock
            .Setup(x => x.GetExpiredFilesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CleanupExpiredFilesAsync(CancellationToken.None));

        Assert.Equal("Database error", ex.Message);
    }

    [Fact]
    public async Task CleanupExpiredFilesAsync_MixedFailures_LogsAndContinues()
    {
        var files = new List<ExpiredFileDto>
        {
            new() { Code = "ok1" },
            new() { Code = "fail1" },
            new() { Code = "fail2" },
            new() { Code = "ok2" },
            new() { Code = "fail3" },
            new() { Code = "ok3" }
        };

        _clientMock
            .Setup(x => x.GetExpiredFilesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(files);

        _clientMock
            .Setup(x => x.DeleteFileAsync(It.Is<string>(c => c.StartsWith("fail")),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Failed"));

        var result = await _sut.CleanupExpiredFilesAsync(CancellationToken.None);

        Assert.Equal(3, result); // Only ok1, ok2, ok3 succeed
    }
}
