using SandKey.Api.Display.Requests;

namespace SandKey.Api.Display.Interfaces;

/// <summary>
/// Handles a kiosk visitor's request to be contacted about a listing.
/// </summary>
public interface IInquiryService
{
    /// <summary>
    /// Sends listing details to the visitor when an email address was supplied, or notifies the
    /// office when a telephone number was supplied.
    /// </summary>
    /// <param name="request">The inquiry, already validated.</param>
    /// <param name="cancellationToken">Token that aborts the call.</param>
    /// <returns>A task that completes once the provider has accepted the message.</returns>
    /// <exception cref="Exceptions.ListingNotFoundException">The listing key does not resolve.</exception>
    Task SubmitAsync(InquiryRequest request, CancellationToken cancellationToken);
}
