using System.Diagnostics.CodeAnalysis;
using MaintenanceService.Api.Clients;
using MaintenanceService.Api.Services;

[ExcludeFromCodeCoverage]
public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // HttpClient -> FileService
        builder.Services.AddHttpClient<IFileServiceClient, FileServiceClient>(
            client =>
            {
                client.BaseAddress = new Uri(
                    builder.Configuration["Services:GatewayUrl"]!);
            });

        // Services
        builder.Services.AddScoped<ICleanupService, CleanupService>();

        var host = builder.Build();

        try
        {
            using var scope = host.Services.CreateScope();

            var cleanupService =
                scope.ServiceProvider.GetRequiredService<ICleanupService>();

            var logger =
                scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Starting cleanup job...");

            var deletedCount =
                await cleanupService.CleanupExpiredFilesAsync();

            logger.LogInformation(
                "Cleanup completed successfully. Deleted {DeletedCount} expired file(s).",
                deletedCount);
        }
        catch (Exception ex)
        {
            var logger =
                host.Services.GetRequiredService<ILogger<Program>>();

            logger.LogError(ex, "Cleanup job failed.");

            Environment.ExitCode = 1;
        }
    }
}