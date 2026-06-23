using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using StorageService.Api.Models;

namespace StorageService.Api.Services
{
    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly AwsSettings _awsSettings;

        public S3Service(IOptions<AwsSettings> awsSettings)
        {
            _awsSettings = awsSettings.Value;
            _s3Client = new AmazonS3Client(_awsSettings.AccessKey, _awsSettings.SecretKey, Amazon.RegionEndpoint.GetBySystemName(_awsSettings.Region));
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

        public async Task<string> GetPresignedUrlAsync(string key)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _awsSettings.BucketName,
                Key = key,
                Expires = DateTime.UtcNow.AddHours(1)
            };
            
            return _s3Client.GetPreSignedURL(request);
        }
    }
}
