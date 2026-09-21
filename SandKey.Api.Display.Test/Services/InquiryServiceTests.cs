using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SandKey.Api.Display.Configurations;
using SandKey.Api.Display.Exceptions;
using SandKey.Api.Display.Interfaces;
using SandKey.Api.Display.Messages;
using SandKey.Api.Display.Requests;
using SandKey.Api.Display.Responses;
using SandKey.Api.Display.Services;

namespace SandKey.Api.Display.Test.Services;

/// <summary>
/// Verifies how an inquiry becomes an email, and that a visitor's contact details never reach
/// a log line.
/// </summary>
public sealed class InquiryServiceTests
{
    private const string VisitorEmail = "visitor@example.com";
    private const string VisitorPhone = "727-555-0100";
    private const string OfficeEmail = "info.request@example.com";

    #region Constructor Tests

    /// <summary>Verifies that each dependency is required.</summary>
    [Fact]
    public void InquiryService_ShouldThrowArgumentNullException_WhenADependencyIsNull()
    {
        // Arrange
        var listingService = Substitute.For<IListingService>();
        var emailClient = Substitute.For<IEmailClient>();
        var options = CreateOptions();
        var logger = new FakeLogger<InquiryService>();

        // Act
        var withoutListings = () => new InquiryService(null!, emailClient, options, logger);
        var withoutEmail = () => new InquiryService(listingService, null!, options, logger);
        var withoutOptions = () => new InquiryService(listingService, emailClient, null!, logger);
        var withoutLogger = () => new InquiryService(listingService, emailClient, options, null!);

        // Assert
        Assert.Throws<ArgumentNullException>(withoutListings);
        Assert.Throws<ArgumentNullException>(withoutEmail);
        Assert.Throws<ArgumentNullException>(withoutOptions);
        Assert.Throws<ArgumentNullException>(withoutLogger);
    }

    #endregion

    #region SubmitAsync Tests

