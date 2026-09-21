using System.Text.Json.Serialization;

namespace SandKey.Api.Display.Payloads;

/// <summary>
/// The envelope every Bridge response arrives in. <typeparamref name="T"/> is a single listing
/// for a by-key request and a list of listings for a collection request.
/// </summary>
/// <typeparam name="T">Shape of the <c>bundle</c> member.</typeparam>
internal sealed record BridgeResultPayload<T>
{
    /// <summary>Whether Bridge considered the request successful.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    /// <summary>Status code Bridge reports inside the body, which need not match the HTTP status.</summary>
    [JsonPropertyName("status")]
    public int Status { get; init; }

    /// <summary>The payload itself.</summary>
    [JsonPropertyName("bundle")]
    public T? Bundle { get; init; }

    /// <summary>
    /// Total matching records, ignoring paging. The legacy application parsed this and then
    /// never used it, which is why the kiosk's Next button pages past the end of the results.
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; init; }
}
