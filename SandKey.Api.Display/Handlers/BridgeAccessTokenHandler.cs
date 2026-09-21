using Microsoft.Extensions.Options;
using SandKey.Api.Display.Configurations;

namespace SandKey.Api.Display.Handlers;

/// <summary>
/// Appends the Bridge access token to every outbound request.
/// </summary>
/// <remarks>
/// Bridge takes its credential as a query-string parameter, which makes it very easy to leak: the
/// legacy application built the token into the URI in the query factory and then wrote that URI
/// to stdout on every call. Appending it here instead means the token exists on the request for
/// exactly one handler's worth of pipeline, and every URI upstream of this point, including every
/// one that reaches a log, is free of it.
/// </remarks>
internal sealed class BridgeAccessTokenHandler : DelegatingHandler
{
    private const string AccessTokenParameter = "access_token";

    private readonly BridgeOptions _options;

    /// <summary>Creates the handler.</summary>
    /// <param name="options">Bridge settings, holding the access token.</param>
    public BridgeAccessTokenHandler(IOptions<BridgeOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
    }

    /// <summary>Adds the token, then passes the request down the pipeline.</summary>
    /// <param name="request">The outbound request.</param>
    /// <param name="cancellationToken">Token that aborts the call.</param>
    /// <returns>The response from the inner handler.</returns>
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.RequestUri is not null)
        {
            request.RequestUri = AppendAccessToken(request.RequestUri);
        }

        return base.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Adds the token to a URI unless it is already present. The check keeps this correct
    /// wherever the handler sits relative to the resilience pipeline: a retried request passes
    /// through the inner handlers again, and appending twice would produce a duplicate parameter.
    /// </summary>
    /// <param name="requestUri">URI to add the token to.</param>
    /// <returns>The URI including the access token.</returns>
    private Uri AppendAccessToken(Uri requestUri)
    {
        var builder = new UriBuilder(requestUri);
        var query = builder.Query.TrimStart('?');

        if (query.Contains(AccessTokenParameter, StringComparison.Ordinal))
        {
            return requestUri;
        }

        var separator = query.Length == 0 ? string.Empty : "&";
        builder.Query = $"{query}{separator}{AccessTokenParameter}={Uri.EscapeDataString(_options.AccessToken)}";

        return builder.Uri;
    }
}
