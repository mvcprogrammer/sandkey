namespace SandKey.Api.Display.Exceptions;

/// <summary>
/// Thrown when the Bridge feed cannot be reached, times out, or answers with a status the
/// application cannot interpret. Surfaces as 502 or 504.
/// </summary>
/// <remarks>
/// The legacy client caught every exception and returned <c>null</c>, which turned an upstream
/// outage into an empty result set that looked identical to "no listings match".
/// </remarks>
public sealed class BridgeUnavailableException : Exception
{
    /// <summary>Creates the exception with a default message.</summary>
    public BridgeUnavailableException()
        : this("The listing feed is unavailable.")
    {
    }

    /// <summary>Creates the exception with a message.</summary>
    /// <param name="message">Description of the failure. Must not contain the access token.</param>
    public BridgeUnavailableException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and the failure that caused it.</summary>
    /// <param name="message">Description of the failure. Must not contain the access token.</param>
    /// <param name="innerException">The underlying transport or parsing failure.</param>
    public BridgeUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Status code Bridge answered with, when it answered at all. Null for a timeout or a
    /// transport failure. Lets a caller distinguish an upstream fault from a timeout without
    /// parsing the message.
    /// </summary>
    public int? UpstreamStatusCode { get; init; }

    /// <summary>True when the request exceeded its timeout rather than failing outright.</summary>
    public bool TimedOut { get; init; }
}
