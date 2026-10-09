namespace SandKey.Api.Display.Responses;

/// <summary>
/// A page of results. Part 4 requires collections to be returned in an envelope rather than as an
/// unbounded array; this is the offset envelope used for the database-backed listing feed.
/// </summary>
/// <typeparam name="T">Type of the items on the page.</typeparam>
public sealed record PagedResponse<T>
{
    /// <summary>The items on this page.</summary>
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>
    /// Total items matching the query across all pages. Lets a caller tell that it has reached
    /// the end, which the legacy kiosk could not do.
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>Zero-based index of this page.</summary>
    public required int Page { get; init; }

    /// <summary>Maximum items on a page.</summary>
    public required int PageSize { get; init; }

    /// <summary>True when a further page exists.</summary>
    public bool HasMore => ((Page + 1) * PageSize) < TotalCount;
}
