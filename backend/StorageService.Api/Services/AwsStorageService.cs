using Amazon.S3;
using Amazon.S3.Model;
using StorageService.Api.Models;
using Microsoft.Extensions.Options;

namespace StorageService.Api.Services;

public class AwsStorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly AwsSettings _awsSettings;

    public AwsStorageService(IOptions<AwsSettings> awsSettings)
    {
        _awsSettings = awsSettings.Value;
        _s3Client = new AmazonS3Client(
            _awsSettings.AccessKey,
            _awsSettings.SecretKey,
            Amazon.RegionEndpoint.GetBySystemName(_awsSettings.Region));
    }

    public async Task<string> UploadFileAsync(IFormFile file)
    {
        var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";

        using (var stream = file.OpenReadStream())
        {
            var request = new PutObjectRequest
            {
                BucketName = _awsSettings.BucketName,
                Key = uniqueFileName,
                InputStream = stream,
                ContentType = file.ContentType
            };

            await _s3Client.PutObjectAsync(request);
        }

        return uniqueFileName;
    }

    public async Task<byte[]> DownloadFileAsync(string key)
    {
        var request = new GetObjectRequest
        {
            BucketName = _awsSettings.BucketName,
            Key = key
        };

        using (var response = await _s3Client.GetObjectAsync(request))
        using (var memoryStream = new MemoryStream())
        {
            await response.ResponseStream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
    }

    public async Task DeleteFileAsync(string key)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _awsSettings.BucketName,
            Key = key
        };

        await _s3Client.DeleteObjectAsync(request);
    }
}
