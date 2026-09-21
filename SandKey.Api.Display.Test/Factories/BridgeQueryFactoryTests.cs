using Microsoft.Extensions.Options;
using NSubstitute;
using SandKey.Api.Display.Configurations;
using SandKey.Api.Display.Enumerations;
using SandKey.Api.Display.Factories;
using SandKey.Api.Display.Interfaces;
using SandKey.Api.Display.Requests;

namespace SandKey.Api.Display.Test.Factories;

/// <summary>
/// Verifies the Bridge query shape against the contract recorded from the live kiosk in
/// <c>docs/bridge-contract.md</c>. A silent change here returns the wrong listings rather than
/// failing, so these are the assertions that matter most in the suite.
/// </summary>
public sealed class BridgeQueryFactoryTests
{
    #region Constructor Tests

    /// <summary>Verifies that the factory rejects a null options accessor.</summary>
    [Fact]
    public void BridgeQueryFactory_ShouldThrowArgumentNullException_WhenOptionsIsNull()
    {
        // Arrange
        var condoService = Substitute.For<ICondoService>();

        // Act
        var act = () => new BridgeQueryFactory(null!, condoService);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    /// <summary>Verifies that the factory rejects a null condo service.</summary>
    [Fact]
    public void BridgeQueryFactory_ShouldThrowArgumentNullException_WhenCondoServiceIsNull()
    {
        // Arrange
        var options = CreateOptions();

        // Act
        var act = () => new BridgeQueryFactory(options, null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    #endregion

    #region CreateListingsUri Tests

    /// <summary>
    /// Verifies that a default request produces the query recorded from the legacy kiosk:
    /// active listings in 33767, nine per page, most expensive first.
    /// </summary>
    [Fact]
    public void CreateListingsUri_ShouldMatchTheRecordedContract_ForADefaultRequest()
    {
        // Arrange
        var factory = CreateFactory();
        var request = new ListingsQueryRequest();

        // Act
        var uri = factory.CreateListingsUri(request).ToString();

        // Assert
        Assert.StartsWith("v2/stellar/listings?", uri, StringComparison.Ordinal);
        Assert.Contains("offset=0", uri, StringComparison.Ordinal);
        Assert.Contains("limit=9", uri, StringComparison.Ordinal);
        Assert.Contains("PropertyType=Residential", uri, StringComparison.Ordinal);
        Assert.Contains("sortBy=ListPrice", uri, StringComparison.Ordinal);
        Assert.Contains("order=desc", uri, StringComparison.Ordinal);
        Assert.Contains("MlsStatus=Active", uri, StringComparison.Ordinal);
        Assert.Contains("PostalCode=33767", uri, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that the page index is multiplied by the page size to produce the record offset.
    /// The legacy route segment was a page number, not an offset, and getting this wrong returns
    /// plausible but incorrect listings.
    /// </summary>
    /// <param name="page">Zero-based page index.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="expectedOffset">Record offset Bridge should be asked for.</param>
    [Theory]
    [InlineData(0, 9, 0)]
    [InlineData(1, 9, 9)]
    [InlineData(2, 9, 18)]
    [InlineData(3, 12, 36)]
    public void CreateListingsUri_ShouldTransformThePageIndexIntoARecordOffset(
        int page,
        int pageSize,
        int expectedOffset)
    {
        // Arrange
        var factory = CreateFactory();
        var request = new ListingsQueryRequest { Page = page, PageSize = pageSize };

        // Act
        var uri = factory.CreateListingsUri(request).ToString();

        // Assert
        Assert.Contains($"offset={expectedOffset}", uri, StringComparison.Ordinal);
        Assert.Contains($"limit={pageSize}", uri, StringComparison.Ordinal);
    }

    /// <summary>Verifies that a lease request asks Bridge for the lease property type.</summary>
    [Fact]
    public void CreateListingsUri_ShouldRequestTheLeasePropertyType_ForALeaseRequest()
    {
        // Arrange
        var factory = CreateFactory();
        var request = new ListingsQueryRequest { Type = ListingType.Lease };

        // Act
        var uri = factory.CreateListingsUri(request).ToString();

        // Assert
        Assert.Contains("PropertyType=Residential%20Lease", uri, StringComparison.Ordinal);
    }

    /// <summary>Verifies that each sort direction maps to the token Bridge expects.</summary>
    /// <param name="sortDirection">Direction requested.</param>
    /// <param name="expected">Token Bridge expects.</param>
    [Theory]
    [InlineData(SortDirection.Ascending, "order=asc")]
    [InlineData(SortDirection.Descending, "order=desc")]
    [InlineData(SortDirection.Unspecified, "order=desc")]
    public void CreateListingsUri_ShouldTransformTheSortDirection(SortDirection sortDirection, string expected)
    {
        // Arrange
        var factory = CreateFactory();
        var request = new ListingsQueryRequest { Order = sortDirection };

        // Act
        var uri = factory.CreateListingsUri(request).ToString();

        // Assert
        Assert.Contains(expected, uri, StringComparison.Ordinal);
    }

    /// <summary>Verifies that a known condo id adds the subdivision filter, encoded.</summary>
    [Fact]
    public void CreateListingsUri_ShouldFilterBySubdivision_ForAKnownCondo()
    {
        // Arrange
        var factory = CreateFactory(Arrange_CondoResolvesTo(100, "LANDMARK TOWERS"));
        var request = new ListingsQueryRequest { Condo = 100 };

        // Act
        var uri = factory.CreateListingsUri(request).ToString();

        // Assert
        Assert.Contains("&SubdivisionName.in=LANDMARK%20TOWERS", uri, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that an unmapped condo id omits the filter entirely rather than sending an empty
    /// one, which would match nothing.
    /// </summary>
    [Fact]
    public void CreateListingsUri_ShouldOmitTheSubdivisionFilter_ForAnUnknownCondo()
    {
        // Arrange
        var factory = CreateFactory();
        var request = new ListingsQueryRequest { Condo = 999 };

        // Act
        var uri = factory.CreateListingsUri(request).ToString();

        // Assert
        Assert.DoesNotContain("SubdivisionName.in=", uri, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that the requested field list carries the twenty-two legacy fields plus the two
    /// the detail screen reads but never asked for.
    /// </summary>
    [Fact]
    public void CreateListingsUri_ShouldRequestEveryFieldTheKioskReads()
    {
        // Arrange
        var factory = CreateFactory();
        var request = new ListingsQueryRequest();

        // Act
        var uri = factory.CreateListingsUri(request).ToString();

        // Assert
        foreach (var field in ExpectedFields())
        {
            Assert.Contains(field, uri, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Verifies that the access token never reaches a URI. The token is appended by a delegating
    /// handler so that nothing upstream can log it; this guards that boundary.
    /// </summary>
    [Fact]
    public void CreateListingsUri_ShouldNotCarryTheAccessToken()
    {
        // Arrange
        var factory = CreateFactory();
        var request = new ListingsQueryRequest();

        // Act
        var uri = factory.CreateListingsUri(request).ToString();

        // Assert
        Assert.DoesNotContain("access_token", uri, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("test-access-token", uri, StringComparison.Ordinal);
    }

    /// <summary>Verifies that a null or blank request is rejected.</summary>
    [Fact]
    public void CreateListingsUri_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var act = () => factory.CreateListingsUri(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    #endregion

    #region CreateListingUri Tests

    /// <summary>Verifies that a single-listing URI addresses the key and asks for the fields.</summary>
    [Fact]
    public void CreateListingUri_ShouldAddressTheListingByKey()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var uri = factory.CreateListingUri("6b26f42527d947fbab7b9e9b4b9ca26d").ToString();

        // Assert
        Assert.StartsWith("v2/stellar/listings/6b26f42527d947fbab7b9e9b4b9ca26d?", uri, StringComparison.Ordinal);
        Assert.Contains("fields=ListPrice", uri, StringComparison.Ordinal);
        Assert.DoesNotContain("access_token", uri, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that a listing key is escaped rather than concatenated verbatim.</summary>
    [Fact]
    public void CreateListingUri_ShouldEscapeTheListingKey()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var uri = factory.CreateListingUri("a b/c").ToString();

        // Assert
        Assert.Contains("a%20b%2Fc", uri, StringComparison.Ordinal);
    }

    /// <summary>Verifies that a missing listing key is rejected.</summary>
    /// <param name="listingKey">Key supplied by the caller.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateListingUri_ShouldThrowArgumentException_WhenListingKeyIsMissing(string? listingKey)
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var act = () => factory.CreateListingUri(listingKey!);

        // Assert
        Assert.ThrowsAny<ArgumentException>(act);
    }

    #endregion

    #region Helper Methods

    /// <summary>Builds a factory over the default settings and an empty condo lookup.</summary>
    /// <param name="condoService">Condo lookup to use, or null for one that resolves nothing.</param>
    /// <returns>The factory under test.</returns>
    private static BridgeQueryFactory CreateFactory(ICondoService? condoService = null) =>
        new(CreateOptions(), condoService ?? Substitute.For<ICondoService>());

    /// <summary>Builds the Bridge settings used across these tests.</summary>
    /// <returns>Options carrying a recognisable dummy token.</returns>
    private static IOptions<BridgeOptions> CreateOptions() =>
        Options.Create(new BridgeOptions { AccessToken = "test-access-token" });

    /// <summary>Configures a condo lookup that resolves one id.</summary>
    /// <param name="condoId">Id to resolve.</param>
    /// <param name="subdivisionName">Subdivision it resolves to.</param>
    /// <returns>The configured substitute.</returns>
    private static ICondoService Arrange_CondoResolvesTo(int condoId, string subdivisionName)
    {
        var condoService = Substitute.For<ICondoService>();
        condoService.FindSubdivisionName(condoId).Returns(subdivisionName);

        return condoService;
    }

    /// <summary>The fields the kiosk depends on, as recorded in the contract document.</summary>
    /// <returns>Field names expected in the query.</returns>
    private static IEnumerable<string> ExpectedFields() =>
    [
        "ListPrice", "SubdivisionName", "BedroomsTotal", "Media", "BathroomsFull",
        "BathroomsHalf", "LivingArea", "PetsAllowed", "ExteriorFeatures", "PoolFeatures",
        "InteriorFeatures", "WaterfrontFeatures", "CommunityFeatures", "YearBuilt", "GarageYN",
        "PublicRemarks", "PropertyType", "PropertySubType", "ListOfficeName", "UnparsedAddress",
        "ListingKey", "ListingId", "BridgeModificationTimestamp", "WaterfrontYN"
    ];

    #endregion
}
