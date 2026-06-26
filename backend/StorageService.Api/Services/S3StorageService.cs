using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using StorageService.Api.Models;

namespace StorageService.Api.Services;

public class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly AwsSettings _awsSettings;

    public S3StorageService(
        IAmazonS3 s3,
        IOptions<AwsSettings> awsSettings)
    {
        _s3 = s3;
        _awsSettings = awsSettings.Value;
    }

    public async Task<string> UploadAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName);
        var storageKey = $"uploads/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}{extension}";

        await using var stream = file.OpenReadStream();

        var request = new PutObjectRequest
        {
            BucketName = _awsSettings.BucketName,
            Key = storageKey,
            InputStream = stream,
            ContentType = file.ContentType
        };

        await _s3.PutObjectAsync(request);

        return storageKey;
    }

    public async Task DeleteAsync(string storageKey)
    {
        await _s3.DeleteObjectAsync(_awsSettings.BucketName, storageKey);
    }

    public Task<string> GenerateSignedUrlAsync(string storageKey)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _awsSettings.BucketName,
            Key = storageKey,
            Expires = DateTime.UtcNow.AddMinutes(15)
        };

        var url = _s3.GetPreSignedURL(request);
        return Task.FromResult(url);
    }
}