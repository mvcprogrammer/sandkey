using System.ComponentModel.DataAnnotations;

namespace SandKey.Api.Display.Configurations;

/// <summary>
/// Settings for outbound inquiry mail, sent through the Mailgun HTTP API.
/// </summary>
public sealed class MailOptions
{
    /// <summary>The configuration section these options bind to.</summary>
    public const string SECTION_NAME = "Mail";

    /// <summary>Base address of the Mailgun API.</summary>
    [Required]
    public Uri BaseAddress { get; set; } = new("https://api.mailgun.net/v3/");

    /// <summary>Mailgun API key. Supplied from AWS Parameter Store and never logged.</summary>
    [Required]
    [MinLength(1)]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Mailgun sending domain.</summary>
    [Required]
    [MinLength(1)]
    public string Domain { get; set; } = string.Empty;

    /// <summary>Address inquiries are sent from, and the address contact requests are delivered to.</summary>
    [Required]
    [EmailAddress]
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>
    /// Addresses blind-copied on every inquiry. Held in Parameter Store rather than source
    /// control because they identify individuals.
    /// </summary>
    public IReadOnlyList<string> BccAddresses { get; set; } = [];

    /// <summary>How long to wait for Mailgun before giving up.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "00:00:30")]
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Brokerage details appended to the body of a sale inquiry.</summary>
    [Required]
    public OfficeOptions Office { get; set; } = new();
}
