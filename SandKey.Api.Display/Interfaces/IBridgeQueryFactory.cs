using SandKey.Api.Display.Requests;

namespace SandKey.Api.Display.Interfaces;

/// <summary>
/// Builds the relative Bridge URIs a request translates to. Kept separate from the client so the
/// query shape can be asserted in tests without issuing an HTTP call.
/// </summary>
internal interface IBridgeQueryFactory
{
    /// <summary>Builds the URI for a listings search.</summary>
    /// <param name="request">Filters, ordering, and paging.</param>
    /// <returns>A relative URI including the access token.</returns>
    Uri CreateListingsUri(ListingsQueryRequest request);

    /// <summary>Builds the URI for a single listing.</summary>
    /// <param name="listingKey">Opaque Bridge listing key.</param>
    /// <returns>A relative URI including the access token.</returns>
    Uri CreateListingUri(string listingKey);
}
