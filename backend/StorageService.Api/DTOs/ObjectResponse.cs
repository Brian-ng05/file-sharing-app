namespace StorageService.Api.DTOs;

public class ObjectResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? StorageKey { get; set; }
}