    /// <summary>Verifies that an email inquiry is addressed to the visitor.</summary>
    [Fact]
    public async Task SubmitAsync_ShouldSendTheListingDetailsToTheVisitor()
    {
        // Arrange
        var emailClient = Substitute.For<IEmailClient>();
        var service = CreateService(CreateListing(), emailClient, out _);
        var request = new InquiryRequest { ListingKey = "key", EmailAddress = VisitorEmail };

        // Act
        await service.SubmitAsync(request, CancellationToken.None);

        // Assert
        var message = CaptureMessage(emailClient);
        Assert.Equal(VisitorEmail, message.To);
        Assert.Contains("TB8412345", message.Subject, StringComparison.Ordinal);
        Assert.Contains("1 Somewhere Drive", message.Body, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that a phone inquiry goes to the office rather than to the visitor, since the
    /// visitor asked to be called rather than emailed.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_ShouldSendTheCallbackRequestToTheOffice()
    {
        // Arrange
        var emailClient = Substitute.For<IEmailClient>();
        var service = CreateService(CreateListing(), emailClient, out _);
        var request = new InquiryRequest { ListingKey = "key", PhoneNumber = VisitorPhone };

        // Act
        await service.SubmitAsync(request, CancellationToken.None);

        // Assert
        var message = CaptureMessage(emailClient);
        Assert.Equal(OfficeEmail, message.To);
        Assert.Contains(VisitorPhone, message.Body, StringComparison.Ordinal);
        Assert.Contains("Contact Request", message.Subject, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that a sale inquiry carries the price formatted as the kiosk shows it, with no
    /// duplicated currency symbol. The legacy rental path prefixed a second dollar sign onto a
    /// string that already had one.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_ShouldTransformThePriceToTheKioskFormat()
    {
        // Arrange
        var emailClient = Substitute.For<IEmailClient>();
        var service = CreateService(CreateListing(), emailClient, out _);
        var request = new InquiryRequest { ListingKey = "key", EmailAddress = VisitorEmail };

        // Act
        await service.SubmitAsync(request, CancellationToken.None);

        // Assert
        var message = CaptureMessage(emailClient);
        Assert.Contains("Price: $37,500,000", message.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("$$", message.Body, StringComparison.Ordinal);
    }

    /// <summary>Verifies that a lease inquiry carries the monthly suffix, once.</summary>
    [Fact]
    public async Task SubmitAsync_ShouldTransformALeasePriceToAMonthlyFigure()
    {
        // Arrange
        var emailClient = Substitute.For<IEmailClient>();
        var listing = CreateListing() with { PropertyType = "Residential Lease", ListPrice = 20_000m };
        var service = CreateService(listing, emailClient, out _);
        var request = new InquiryRequest { ListingKey = "key", EmailAddress = VisitorEmail };

        // Act
        await service.SubmitAsync(request, CancellationToken.None);

        // Assert
        var message = CaptureMessage(emailClient);
        Assert.Contains("Price: $20,000/Month", message.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("$$", message.Body, StringComparison.Ordinal);
    }

    /// <summary>Verifies that the configured blind-copy addresses are applied.</summary>
    [Fact]
    public async Task SubmitAsync_ShouldUseTheConfiguredBlindCopyAddresses()
    {
        // Arrange
        var emailClient = Substitute.For<IEmailClient>();
        var service = CreateService(CreateListing(), emailClient, out _);
        var request = new InquiryRequest { ListingKey = "key", EmailAddress = VisitorEmail };

        // Act
        await service.SubmitAsync(request, CancellationToken.None);

        // Assert
        var message = CaptureMessage(emailClient);
        Assert.Equal(["manager@example.com"], message.Bcc);
    }

    /// <summary>Verifies that a successful send is recorded at information level.</summary>
    [Fact]
    public async Task SubmitAsync_ShouldLogTheListingId_OnSuccess()
    {
        // Arrange
        var service = CreateService(CreateListing(), Substitute.For<IEmailClient>(), out var logger);
        var request = new InquiryRequest { ListingKey = "key", EmailAddress = VisitorEmail };

        // Act
        await service.SubmitAsync(request, CancellationToken.None);

        // Assert
        var record = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Information, record.Level);
        Assert.Contains("TB8412345", record.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that neither the visitor's email address nor their telephone number is written to
    /// a log. This is the rule the legacy routes broke by putting both in the URL, where IIS
    /// logged every one.
    /// </summary>
    /// <param name="emailAddress">Address supplied, or null for a phone inquiry.</param>
    /// <param name="phoneNumber">Number supplied, or null for an email inquiry.</param>
    [Theory]
    [InlineData(VisitorEmail, null)]
    [InlineData(null, VisitorPhone)]
    public async Task SubmitAsync_ShouldLogNothingThatIdentifiesTheVisitor(
        string? emailAddress,
        string? phoneNumber)
    {
        // Arrange
        var service = CreateService(CreateListing(), Substitute.For<IEmailClient>(), out var logger);
        var request = new InquiryRequest
        {
            ListingKey = "key",
            EmailAddress = emailAddress,
            PhoneNumber = phoneNumber
        };

        // Act
        await service.SubmitAsync(request, CancellationToken.None);

        // Assert
        foreach (var record in logger.Collector.GetSnapshot())
        {
            Assert.DoesNotContain(VisitorEmail, record.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(VisitorPhone, record.Message, StringComparison.Ordinal);
        }
    }

    /// <summary>Verifies that an unknown listing key stops the inquiry before any mail is sent.</summary>
    [Fact]
    public async Task SubmitAsync_ShouldThrowListingNotFoundException_ForAnUnknownKey()
    {
        // Arrange
        var listingService = Substitute.For<IListingService>();
        listingService.GetListingAsync("missing", Arg.Any<CancellationToken>())
            .ThrowsAsync(new ListingNotFoundException());
        var emailClient = Substitute.For<IEmailClient>();
        var service = new InquiryService(
            listingService,
            emailClient,
            CreateOptions(),
            new FakeLogger<InquiryService>());
        var request = new InquiryRequest { ListingKey = "missing", EmailAddress = VisitorEmail };

        // Act
        var act = () => service.SubmitAsync(request, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ListingNotFoundException>(act);
        await emailClient.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that a provider failure propagates and is not logged as a success.</summary>
    [Fact]
    public async Task SubmitAsync_ShouldThrowMailDeliveryException_WhenTheProviderRejectsTheMessage()
    {
        // Arrange
        var emailClient = Substitute.For<IEmailClient>();
        emailClient.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new MailDeliveryException());
        var service = CreateService(CreateListing(), emailClient, out var logger);
        var request = new InquiryRequest { ListingKey = "key", EmailAddress = VisitorEmail };

        // Act
        var act = () => service.SubmitAsync(request, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<MailDeliveryException>(act);
        Assert.Empty(logger.Collector.GetSnapshot());
    }

    /// <summary>Verifies that the cancellation token reaches both dependencies.</summary>
    [Fact]
    public async Task SubmitAsync_ShouldForwardTheCancellationToken()
    {
        // Arrange
        var emailClient = Substitute.For<IEmailClient>();
        var listingService = Substitute.For<IListingService>();
        listingService.GetListingAsync("key", Arg.Any<CancellationToken>()).Returns(CreateListing());
        var service = new InquiryService(
            listingService,
            emailClient,
            CreateOptions(),
            new FakeLogger<InquiryService>());
        using var cancellation = new CancellationTokenSource();
        var request = new InquiryRequest { ListingKey = "key", EmailAddress = VisitorEmail };

        // Act
        await service.SubmitAsync(request, cancellation.Token);

        // Assert
        await listingService.Received(1).GetListingAsync("key", cancellation.Token);
        await emailClient.Received(1).SendAsync(Arg.Any<EmailMessage>(), cancellation.Token);
    }

    /// <summary>Verifies that a null request is rejected.</summary>
    [Fact]
    public async Task SubmitAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        var service = CreateService(CreateListing(), Substitute.For<IEmailClient>(), out _);

        // Act
        var act = () => service.SubmitAsync(null!, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    #endregion

    #region Helper Methods

    /// <summary>Builds the service over a listing service that returns one listing.</summary>
    /// <param name="listing">Listing the inquiry refers to.</param>
    /// <param name="emailClient">Mail client to use.</param>
    /// <param name="logger">Receives the fake logger, for log assertions.</param>
    /// <returns>The service under test.</returns>
    private static InquiryService CreateService(
        ListingResponse listing,
        IEmailClient emailClient,
        out FakeLogger<InquiryService> logger)
    {
        var listingService = Substitute.For<IListingService>();
        listingService.GetListingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(listing);
        logger = new FakeLogger<InquiryService>();

        return new InquiryService(listingService, emailClient, CreateOptions(), logger);
    }

    /// <summary>Builds mail settings with a recognisable signature block.</summary>
    /// <returns>The options accessor.</returns>
    private static IOptions<MailOptions> CreateOptions() => Options.Create(new MailOptions
    {
        ApiKey = "test-api-key",
        Domain = "example.com",
        FromAddress = OfficeEmail,
        BccAddresses = ["manager@example.com"],
        Office = new OfficeOptions
        {
            Name = "Test Brokerage",
            Phone = "727-555-0199",
            Website = "www.example.com",
            AddressLines = ["1 Test Street", "Clearwater Beach, Fl 33767"],
            ListingUrlTemplate = "https://www.example.com/property/{0}/"
        }
    });

    /// <summary>Builds the listing these tests send details for.</summary>
    /// <returns>A listing response.</returns>
    private static ListingResponse CreateListing() => new()
    {
        ListingKey = "key",
        ListingId = "TB8412345",
        ListPrice = 37_500_000m,
        SubdivisionName = "MANDALAY POINT SUB 1ST ADD",
        UnparsedAddress = "1 Somewhere Drive",
        PropertyType = "Residential",
        PropertySubType = "Single Family Residence",
        BedroomsTotal = 3,
        BathroomsFull = 3,
        BathroomsHalf = 1,
        LivingArea = 3434m,
        YearBuilt = 1974,
        IsWaterfront = true,
        PublicRemarks = "A description.",
        ListOfficeName = "A listing office.",
        LastModified = DateTimeOffset.UnixEpoch
    };

    /// <summary>Pulls the single message handed to the mail client.</summary>
    /// <param name="emailClient">The substitute that received it.</param>
    /// <returns>The message.</returns>
    private static EmailMessage CaptureMessage(IEmailClient emailClient)
    {
        var call = Assert.Single(emailClient.ReceivedCalls());

        return Assert.IsType<EmailMessage>(call.GetArguments()[0]);
    }

    #endregion
}
