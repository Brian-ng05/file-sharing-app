﻿using Microsoft.AspNetCore.Http;
using StorageService.Api.DTOs;

namespace StorageService.Api.Services;

public interface IStorageService
{
    Task<UploadResponse> UploadAsync(IFormFile file);

    Task DeleteAsync(string storageKey);

    Task<string> GenerateSignedUrlAsync(string storageKey);

    Task<bool> ExistsAsync(string storageKey);
}