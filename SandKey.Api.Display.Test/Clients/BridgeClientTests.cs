using System.Net;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using SandKey.Api.Display.Clients;
using SandKey.Api.Display.Exceptions;
using SandKey.Api.Display.Interfaces;
using SandKey.Api.Display.Requests;
using SandKey.Api.Display.Test.TestDoubles;

namespace SandKey.Api.Display.Test.Clients;

/// <summary>
/// Verifies that every way the feed can fail produces a typed exception rather than a null
/// return. The legacy client caught everything and returned null, so an outage was
/// indistinguishable from "no listings match".
/// </summary>
public sealed class BridgeClientTests
{
    private const string LISTINGS_BODY =
        """{"success":true,"status":200,"total":42,"bundle":[{"ListingKey":"key-1","ListPrice":500000}]}""";

    private const string LISTING_BODY =
        """{"success":true,"status":200,"bundle":{"ListingKey":"key-1","ListingId":"TB1"}}""";

    #region Constructor Tests

    /// <summary>Verifies that each dependency is required.</summary>
    [Fact]
    public void BridgeClient_ShouldThrowArgumentNullException_WhenADependencyIsNull()
    {
        // Arrange
        using var httpClient = new HttpClient();
        IBridgeQueryFactory? queryFactory = Substitute.For<IBridgeQueryFactory>();
        var logger = new FakeLogger<BridgeClient>();

        // Act
        var withoutHttpClient = () => new BridgeClient(null!, queryFactory, logger);
        var withoutFactory = () => new BridgeClient(httpClient, null!, logger);
        var withoutLogger = () => new BridgeClient(httpClient, queryFactory, null!);

        // Assert
        Assert.Throws<ArgumentNullException>(withoutHttpClient);
        Assert.Throws<ArgumentNullException>(withoutFactory);
        Assert.Throws<ArgumentNullException>(withoutLogger);
    }

    #endregion

    #region GetListingsAsync Tests

    /// <summary>Verifies that a successful response is deserialized into the envelope.</summary>
    [Fact]
    public async Task GetListingsAsync_ShouldSerializeTheEnvelope()
    {
        // Arrange
        using var handler = StubHttpMessageHandler.RespondingWith(HttpStatusCode.OK, LISTINGS_BODY);
        var client = CreateClient(handler);

        // Act
        var payload = await client.GetListingsAsync(new ListingsQueryRequest(), CancellationToken.None);

        // Assert
        Assert.True(payload.Success);
        Assert.Equal(42, payload.Total);
        Assert.Equal("key-1", Assert.Single(payload.Bundle!).ListingKey);
    }

