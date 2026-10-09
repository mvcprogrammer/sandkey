namespace SandKey.Api.Display.Payloads;

/// <summary>
/// One media item attached to a Bridge listing. Categories other than <c>Photo</c> appear here,
/// so the collection is filtered before it reaches a response model.
/// </summary>
internal sealed record BridgeMediaPayload
{
    /// <summary>Display order within the listing's media set.</summary>
    public long Order { get; init; }

    /// <summary>Absolute URL of the item on the Bridge media CDN.</summary>
    public Uri? MediaUrl { get; init; }

    /// <summary>Kind of media, for example <c>Photo</c>.</summary>
    public string? MediaCategory { get; init; }
}
