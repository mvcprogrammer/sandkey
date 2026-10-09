namespace SandKey.Api.Display.Messages;

/// <summary>
/// One outbound email, independent of the provider that delivers it.
/// </summary>
internal sealed record EmailMessage
{
    /// <summary>Recipient address.</summary>
    public required string To { get; init; }

    /// <summary>Subject line.</summary>
    public required string Subject { get; init; }

    /// <summary>Plain-text body.</summary>
    public required string Body { get; init; }

    /// <summary>Addresses blind-copied on the message.</summary>
    public IReadOnlyList<string> Bcc { get; init; } = [];
}
