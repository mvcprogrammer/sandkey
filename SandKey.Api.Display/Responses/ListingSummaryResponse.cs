namespace SandKey.Api.Display.Responses;

/// <summary>
/// A listing as shown in the kiosk results grid. Prices and areas are returned as numbers, not
/// preformatted strings, so the presentation layer owns formatting.
/// </summary>
public sealed record ListingSummaryResponse
{
    /// <summary>Opaque identifier. Use it to request the full listing.</summary>
    public required string ListingKey { get; init; }

    /// <summary>Advertised price. A monthly figure when the listing is a lease.</summary>
    public required decimal ListPrice { get; init; }

    /// <summary>Subdivision the property sits in.</summary>
    public required string SubdivisionName { get; init; }

    /// <summary>Broad property class, for example <c>Residential</c> or <c>Residential Lease</c>.</summary>
    public required string PropertyType { get; init; }

    /// <summary>Specific property type, for example <c>Condominium</c>.</summary>
    public required string PropertySubType { get; init; }

    /// <summary>Total bedrooms.</summary>
    public required int BedroomsTotal { get; init; }

    /// <summary>Full bathrooms.</summary>
    public required int BathroomsFull { get; init; }

    /// <summary>Heated living area in square feet.</summary>
    public required decimal LivingArea { get; init; }

    /// <summary>First photo, or null when the listing has none.</summary>
    public MediaResponse? PrimaryPhoto { get; init; }
}
