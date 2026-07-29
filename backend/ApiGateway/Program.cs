using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.Extensions.Configuration.Json;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    builder.Configuration.Sources
        .OfType<JsonConfigurationSource>()
        .ToList()
        .ForEach(source => source.ReloadOnChange = false);
}

// Development -> ocelot.json
// Production  -> ocelot.render.json
var ocelotConfigFile = builder.Environment.IsDevelopment()
    ? "ocelot.json"
    : "ocelot.render.json";

builder.Configuration.AddJsonFile(
    ocelotConfigFile,
    optional: false,
    reloadOnChange: builder.Environment.IsDevelopment()
);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins("https://file-sharing-frontend-c96a.onrender.com")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddOcelot();

var app = builder.Build();

// Enable CORS
app.UseCors("Frontend");

await app.UseOcelot();

app.Run();