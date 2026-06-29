using Microsoft.AspNetCore.Http;
using StorageService.Api.DTOs;
using StorageService.Api.Services;

namespace StorageService.Test.Helpers;

public class FakeStorageService : IStorageService
{
    private readonly Dictionary<string, bool> _objects = new();

    public Task<bool> ExistsAsync(string storageKey)
    {
        return Task.FromResult(_objects.ContainsKey(storageKey) && _objects[storageKey]);
    }

    public Task DeleteAsync(string storageKey)
    {
        _objects[storageKey] = false;
        return Task.CompletedTask;
    }

    public Task<string> GenerateSignedUrlAsync(string storageKey)
    {
        if (!_objects.ContainsKey(storageKey) || !_objects[storageKey])
        {
            throw new FileNotFoundException($"Object not found: {storageKey}", storageKey);
        }
        return Task.FromResult($"https://fake-s3-url/{storageKey}");
    }

    public Task<UploadResponse> UploadAsync(Microsoft.AspNetCore.Http.IFormFile file)
    {
        var storageKey = $"uploads/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        _objects[storageKey] = true;
        return Task.FromResult(new UploadResponse { StorageKey = storageKey });
    }

    public void AddObject(string storageKey)
    {
        _objects[storageKey] = true;
    }
}
