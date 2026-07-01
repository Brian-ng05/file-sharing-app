using FileService.Api.Dtos;
using StorageService.Api.Dtos;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace FileService.Api.Services
{
    public class StorageApiClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<StorageApiClient> _logger;

        public StorageApiClient(HttpClient http, ILogger<StorageApiClient> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<UploadResponse> UploadFileAsync(IFormFile file)
        {
            _logger.LogInformation("[StorageApiClient] Uploading file: {FileName}", file.FileName);
            
            using var content = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();

            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

            content.Add(
                fileContent,
                "file",
                file.FileName);

            var response = await _http.PostAsync("api/objects", content);

            _logger.LogInformation("[StorageApiClient] Upload response status: {StatusCode}", response.StatusCode);
            response.EnsureSuccessStatusCode();

            var result =
                await response.Content.ReadFromJsonAsync<UploadResponse>();

            if (result is null)
            {
                throw new Exception(
                    "Storage service returned an empty upload response.");
            }

            _logger.LogInformation("[StorageApiClient] Upload successful, storage key: {StorageKey}", result.StorageKey);
            return result;
        }

        public async Task<SignedUrlResponse> GetSignedUrlAsync(string storageKey)
        {
            _logger.LogInformation("[StorageApiClient] Getting signed URL for storage key: {StorageKey}", storageKey);
            
            var result = await _http.GetFromJsonAsync<SignedUrlResponse>(
                $"api/objects/signed-url?storageKey={Uri.EscapeDataString(storageKey)}");

            if (result is null)
            {
                throw new Exception(
                    "Storage service returned an empty signed url response.");
            }

            _logger.LogInformation("[StorageApiClient] Got signed URL successfully");
            return result;
        }

        public async Task DeleteFileAsync(string storageKey)
        {
            _logger.LogInformation("[StorageApiClient] Deleting storage key: {StorageKey}", storageKey);
            
            if (string.IsNullOrWhiteSpace(storageKey))
            {
                throw new ArgumentException("Storage key is required.", nameof(storageKey));
            }

            var encodedSegments = storageKey
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString);

            var encodedPath = string.Join('/', encodedSegments);

            var response = await _http.DeleteAsync($"api/objects/{encodedPath}");

            _logger.LogInformation("[StorageApiClient] Delete response status: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("[StorageApiClient] Delete failed. Body: {Body}", body);
                throw new Exception(
                    $"Failed to delete storage object. Status: {response.StatusCode}. Body: {body}");
            }
            
            _logger.LogInformation("[StorageApiClient] Delete successful for storage key: {StorageKey}", storageKey);
        }
    }
}