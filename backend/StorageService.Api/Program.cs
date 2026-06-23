
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
            builder.Services.AddScoped<IS3Service, S3Service>();
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
