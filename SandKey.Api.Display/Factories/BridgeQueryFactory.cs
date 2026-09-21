using System.Text;
using Microsoft.Extensions.Options;
using SandKey.Api.Display.Configurations;
using SandKey.Api.Display.Enumerations;
using SandKey.Api.Display.Interfaces;
using SandKey.Api.Display.Requests;

namespace SandKey.Api.Display.Factories;

/// <summary>
/// Builds the relative Bridge URIs a kiosk request translates to.
/// </summary>
/// <remarks>
/// Replaces the legacy <c>RouteData</c> class hierarchy. The query shape is recorded in
/// <c>docs/bridge-contract.md</c> and asserted in <c>BridgeQueryFactoryTests</c>, because a silent
/// change here returns the wrong listings rather than failing.
/// </remarks>
internal sealed class BridgeQueryFactory : IBridgeQueryFactory
{
    /// <summary>
    /// Fields requested from Bridge. The first twenty-two are the legacy set. The last two are
    /// added deliberately: Bridge returns them today without being asked, and the detail screen
    /// reads both, so requesting them explicitly stops a feed change from silently blanking the
    /// "last refreshed" line and the waterfront flag.
    /// </summary>
    private static readonly string[] _fields =
    [
        "ListPrice",
        "SubdivisionName",
        "BedroomsTotal",
        "Media",
        "BathroomsFull",
        "BathroomsHalf",
        "LivingArea",
        "PetsAllowed",
        "ExteriorFeatures",
        "PoolFeatures",
        "InteriorFeatures",
        "WaterfrontFeatures",
        "CommunityFeatures",
        "YearBuilt",
        "GarageYN",
        "PublicRemarks",
        "PropertyType",
        "PropertySubType",
        "ListOfficeName",
        "UnparsedAddress",
        "ListingKey",
        "ListingId",
        "BridgeModificationTimestamp",
        "WaterfrontYN"
    ];

    /// <summary>Comma-joined field list. Bridge accepts the commas unescaped.</summary>
    private static readonly string _fieldList = string.Join(',', _fields);

    private readonly BridgeOptions _options;
    private readonly ICondoService _condoService;

    /// <summary>Creates the factory.</summary>
    /// <param name="options">Bridge settings, including the access token and the fixed filters.</param>
    /// <param name="condoService">Resolves a hotspot id to the subdivision it filters on.</param>
    public BridgeQueryFactory(IOptions<BridgeOptions> options, ICondoService condoService)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(condoService);

        _options = options.Value;
        _condoService = condoService;
    }

    /// <inheritdoc/>
    public Uri CreateListingsUri(ListingsQueryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // The legacy kiosk treats its route segment as a page index, not a record offset.
        var offset = request.Page * request.PageSize;

        var query = new StringBuilder(_options.ListingsPath)
            .Append("?offset=").Append(offset)
            .Append("&limit=").Append(request.PageSize)
            .Append("&PropertyType=").Append(Uri.EscapeDataString(ToPropertyType(request.Type)))
            .Append("&sortBy=ListPrice")
            .Append("&order=").Append(ToOrder(request.Order))
            .Append("&fields=").Append(_fieldList)
            .Append("&MlsStatus=").Append(Uri.EscapeDataString(_options.MlsStatus))
            .Append("&PostalCode=").Append(Uri.EscapeDataString(_options.PostalCode));

        var subdivisionName = _condoService.FindSubdivisionName(request.Condo);

        if (subdivisionName is not null)
        {
            query.Append("&SubdivisionName.in=").Append(Uri.EscapeDataString(subdivisionName));
        }

        return new Uri(query.ToString(), UriKind.Relative);
    }

    /// <inheritdoc/>
    public Uri CreateListingUri(string listingKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(listingKey);

        var query = $"{_options.ListingsPath}/{Uri.EscapeDataString(listingKey)}?fields={_fieldList}";

        return new Uri(query, UriKind.Relative);
    }

    private static string ToPropertyType(ListingType listingType) => listingType switch
    {
        ListingType.Lease => "Residential Lease",
        _ => "Residential"
    };

    private static string ToOrder(SortDirection sortDirection) => sortDirection switch
    {
        SortDirection.Ascending => "asc",
        _ => "desc"
    };
}
