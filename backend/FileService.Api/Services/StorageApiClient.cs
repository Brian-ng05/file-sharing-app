using FileService.Api.Dtos;
using StorageService.Api.Dtos;
using System.Net.Http.Json;

namespace FileService.Api.Services
{
    public class StorageApiClient
    {
        private readonly HttpClient _http;

        public StorageApiClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<UploadResponse> UploadFileAsync(IFormFile file)
        {
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

            response.EnsureSuccessStatusCode();

            var result =
                await response.Content.ReadFromJsonAsync<UploadResponse>();

            if (result is null)
            {
                throw new Exception(
                    "Storage service returned an empty upload response.");
            }

            return result;
        }

        public async Task<SignedUrlResponse> GetSignedUrlAsync(string storageKey)
        {
            var result = await _http.GetFromJsonAsync<SignedUrlResponse>(
                $"api/objects/signed-url?storageKey={Uri.EscapeDataString(storageKey)}");

            if (result is null)
            {
                throw new Exception(
                    "Storage service returned an empty signed url response.");
            }

            return result;
        }

        public async Task DeleteFileAsync(string storageKey)
        {
            // Encode individual path segments but preserve slashes so the catch-all route receives the correct key.
            var encodedSegments = storageKey
                .Split('/')
                .Select(s => Uri.EscapeDataString(s));
            var encodedPath = string.Join('/', encodedSegments);

            // Build an absolute Uri to avoid double-encoding issues when HttpClient combines base address + relative.
            var requestUri = new Uri(_http.BaseAddress!, $"api/objects/{encodedPath}");

            Console.WriteLine($"[StorageApiClient] Deleting object via: {requestUri}");

            var response = await _http.DeleteAsync(requestUri);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to delete storage object. StatusCode={response.StatusCode}, Body={body}");
            }
        }
    }
}