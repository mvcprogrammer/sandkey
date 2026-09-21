namespace SandKey.Api.Display.Policies;

/// <summary>
/// Names of the rate-limiting policies registered at startup.
/// </summary>
public static class RateLimitingPolicies
{
    /// <summary>
    /// Limits inquiry submissions. The endpoint is unauthenticated because the kiosk has no
    /// concept of a user, so a limit is what stops it being used to mail arbitrary addresses.
    /// </summary>
    public const string Inquiries = "inquiries";
}
