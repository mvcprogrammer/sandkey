using System.Net;

namespace SandKey.Api.Display.Test.TestDoubles;

/// <summary>
/// Stands in for the network at the bottom of an <see cref="HttpClient"/> pipeline, so client
/// behaviour can be asserted without issuing a real request.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;

    /// <summary>Creates a handler that runs the supplied responder for every request.</summary>
    /// <param name="responder">Produces the response, or throws to simulate a transport failure.</param>
    public StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
    {
        ArgumentNullException.ThrowIfNull(responder);

        _responder = responder;
    }

    /// <summary>Every request this handler has seen, in order.</summary>
    public List<HttpRequestMessage> Requests { get; } = [];

    /// <summary>Creates a handler that answers every request with the same status and body.</summary>
    /// <param name="statusCode">Status to answer with.</param>
    /// <param name="jsonBody">JSON body to answer with.</param>
    /// <returns>The configured handler.</returns>
    public static StubHttpMessageHandler RespondingWith(HttpStatusCode statusCode, string jsonBody = "{}") =>
        new((_, _) => Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json")
        }));

    /// <summary>Creates a handler that throws, simulating a transport failure.</summary>
    /// <param name="exception">Failure to throw.</param>
    /// <returns>The configured handler.</returns>
    public static StubHttpMessageHandler Throwing(Exception exception) =>
        new((_, _) => Task.FromException<HttpResponseMessage>(exception));

    /// <summary>Records the request, then delegates to the responder.</summary>
    /// <param name="request">The outbound request.</param>
    /// <param name="cancellationToken">Token that aborts the call.</param>
    /// <returns>The stubbed response.</returns>
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Requests.Add(request);

        return _responder(request, cancellationToken);
    }
}
