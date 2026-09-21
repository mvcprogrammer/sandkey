using Microsoft.AspNetCore.Mvc;
using SandKey.Api.Display.Interfaces;
using SandKey.Api.Display.Responses;

namespace SandKey.Api.Display.Controllers;

/// <summary>
/// The condominium hotspots on the kiosk home screen.
/// </summary>
[ApiController]
[Route("api/display/[controller]")]
[Produces("application/json")]
public sealed class CondosController : ControllerBase
{
    private readonly ICondoService _condoService;

    /// <summary>Creates the controller.</summary>
    /// <param name="condoService">Supplies the hotspot reference data.</param>
    public CondosController(ICondoService condoService)
    {
        ArgumentNullException.ThrowIfNull(condoService);

        _condoService = condoService;
    }

    /// <summary>
    /// Returns every condominium that can be used as a listings filter.
    /// </summary>
    /// <returns>The condominium hotspots, ordered by id.</returns>
    /// <response code="200">The condominiums. This set is fixed and changes only with a release.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<CondoResponse>), StatusCodes.Status200OK)]
    public ActionResult<PagedResponse<CondoResponse>> GetCondos()
    {
        var condos = _condoService.GetCondos();

        return Ok(new PagedResponse<CondoResponse>
        {
            Items = condos,
            TotalCount = condos.Count,
            Page = 0,
            PageSize = condos.Count
        });
    }
}
