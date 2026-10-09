using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SandKey.Api.Display.Exceptions;
using SandKey.Api.Display.Interfaces;
using SandKey.Api.Display.Payloads;
using SandKey.Api.Display.Requests;

namespace SandKey.Api.Display.Clients;

/// <summary>
/// Reads listings from the Bridge Data Output feed over HTTP.
/// </summary>
/// <remarks>
/// Registered as a typed client so the handler, timeout, and resilience pipeline are owned by
/// <c>IHttpClientFactory</c>. The legacy client constructed and disposed an
/// <c>HttpClient</c> on every call, and swallowed every exception into a null return.
/// </remarks>
internal sealed partial class BridgeClient : IBridgeClient
{
    private readonly HttpClient _httpClient;
    private readonly IBridgeQueryFactory _queryFactory;
    private readonly ILogger<BridgeClient> _logger;

    /// <summary>Creates the client.</summary>
    /// <param name="httpClient">Configured client supplied by <c>IHttpClientFactory</c>.</param>
    /// <param name="queryFactory">Builds the relative URI for each request.</param>
    /// <param name="logger">Typed logger.</param>
    public BridgeClient(HttpClient httpClient, IBridgeQueryFactory queryFactory, ILogger<BridgeClient> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(queryFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _queryFactory = queryFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<BridgeResultPayload<List<BridgeListingPayload>>> GetListingsAsync(
        ListingsQueryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var uri = _queryFactory.CreateListingsUri(request);
        var payload = await GetAsync<BridgeResultPayload<List<BridgeListingPayload>>>(uri, cancellationToken);

        return payload ?? throw new BridgeUnavailableException("The listing feed returned an empty body.");
    }

    /// <inheritdoc/>
    public async Task<BridgeListingPayload?> GetListingAsync(string listingKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(listingKey);

        var uri = _queryFactory.CreateListingUri(listingKey);
        var payload = await GetAsync<BridgeResultPayload<BridgeListingPayload>>(uri, cancellationToken);

        return payload?.Bundle;
    }

    /// <summary>
    /// Issues the request and deserializes the envelope, translating every transport and parsing
    /// failure into <see cref="BridgeUnavailableException"/>. Nothing is logged at error level
    /// here; the exception handler is the single place an unhandled failure is logged.
    /// </summary>
    /// <typeparam name="T">Envelope type to deserialize into.</typeparam>
    /// <param name="uri">Relative request URI, including the access token.</param>
    /// <param name="cancellationToken">Token that aborts the call.</param>
    /// <returns>The envelope, or null when the feed answered 404.</returns>
    private async Task<T?> GetAsync<T>(Uri uri, CancellationToken cancellationToken)
    {
        LogRequesting(uri);

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.GetAsync(uri, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new BridgeUnavailableException("The listing feed could not be reached.", exception);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Cancellation the caller did not ask for is the resilience pipeline's timeout firing.
            throw new BridgeUnavailableException("The listing feed did not respond in time.", exception)
            {
                TimedOut = true
            };
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return default;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new BridgeUnavailableException("The listing feed returned an unexpected status.")
                {
                    UpstreamStatusCode = (int)response.StatusCode
                };
            }

            try
            {
                return await response.Content.ReadFromJsonAsync<T>(
                    BridgeJsonContext.Default.Options,
                    cancellationToken);
            }
            catch (JsonException exception)
            {
                throw new BridgeUnavailableException("The listing feed returned a body that could not be read.", exception);
            }
        }
    }

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Debug,
        Message = "Requesting listings from the feed: {RequestUri}")]
    private partial void LogRequesting(Uri requestUri);
}
