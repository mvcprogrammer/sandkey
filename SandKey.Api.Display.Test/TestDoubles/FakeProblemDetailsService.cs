using Microsoft.AspNetCore.Http;

namespace SandKey.Api.Display.Test.TestDoubles;

/// <summary>
/// Records the problem details written to it, so a handler's output can be asserted.
/// </summary>
/// <remarks>
/// Hand-written rather than substituted: arranging a <see cref="ValueTask{TResult}"/>-returning
/// method through a mocking library trips CA2012, and a fake this small is clearer anyway.
/// </remarks>
public sealed class FakeProblemDetailsService : IProblemDetailsService
{
    /// <summary>Every context written, in order.</summary>
    public List<ProblemDetailsContext> Written { get; } = [];

    /// <summary>Records the context and reports that it was written.</summary>
    /// <param name="context">Problem details produced by the handler.</param>
    /// <returns>Always true.</returns>
    public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Written.Add(context);

        return ValueTask.FromResult(true);
    }

    /// <summary>Records the context.</summary>
    /// <param name="context">Problem details produced by the handler.</param>
    /// <returns>A completed task.</returns>
    public ValueTask WriteAsync(ProblemDetailsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Written.Add(context);

        return ValueTask.CompletedTask;
    }
}
