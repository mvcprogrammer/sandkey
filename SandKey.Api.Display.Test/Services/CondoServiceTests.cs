using SandKey.Api.Display.Services;

namespace SandKey.Api.Display.Test.Services;

/// <summary>
/// Verifies the condominium reference data carried over from the legacy image map.
/// </summary>
public sealed class CondoServiceTests
{
    /// <summary>Hotspots that deliberately share the South Beach subdivision.</summary>
    private static readonly int[] _southBeachIds = [107, 108, 109];

    /// <summary>Hotspots that deliberately share the Lighthouse Towers subdivision.</summary>
    private static readonly int[] _lighthouseIds = [113, 114];

    #region GetCondos Tests

    /// <summary>Verifies that every hotspot on the legacy home screen is present.</summary>
    [Fact]
    public void GetCondos_ShouldReturnEveryHotspotFromTheLegacyImageMap()
    {
        // Arrange
        var service = CreateService();

        // Act
        var condos = service.GetCondos();

        // Assert
        Assert.Equal(18, condos.Count);
        Assert.Equal(100, condos[0].Id);
        Assert.Equal(117, condos[^1].Id);
    }

    /// <summary>Verifies that ids run contiguously, so no hotspot is unreachable.</summary>
    [Fact]
    public void GetCondos_ShouldReturnContiguousIds()
    {
        // Arrange
        var service = CreateService();

        // Act
        var ids = service.GetCondos().Select(condo => condo.Id).ToList();

        // Assert
        Assert.Equal(Enumerable.Range(100, 18), ids);
    }

    /// <summary>Verifies that no hotspot carries a blank name, which would filter to nothing.</summary>
    [Fact]
    public void GetCondos_ShouldReturnANameForEveryHotspot()
    {
        // Arrange
        var service = CreateService();

        // Act
        var condos = service.GetCondos();

        // Assert
        Assert.All(condos, condo => Assert.False(string.IsNullOrWhiteSpace(condo.Name)));
    }

    #endregion

    #region FindSubdivisionName Tests

    /// <summary>Verifies that a known hotspot resolves to its subdivision.</summary>
    /// <param name="condoId">Hotspot id.</param>
    /// <param name="expected">Subdivision it filters on.</param>
    [Theory]
    [InlineData(100, "LANDMARK TOWERS")]
    [InlineData(101, "HARBOUR LIGHT TOWERS")]
    [InlineData(115, "HARBOUR LIGHT")]
    [InlineData(117, "GRANDE ON SAND KEY")]
    public void FindSubdivisionName_ShouldResolveAKnownHotspot(int condoId, string expected)
    {
        // Arrange
        var service = CreateService();

        // Act
        var name = service.FindSubdivisionName(condoId);

        // Assert
        Assert.Equal(expected, name);
    }

    /// <summary>
    /// Verifies that the deliberately shared subdivisions still share. Three South Beach hotspots
    /// and two Lighthouse Towers hotspots resolve to one subdivision each, so those buttons return
    /// identical results. This is existing behaviour, asserted so a future edit has to be
    /// deliberate about changing it.
    /// </summary>
    [Fact]
    public void FindSubdivisionName_ShouldKeepTheDeliberatelySharedSubdivisions()
    {
        // Arrange
        var service = CreateService();

        // Act
        var southBeach = _southBeachIds.Select(service.FindSubdivisionName).ToList();
        var lighthouse = _lighthouseIds.Select(service.FindSubdivisionName).ToList();

        // Assert
        Assert.All(southBeach, name => Assert.Equal("SOUTH BEACH", name));
        Assert.All(lighthouse, name => Assert.Equal("LIGHTHOUSE TOWERS", name));
    }

    /// <summary>Verifies that an unknown id resolves to null rather than to a blank name.</summary>
    /// <param name="condoId">Id that is not a hotspot.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    [InlineData(118)]
    [InlineData(-1)]
    public void FindSubdivisionName_ShouldReturnNull_ForAnUnknownHotspot(int condoId)
    {
        // Arrange
        var service = CreateService();

        // Act
        var name = service.FindSubdivisionName(condoId);

        // Assert
        Assert.Null(name);
    }

    #endregion

    #region Helper Methods

    /// <summary>Builds the service under test.</summary>
    /// <returns>The service.</returns>
    private static CondoService CreateService() => new();

    #endregion
}
