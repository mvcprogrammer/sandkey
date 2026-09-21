using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SandKey.Api.Display.Interfaces;
using SandKey.Api.Display.Policies;
using SandKey.Api.Display.Requests;

namespace SandKey.Api.Display.Controllers;

/// <summary>
/// Requests from kiosk visitors to be sent listing details or to be called back.
/// </summary>
[ApiController]
[Route("api/display/[controller]")]
[Produces("application/json")]
public sealed class InquiriesController : ControllerBase
{
    private readonly IInquiryService _inquiryService;

    /// <summary>Creates the controller.</summary>
    /// <param name="inquiryService">Turns an inquiry into an outbound message.</param>
    public InquiriesController(IInquiryService inquiryService)
    {
        ArgumentNullException.ThrowIfNull(inquiryService);

        _inquiryService = inquiryService;
    }

    /// <summary>
    /// Submits an inquiry about a listing. Supplying an email address sends the listing details
    /// to the visitor; supplying a telephone number asks the office to call them back.
    /// </summary>
    /// <param name="request">The listing key and one contact method.</param>
    /// <param name="cancellationToken">Token that aborts the request.</param>
    /// <returns>No content. The response body is empty on success.</returns>
    /// <response code="202">The message was accepted by the mail provider.</response>
    /// <response code="400">The body is malformed, or neither or both contact methods were supplied.</response>
    /// <response code="404">No listing has that key.</response>
    /// <response code="429">Too many inquiries from this caller. Retry after the interval in the header.</response>
    /// <response code="502">The mail provider answered with an error.</response>
    /// <response code="504">The mail provider did not respond in time.</response>
    /// <remarks>
    /// The legacy application exposed this as three GET routes that carried the visitor's email
    /// address or telephone number as path segments. Both are personal information and Part 4
    /// forbids them in a URL, so they travel in the body here.
    /// </remarks>
    [HttpPost]
    [EnableRateLimiting(RateLimitingPolicies.INQUIRIES)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> SubmitInquiryAsync(
        [FromBody] InquiryRequest request,
        CancellationToken cancellationToken)
    {
        await _inquiryService.SubmitAsync(request, cancellationToken);

        return Accepted();
    }
}
