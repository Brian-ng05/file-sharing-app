namespace MaintenanceService.Api.Clients;
using System.Net.Http.Json;
using MaintenanceService.Api.DTOs;

public class FileServiceClient : IFileServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FileServiceClient> _logger;

    public FileServiceClient(
        HttpClient httpClient,
        ILogger<FileServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<ExpiredFileDto>> GetExpiredFilesAsync(
    CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            "/files/expired",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogError(
                "Failed to retrieve expired files. Status: {StatusCode}. Response: {Response}",
                response.StatusCode,
                error);

            response.EnsureSuccessStatusCode();
        }

        var expiredFiles =
            await response.Content.ReadFromJsonAsync<List<ExpiredFileDto>>(
                cancellationToken: cancellationToken);

        return expiredFiles ?? [];
    }

    public async Task DeleteFileAsync(
    string code,
    CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync(
            $"/files/{code}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogError(
                "Failed to delete file {Code}. Status: {StatusCode}. Response: {Response}",
                code,
                response.StatusCode,
                error);

            response.EnsureSuccessStatusCode();
        }
    }
}