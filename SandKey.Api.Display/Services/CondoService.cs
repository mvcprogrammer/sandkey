using SandKey.Api.Display.Interfaces;
using SandKey.Api.Display.Responses;

namespace SandKey.Api.Display.Services;

/// <summary>
/// Serves the fixed set of condominium hotspots on the kiosk home screen.
/// </summary>
/// <remarks>
/// Carried over from the legacy <c>CondoConverter</c>. The values were stored URL-encoded there
/// and decoded at the point of display; they are held decoded here and encoded once, when the
/// Bridge query is built.
/// </remarks>
internal sealed class CondoService : ICondoService
{
    private static readonly IReadOnlyList<CondoResponse> _condos =
    [
        new() { Id = 100, Name = "LANDMARK TOWERS" },
        new() { Id = 101, Name = "HARBOUR LIGHT TOWERS" },
        new() { Id = 102, Name = "SOUTH BAY" },
        new() { Id = 103, Name = "BAYSIDE" },
        new() { Id = 104, Name = "DANS ISLAND" },
        new() { Id = 105, Name = "CABANA CLUB" },
        new() { Id = 106, Name = "ULTIMAR" },
        new() { Id = 107, Name = "SOUTH BEACH" },
        new() { Id = 108, Name = "SOUTH BEACH" },
        new() { Id = 109, Name = "SOUTH BEACH" },
        new() { Id = 110, Name = "SAND KEY CLUB" },
        new() { Id = 111, Name = "UTOPIA" },
        new() { Id = 112, Name = "CRESCENT BEACH CLUB" },
        new() { Id = 113, Name = "LIGHTHOUSE TOWERS" },
        new() { Id = 114, Name = "LIGHTHOUSE TOWERS" },
        new() { Id = 115, Name = "HARBOUR LIGHT" },
        new() { Id = 116, Name = "MERIDIAN ON SAND KEY" },
        new() { Id = 117, Name = "GRANDE ON SAND KEY" }
    ];

    private static readonly Dictionary<int, string> _nameById =
        _condos.ToDictionary(condo => condo.Id, condo => condo.Name);

    /// <inheritdoc/>
    public IReadOnlyList<CondoResponse> GetCondos() => _condos;

    /// <inheritdoc/>
    public string? FindSubdivisionName(int condoId) =>
        _nameById.TryGetValue(condoId, out var name) ? name : null;
}
