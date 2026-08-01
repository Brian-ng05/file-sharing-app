using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;

namespace FileService.Api.Dtos;

[ExcludeFromCodeCoverage]
public class UploadRequest
{
    public IFormFile File { get; set; } = null!;
}
