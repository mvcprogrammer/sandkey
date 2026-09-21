using Microsoft.AspNetCore.Mvc;
using SandKey.Api.Display.Interfaces;
using SandKey.Api.Display.Requests;
using SandKey.Api.Display.Responses;

namespace SandKey.Api.Display.Controllers;

/// <summary>
/// Property listings on Clearwater Beach, for sale or for lease.
/// </summary>
[ApiController]
[Route("api/display/[controller]")]
[Produces("application/json")]
public sealed class ListingsController : ControllerBase
{
    private readonly IListingService _listingService;

    /// <summary>Creates the controller.</summary>
    /// <param name="listingService">Retrieves listings from the feed.</param>
    public ListingsController(IListingService listingService)
    {
        ArgumentNullException.ThrowIfNull(listingService);

        _listingService = listingService;
    }

    /// <summary>
    /// Returns a page of listings, filtered and ordered by the supplied query parameters.
    /// </summary>
    /// <param name="request">Listing type, condominium, ordering, and paging.</param>
    /// <param name="cancellationToken">Token that aborts the request.</param>
    /// <returns>A page of listing summaries.</returns>
    /// <response code="200">The page of listings. May be empty when nothing matches.</response>
    /// <response code="400">A query parameter is out of range or not a recognised value.</response>
    /// <response code="502">The listing feed answered with an error.</response>
    /// <response code="504">The listing feed did not respond in time.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ListingSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<PagedResponse<ListingSummaryResponse>>> GetListingsAsync(
        [FromQuery] ListingsQueryRequest request,
        CancellationToken cancellationToken)
    {
        var listings = await _listingService.GetListingsAsync(request, cancellationToken);

        return Ok(listings);
    }

    /// <summary>
    /// Returns one listing in full, including its photos and feature lists.
    /// </summary>
    /// <param name="listingKey">Opaque listing key, as returned in a listing summary.</param>
    /// <param name="cancellationToken">Token that aborts the request.</param>
    /// <returns>The listing.</returns>
    /// <response code="200">The listing.</response>
    /// <response code="400">The listing key is missing or empty.</response>
    /// <response code="404">No listing has that key.</response>
    /// <response code="502">The listing feed answered with an error.</response>
    /// <response code="504">The listing feed did not respond in time.</response>
    [HttpGet("{listingKey}")]
    [ProducesResponseType(typeof(ListingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<ListingResponse>> GetListingAsync(
        string listingKey,
        CancellationToken cancellationToken)
    {
        var listing = await _listingService.GetListingAsync(listingKey, cancellationToken);

        return Ok(listing);
    }
}
