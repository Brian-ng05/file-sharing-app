using Microsoft.AspNetCore.Mvc;
using StorageService.Api.DTOs;
using StorageService.Api.Services;
using Microsoft.Extensions.Logging;

namespace StorageService.Api.Controllers;

[ApiController]
[Route("api/objects")]
public class StorageController : ControllerBase
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    private readonly IStorageService _storageService;
    private readonly ILogger<StorageController> _logger;

    public StorageController(IStorageService storageService, ILogger<StorageController> logger)
    {
        _storageService = storageService;
        _logger = logger;
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

    [HttpGet("signed-url")]
    public async Task<IActionResult> GetSignedUrl([FromQuery] string? storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return BadRequest(new ObjectResponse
            {
                Success = false,
                Message = "StorageKey is required",
                StorageKey = storageKey
            });
        }

        try
        {
            var url = await _storageService.GenerateSignedUrlAsync(storageKey);

            return Ok(new SignedUrlResponse
            {
                Url = url
            });
        }
        catch (FileNotFoundException)
        {
            return NotFound(new ObjectResponse
            {
                Success = false,
                Message = "Object does not exist in storage.",
                StorageKey = storageKey
            });
        }
    }

    [HttpDelete("{*storageKey}")]
    public async Task<IActionResult> DeleteObject([FromRoute] string? storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return BadRequest(new ObjectResponse
            {
                Success = false,
                Message = "StorageKey is required.",
                StorageKey = storageKey
            });

        try
        {
            await _storageService.DeleteAsync(storageKey);

            return Ok(new ObjectResponse
            {
                Success = true,
                Message = "Object deleted successfully.",
                StorageKey = storageKey
            });
        }
        catch (FileNotFoundException)
        {
            return NotFound(new ObjectResponse
            {
                Success = false,
                Message = "Object does not exist in storage.",
                StorageKey = storageKey
            });
        }
    }
}