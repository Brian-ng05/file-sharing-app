using MaintenanceService.Api.Clients;
using MaintenanceService.Api.DTOs;
using MaintenanceService.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace MaintenanceService.Test.Unit;

public class CleanupServiceTests
{
    private readonly Mock<IFileServiceClient> _clientMock;
    private readonly CleanupService _sut;

    public CleanupServiceTests()
    {
        _clientMock = new Mock<IFileServiceClient>();
        var loggerMock = new Mock<ILogger<CleanupService>>();
        _sut = new CleanupService(_clientMock.Object, loggerMock.Object);
    }

    [Fact]
    public async Task CleanupExpiredFilesAsync_WhenNoExpiredFiles_ReturnsZero()
    {
        _clientMock
            .Setup(x => x.GetExpiredFilesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExpiredFileDto>());

        var result = await _sut.CleanupExpiredFilesAsync(CancellationToken.None);

        Assert.Equal(0, result);
        _clientMock.Verify(
            x => x.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CleanupExpiredFilesAsync_WhenAllDeletesSucceed_ReturnsDeletedCount()
    {
        var files = new List<ExpiredFileDto>
        {
            new() { Code = "A" }, new() { Code = "B" }, new() { Code = "C" }
        };

        _clientMock
            .Setup(x => x.GetExpiredFilesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(files);

        var result = await _sut.CleanupExpiredFilesAsync(CancellationToken.None);

        Assert.Equal(3, result);
        _clientMock.Verify(x => x.DeleteFileAsync("A", It.IsAny<CancellationToken>()), Times.Once);
        _clientMock.Verify(x => x.DeleteFileAsync("B", It.IsAny<CancellationToken>()), Times.Once);
        _clientMock.Verify(x => x.DeleteFileAsync("C", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CleanupExpiredFilesAsync_WhenOneDeleteFails_ContinuesWithRemainingFiles()
    {
        var files = new List<ExpiredFileDto>
        {
            new() { Code = "A" }, new() { Code = "B" }, new() { Code = "C" }
        };

        _clientMock
            .Setup(x => x.GetExpiredFilesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(files);

        // B throws
        _clientMock
            .Setup(x => x.DeleteFileAsync("B", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Service unavailable"));

        var result = await _sut.CleanupExpiredFilesAsync(CancellationToken.None);

        // A and C should succeed
        Assert.Equal(2, result);
        _clientMock.Verify(x => x.DeleteFileAsync("A", It.IsAny<CancellationToken>()), Times.Once);
        _clientMock.Verify(x => x.DeleteFileAsync("B", It.IsAny<CancellationToken>()), Times.Once);
        _clientMock.Verify(x => x.DeleteFileAsync("C", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CleanupExpiredFilesAsync_WhenGetExpiredFilesFails_PropagatesException()
    {
        _clientMock
            .Setup(x => x.GetExpiredFilesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Gateway timeout"));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => _sut.CleanupExpiredFilesAsync(CancellationToken.None));

        Assert.Equal("Gateway timeout", ex.Message);
        _clientMock.Verify(
            x => x.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
