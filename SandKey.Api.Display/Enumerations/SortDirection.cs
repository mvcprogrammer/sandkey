namespace SandKey.Api.Display.Enumerations;

/// <summary>
/// The direction results are ordered in.
/// </summary>
public enum SortDirection
{
    /// <summary>No direction was supplied. Treated as <see cref="DESCENDING"/>.</summary>
    UNSPECIFIED = 0,

    /// <summary>Lowest value first.</summary>
    ASCENDING = 1,

    /// <summary>Highest value first.</summary>
    DESCENDING = 2
}
