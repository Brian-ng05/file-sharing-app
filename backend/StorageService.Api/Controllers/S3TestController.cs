using Microsoft.AspNetCore.Mvc;
using StorageService.Api.Services;

namespace StorageService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class S3TestController : ControllerBase
    {
        private readonly IS3Service _s3Service;

        public S3TestController(IS3Service s3Service)
        {
            _s3Service = s3Service;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "No file uploaded" });
            }

            try
            {
                var key = await _s3Service.UploadFileAsync(file);
                return Ok(new { success = true, key = key, message = "Upload successful" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("url/{key}")]
        public async Task<IActionResult> GetUrl(string key)
        {
            try
            {
                var url = await _s3Service.GetPresignedUrlAsync(key);
                return Ok(new { url = url });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
