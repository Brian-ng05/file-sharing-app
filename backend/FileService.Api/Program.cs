
using FileService.Api.Data;
using FileService.Api.Repository;
using FileManagementService = FileService.Api.Services.FileService;   
using FileService.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace FileService.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<ApplicationDbContext>(
                options =>
                    options.UseSqlServer(
                        builder.Configuration
                            .GetConnectionString("Default")));

            builder.Services.AddScoped<IFileRepository, FileRepository>();

            builder.Services.AddScoped<IFileService, FileManagementService>();

            builder.Services.AddControllers();

            var app = builder.Build();

            app.UseStaticFiles();

            app.MapControllers();

            app.Run();
        }
    }
}
