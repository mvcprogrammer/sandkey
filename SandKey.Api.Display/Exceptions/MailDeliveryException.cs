namespace SandKey.Api.Display.Exceptions;

/// <summary>
/// Thrown when the mail provider cannot be reached or refuses a message. Surfaces as 502 or 504.
/// </summary>
public sealed class MailDeliveryException : Exception
{
    /// <summary>Creates the exception with a default message.</summary>
    public MailDeliveryException()
        : this("The message could not be delivered.")
    {
    }

    /// <summary>Creates the exception with a message.</summary>
    /// <param name="message">Description of the failure. Must not contain the recipient address.</param>
    public MailDeliveryException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and the failure that caused it.</summary>
    /// <param name="message">Description of the failure. Must not contain the recipient address.</param>
    /// <param name="innerException">The underlying transport failure.</param>
    public MailDeliveryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Status the provider answered with, when it answered at all.</summary>
    public int? UpstreamStatusCode { get; init; }

    /// <summary>True when the request exceeded its timeout rather than failing outright.</summary>
    public bool TimedOut { get; init; }
}
