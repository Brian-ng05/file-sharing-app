using StorageService.Api.Dtos;
using StorageService.Api.Models;
using StorageService.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace StorageService.Api.Controllers;

[ApiController]
[Route("storage")]
public class StorageController : ControllerBase
{
    private readonly IStorageService _storageService;
    private readonly AwsSettings _awsSettings;

    public StorageController(
        IStorageService storageService,
        IOptions<AwsSettings> awsSettings)
    {
        _storageService = storageService;
        _awsSettings = awsSettings.Value;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Empty file");

        var key = await _storageService.UploadFileAsync(file);

        return Ok(new UploadStorageResponse
        {
            Key = key,
            BucketName = _awsSettings.BucketName
        });
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> Download(string key)
    {
        var bytes = await _storageService.DownloadFileAsync(key);
        return File(bytes, "application/octet-stream");
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(string key)
    {
        await _storageService.DeleteFileAsync(key);
        return NoContent();
    }
}
