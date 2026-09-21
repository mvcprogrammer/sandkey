namespace SandKey.Api.Display.Responses;

/// <summary>
/// A condominium hotspot on the kiosk home screen.
/// </summary>
public sealed record CondoResponse
{
    /// <summary>Identifier used in the home screen image map and in the listings condo filter.</summary>
    public required int Id { get; init; }

    /// <summary>
    /// Subdivision name, shown as the condo screen heading and used as the listings filter.
    /// Several hotspots deliberately share a name, so two ids can return the same listings:
    /// 107, 108 and 109 are all South Beach, and 113 and 114 are both Lighthouse Towers.
    /// </summary>
    public required string Name { get; init; }
}
