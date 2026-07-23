using FileService.Api.Dtos.UploadFileRequest;
using FileService.Api.Dtos;
using FileService.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileService.Api.Controllers
{
    [ApiController]
    [Route("files")]
    public class FilesController : ControllerBase
    {
        private readonly IFileService _service;
        private readonly ILogger<FilesController> _logger;

        public FilesController(
            IFileService service,
            ILogger<FilesController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> Upload(
            [FromForm] UploadFileRequest request)
        {
            try
            {
                var result = await _service.UploadAsync(request);

                return Ok(result);
            }
            catch (Exception ex) when (IsWellKnownMessage(ex.Message))
            {
                return MapExceptionToResult(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error during file upload.");

                return Problem(
                    title: "An unexpected error occurred.",
                    statusCode: 500);
            }
        }

        [HttpGet("{code}")]
        public async Task<IActionResult> Download(string code)
        {
            try
            {
                var signedUrl = await _service.DownloadAsync(code);

                return Redirect(signedUrl);
            }
            catch (Exception ex) when (IsWellKnownMessage(ex.Message))
            {
                return MapExceptionToResult(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error during file download for {Code}.", code);

                return Problem(
                    title: "An unexpected error occurred.",
                    statusCode: 500);
            }
        }

        [HttpDelete("{code}")]
        public async Task<IActionResult> Delete(string code)
        {
            try
            {
                await _service.DeleteAsync(code);

                return NoContent();
            }
            catch (Exception ex) when (IsWellKnownMessage(ex.Message))
            {
                return MapExceptionToResult(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error during file delete for {Code}.", code);

                return Problem(
                    title: "An unexpected error occurred.",
                    statusCode: 500);
            }
        }

        [HttpGet("expired")]
        public async Task<IActionResult> GetExpiredFiles()
        {
            try
            {
                var files = await _service.GetExpiredFilesAsync();

                var response = files.Select(f => new ExpiredFileDto
                {
                    Code = f.Code
                });

                return Ok(response);
            }
            catch (Exception ex) when (IsWellKnownMessage(ex.Message))
            {
                return MapExceptionToResult(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error while retrieving expired files.");

                return Problem(
                    title: "An unexpected error occurred.",
                    statusCode: 500);
            }
        }

        private static bool IsWellKnownMessage(string message)
        {
            return message switch
            {
                "File is required." or
                "Empty file." or
                "File exceeds 10MB limit." or
                "Invalid MIME type." or
                "Storage upload failed." or
                "Storage service returned an empty upload response." or
                "File not found." or
                "File expired." or
                "Download limit reached." or
                "Failed to generate signed URL." or
                "Storage service returned an empty signed url response."
                    => true,
                string msg when msg.StartsWith("Failed to delete storage object.")
                    => true,
                _ => false
            };
        }

        private IActionResult MapExceptionToResult(string message)
        {
            return message switch
            {
                "File is required." or
                "Empty file." or
                "Invalid MIME type."
                    => BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = message,
                        Status = 400
                    }),

                "File exceeds 10MB limit."
                    => StatusCode(413, new ProblemDetails
                    {
                        Title = "Payload Too Large",
                        Detail = message,
                        Status = 413
                    }),

                "File not found."
                    => NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = message,
                        Status = 404
                    }),

                "File expired." or
                "Download limit reached."
                    => StatusCode(410, new ProblemDetails
                    {
                        Title = "Gone",
                        Detail = message,
                        Status = 410
                    }),

                "Storage upload failed." or
                "Storage service returned an empty upload response." or
                "Failed to generate signed URL." or
                "Storage service returned an empty signed url response."
                    => Problem(
                        title: "Upstream Service Error",
                        detail: message,
                        statusCode: 502),

                _ when message.StartsWith("Failed to delete storage object.")
                    => Problem(
                        title: "Upstream Service Error",
                        detail: message,
                        statusCode: 502),

                _ => Problem(
                    title: "An unexpected error occurred.",
                    statusCode: 500)
            };
        }
    }
}
