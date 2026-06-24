using System.Net.Http.Headers;
using System.Net.Http.Json;
using FileService.Api.Dtos;

namespace FileService.Api.Services;

public class StorageHttpClient : IStorageClient
{
    private readonly HttpClient _httpClient;

    public StorageHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(string Key, string BucketName)> UploadFileAsync(IFormFile file)
    {
        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(streamContent, "file", file.FileName);

        var response = await _httpClient.PostAsync("storage/upload", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<StorageUploadResponse>()
            ?? throw new Exception("Invalid storage upload response");

        return (result.Key, result.BucketName);
    }

    public async Task<byte[]> DownloadFileAsync(string key)
    {
        var encodedKey = Uri.EscapeDataString(key);
        var response = await _httpClient.GetAsync($"storage/{encodedKey}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task DeleteFileAsync(string key)
    {
        var encodedKey = Uri.EscapeDataString(key);
        var response = await _httpClient.DeleteAsync($"storage/{encodedKey}");
        response.EnsureSuccessStatusCode();
    }
}
