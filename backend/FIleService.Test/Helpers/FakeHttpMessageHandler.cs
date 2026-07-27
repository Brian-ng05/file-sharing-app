using Microsoft.AspNetCore.Http;
using Moq;

namespace FIleService.Test.Helpers;

/// <summary>
/// Reusable fake HttpMessageHandler for HTTP client tests.
/// </summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

    public FakeHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return _handler(request);
    }
}
