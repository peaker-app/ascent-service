using System.Net;
using System.Net.Http.Headers;

namespace AscentService.IntegrationTests.Fakes;

internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string? _body;
    private readonly Exception? _transportFailure;

    private StubHttpMessageHandler(HttpStatusCode statusCode, string? body, Exception? transportFailure)
    {
        _statusCode = statusCode;
        _body = body;
        _transportFailure = transportFailure;
    }

    public static StubHttpMessageHandler Responding(HttpStatusCode statusCode, string? body = null) =>
        new(statusCode, body, null);

    public static StubHttpMessageHandler Failing(Exception transportFailure) =>
        new(HttpStatusCode.OK, null, transportFailure);

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_transportFailure is not null)
        {
            return Task.FromException<HttpResponseMessage>(_transportFailure);
        }

        HttpResponseMessage response = new(_statusCode);

        if (_body is not null)
        {
            response.Content = new StringContent(_body);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }

        return Task.FromResult(response);
    }
}
