using SandKey.Api.Display.Responses;

namespace SandKey.Api.Display.Interfaces;

/// <summary>
/// Supplies the condominium hotspots shown on the kiosk home screen. This is fixed reference
/// data, so nothing here performs I/O.
/// </summary>
public interface ICondoService
{
    /// <summary>Returns every hotspot, ordered by id.</summary>
    /// <returns>The condominium hotspots.</returns>
    IReadOnlyList<CondoResponse> GetCondos();

    /// <summary>Resolves a hotspot id to the subdivision it filters on.</summary>
    /// <param name="condoId">Hotspot id from the home screen image map.</param>
    /// <returns>The subdivision name, or null when the id is not a known hotspot.</returns>
    string? FindSubdivisionName(int condoId);
}
