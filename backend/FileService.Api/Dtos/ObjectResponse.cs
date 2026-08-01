using System.Diagnostics.CodeAnalysis;

namespace FileService.Api.Dtos;

[ExcludeFromCodeCoverage]
public class ObjectResponse
{
    public string StorageKey { get; set; } = string.Empty;
}
