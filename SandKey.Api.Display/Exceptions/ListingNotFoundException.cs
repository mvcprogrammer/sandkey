namespace SandKey.Api.Display.Exceptions;

/// <summary>
/// Thrown when a listing key does not resolve to a listing. Surfaces as 404.
/// </summary>
public sealed class ListingNotFoundException : Exception
{
    /// <summary>Creates the exception with a default message.</summary>
    public ListingNotFoundException()
        : this("The listing was not found.")
    {
    }

    /// <summary>Creates the exception with a message.</summary>
    /// <param name="message">Description of the failure.</param>
    public ListingNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and the failure that caused it.</summary>
    /// <param name="message">Description of the failure.</param>
    /// <param name="innerException">The underlying failure.</param>
    public ListingNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// The listing key that was requested. A Bridge listing key is an opaque identifier and
    /// carries no personal information, so it is safe to log and to return in a problem detail.
    /// </summary>
    public string? ListingKey { get; init; }
}