    /// <summary>Verifies that an upstream error status becomes a typed exception carrying it.</summary>
    /// <param name="statusCode">Status the feed answered with.</param>
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task GetListingsAsync_ShouldThrowBridgeUnavailableException_ForAnErrorStatus(
        HttpStatusCode statusCode)
    {
        // Arrange
        using var handler = StubHttpMessageHandler.RespondingWith(statusCode);
        var client = CreateClient(handler);

        // Act
        var act = () => client.GetListingsAsync(new ListingsQueryRequest(), CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<BridgeUnavailableException>(act);
        Assert.Equal((int)statusCode, exception.UpstreamStatusCode);
        Assert.False(exception.TimedOut, "An error status is not a timeout.");
    }

    /// <summary>Verifies that a transport failure becomes a typed exception.</summary>
    [Fact]
    public async Task GetListingsAsync_ShouldThrowBridgeUnavailableException_WhenTheFeedCannotBeReached()
    {
        // Arrange
        using var handler = StubHttpMessageHandler.Throwing(new HttpRequestException("no route to host"));
        var client = CreateClient(handler);

        // Act
        var act = () => client.GetListingsAsync(new ListingsQueryRequest(), CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<BridgeUnavailableException>(act);
        Assert.IsType<HttpRequestException>(exception.InnerException);
    }

    /// <summary>
    /// Verifies that a timeout is reported as a timeout rather than as a generic outage, so the
    /// handler can answer 504 instead of 502.
    /// </summary>
    [Fact]
    public async Task GetListingsAsync_ShouldThrowBridgeUnavailableException_WhenTheFeedTimesOut()
    {
        // Arrange
        using var handler = StubHttpMessageHandler.Throwing(new TaskCanceledException("timed out"));
        var client = CreateClient(handler);

        // Act
        var act = () => client.GetListingsAsync(new ListingsQueryRequest(), CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<BridgeUnavailableException>(act);
        Assert.True(exception.TimedOut, "A cancellation the caller did not request is a timeout.");
    }

    /// <summary>Verifies that an unreadable body becomes a typed exception.</summary>
    [Fact]
    public async Task GetListingsAsync_ShouldThrowBridgeUnavailableException_ForABodyThatCannotBeRead()
    {
        // Arrange
        using var handler = StubHttpMessageHandler.RespondingWith(HttpStatusCode.OK, "not json at all");
        var client = CreateClient(handler);

        // Act
        var act = () => client.GetListingsAsync(new ListingsQueryRequest(), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<BridgeUnavailableException>(act);
    }

    /// <summary>
    /// Verifies that cancellation the caller asked for surfaces as cancellation, not as an
    /// outage. Conflating the two would report a feed failure every time a visitor walked away.
    /// </summary>
    [Fact]
    public async Task GetListingsAsync_ShouldThrowOperationCanceledException_WhenTheCallerCancels()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        using var handler = new StubHttpMessageHandler((_, token) =>
        {
            cancellation.Cancel();
            token.ThrowIfCancellationRequested();

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        var client = CreateClient(handler);

        // Act
        var act = () => client.GetListingsAsync(new ListingsQueryRequest(), cancellation.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(act);
    }

    /// <summary>Verifies that a null request is rejected.</summary>
    [Fact]
    public async Task GetListingsAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        using var handler = StubHttpMessageHandler.RespondingWith(HttpStatusCode.OK, LISTINGS_BODY);
        var client = CreateClient(handler);

        // Act
        var act = () => client.GetListingsAsync(null!, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    #endregion

    #region GetListingAsync Tests

    /// <summary>Verifies that a single listing is unwrapped from its envelope.</summary>
    [Fact]
    public async Task GetListingAsync_ShouldSerializeTheListing()
    {
        // Arrange
        using var handler = StubHttpMessageHandler.RespondingWith(HttpStatusCode.OK, LISTING_BODY);
        var client = CreateClient(handler);

        // Act
        var listing = await client.GetListingAsync("key-1", CancellationToken.None);

        // Assert
        Assert.NotNull(listing);
        Assert.Equal("TB1", listing.ListingId);
    }

    /// <summary>
    /// Verifies that a 404 returns null rather than throwing, so the service can raise a
    /// not-found exception carrying the key instead.
    /// </summary>
    [Fact]
    public async Task GetListingAsync_ShouldReturnNull_WhenTheFeedAnswersNotFound()
    {
        // Arrange
        using var handler = StubHttpMessageHandler.RespondingWith(HttpStatusCode.NotFound);
        var client = CreateClient(handler);

        // Act
        var listing = await client.GetListingAsync("missing", CancellationToken.None);

        // Assert
        Assert.Null(listing);
    }

    /// <summary>Verifies that a missing listing key is rejected before any request is issued.</summary>
    /// <param name="listingKey">Key supplied by the caller.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetListingAsync_ShouldThrowArgumentException_WhenListingKeyIsMissing(string? listingKey)
    {
        // Arrange
        using var handler = StubHttpMessageHandler.RespondingWith(HttpStatusCode.OK, LISTING_BODY);
        var client = CreateClient(handler);

        // Act
        var act = () => client.GetListingAsync(listingKey!, CancellationToken.None);

        // Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(act);
        Assert.Empty(handler.Requests);
    }

    #endregion

    #region Helper Methods

    /// <summary>Builds the client over a stubbed network.</summary>
    /// <param name="handler">Stub standing in for the network.</param>
    /// <returns>The client under test.</returns>
    private static BridgeClient CreateClient(StubHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://feed.example.com/api/")
        };

        var queryFactory = Substitute.For<IBridgeQueryFactory>();
        queryFactory.CreateListingsUri(Arg.Any<ListingsQueryRequest>())
            .Returns(new Uri("v2/stellar/listings?limit=9", UriKind.Relative));
        queryFactory.CreateListingUri(Arg.Any<string>())
            .Returns(new Uri("v2/stellar/listings/key-1", UriKind.Relative));

        return new BridgeClient(httpClient, queryFactory, new FakeLogger<BridgeClient>());
    }

    #endregion
}
