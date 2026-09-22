using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using SandKey.Api.Display.Configurations;
using SandKey.Api.Display.Exceptions;
using SandKey.Api.Display.Interfaces;
using SandKey.Api.Display.Messages;

namespace SandKey.Api.Display.Clients;

/// <summary>
/// Delivers mail through the Mailgun HTTP API.
/// </summary>
/// <remarks>
/// Replaces the legacy FluentEmail and MailKit SMTP path. HTTP keeps the same outbound resilience
/// pipeline as the listing feed, avoids holding an SMTP connection open from a Lambda, and drops
/// three dependencies, one of which had published advisories against it.
/// </remarks>
internal sealed class MailgunEmailClient : IEmailClient
{
    private readonly HttpClient _httpClient;
    private readonly MailOptions _options;

    /// <summary>Creates the client.</summary>
    /// <param name="httpClient">Configured client supplied by <c>IHttpClientFactory</c>.</param>
    /// <param name="options">Mail settings, including the API key and sending domain.</param>
    public MailgunEmailClient(HttpClient httpClient, IOptions<MailOptions> options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        _httpClient = httpClient;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var fields = new List<KeyValuePair<string, string>>
        {
            new("from", _options.FromAddress),
            new("to", message.To),
            new("subject", message.Subject),
            new("text", message.Body)
        };

        foreach (var bcc in message.Bcc)
        {
            fields.Add(new KeyValuePair<string, string>("bcc", bcc));
        }

        using var content = new FormUrlEncodedContent(fields);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri($"{_options.Domain}/messages", UriKind.Relative));
        request.Content = content;

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"api:{_options.ApiKey}")));

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new MailDeliveryException("The mail provider could not be reached.", exception);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new MailDeliveryException("The mail provider did not respond in time.", exception)
            {
                TimedOut = true
            };
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new MailDeliveryException("The mail provider rejected the message.")
                {
                    UpstreamStatusCode = (int)response.StatusCode
                };
            }
        }
    }
}
