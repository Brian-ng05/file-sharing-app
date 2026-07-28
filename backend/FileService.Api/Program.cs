
using FileService.Api.Data;
using FileService.Api.Repository;
using FileManagementService = FileService.Api.Services.FileService;   
using FileService.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.Json;

namespace FileService.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            LoadEnvFile();

            var builder = WebApplication.CreateBuilder(args);

            if (!builder.Environment.IsDevelopment())
                {
                    builder.Configuration.Sources
                        .OfType<JsonConfigurationSource>()
                        .ToList()
                        .ForEach(source => source.ReloadOnChange = false);
                }

            builder.Services.AddDbContext<ApplicationDbContext>(
                options =>
                    options.UseNpgsql(
                        builder.Configuration
                            .GetConnectionString("Default")));

            var gatewayUrl = builder.Configuration["Services:GatewayUrl"]!;

            builder.Services.AddHttpClient<IStorageApiClient, StorageApiClient>(c =>
                c.BaseAddress = new Uri(gatewayUrl));

            builder.Services.AddScoped<IFileRepository, FileRepository>();

            builder.Services.AddScoped<IFileService, FileManagementService>();

            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Swagger UI
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseStaticFiles();

            app.MapControllers();

            app.Run();
        }

        private static void LoadEnvFile()
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null && !dir.GetFiles(".env").Any())
                dir = dir.Parent;

            if (dir == null) return;

            var envPath = Path.Combine(dir.FullName, ".env");
            foreach (var line in File.ReadAllLines(envPath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
                var eq = line.IndexOf('=');
                if (eq > 0)
                    Environment.SetEnvironmentVariable(line[..eq].Trim(), line[(eq + 1)..].Trim());
            }
        }
    }
}
