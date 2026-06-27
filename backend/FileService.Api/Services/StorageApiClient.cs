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
                $"api/objects/{Uri.EscapeDataString(storageKey)}/signed-url");

            if (result is null)
            {
                throw new Exception(
                    "Storage service returned an empty signed url response.");
            }

            return result;
        }

        public async Task DeleteFileAsync(string storageKey)
        {
            var response = await _http.DeleteAsync(
                $"api/objects/{Uri.EscapeDataString(storageKey)}");

            response.EnsureSuccessStatusCode();
        }
    }
}