using FileService.Api.Dtos.UploadFileRequest;
using FileService.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileService.Api.Controllers
{
    [ApiController]
    [Route("files")]
    public class FilesController : ControllerBase
    {
        private readonly IFileService _service;

        public FilesController(IFileService service)
        {
            _service = service;
        }

        [HttpPost]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> Upload(
            [FromForm] UploadFileRequest request)
        {
            var result = await _service.UploadAsync(request);

            return Ok(result);
        }

        [HttpGet("{code}")]
        public async Task<IActionResult> Download(string code)
        {
            var signedUrl = await _service.DownloadAsync(code);

            return Redirect(signedUrl);
        }

        [HttpDelete("{code}")]
        public async Task<IActionResult> Delete(string code)
        {
            await _service.DeleteAsync(code);

            return NoContent();
        }
    }
}