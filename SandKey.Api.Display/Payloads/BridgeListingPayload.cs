using System.Text.Json.Serialization;

namespace SandKey.Api.Display.Payloads;

/// <summary>
/// A listing as Bridge returns it. Field names follow the RESO Data Dictionary, so they are
/// PascalCase on the wire and match these property names without a naming policy.
/// </summary>
internal sealed record BridgeListingPayload
{
    /// <summary>Opaque Bridge identifier for the listing. Used for by-key lookups.</summary>
    public string ListingKey { get; init; } = string.Empty;

    /// <summary>MLS number shown to the public, for example <c>TB8412345</c>.</summary>
    public string ListingId { get; init; } = string.Empty;

    /// <summary>Advertised price. A monthly figure for a lease.</summary>
    public decimal? ListPrice { get; init; }

    /// <summary>Subdivision the property sits in.</summary>
    public string SubdivisionName { get; init; } = string.Empty;

    /// <summary>Street address as supplied by the listing office.</summary>
    public string UnparsedAddress { get; init; } = string.Empty;

    /// <summary>Total bedrooms.</summary>
    public int? BedroomsTotal { get; init; }

    /// <summary>Full bathrooms.</summary>
    public int? BathroomsFull { get; init; }

    /// <summary>Half bathrooms.</summary>
    public int? BathroomsHalf { get; init; }

    /// <summary>Heated living area in square feet.</summary>
    public decimal? LivingArea { get; init; }

    /// <summary>Year the property was built.</summary>
    public int? YearBuilt { get; init; }

    /// <summary>Whether the property has a garage. Null when the listing office left it blank.</summary>
    [JsonPropertyName("GarageYN")]
    public bool? GarageYn { get; init; }

    /// <summary>Whether the property is waterfront.</summary>
    [JsonPropertyName("WaterfrontYN")]
    public bool WaterfrontYn { get; init; }

    /// <summary>Broad property class, for example <c>Residential</c> or <c>Residential Lease</c>.</summary>
    public string PropertyType { get; init; } = string.Empty;

    /// <summary>Specific property type, for example <c>Condominium</c>.</summary>
    public string PropertySubType { get; init; } = string.Empty;

    /// <summary>Marketing description.</summary>
    public string PublicRemarks { get; init; } = string.Empty;

    /// <summary>Listing office, shown in the IDX attribution line.</summary>
    public string ListOfficeName { get; init; } = string.Empty;

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

    /// <summary>Photos and other attachments.</summary>
    public IReadOnlyList<BridgeMediaPayload> Media { get; init; } = [];

    /// <summary>
    /// When Bridge last refreshed the record. Returned even though it is not in the requested
    /// field list; see <c>docs/bridge-contract.md</c>.
    /// </summary>
    public DateTimeOffset BridgeModificationTimestamp { get; init; }
}
