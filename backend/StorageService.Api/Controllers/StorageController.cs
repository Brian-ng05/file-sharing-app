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

        try
        {
            var response = await _storageService.UploadAsync(request.File);

            return Ok(new UploadResponse
            {
                StorageKey = response.StorageKey
            });
        }
        catch (StorageOperationException ex)
        {
            return StorageProblem(ex);
        }
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
        catch (StorageOperationException ex)
        {
            return StorageProblem(ex);
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

        try
        {
            await _storageService.DeleteAsync(storageKey);

            return NoContent();
        }
        catch (StorageOperationException ex)
        {
            return StorageProblem(ex);
        }
    }

    private ObjectResult StorageProblem(StorageOperationException ex)
    {
        return Problem(
            title: "Storage request failed",
            detail: ex.Message,
            statusCode: (int)ex.StatusCode);
    }
}
