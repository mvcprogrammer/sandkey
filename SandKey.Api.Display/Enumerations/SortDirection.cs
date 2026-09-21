namespace SandKey.Api.Display.Enumerations;

/// <summary>
/// The direction results are ordered in.
/// </summary>
public enum SortDirection
{
    /// <summary>No direction was supplied. Treated as <see cref="Descending"/>.</summary>
    Unspecified = 0,

    /// <summary>Lowest value first.</summary>
    Ascending = 1,

    /// <summary>Highest value first.</summary>
    Descending = 2
}
