using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using StorageService.Api.DTOs;
using StorageService.Api.Models;
using System.Net;

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

    public async Task<UploadResponse> UploadAsync(IFormFile file)
    {
        try
        {
            var extension = Path.GetExtension(file.FileName);

            var storageKey =
                $"uploads/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}{extension}";

            await using var stream = file.OpenReadStream();

            var request = new PutObjectRequest
            {
                BucketName = _awsSettings.BucketName,
                Key = storageKey,
                InputStream = stream,
                ContentType = file.ContentType
            };

            await _s3.PutObjectAsync(request);

            return new UploadResponse
            {
                StorageKey = storageKey,
            };
        }
        catch (AmazonS3Exception ex)
        {
            throw new Exception(
                $"Unable to upload file to storage. {ex.Message}",
                ex);
        }
    }

    public async Task DeleteAsync(string storageKey)
    {
        Console.WriteLine($"[StorageService] DeleteAsync called with key: {storageKey}");

        try
        {
            await _s3.GetObjectMetadataAsync(
                _awsSettings.BucketName,
                storageKey);
            Console.WriteLine($"[StorageService] File found in S3: {storageKey}");
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Console.WriteLine(
                $"[StorageService] File not found in S3: {storageKey}, S3Message={ex.Message}");
            return;
        }

        var deleteResponse = await _s3.DeleteObjectAsync(
            _awsSettings.BucketName,
            storageKey);

        Console.WriteLine(
            $"[StorageService] Delete response for {storageKey}: {deleteResponse.HttpStatusCode}");
    }

    public async Task<bool> ExistsAsync(string storageKey)
    {
        try
        {
            await _s3.GetObjectMetadataAsync(_awsSettings.BucketName, storageKey);
            return true;
        }
        catch (AmazonS3Exception ex) when (
            ex.StatusCode == HttpStatusCode.NotFound ||
            ex.ErrorCode == "NoSuchKey" ||
            ex.ErrorCode == "NotFound")
        {
            return false;
        }
    }

    public async Task<string> GenerateSignedUrlAsync(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("StorageKey cannot be empty", nameof(storageKey));
        }

        var exists = await ExistsAsync(storageKey);
        if (!exists)
        {
            throw new FileNotFoundException($"Object with key {storageKey} not found", storageKey);
        }

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _awsSettings.BucketName,
            Key = storageKey,
            Expires = DateTime.UtcNow.AddMinutes(15)
        };

        var url = _s3.GetPreSignedURL(request);

        return url;
    }
}