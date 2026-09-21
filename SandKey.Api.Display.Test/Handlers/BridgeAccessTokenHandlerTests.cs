using System.Net;
using Microsoft.Extensions.Options;
using SandKey.Api.Display.Configurations;
using SandKey.Api.Display.Handlers;
using SandKey.Api.Display.Test.TestDoubles;

namespace SandKey.Api.Display.Test.Handlers;

/// <summary>
/// Verifies that the access token is attached at the last possible moment. Bridge takes its
/// credential in the query string, which is how the legacy service ended up writing it to stdout;
/// confining it to this handler is what keeps every logged URI clean.
/// </summary>
public sealed class BridgeAccessTokenHandlerTests
{
    private const string AccessToken = "test-access-token";

    #region Constructor Tests

    /// <summary>Verifies that the handler rejects a null options accessor.</summary>
    [Fact]
    public void BridgeAccessTokenHandler_ShouldThrowArgumentNullException_WhenOptionsIsNull()
    {
        // Arrange, Act
        var act = () => new BridgeAccessTokenHandler(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    #endregion

    #region SendAsync Tests

    /// <summary>Verifies that the token is appended to a URI that already has a query.</summary>
    [Fact]
    public async Task SendAsync_ShouldAppendTheAccessToken_ToAUriWithAQuery()
    {
        // Arrange
        using var stub = StubHttpMessageHandler.RespondingWith(HttpStatusCode.OK);
        using var invoker = CreateInvoker(stub);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://feed.example.com/api/v2/stellar/listings?limit=9");

        // Act
        using var response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        var sent = Assert.Single(stub.Requests).RequestUri!.AbsoluteUri;
        Assert.Contains("limit=9", sent, StringComparison.Ordinal);
        Assert.Contains($"access_token={AccessToken}", sent, StringComparison.Ordinal);
    }

    /// <summary>Verifies that the token is appended to a URI with no query at all.</summary>
    [Fact]
    public async Task SendAsync_ShouldAppendTheAccessToken_ToAUriWithoutAQuery()
    {
        // Arrange
        using var stub = StubHttpMessageHandler.RespondingWith(HttpStatusCode.OK);
        using var invoker = CreateInvoker(stub);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://feed.example.com/api/v2/stellar/listings");

        // Act
        using var response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        var sent = Assert.Single(stub.Requests).RequestUri!.AbsoluteUri;
        Assert.EndsWith($"?access_token={AccessToken}", sent, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that a request passing through twice is not given two tokens. A retry replays the
    /// request through the inner handlers, so appending unconditionally would produce a duplicate
    /// parameter the moment the resilience pipeline retried anything.
    /// </summary>
    [Fact]
    public async Task SendAsync_ShouldAppendTheAccessTokenOnce_WhenTheRequestIsReplayed()
    {
        // Arrange
        using var stub = StubHttpMessageHandler.RespondingWith(HttpStatusCode.OK);
        using var invoker = CreateInvoker(stub);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://feed.example.com/api/v2/stellar/listings?limit=9");

        // Act
        using var first = await invoker.SendAsync(request, CancellationToken.None);
        using var second = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        var sent = stub.Requests[^1].RequestUri!.AbsoluteUri;
        var occurrences = sent.Split("access_token=", StringSplitOptions.None).Length - 1;
        Assert.Equal(1, occurrences);
    }

    /// <summary>Verifies that a token needing escaping is escaped.</summary>
    [Fact]
    public async Task SendAsync_ShouldTransformATokenThatNeedsEscaping()
    {
        // Arrange
        using var stub = StubHttpMessageHandler.RespondingWith(HttpStatusCode.OK);
        using var invoker = CreateInvoker(stub, accessToken: "a b&c");
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://feed.example.com/api/v2/stellar/listings");

        // Act
        using var response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        var sent = Assert.Single(stub.Requests).RequestUri!.AbsoluteUri;
        Assert.Contains("access_token=a%20b%26c", sent, StringComparison.Ordinal);
    }

    #endregion

    #region Helper Methods

    /// <summary>Chains the handler under test onto a stubbed network.</summary>
    /// <param name="stub">Stub standing in for the network.</param>
    /// <param name="accessToken">Token the handler should append.</param>
    /// <returns>An invoker that runs the handler.</returns>
    private static HttpMessageInvoker CreateInvoker(
        StubHttpMessageHandler stub,
        string accessToken = AccessToken)
    {
        var options = Options.Create(new BridgeOptions { AccessToken = accessToken });
        var handler = new BridgeAccessTokenHandler(options) { InnerHandler = stub };

        return new HttpMessageInvoker(handler, disposeHandler: false);
    }

    #endregion
}
