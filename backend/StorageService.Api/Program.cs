using Amazon.S3;
using StorageService.Api.Models;
using StorageService.Api.Services;

namespace StorageService.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<AwsSettings>(builder.Configuration.GetSection("AwsSettings"));

            var awsSettings = builder.Configuration.GetSection("AwsSettings").Get<AwsSettings>();
            builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
                awsSettings?.AccessKey,
                awsSettings?.SecretKey,
                Amazon.RegionEndpoint.GetBySystemName(awsSettings?.Region ?? "ap-southeast-1")));

            builder.Services.AddScoped<IStorageService, S3StorageService>();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
