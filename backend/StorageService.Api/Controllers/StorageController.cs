using Microsoft.AspNetCore.Mvc;
using StorageService.Api.DTOs;
using StorageService.Api.Services;

namespace StorageService.Api.Controllers;

[ApiController]
[Route("api/storage")]
public class StorageController : ControllerBase
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
    private readonly IStorageService _storageService;

    public StorageController(IStorageService storageService)
    {
        _storageService = storageService;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UploadResponse>> Upload([FromForm] UploadRequest request)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        if (request.File.Length > MaxFileSize)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, "File size exceeds 10 MB limit");
        }

        var storageKey = await _storageService.UploadAsync(request.File);
        return Ok(new UploadResponse { StorageKey = storageKey });
    }

    [HttpPost("signed-url")]
    public async Task<ActionResult<SignedUrlResponse>> GetSignedUrl([FromBody] SignedUrlRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.StorageKey))
        {
            return BadRequest("StorageKey is required");
        }

        var url = await _storageService.GenerateSignedUrlAsync(request.StorageKey);
        return Ok(new SignedUrlResponse { Url = url });
    }

    [HttpDelete("object")]
    public async Task<IActionResult> DeleteObject([FromBody] DeleteObjectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.StorageKey))
        {
            return BadRequest("StorageKey is required");
        }

        await _storageService.DeleteAsync(request.StorageKey);
        return NoContent();
    }
}