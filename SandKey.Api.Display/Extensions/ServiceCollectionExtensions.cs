using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SandKey.Api.Display.Clients;
using SandKey.Api.Display.Configurations;
using SandKey.Api.Display.Factories;
using SandKey.Api.Display.HealthChecks;
using SandKey.Api.Display.Handlers;
using SandKey.Api.Display.Interfaces;
using SandKey.Api.Display.Policies;
using SandKey.Api.Display.Services;

namespace SandKey.Api.Display.Extensions;

/// <summary>
/// Registers the application's services on the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Service name reported to the telemetry collector.</summary>
    private const string ServiceName = "SandKey.Api.Display";

    /// <summary>
    /// Binds and validates configuration. Part 6 requires a misconfigured service to fail at
    /// startup rather than on the first request that needs the missing value.
    /// </summary>
    /// <param name="services">The container.</param>
    /// <param name="configuration">Configuration root.</param>
    /// <returns>The container, for chaining.</returns>
    public static IServiceCollection AddDisplayOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<BridgeOptions>()
            .Bind(configuration.GetSection(BridgeOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<MailOptions>()
            .Bind(configuration.GetSection(MailOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    /// <summary>
    /// Registers the application services. Everything is stateless, so nothing needs to be
    /// scoped to a request.
    /// </summary>
    /// <param name="services">The container.</param>
    /// <returns>The container, for chaining.</returns>
    public static IServiceCollection AddDisplayServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient<BridgeAccessTokenHandler>();
        services.AddSingleton<ICondoService, CondoService>();
        services.AddSingleton<IBridgeQueryFactory, BridgeQueryFactory>();
        services.AddScoped<IListingService, ListingService>();
        services.AddScoped<IInquiryService, InquiryService>();

        services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }

    /// <summary>
    /// Registers the outbound HTTP clients. Part 2 requires every client to come from
    /// <c>IHttpClientFactory</c> and to carry the standard resilience pipeline, which supplies
    /// the retry, circuit breaker, and per-attempt timeout.
    /// </summary>
    /// <param name="services">The container.</param>
    /// <returns>The container, for chaining.</returns>
    public static IServiceCollection AddDisplayHttpClients(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpClient<IBridgeClient, BridgeClient>(static (provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<BridgeOptions>>().Value;
                client.BaseAddress = options.BaseAddress;
                client.Timeout = options.Timeout;
            })
            .AddHttpMessageHandler<BridgeAccessTokenHandler>()
            .AddStandardResilienceHandler();

        services.AddHttpClient<IEmailClient, MailgunEmailClient>(static (provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<MailOptions>>().Value;
                client.BaseAddress = options.BaseAddress;
                client.Timeout = options.Timeout;
            })
            .AddStandardResilienceHandler();

        return services;
    }

    /// <summary>
    /// Limits how often a single caller can submit an inquiry. The endpoint is unauthenticated,
    /// so this is what prevents it being driven as a mail relay.
    /// </summary>
    /// <param name="services">The container.</param>
    /// <returns>The container, for chaining.</returns>
    public static IServiceCollection AddDisplayRateLimiting(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(RateLimitingPolicies.Inquiries, static context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.GetClientAddress(),
                    static _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(5),
                        QueueLimit = 0
                    }));

            options.OnRejected = static (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                return ValueTask.CompletedTask;
            };
        });

        return services;
    }

    /// <summary>
    /// Registers liveness and readiness checks. The feed check is tagged for readiness only.
    /// </summary>
    /// <param name="services">The container.</param>
    /// <returns>The container, for chaining.</returns>
    public static IServiceCollection AddDisplayHealthChecks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHealthChecks()
            .AddCheck<BridgeHealthCheck>("listing-feed", tags: [HealthCheckTags.Readiness]);

        return services;
    }

    /// <summary>
    /// Registers OpenTelemetry traces and metrics. The OTLP exporter is only wired up when a
    /// collector endpoint is configured, so local runs do not spend time trying to reach one.
    /// </summary>
    /// <param name="services">The container.</param>
    /// <param name="configuration">Configuration root.</param>
    /// <returns>The container, for chaining.</returns>
    public static IServiceCollection AddDisplayObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var hasCollector = !string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (hasCollector)
                {
                    tracing.AddOtlpExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (hasCollector)
                {
                    metrics.AddOtlpExporter();
                }
            });

        return services;
    }
}
