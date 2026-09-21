namespace SandKey.Api.Display.Enumerations;

/// <summary>
/// The field results are ordered by.
/// </summary>
public enum SortField
{
    /// <summary>No field was supplied. Treated as <see cref="LIST_PRICE"/>.</summary>
    UNSPECIFIED = 0,

    /// <summary>The advertised price of the listing.</summary>
    LIST_PRICE = 1
}
