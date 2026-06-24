using Microsoft.AspNetCore.Mvc;
using StorageService.Api.DTOs;
using StorageService.Api.Services;

namespace StorageService.Api.Controllers;

[ApiController]
[Route("api/storage")]
public class StorageController : ControllerBase
{
    private readonly IStorageService _storageService;

    public StorageController(
        IStorageService storageService)
    {
        _storageService = storageService;
    }

    [HttpPost("upload")]
    public async Task<ActionResult<UploadResponse>>
        Upload(IFormFile file)
    {
        var key =
            await _storageService.UploadAsync(file);

        return Ok(new UploadResponse
        {
            StorageKey = key
        });
    }

    [HttpDelete("{*storageKey}")]
    public async Task<IActionResult>
        Delete(string storageKey)
    {
        await _storageService.DeleteAsync(storageKey);

        return NoContent();
    }

    [HttpGet("signed-url")]
    public async Task<ActionResult<SignedUrlResponse>>
        GetSignedUrl([FromQuery] string storageKey)
    {
        var url =
            await _storageService
                .GenerateSignedUrlAsync(storageKey);

        return Ok(new SignedUrlResponse
        {
            Url = url
        });
    }
}