using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;
using SandKey.Api.Display.Configurations;
using SandKey.Api.Display.Interfaces;
using SandKey.Api.Display.Messages;
using SandKey.Api.Display.Requests;
using SandKey.Api.Display.Responses;

namespace SandKey.Api.Display.Services;

/// <summary>
/// Turns a kiosk inquiry into an email, either to the visitor or to the office.
/// </summary>
/// <remarks>
/// Replaces the legacy <c>EmailController</c>, which built these bodies inline and hard-coded the
/// brokerage's contact details in source. The details now come from configuration.
/// </remarks>
internal sealed partial class InquiryService : IInquiryService
{
    private const string LeaseMarker = "LEASE";

    private readonly IListingService _listingService;
    private readonly IEmailClient _emailClient;
    private readonly MailOptions _options;
    private readonly ILogger<InquiryService> _logger;

    /// <summary>Creates the service.</summary>
    /// <param name="listingService">Loads the listing the inquiry refers to.</param>
    /// <param name="emailClient">Delivers the message.</param>
    /// <param name="options">Mail settings, including the signature block.</param>
    /// <param name="logger">Typed logger.</param>
    public InquiryService(
        IListingService listingService,
        IEmailClient emailClient,
        IOptions<MailOptions> options,
        ILogger<InquiryService> logger)
    {
        ArgumentNullException.ThrowIfNull(listingService);
        ArgumentNullException.ThrowIfNull(emailClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _listingService = listingService;
        _emailClient = emailClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task SubmitAsync(InquiryRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await _listingService.GetListingAsync(request.ListingKey, cancellationToken);

        var message = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? BuildVisitorMessage(listing, request.EmailAddress!)
            : BuildOfficeMessage(listing, request.PhoneNumber);

        await _emailClient.SendAsync(message, cancellationToken);

        // The listing id is an MLS number, not personal information, so it is safe to log.
        // The visitor's address and telephone number are not, and never appear in a log line.
        LogInquirySent(listing.ListingId);
    }

    /// <summary>Builds the listing-details email sent to the visitor.</summary>
    /// <param name="listing">The listing being asked about.</param>
    /// <param name="emailAddress">Where to send it.</param>
    /// <returns>The message to deliver.</returns>
    private EmailMessage BuildVisitorMessage(ListingResponse listing, string emailAddress)
    {
        var isLease = listing.PropertyType.Contains(LeaseMarker, StringComparison.OrdinalIgnoreCase);

        var body = new StringBuilder()
            .Append("Address: ").AppendLine(listing.UnparsedAddress)
            .Append(listing.BedroomsTotal).Append(" beds, ")
            .Append(listing.BathroomsFull).Append(" full baths ")
            .Append(listing.BathroomsHalf).AppendLine(" half bath")
            .Append("Property Type: ").AppendLine(listing.PropertySubType)
            .Append("Price: ").AppendLine(FormatPrice(listing.ListPrice, isLease))
            .AppendLine();

        if (!isLease)
        {
            body.Append("More info link: ")
                .AppendLine(string.Format(CultureInfo.InvariantCulture, _options.Office.ListingUrlTemplate, listing.ListingId))
                .AppendLine();
        }

        body.AppendLine("Description:")
            .AppendLine(listing.PublicRemarks)
            .AppendLine();

        if (!isLease)
        {
            AppendSignature(body);
        }

        body.AppendLine("Information deemed reliable but not guaranteed. Parties are advised to verify.");

        var subject = isLease
            ? $"Rental Details Request: {listing.ListingId}"
            : $"Property Details Request from Sand Key Display: {listing.ListingId}";

        return new EmailMessage
        {
            To = emailAddress,
            Subject = subject,
            Body = body.ToString(),
            Bcc = _options.BccAddresses
        };
    }

    /// <summary>Builds the callback request sent to the office.</summary>
    /// <param name="listing">The listing being asked about.</param>
    /// <param name="phoneNumber">Number the visitor asked to be called on.</param>
    /// <returns>The message to deliver.</returns>
    private EmailMessage BuildOfficeMessage(ListingResponse listing, string phoneNumber)
    {
        var body = new StringBuilder()
            .Append("Contact request from ").AppendLine(phoneNumber)
            .AppendLine()
            .AppendLine(string.Format(CultureInfo.InvariantCulture, _options.Office.ListingUrlTemplate, listing.ListingId))
            .AppendLine()
            .Append("Address: ").AppendLine(listing.UnparsedAddress)
            .Append("Price: ").AppendLine(FormatPrice(listing.ListPrice, isLease: false));

        return new EmailMessage
        {
            To = _options.FromAddress,
            Subject = $"Contact Request from Sand Key Display: {listing.ListingId}",
            Body = body.ToString(),
            Bcc = _options.BccAddresses
        };
    }

    /// <summary>Appends the brokerage signature block.</summary>
    /// <param name="body">Body being built.</param>
    private void AppendSignature(StringBuilder body)
    {
        var office = _options.Office;

        body.AppendLine("Thank you, and I look forward to hearing from you.")
            .AppendLine()
            .Append("Office Manager - ").AppendLine(office.Name)
            .Append("Office: ").AppendLine(office.Phone)
            .AppendLine(_options.FromAddress)
            .AppendLine(office.Website)
            .AppendLine()
            .AppendLine(office.Name);

        foreach (var line in office.AddressLines)
        {
            body.AppendLine(line);
        }

        body.AppendLine();
    }

    /// <summary>
    /// Formats a price the way the kiosk does. Invariant culture keeps the output identical
    /// wherever the process happens to run.
    /// </summary>
    /// <param name="price">Amount to format.</param>
    /// <param name="isLease">Whether to append the monthly suffix.</param>
    /// <returns>The formatted price, for example <c>$37,500,000</c>.</returns>
    private static string FormatPrice(decimal price, bool isLease)
    {
        var formatted = $"${price.ToString("N0", CultureInfo.InvariantCulture)}";
        return isLease ? $"{formatted}/Month" : formatted;
    }

    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Information,
        Message = "Inquiry sent for listing {ListingId}")]
    private partial void LogInquirySent(string listingId);
}
