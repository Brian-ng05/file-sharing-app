using Amazon.S3;
using Amazon.S3.Model;
using Moq;
using StorageService.Api.Models;
using StorageService.Api.Services;
using Xunit;

namespace StorageService.Test.Unit;

public class S3StorageServiceTests
{
    private readonly Mock<IAmazonS3> _mockS3;
    private readonly AwsSettings _awsSettings;
    private readonly S3StorageService _sut;

    public S3StorageServiceTests()
    {
        _mockS3 = new Mock<IAmazonS3>();
        _awsSettings = new AwsSettings
        {
            BucketName = "test-bucket"
        };
        _sut = new S3StorageService(_mockS3.Object, Microsoft.Extensions.Options.Options.Create(_awsSettings));
    }

    [Fact]
    public async Task ExistsAsync_WithExistingObject_ReturnsTrue()
    {
        // Arrange
        var testKey = "uploads/2026/06/27/test.pdf";
        _mockS3
            .Setup(x => x.GetObjectMetadataAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(new GetObjectMetadataResponse());

        // Act
        var result = await _sut.ExistsAsync(testKey);

        // Assert
        Assert.True(result);
        _mockS3.Verify(x => x.GetObjectMetadataAsync(_awsSettings.BucketName, testKey, default), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_WithMissingObject_ReturnsFalse()
    {
        // Arrange
        var testKey = "uploads/2026/06/27/missing.pdf";
        _mockS3
            .Setup(x => x.GetObjectMetadataAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ThrowsAsync(new AmazonS3Exception("Not Found") { StatusCode = System.Net.HttpStatusCode.NotFound });

        // Act
        var result = await _sut.ExistsAsync(testKey);

        // Assert
        Assert.False(result);
        _mockS3.Verify(x => x.GetObjectMetadataAsync(_awsSettings.BucketName, testKey, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithValidKey_CallsS3DeleteObject()
    {
        // Arrange
        var testKey = "uploads/2026/06/27/delete-me.pdf";
        _mockS3
            .Setup(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(new DeleteObjectResponse());

        // Act
        await _sut.DeleteAsync(testKey);

        // Assert
        _mockS3.Verify(x => x.DeleteObjectAsync(_awsSettings.BucketName, testKey, default), Times.Once);
    }

    [Fact]
    public async Task GenerateSignedUrlAsync_WithMissingObject_ThrowsFileNotFoundException()
    {
        // Arrange
        var testKey = "uploads/2026/06/27/missing.pdf";
        _mockS3
            .Setup(x => x.GetObjectMetadataAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ThrowsAsync(new AmazonS3Exception("Not Found") { StatusCode = System.Net.HttpStatusCode.NotFound });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(() => _sut.GenerateSignedUrlAsync(testKey));
        Assert.Contains(testKey, exception.Message);
        _mockS3.Verify(x => x.GetObjectMetadataAsync(_awsSettings.BucketName, testKey, default), Times.Once);
    }
}
