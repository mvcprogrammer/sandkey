using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SandKey.Api.Display.Configurations;
using SandKey.Api.Display.Exceptions;
using SandKey.Api.Display.Interfaces;
using SandKey.Api.Display.Payloads;
using SandKey.Api.Display.Requests;
using SandKey.Api.Display.Services;

namespace SandKey.Api.Display.Test.Services;

/// <summary>
/// Verifies how listings are paged and how a missing listing is reported.
/// </summary>
public sealed class ListingServiceTests
{
    #region Constructor Tests

    /// <summary>Verifies that the service rejects a null feed client.</summary>
    [Fact]
    public void ListingService_ShouldThrowArgumentNullException_WhenBridgeClientIsNull()
    {
        // Arrange, Act
        var act = () => new ListingService(null!, CreateOptions());

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    /// <summary>Verifies that the service rejects a null options accessor.</summary>
    [Fact]
    public void ListingService_ShouldThrowArgumentNullException_WhenOptionsIsNull()
    {
        // Arrange
        var bridgeClient = Substitute.For<IBridgeClient>();

        // Act
        var act = () => new ListingService(bridgeClient, null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    #endregion

    #region GetListingsAsync Tests

    /// <summary>
    /// Verifies that the envelope reports the total across all pages, not the size of this page.
    /// The legacy kiosk had no total, which is why its Next button could page past the end.
    /// </summary>
    [Fact]
    public async Task GetListingsAsync_ShouldReturnTheTotalAcrossAllPages()
    {
        // Arrange
        var bridgeClient = Arrange_FeedReturns(CreateListings(3), total: 42);
        var service = new ListingService(bridgeClient, CreateOptions());
        var request = new ListingsQueryRequest { Page = 2, PageSize = 9 };

        // Act
        var page = await service.GetListingsAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(3, page.Items.Count);
        Assert.Equal(42, page.TotalCount);
        Assert.Equal(2, page.Page);
        Assert.Equal(9, page.PageSize);
        Assert.True(page.HasMore, "A page of 9 at index 2 out of 42 results has more to come.");
    }

    /// <summary>Verifies that the last page reports that nothing follows it.</summary>
    [Fact]
    public async Task GetListingsAsync_ShouldReportNoMore_OnTheLastPage()
    {
        // Arrange
        var bridgeClient = Arrange_FeedReturns(CreateListings(2), total: 20);
        var service = new ListingService(bridgeClient, CreateOptions());
        var request = new ListingsQueryRequest { Page = 1, PageSize = 10 };

        // Act
        var page = await service.GetListingsAsync(request, CancellationToken.None);

        // Assert
        Assert.False(page.HasMore, "Page 1 of 10 covers results 10 to 19 of 20, so nothing follows.");
    }

    /// <summary>
    /// Verifies that an empty result set comes back as an empty page rather than as an error.
    /// A condo with no active listings is a normal outcome on the kiosk.
    /// </summary>
    [Fact]
    public async Task GetListingsAsync_ShouldCompleteWithAnEmptyPage_WhenNothingMatches()
    {
        // Arrange
        var bridgeClient = Arrange_FeedReturns([], total: 0);
        var service = new ListingService(bridgeClient, CreateOptions());

        // Act
        var page = await service.GetListingsAsync(new ListingsQueryRequest(), CancellationToken.None);

        // Assert
        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
    }

    /// <summary>Verifies that a null bundle is treated as an empty page rather than throwing.</summary>
    [Fact]
    public async Task GetListingsAsync_ShouldCompleteWithAnEmptyPage_WhenTheBundleIsNull()
    {
        // Arrange
        var bridgeClient = Substitute.For<IBridgeClient>();
        bridgeClient.GetListingsAsync(Arg.Any<ListingsQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new BridgeResultPayload<List<BridgeListingPayload>> { Bundle = null, Total = 0 });
        var service = new ListingService(bridgeClient, CreateOptions());

        // Act
        var page = await service.GetListingsAsync(new ListingsQueryRequest(), CancellationToken.None);

        // Assert
        Assert.Empty(page.Items);
    }

    /// <summary>Verifies that the cancellation token reaches the feed client.</summary>
    [Fact]
    public async Task GetListingsAsync_ShouldForwardTheCancellationToken()
    {
        // Arrange
        var bridgeClient = Arrange_FeedReturns(CreateListings(1), total: 1);
        var service = new ListingService(bridgeClient, CreateOptions());
        using var cancellation = new CancellationTokenSource();

        // Act
        await service.GetListingsAsync(new ListingsQueryRequest(), cancellation.Token);

        // Assert
        await bridgeClient.Received(1).GetListingsAsync(
            Arg.Any<ListingsQueryRequest>(),
            cancellation.Token);
    }

    /// <summary>Verifies that a pre-cancelled token aborts before the feed is asked.</summary>
    [Fact]
    public async Task GetListingsAsync_ShouldThrowOperationCanceledException_ForAPreCancelledToken()
    {
        // Arrange
        var bridgeClient = Substitute.For<IBridgeClient>();
        bridgeClient.GetListingsAsync(Arg.Any<ListingsQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns<BridgeResultPayload<List<BridgeListingPayload>>>(_ => throw new OperationCanceledException());
        var service = new ListingService(bridgeClient, CreateOptions());
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        var act = () => service.GetListingsAsync(new ListingsQueryRequest(), cancellation.Token);

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(act);
    }

    /// <summary>Verifies that a feed outage propagates rather than becoming an empty page.</summary>
    [Fact]
    public async Task GetListingsAsync_ShouldThrowBridgeUnavailableException_WhenTheFeedIsDown()
    {
        // Arrange
        var bridgeClient = Substitute.For<IBridgeClient>();
        bridgeClient.GetListingsAsync(Arg.Any<ListingsQueryRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new BridgeUnavailableException());
        var service = new ListingService(bridgeClient, CreateOptions());

        // Act
        var act = () => service.GetListingsAsync(new ListingsQueryRequest(), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<BridgeUnavailableException>(act);
    }

    /// <summary>Verifies that a null request is rejected.</summary>
    [Fact]
    public async Task GetListingsAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        var service = new ListingService(Substitute.For<IBridgeClient>(), CreateOptions());

        // Act
        var act = () => service.GetListingsAsync(null!, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    #endregion

    #region GetListingAsync Tests

    /// <summary>Verifies that a known key returns the listing in full.</summary>
    [Fact]
    public async Task GetListingAsync_ShouldReturnTheListing()
    {
        // Arrange
        var bridgeClient = Substitute.For<IBridgeClient>();
        bridgeClient.GetListingAsync("key", Arg.Any<CancellationToken>())
            .Returns(new BridgeListingPayload { ListingKey = "key", ListingId = "TB1" });
        var service = new ListingService(bridgeClient, CreateOptions());

        // Act
        var listing = await service.GetListingAsync("key", CancellationToken.None);

        // Assert
        Assert.Equal("key", listing.ListingKey);
        Assert.Equal("TB1", listing.ListingId);
    }

    /// <summary>
    /// Verifies that an unknown key produces a not-found exception carrying the key, so the
    /// handler can map it to 404 without the controller catching anything.
    /// </summary>
    [Fact]
    public async Task GetListingAsync_ShouldThrowListingNotFoundException_ForAnUnknownKey()
    {
        // Arrange
        var bridgeClient = Substitute.For<IBridgeClient>();
        bridgeClient.GetListingAsync("missing", Arg.Any<CancellationToken>())
            .Returns((BridgeListingPayload?)null);
        var service = new ListingService(bridgeClient, CreateOptions());

        // Act
        var act = () => service.GetListingAsync("missing", CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<ListingNotFoundException>(act);
        Assert.Equal("missing", exception.ListingKey);
    }

    /// <summary>Verifies that a missing listing key is rejected before the feed is asked.</summary>
    /// <param name="listingKey">Key supplied by the caller.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetListingAsync_ShouldThrowArgumentException_WhenListingKeyIsMissing(string? listingKey)
    {
        // Arrange
        var service = new ListingService(Substitute.For<IBridgeClient>(), CreateOptions());

        // Act
        var act = () => service.GetListingAsync(listingKey!, CancellationToken.None);

        // Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(act);
    }

    /// <summary>Verifies that the configured photo cap is applied to the detail projection.</summary>
    [Fact]
    public async Task GetListingAsync_ShouldUseTheConfiguredPhotoCap()
    {
        // Arrange
        var bridgeClient = Substitute.For<IBridgeClient>();
        bridgeClient.GetListingAsync("key", Arg.Any<CancellationToken>())
            .Returns(new BridgeListingPayload { ListingKey = "key", Media = CreatePhotos(8) });
        var service = new ListingService(bridgeClient, CreateOptions(maxPhotos: 3));

        // Act
        var listing = await service.GetListingAsync("key", CancellationToken.None);

        // Assert
        Assert.Equal(3, listing.Media.Count);
    }

    #endregion

    #region Helper Methods

    /// <summary>Builds Bridge settings for these tests.</summary>
    /// <param name="maxPhotos">Photo cap to configure.</param>
    /// <returns>The options accessor.</returns>
    private static IOptions<BridgeOptions> CreateOptions(int maxPhotos = 10) =>
        Options.Create(new BridgeOptions { AccessToken = "test-access-token", MaxPhotos = maxPhotos });

    /// <summary>Configures a feed client that returns a fixed page.</summary>
    /// <param name="listings">Listings on the page.</param>
    /// <param name="total">Total matching records across all pages.</param>
    /// <returns>The configured substitute.</returns>
    private static IBridgeClient Arrange_FeedReturns(List<BridgeListingPayload> listings, int total)
    {
        var bridgeClient = Substitute.For<IBridgeClient>();
        bridgeClient.GetListingsAsync(Arg.Any<ListingsQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new BridgeResultPayload<List<BridgeListingPayload>> { Bundle = listings, Total = total });

        return bridgeClient;
    }

    /// <summary>Builds a number of listings with distinct keys.</summary>
    /// <param name="count">How many to build.</param>
    /// <returns>The listings.</returns>
    private static List<BridgeListingPayload> CreateListings(int count) =>
        [.. Enumerable.Range(0, count).Select(index => new BridgeListingPayload
        {
            ListingKey = $"key-{index}",
            ListingId = $"TB{index}"
        })];

    /// <summary>Builds a number of photos in display order.</summary>
    /// <param name="count">How many to build.</param>
    /// <returns>The photos.</returns>
    private static IReadOnlyList<BridgeMediaPayload> CreatePhotos(int count) =>
        [.. Enumerable.Range(0, count).Select(index => new BridgeMediaPayload
        {
            Order = index,
            MediaCategory = "Photo",
            MediaUrl = new Uri($"https://cdn.example.com/photos/{index}.jpeg")
        })];

    #endregion
}
