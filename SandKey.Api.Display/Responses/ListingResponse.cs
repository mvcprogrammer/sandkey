namespace SandKey.Api.Display.Responses;

/// <summary>
/// A listing in full, as shown on the kiosk detail screen.
/// </summary>
public sealed record ListingResponse
{
    /// <inheritdoc cref="ListingSummaryResponse.ListingKey"/>
    public required string ListingKey { get; init; }

    /// <summary>MLS number shown to the public, for example <c>TB8412345</c>.</summary>
    public required string ListingId { get; init; }

    /// <inheritdoc cref="ListingSummaryResponse.ListPrice"/>
    public required decimal ListPrice { get; init; }

    /// <inheritdoc cref="ListingSummaryResponse.SubdivisionName"/>
    public required string SubdivisionName { get; init; }

    /// <summary>Street address as supplied by the listing office.</summary>
    public required string UnparsedAddress { get; init; }

    /// <inheritdoc cref="ListingSummaryResponse.PropertyType"/>
    public required string PropertyType { get; init; }

    /// <inheritdoc cref="ListingSummaryResponse.PropertySubType"/>
    public required string PropertySubType { get; init; }

    /// <inheritdoc cref="ListingSummaryResponse.BedroomsTotal"/>
    public required int BedroomsTotal { get; init; }

    /// <inheritdoc cref="ListingSummaryResponse.BathroomsFull"/>
    public required int BathroomsFull { get; init; }

    /// <summary>Half bathrooms.</summary>
    public required int BathroomsHalf { get; init; }

    /// <inheritdoc cref="ListingSummaryResponse.LivingArea"/>
    public required decimal LivingArea { get; init; }

    /// <summary>Year the property was built.</summary>
    public required int YearBuilt { get; init; }

    /// <summary>Whether the property has a garage. Null when the listing office left it blank.</summary>
    public bool? HasGarage { get; init; }

    /// <summary>Whether the property is waterfront.</summary>
    public required bool IsWaterfront { get; init; }

    /// <summary>Marketing description.</summary>
    public required string PublicRemarks { get; init; }

    /// <summary>Listing office, shown in the IDX attribution line.</summary>
    public required string ListOfficeName { get; init; }

    /// <summary>Pet policy.</summary>
    public IReadOnlyList<string> PetsAllowed { get; init; } = [];

    /// <summary>Exterior features.</summary>
    public IReadOnlyList<string> ExteriorFeatures { get; init; } = [];

    /// <summary>Pool features.</summary>
    public IReadOnlyList<string> PoolFeatures { get; init; } = [];

    /// <summary>Interior features.</summary>
    public IReadOnlyList<string> InteriorFeatures { get; init; } = [];

    /// <summary>Waterfront features.</summary>
    public IReadOnlyList<string> WaterfrontFeatures { get; init; } = [];

    /// <summary>Community features.</summary>
    public IReadOnlyList<string> CommunityFeatures { get; init; } = [];

    /// <summary>Photos, ordered, capped at the configured maximum.</summary>
    public IReadOnlyList<MediaResponse> Media { get; init; } = [];

    /// <summary>When the feed last refreshed this record, for the IDX attribution line.</summary>
    public required DateTimeOffset LastModified { get; init; }
}
