namespace SandKey.Api.Display.Enumerations;

/// <summary>
/// The kind of listing to retrieve. Maps to the Bridge <c>PropertyType</c> filter.
/// </summary>
public enum ListingType
{
    /// <summary>No listing type was supplied. Treated as <see cref="Sale"/>.</summary>
    Unspecified = 0,

    /// <summary>Residential property offered for sale.</summary>
    Sale = 1,

    /// <summary>Residential property offered for lease.</summary>
    Lease = 2
}
