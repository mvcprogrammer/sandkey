using System.Net;
using Microsoft.AspNetCore.Http;
using SandKey.Api.Display.Extensions;

namespace SandKey.Api.Display.Test.Extensions;

/// <summary>
/// Verifies how the originating client is identified. The rate limiter partitions on this value,
/// so behind CloudFront it has to see the visitor, not the edge node.
/// </summary>
public sealed class HttpContextExtensionsTests
{
    private const string EdgeAddress = "10.0.0.1";

    private const string ClientAddress = "203.0.113.7";

    #region GetClientAddress Tests

    /// <summary>Verifies that a null context is rejected.</summary>
    [Fact]
    public void GetClientAddress_ShouldThrowArgumentNullException_WhenContextIsNull()
    {
        // Arrange, Act
        var act = () => HttpContextExtensions.GetClientAddress(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    /// <summary>Verifies that the first forwarded address wins over the connection address.</summary>
    [Fact]
    public void GetClientAddress_ShouldUseTheFirstForwardedAddress()
    {
        // Arrange
        var context = CreateContext(EdgeAddress, forwardedFor: $"{ClientAddress}, {EdgeAddress}");

        // Act
        var address = context.GetClientAddress();

        // Assert
        Assert.Equal(ClientAddress, address);
    }

    /// <summary>Verifies that surrounding whitespace in the header is ignored.</summary>
    [Fact]
    public void GetClientAddress_ShouldTransformAPaddedForwardedAddress()
    {
        // Arrange
        var context = CreateContext(EdgeAddress, forwardedFor: $"  {ClientAddress}  ");

        // Act
        var address = context.GetClientAddress();

        // Assert
        Assert.Equal(ClientAddress, address);
    }

    /// <summary>Verifies that the connection address is used when nothing was forwarded.</summary>
    [Fact]
    public void GetClientAddress_ShouldUseTheConnectionAddress_WhenNoHeaderIsPresent()
    {
        // Arrange
        var context = CreateContext(EdgeAddress, forwardedFor: null);

        // Act
        var address = context.GetClientAddress();

        // Assert
        Assert.Equal(EdgeAddress, address);
    }

    /// <summary>Verifies that an empty header does not mask the connection address.</summary>
    [Fact]
    public void GetClientAddress_ShouldUseTheConnectionAddress_WhenTheHeaderIsEmpty()
    {
        // Arrange
        var context = CreateContext(EdgeAddress, forwardedFor: " , ");

        // Act
        var address = context.GetClientAddress();

        // Assert
        Assert.Equal(EdgeAddress, address);
    }

    /// <summary>Verifies the fallback when neither source has an address.</summary>
    [Fact]
    public void GetClientAddress_ShouldReturnUnknown_WhenNothingIsAvailable()
    {
        // Arrange
        var context = CreateContext(remoteAddress: null, forwardedFor: null);

        // Act
        var address = context.GetClientAddress();

        // Assert
        Assert.Equal("unknown", address);
    }

    #endregion

    #region Helper Methods

    /// <summary>Builds a request context with the given connection and forwarded addresses.</summary>
    private static DefaultHttpContext CreateContext(string? remoteAddress, string? forwardedFor)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = remoteAddress is null ? null : IPAddress.Parse(remoteAddress);

        if (forwardedFor is not null)
        {
            context.Request.Headers["X-Forwarded-For"] = forwardedFor;
        }

        return context;
    }

    #endregion
}
