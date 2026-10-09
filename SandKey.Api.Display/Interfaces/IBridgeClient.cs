using SandKey.Api.Display.Payloads;
using SandKey.Api.Display.Requests;

namespace SandKey.Api.Display.Interfaces;

/// <summary>
/// Reads listings from the Bridge Data Output feed.
/// </summary>
internal interface IBridgeClient
{
    /// <summary>Retrieves a page of listings matching the supplied filters.</summary>
    /// <param name="request">Filters, ordering, and paging.</param>
    /// <param name="cancellationToken">Token that aborts the call.</param>
    /// <returns>The Bridge envelope, including the total matching record count.</returns>
    Task<BridgeResultPayload<List<BridgeListingPayload>>> GetListingsAsync(
        ListingsQueryRequest request,
        CancellationToken cancellationToken);

    /// <summary>Retrieves a single listing.</summary>
    /// <param name="listingKey">Opaque Bridge listing key.</param>
    /// <param name="cancellationToken">Token that aborts the call.</param>
    /// <returns>The listing, or null when the key does not resolve.</returns>
    Task<BridgeListingPayload?> GetListingAsync(string listingKey, CancellationToken cancellationToken);
}
