using Microsoft.Extensions.Diagnostics.HealthChecks;
using SandKey.Api.Display.Interfaces;
using SandKey.Api.Display.Requests;

namespace SandKey.Api.Display.HealthChecks;

/// <summary>
/// Reports whether the listing feed can be reached. Part 6 puts dependency checks behind
/// readiness, not liveness: a feed outage should stop traffic being routed here, not restart
/// the process.
/// </summary>
internal sealed class BridgeHealthCheck : IHealthCheck
{
    private static readonly ListingsQueryRequest _probe = new() { PageSize = 1 };

    private readonly IBridgeClient _bridgeClient;

    /// <summary>Creates the check.</summary>
    /// <param name="bridgeClient">Reads listings from the feed.</param>
    public BridgeHealthCheck(IBridgeClient bridgeClient)
    {
        ArgumentNullException.ThrowIfNull(bridgeClient);

        _bridgeClient = bridgeClient;
    }

    /// <summary>Issues the smallest request the feed accepts and reports the outcome.</summary>
    /// <param name="context">Check registration context.</param>
    /// <param name="cancellationToken">Token that aborts the check.</param>
    /// <returns>Healthy when the feed answered, unhealthy otherwise.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _bridgeClient.GetListingsAsync(_probe, cancellationToken);

            return HealthCheckResult.Healthy("The listing feed responded.");
        }
        catch (Exceptions.BridgeUnavailableException exception)
        {
            return HealthCheckResult.Unhealthy("The listing feed did not respond.", exception);
        }
    }
}
