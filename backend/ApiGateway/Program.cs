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

// Load ocelot.json as additional config
builder.Configuration.AddJsonFile(
    "ocelot.json",
    optional: false,
    reloadOnChange: builder.Environment.IsDevelopment()
);

builder.Services.AddOcelot();

var app = builder.Build();

await app.UseOcelot();

app.Run();