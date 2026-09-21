using System.ComponentModel.DataAnnotations;

namespace SandKey.Api.Display.Configurations;

/// <summary>
/// Brokerage contact details used in the signature block of an inquiry email.
/// The legacy application hard-coded these inside the controller.
/// </summary>
public sealed class OfficeOptions
{
    /// <summary>Brokerage name.</summary>
    [Required]
    [MinLength(1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Contact telephone number.</summary>
    [Required]
    [MinLength(1)]
    public string Phone { get; set; } = string.Empty;

    /// <summary>Public-facing website.</summary>
    [Required]
    [MinLength(1)]
    public string Website { get; set; } = string.Empty;

    /// <summary>Street address lines, rendered one per line.</summary>
    public IReadOnlyList<string> AddressLines { get; set; } = [];

    /// <summary>
    /// Template for the public listing page, with <c>{0}</c> replaced by the MLS listing id.
    /// </summary>
    [Required]
    [MinLength(1)]
    public string ListingUrlTemplate { get; set; } = string.Empty;
}
