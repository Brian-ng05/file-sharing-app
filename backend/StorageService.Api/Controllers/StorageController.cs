using Microsoft.AspNetCore.Mvc;
using StorageService.Api.DTOs;
using StorageService.Api.Services;

namespace StorageService.Api.Controllers;

[ApiController]
[Route("api/objects")]
public class StorageController : ControllerBase
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    private readonly IStorageService _storageService;

    public StorageController(IStorageService storageService)
    {
        _storageService = storageService;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UploadResponse>> Upload([FromForm] UploadRequest request)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        if (request.File.Length > MaxFileSize)
        {
            return StatusCode(
                StatusCodes.Status413PayloadTooLarge,
                "File size exceeds 10 MB limit");
        }

        var response = await _storageService.UploadAsync(request.File);

        return Ok(new UploadResponse
        {
            StorageKey = response.StorageKey
        });
    }

    [HttpGet("{storageKey}/signed-url")]
    public async Task<ActionResult<SignedUrlResponse>> GetSignedUrl(
        [FromRoute] string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return BadRequest("StorageKey is required");
        }

        var url = await _storageService.GenerateSignedUrlAsync(storageKey);

        return Ok(new SignedUrlResponse
        {
            Url = url
        });
    }

    [HttpDelete("{storageKey}")]
    public async Task<IActionResult> DeleteObject(
        [FromRoute] string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return BadRequest("StorageKey is required");
        }

        await _storageService.DeleteAsync(storageKey);

        return NoContent();
    }
}