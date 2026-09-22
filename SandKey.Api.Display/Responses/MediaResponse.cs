namespace SandKey.Api.Display.Responses;

/// <summary>
/// One photo attached to a listing.
/// </summary>
public sealed record MediaResponse
{
    /// <summary>Display order within the listing's photo set, ascending.</summary>
    public required long Order { get; init; }

    /// <summary>
    /// Absolute URL of the photo on the Bridge media CDN, for example
    /// <c>https://dvvjkgh94f2v6.cloudfront.net/735d922b/715185323/83dcefb7.jpeg</c>. The kiosk
    /// loads it directly; image bytes never pass through this API.
    /// </summary>
    public required string Url { get; init; }
}
