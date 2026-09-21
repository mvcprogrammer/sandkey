using System.ComponentModel.DataAnnotations;

namespace SandKey.Api.Display.Requests;

/// <summary>
/// A request from a kiosk visitor to be contacted about a listing, either by email or by phone.
/// </summary>
/// <remarks>
/// The legacy application accepted the visitor's email address and telephone number as path
/// segments of a GET request, which put personal information into every access log and made a
/// state-changing operation look idempotent. Both now travel in the body of a POST.
/// </remarks>
public sealed record InquiryRequest : IValidatableObject
{
    private readonly string? _emailAddress;
    private readonly string? _phoneNumber;

    /// <summary>Listing the visitor is asking about.</summary>
    [Required]
    [MinLength(1)]
    public required string ListingKey { get; init; }

    /// <summary>
    /// Address to send the listing details to. Supply this or <see cref="PhoneNumber"/>,
    /// not both. A blank value is treated as absent, because the on-screen keyboard posts an
    /// empty field rather than omitting it.
    /// </summary>
    [EmailAddress]
    [MaxLength(254)]
    public string? EmailAddress
    {
        get => _emailAddress;
        init => _emailAddress = Normalize(value);
    }

    /// <summary>
    /// Number for the office to call back on. Supply this or <see cref="EmailAddress"/>,
    /// not both. A blank value is treated as absent.
    /// </summary>
    [Phone]
    [MaxLength(32)]
    public string? PhoneNumber
    {
        get => _phoneNumber;
        init => _phoneNumber = Normalize(value);
    }

    /// <summary>
    /// Enforces that exactly one contact method is supplied, which decides whether the visitor
    /// receives listing details or the office receives a callback request.
    /// </summary>
    /// <param name="validationContext">Context supplied by the validation framework.</param>
    /// <returns>One result when neither or both contact methods are present; otherwise none.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if ((EmailAddress is not null) == (PhoneNumber is not null))
        {
            yield return new ValidationResult(
                "Supply either an email address or a phone number, but not both.",
                [nameof(EmailAddress), nameof(PhoneNumber)]);
        }
    }

    /// <summary>Treats a blank value as absent, and trims what remains.</summary>
    /// <param name="value">Value as supplied by the caller.</param>
    /// <returns>The trimmed value, or null when it carries nothing.</returns>
    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
