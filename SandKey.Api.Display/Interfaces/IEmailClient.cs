using SandKey.Api.Display.Messages;

namespace SandKey.Api.Display.Interfaces;

/// <summary>
/// Delivers an email through an external provider.
/// </summary>
internal interface IEmailClient
{
    /// <summary>Hands the message to the provider.</summary>
    /// <param name="message">The message to deliver.</param>
    /// <param name="cancellationToken">Token that aborts the call.</param>
    /// <returns>A task that completes once the provider has accepted the message.</returns>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
