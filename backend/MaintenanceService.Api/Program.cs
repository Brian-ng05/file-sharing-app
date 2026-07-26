using MaintenanceService.Api.BackgroundServices;
using MaintenanceService.Api.Clients;
using MaintenanceService.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// OpenAPI / Swagger
builder.Services.AddOpenApi();

// HttpClient -> FileService
builder.Services.AddHttpClient<IFileServiceClient, FileServiceClient>(
    client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["Services:GatewayUrl"]!);
    });

// Services
builder.Services.AddScoped<ICleanupService, CleanupService>();

// Background Job
builder.Services.AddHostedService<CleanupBackgroundService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

app.Run();