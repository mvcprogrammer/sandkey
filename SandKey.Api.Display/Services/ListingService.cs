using Microsoft.Extensions.Options;
using SandKey.Api.Display.Configurations;
using SandKey.Api.Display.Exceptions;
using SandKey.Api.Display.Interfaces;
using SandKey.Api.Display.Mappers;
using SandKey.Api.Display.Requests;
using SandKey.Api.Display.Responses;

namespace SandKey.Api.Display.Services;

/// <summary>
/// Retrieves listings from the feed and shapes them for the kiosk.
/// </summary>
internal sealed class ListingService : IListingService
{
    private readonly IBridgeClient _bridgeClient;
    private readonly BridgeOptions _options;

    /// <summary>Creates the service.</summary>
    /// <param name="bridgeClient">Reads listings from the feed.</param>
    /// <param name="options">Bridge settings, for the photo cap.</param>
    public ListingService(IBridgeClient bridgeClient, IOptions<BridgeOptions> options)
    {
        ArgumentNullException.ThrowIfNull(bridgeClient);
        ArgumentNullException.ThrowIfNull(options);

        _bridgeClient = bridgeClient;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public async Task<PagedResponse<ListingSummaryResponse>> GetListingsAsync(
        ListingsQueryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var payload = await _bridgeClient.GetListingsAsync(request, cancellationToken);

        var items = (payload.Bundle ?? [])
            .Select(BridgeListingMapper.ToSummary)
            .ToList();

        return new PagedResponse<ListingSummaryResponse>
        {
            Items = items,
            TotalCount = payload.Total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    /// <inheritdoc/>
    public async Task<ListingResponse> GetListingAsync(string listingKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(listingKey);

        var payload = await _bridgeClient.GetListingAsync(listingKey, cancellationToken);

        if (payload is null)
        {
            throw new ListingNotFoundException("The listing was not found.")
            {
                ListingKey = listingKey
            };
        }

        return BridgeListingMapper.ToDetail(payload, _options.MaxPhotos);
    }
}
