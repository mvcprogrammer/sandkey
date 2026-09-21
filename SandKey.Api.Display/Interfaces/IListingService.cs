using SandKey.Api.Display.Requests;
using SandKey.Api.Display.Responses;

namespace SandKey.Api.Display.Interfaces;

/// <summary>
/// Retrieves listings for the kiosk.
/// </summary>
public interface IListingService
{
    /// <summary>Retrieves a page of listings matching the supplied filters.</summary>
    /// <param name="request">Filters, ordering, and paging.</param>
    /// <param name="cancellationToken">Token that aborts the call.</param>
    /// <returns>A page of listing summaries.</returns>
    Task<PagedResponse<ListingSummaryResponse>> GetListingsAsync(
        ListingsQueryRequest request,
        CancellationToken cancellationToken);

    /// <summary>Retrieves one listing in full.</summary>
    /// <param name="listingKey">Opaque Bridge listing key.</param>
    /// <param name="cancellationToken">Token that aborts the call.</param>
    /// <returns>The listing.</returns>
    /// <exception cref="Exceptions.ListingNotFoundException">The key does not resolve to a listing.</exception>
    Task<ListingResponse> GetListingAsync(string listingKey, CancellationToken cancellationToken);
}
