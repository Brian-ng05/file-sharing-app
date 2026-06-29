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
    public async Task<IActionResult> GetSignedUrl([FromQuery] string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return BadRequest("StorageKey is required");
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
            return NotFound();
        }
    }

    [HttpDelete("{*storageKey}")]
    public async Task<IActionResult> DeleteObject([FromRoute] string storageKey)
    {
        // Structured diagnostic logging to catch any encoding / routing mismatch
        _logger.LogInformation("[StorageController] DeleteObject called.");
        _logger.LogDebug("Route storageKey: '{StorageKey}'", storageKey);
        _logger.LogDebug("Request.Path: '{RequestPath}'", Request.Path);
        _logger.LogDebug("Request.QueryString: '{QueryString}'", Request.QueryString);

        if (string.IsNullOrWhiteSpace(storageKey))
            return BadRequest("StorageKey is required.");

        await _storageService.DeleteAsync(storageKey);

        return NoContent();
    }

    // Alternate delete endpoint that accepts storageKey via query string.
    // Use this when the storage key contains characters that make route-based
    // deletion unreliable (for example unencoded slashes).
    [HttpDelete]
    public async Task<IActionResult> DeleteObjectByQuery([FromQuery] string storageKey)
    {
        _logger.LogInformation("[StorageController] DeleteObjectByQuery called.");
        _logger.LogDebug("Query storageKey: '{StorageKey}'", storageKey);

        if (string.IsNullOrWhiteSpace(storageKey))
            return BadRequest("StorageKey is required.");

        await _storageService.DeleteAsync(storageKey);

        return NoContent();
    }
}