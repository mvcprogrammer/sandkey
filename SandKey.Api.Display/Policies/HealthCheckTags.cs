namespace SandKey.Api.Display.Policies;

/// <summary>
/// Tags applied to health check registrations, used to split liveness from readiness.
/// </summary>
public static class HealthCheckTags
{
    /// <summary>
    /// Marks a check as part of readiness. Part 6 keeps dependency checks out of liveness so a
    /// failing dependency stops traffic being routed here rather than restarting the process.
    /// </summary>
    public const string READINESS = "ready";
}
