using System.Net;

namespace StorageService.Api.Services;

public sealed class StorageOperationException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public StorageOperationException(
        HttpStatusCode statusCode,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
