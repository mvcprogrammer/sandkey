namespace SandKey.Api.Display.Responses;

/// <summary>
/// One photo attached to a listing.
/// </summary>
public sealed record MediaResponse
{
    /// <summary>Display order within the listing's photo set, ascending.</summary>
    public required long Order { get; init; }

    /// <summary>
    /// Path to the photo, relative to the site root, for example
    /// <c>/media/735d922b/715185323/83dcefb7.jpeg</c>. Served by the CDN rather than by this API.
    /// </summary>
    public required string Url { get; init; }
}
