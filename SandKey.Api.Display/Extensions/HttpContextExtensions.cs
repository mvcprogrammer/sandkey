using Microsoft.Extensions.Primitives;

namespace SandKey.Api.Display.Extensions;

/// <summary>
/// Extension methods on <see cref="HttpContext"/>.
/// </summary>
public static class HttpContextExtensions
{
    private const string ForwardedForHeader = "X-Forwarded-For";

    private const string UnknownAddress = "unknown";

    /// <summary>
    /// Resolves the address of the client that originated the request.
    /// </summary>
    /// <remarks>
    /// In production the API sits behind CloudFront, so the connection's remote address is an
    /// edge node and every visitor would share one rate-limit partition. The originating client
    /// is the first entry in <c>X-Forwarded-For</c>, which CloudFront always sets. The function
    /// URL accepts only requests signed by that distribution, which is what makes the header
    /// trustworthy here; a request with no such header falls back to the connection.
    /// </remarks>
    /// <param name="context">The current request.</param>
    /// <returns>The client address, or <c>unknown</c> when neither source has one.</returns>
    public static string GetClientAddress(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Request.Headers.TryGetValue(ForwardedForHeader, out StringValues forwardedFor))
        {
            return context.Connection.RemoteIpAddress?.ToString() ?? UnknownAddress;
        }

        var first = forwardedFor.ToString()
            .Split(',', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        if (!string.IsNullOrEmpty(first))
        {
            return first;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? UnknownAddress;
    }
}
