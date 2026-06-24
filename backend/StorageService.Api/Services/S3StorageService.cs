using Amazon.S3;
using Amazon.S3.Model;

namespace StorageService.Api.Services;

public class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly IConfiguration _configuration;

    public S3StorageService(
        IAmazonS3 s3,
        IConfiguration configuration)
    {
        _s3 = s3;
        _configuration = configuration;
    }

    public async Task<string> UploadAsync(IFormFile file)
    {
        var bucketName =
            _configuration["AWS:BucketName"];

        var extension =
            Path.GetExtension(file.FileName);

        var storageKey =
            $"uploads/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}{extension}";

        await using var stream =
            file.OpenReadStream();

        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = storageKey,
            InputStream = stream,
            ContentType = file.ContentType
        };

        await _s3.PutObjectAsync(request);

        return storageKey;
    }

    public async Task DeleteAsync(string storageKey)
    {
        var bucketName =
            _configuration["AWS:BucketName"];

        await _s3.DeleteObjectAsync(
            bucketName,
            storageKey);
    }

    public Task<string> GenerateSignedUrlAsync(
        string storageKey)
    {
        var bucketName =
            _configuration["AWS:BucketName"];

        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = storageKey,
            Expires = DateTime.UtcNow.AddMinutes(15)
        };

        var url =
            _s3.GetPreSignedURL(request);

        return Task.FromResult(url);
    }
}