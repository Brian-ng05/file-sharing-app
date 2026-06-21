using FileService.Api.Dtos.UploadFileResponse;
using FileService.Api.Dtos.UploadFileRequest;

namespace FileService.Api.Services
{

    public interface IFileService
    {
        Task<UploadFileResponse> UploadAsync(
            UploadFileRequest request);

        Task<(byte[] Content, string FileName, string MimeType)>
            DownloadAsync(string code);

        Task DeleteAsync(string code);
    }
}
