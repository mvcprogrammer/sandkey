using Amazon.Lambda.AspNetCoreServer.Hosting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using SandKey.Api.Display.Extensions;
using SandKey.Api.Display.Policies;

var builder = WebApplication.CreateBuilder(args);

// Part 6: configuration comes from environment variables and Parameter Store. appsettings.json
// carries defaults only; the Bridge access token, the Mailgun API key, and the blind-copy
// addresses are never in source control.
var parameterStorePath = builder.Configuration["Aws:ParameterStorePath"];

if (!string.IsNullOrWhiteSpace(parameterStorePath))
{
    builder.Configuration.AddSystemsManager(parameterStorePath, TimeSpan.FromMinutes(5));
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDisplayOptions(builder.Configuration);
builder.Services.AddDisplayServices();
builder.Services.AddDisplayHttpClients();
builder.Services.AddDisplayRateLimiting();
builder.Services.AddDisplayHealthChecks();
builder.Services.AddDisplayObservability(builder.Configuration);

// No-op outside Lambda, so the same build runs locally under Kestrel.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

var app = builder.Build();

// Part 4: the single place an unhandled exception is turned into a response, and logged.
app.UseExceptionHandler();
app.UseRateLimiter();

// TLS terminates at CloudFront and the kiosk is served over the edge, so there is no
// HTTPS redirection here. No authentication scheme is registered either; see the README for
// why Part 3 does not apply to this service.
app.MapControllers();

// Liveness reports process health only. Readiness verifies the listing feed.
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = static _ => false });
app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = static check => check.Tags.Contains(HealthCheckTags.READINESS)
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await app.RunAsync();

/// <summary>
/// Entry point. Declared partial so the test project can host the application in memory.
/// </summary>
public partial class Program;
