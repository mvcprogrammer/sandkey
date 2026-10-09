using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using SandKey.Api.Display.Exceptions;
using SandKey.Api.Display.Handlers;
using SandKey.Api.Display.Test.TestDoubles;

namespace SandKey.Api.Display.Test.Handlers;

/// <summary>
/// Verifies the one place an exception becomes a status code. Part 4 puts this mapping here
/// rather than in each controller, and Part 2 makes this the only layer that logs a failure.
/// </summary>
public sealed class ProblemDetailsExceptionHandlerTests
{
    #region Constructor Tests

    /// <summary>Verifies that each dependency is required.</summary>
    [Fact]
    public void ProblemDetailsExceptionHandler_ShouldThrowArgumentNullException_WhenADependencyIsNull()
    {
        // Arrange
        var problemDetailsService = new FakeProblemDetailsService();
        var logger = new FakeLogger<ProblemDetailsExceptionHandler>();

        // Act
        var withoutService = () => new ProblemDetailsExceptionHandler(null!, logger);
        var withoutLogger = () => new ProblemDetailsExceptionHandler(problemDetailsService, null!);

        // Assert
        Assert.Throws<ArgumentNullException>(withoutService);
        Assert.Throws<ArgumentNullException>(withoutLogger);
    }

    #endregion

    #region TryHandleAsync Tests

    /// <summary>
    /// Verifies the exception-to-status mapping, including that a timeout is distinguished from
    /// a general upstream failure.
    /// </summary>
    /// <param name="exception">Exception raised further down the stack.</param>
    /// <param name="expectedStatusCode">Status the caller should see.</param>
    [Theory]
    [MemberData(nameof(ExceptionMappings))]
    public async Task TryHandleAsync_ShouldTransformTheExceptionToAStatusCode(
        Exception exception,
        int expectedStatusCode)
    {
        // Arrange
        var context = new DefaultHttpContext();
        var handler = CreateHandler(out _);

        // Act
        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.True(handled, "The handler writes a response for every exception it is given.");
        Assert.Equal(expectedStatusCode, context.Response.StatusCode);
    }

    /// <summary>
    /// Verifies that a server failure is logged at error level, exactly once, with the exception
    /// attached so the stack trace is captured.
    /// </summary>
    [Fact]
    public async Task TryHandleAsync_ShouldLogOnceAtErrorLevel_ForAServerFailure()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var handler = CreateHandler(out var logger);
        var exception = new InvalidOperationException("something went wrong");

        // Act
        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        var record = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Error, record.Level);
        Assert.Same(exception, record.Exception);
    }

    /// <summary>
    /// Verifies that a caller error is logged at warning level rather than error, so a visitor
    /// tapping a stale listing does not register as a service fault.
    /// </summary>
    [Fact]
    public async Task TryHandleAsync_ShouldLogAtWarningLevel_ForACallerError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var handler = CreateHandler(out var logger);

        // Act
        await handler.TryHandleAsync(context, new ListingNotFoundException(), CancellationToken.None);

        // Assert
        var record = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Warning, record.Level);
    }

    /// <summary>
    /// Verifies that the response carries a correlation id and nothing about the exception
    /// itself. Part 4 forbids returning exception detail.
    /// </summary>
    [Fact]
    public async Task TryHandleAsync_ShouldIncludeACorrelationIdAndNoExceptionDetail()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var handler = CreateHandler(out _, out var captured);
        var exception = new InvalidOperationException("connection string is bad");

        // Act
        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        var problemDetails = captured.Single().ProblemDetails;
        Assert.True(problemDetails.Extensions.ContainsKey("correlationId"), "A caller needs an id to quote.");
        Assert.Equal("An unexpected error occurred.", problemDetails.Title);
        Assert.DoesNotContain("connection string", problemDetails.Title!, StringComparison.Ordinal);
        Assert.Null(problemDetails.Detail);
    }

    /// <summary>Verifies that a null context or exception is rejected.</summary>
    [Fact]
    public async Task TryHandleAsync_ShouldThrowArgumentNullException_WhenAnArgumentIsNull()
    {
        // Arrange
        var handler = CreateHandler(out _);
        var context = new DefaultHttpContext();

        // Act
        var withoutContext = async () =>
            await handler.TryHandleAsync(null!, new InvalidOperationException(), CancellationToken.None);
        var withoutException = async () => await handler.TryHandleAsync(context, null!, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(withoutContext);
        await Assert.ThrowsAsync<ArgumentNullException>(withoutException);
    }

    #endregion

    #region Helper Methods

    /// <summary>The exception-to-status mapping this service promises its callers.</summary>
    /// <returns>Each exception paired with the status it should produce.</returns>
    public static TheoryData<Exception, int> ExceptionMappings() => new()
    {
        { new ListingNotFoundException(), StatusCodes.Status404NotFound },
        { new BridgeUnavailableException(), StatusCodes.Status502BadGateway },
        { new BridgeUnavailableException { TimedOut = true }, StatusCodes.Status504GatewayTimeout },
        { new MailDeliveryException(), StatusCodes.Status502BadGateway },
        { new MailDeliveryException { TimedOut = true }, StatusCodes.Status504GatewayTimeout },
        { new InvalidOperationException(), StatusCodes.Status500InternalServerError }
    };

    /// <summary>Builds the handler over a problem details service that always succeeds.</summary>
    /// <param name="logger">Receives the fake logger, for log assertions.</param>
    /// <returns>The handler under test.</returns>
    private static ProblemDetailsExceptionHandler CreateHandler(
        out FakeLogger<ProblemDetailsExceptionHandler> logger) =>
        CreateHandler(out logger, out _);

    /// <summary>Builds the handler and captures the problem details it writes.</summary>
    /// <param name="logger">Receives the fake logger, for log assertions.</param>
    /// <param name="captured">Receives each problem details context written.</param>
    /// <returns>The handler under test.</returns>
    private static ProblemDetailsExceptionHandler CreateHandler(
        out FakeLogger<ProblemDetailsExceptionHandler> logger,
        out List<ProblemDetailsContext> captured)
    {
        var problemDetailsService = new FakeProblemDetailsService();

        logger = new FakeLogger<ProblemDetailsExceptionHandler>();
        captured = problemDetailsService.Written;

        return new ProblemDetailsExceptionHandler(problemDetailsService, logger);
    }

    #endregion
}
