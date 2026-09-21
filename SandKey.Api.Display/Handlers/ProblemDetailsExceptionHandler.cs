using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SandKey.Api.Display.Exceptions;

namespace SandKey.Api.Display.Handlers;

/// <summary>
/// Translates unhandled exceptions into RFC 9457 problem details.
/// </summary>
/// <remarks>
/// This is the single place an unhandled exception is logged, as Part 2 requires. Lower layers
/// throw and do not log, so a failure produces one log entry rather than one per layer. Exception
/// detail never reaches the caller; the correlation id is how a report is tied back to the logs.
/// </remarks>
internal sealed partial class ProblemDetailsExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<ProblemDetailsExceptionHandler> _logger;

    /// <summary>Creates the handler.</summary>
    /// <param name="problemDetailsService">Writes the problem details response.</param>
    /// <param name="logger">Typed logger.</param>
    public ProblemDetailsExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<ProblemDetailsExceptionHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(problemDetailsService);
        ArgumentNullException.ThrowIfNull(logger);

        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    /// <summary>Handles the exception by writing a problem details response.</summary>
    /// <param name="httpContext">The request being handled.</param>
    /// <param name="exception">The unhandled exception.</param>
    /// <param name="cancellationToken">Token that aborts the write.</param>
    /// <returns>True once the response has been written.</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var (statusCode, title) = Classify(exception);
        var correlationId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            LogServerFailure(exception, httpContext.Request.Method, httpContext.Request.Path.Value, statusCode, correlationId);
        }
        else
        {
            LogClientFailure(exception, httpContext.Request.Method, httpContext.Request.Path.Value, statusCode, correlationId);
        }

        httpContext.Response.StatusCode = statusCode;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Instance = httpContext.Request.Path,
                Extensions = { ["correlationId"] = correlationId }
            }
        });
    }

    /// <summary>
    /// Maps an exception to the status and title the caller sees. Part 4 requires this mapping to
    /// live in one place rather than being repeated in each controller.
    /// </summary>
    /// <param name="exception">The unhandled exception.</param>
    /// <returns>The status code and a title that leaks nothing about the failure.</returns>
    private static (int StatusCode, string Title) Classify(Exception exception) => exception switch
    {
        ListingNotFoundException => (StatusCodes.Status404NotFound, "Listing not found."),
        BridgeUnavailableException { TimedOut: true } => (StatusCodes.Status504GatewayTimeout, "The listing feed timed out."),
        BridgeUnavailableException => (StatusCodes.Status502BadGateway, "The listing feed is unavailable."),
        MailDeliveryException { TimedOut: true } => (StatusCodes.Status504GatewayTimeout, "The mail provider timed out."),
        MailDeliveryException => (StatusCodes.Status502BadGateway, "The mail provider is unavailable."),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
    };

    [LoggerMessage(
        EventId = 3000,
        Level = LogLevel.Error,
        Message = "Request {Method} {Path} failed with {StatusCode}. Correlation {CorrelationId}")]
    private partial void LogServerFailure(
        Exception exception,
        string method,
        string? path,
        int statusCode,
        string correlationId);

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Warning,
        Message = "Request {Method} {Path} rejected with {StatusCode}. Correlation {CorrelationId}")]
    private partial void LogClientFailure(
        Exception exception,
        string method,
        string? path,
        int statusCode,
        string correlationId);
}
