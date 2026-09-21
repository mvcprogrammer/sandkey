using System.ComponentModel.DataAnnotations;

namespace SandKey.Api.Display.Configurations;

/// <summary>
/// Settings for the Bridge Data Output (Stellar MLS) feed.
/// </summary>
public sealed class BridgeOptions
{
    /// <summary>The configuration section these options bind to.</summary>
    public const string SectionName = "Bridge";

    /// <summary>Base address of the Bridge API.</summary>
    [Required]
    public Uri BaseAddress { get; set; } = new("https://api.bridgedataoutput.com/api/");

    /// <summary>
    /// Bridge access token. Supplied from AWS Parameter Store, never from source control,
    /// and never written to a log.
    /// </summary>
    [Required]
    [MinLength(1)]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Dataset path segment, for example <c>v2/stellar/listings</c>.</summary>
    [Required]
    [MinLength(1)]
    public string ListingsPath { get; set; } = "v2/stellar/listings";

    /// <summary>Postal code the kiosk is restricted to. Clearwater Beach is 33767.</summary>
    [Required]
    [MinLength(1)]
    public string PostalCode { get; set; } = "33767";

    /// <summary>MLS status filter. Only active listings are shown on the kiosk.</summary>
    [Required]
    [MinLength(1)]
    public string MlsStatus { get; set; } = "Active";

    /// <summary>How long to wait for Bridge before giving up. Part 2 forbids relying on library defaults.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "00:00:30")]
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Maximum photos carried on a listing. The kiosk detail screen shows ten.</summary>
    [Range(1, 50)]
    public int MaxPhotos { get; set; } = 10;
}
