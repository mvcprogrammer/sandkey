using SandKey.Api.Display.Mappers;
using SandKey.Api.Display.Payloads;

namespace SandKey.Api.Display.Test.Mappers;

/// <summary>
/// Verifies the projection from a Bridge payload onto the kiosk response models, including the
/// fields the legacy collection mapper silently dropped.
/// </summary>
public sealed class BridgeListingMapperTests
{
    #region ToSummary Tests

    /// <summary>Verifies that the grid summary carries the values the results screen renders.</summary>
    [Fact]
    public void ToSummary_ShouldTransformTheFieldsTheGridRenders()
    {
        // Arrange
        var payload = CreateListing();

        // Act
        var summary = BridgeListingMapper.ToSummary(payload);

        // Assert
        Assert.Equal("6b26f42527d947fbab7b9e9b4b9ca26d", summary.ListingKey);
        Assert.Equal(37_500_000m, summary.ListPrice);
        Assert.Equal("MANDALAY POINT SUB 1ST ADD", summary.SubdivisionName);
        Assert.Equal("Residential", summary.PropertyType);
        Assert.Equal("Single Family Residence", summary.PropertySubType);
        Assert.Equal(3, summary.BedroomsTotal);
        Assert.Equal(3, summary.BathroomsFull);
        Assert.Equal(3434m, summary.LivingArea);
    }

    /// <summary>Verifies that the summary carries the first photo in display order.</summary>
    [Fact]
    public void ToSummary_ShouldTransformTheFirstPhotoInDisplayOrder()
    {
        // Arrange
        var payload = CreateListing() with { Media = CreateMedia() };

        // Act
        var summary = BridgeListingMapper.ToSummary(payload);

        // Assert
        Assert.NotNull(summary.PrimaryPhoto);
        Assert.Equal("https://cdn.example.com/photos/first.jpeg", summary.PrimaryPhoto.Url);
    }

    /// <summary>Verifies that a listing with no photos produces no primary photo.</summary>
    [Fact]
    public void ToSummary_ShouldCompleteWithoutAPhoto_WhenTheListingHasNoMedia()
    {
        // Arrange
        var payload = CreateListing() with { Media = [] };

        // Act
        var summary = BridgeListingMapper.ToSummary(payload);

        // Assert
        Assert.Null(summary.PrimaryPhoto);
    }

    /// <summary>Verifies that missing numeric values become zero rather than failing.</summary>
    [Fact]
    public void ToSummary_ShouldTransformMissingNumbersToZero()
    {
        // Arrange
        var payload = new BridgeListingPayload { ListingKey = "key" };

        // Act
        var summary = BridgeListingMapper.ToSummary(payload);

        // Assert
        Assert.Equal(0m, summary.ListPrice);
        Assert.Equal(0, summary.BedroomsTotal);
        Assert.Equal(0, summary.BathroomsFull);
        Assert.Equal(0m, summary.LivingArea);
    }

    /// <summary>Verifies that a null payload is rejected.</summary>
    [Fact]
    public void ToSummary_ShouldThrowArgumentNullException_WhenPayloadIsNull()
    {
        // Arrange, Act
        var act = () => BridgeListingMapper.ToSummary(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    #endregion

    #region ToDetail Tests

    /// <summary>
    /// Verifies that the detail projection carries the six fields the legacy collection mapper
    /// dropped. Those omissions are the reason both mappers were replaced by one.
    /// </summary>
    [Fact]
    public void ToDetail_ShouldTransformTheFieldsTheLegacyCollectionMapperDropped()
    {
        // Arrange
        var payload = CreateListing();

        // Act
        var detail = BridgeListingMapper.ToDetail(payload, maxPhotos: 10);

        // Assert
        Assert.Equal(["Gulf/Ocean"], detail.WaterfrontFeatures);
        Assert.Equal(["Pool"], detail.CommunityFeatures);
        Assert.Equal(["Cats OK"], detail.PetsAllowed);
        Assert.Equal(1974, detail.YearBuilt);
        Assert.True(detail.HasGarage);
        Assert.Equal(new DateTimeOffset(2026, 7, 19, 23, 11, 41, TimeSpan.Zero), detail.LastModified);
    }

    /// <summary>Verifies that an unspecified garage stays unspecified rather than becoming false.</summary>
    [Fact]
    public void ToDetail_ShouldTransformAnUnspecifiedGarageToNull()
    {
        // Arrange
        var payload = CreateListing() with { GarageYn = null };

        // Act
        var detail = BridgeListingMapper.ToDetail(payload, maxPhotos: 10);

        // Assert
        Assert.Null(detail.HasGarage);
    }

    /// <summary>Verifies that only photos are carried, and that they are ordered and capped.</summary>
    [Fact]
    public void ToDetail_ShouldTransformOnlyPhotosInOrderUpToTheCap()
    {
        // Arrange
        var payload = CreateListing() with { Media = CreateMedia() };

        // Act
        var detail = BridgeListingMapper.ToDetail(payload, maxPhotos: 2);

        // Assert
        Assert.Equal(2, detail.Media.Count);
        Assert.Equal("https://cdn.example.com/photos/first.jpeg", detail.Media[0].Url);
        Assert.Equal("https://cdn.example.com/photos/second.jpeg", detail.Media[1].Url);
        Assert.DoesNotContain(detail.Media, media => media.Url.Contains("brochure", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that media without a URL is skipped rather than producing a broken image path.
    /// </summary>
    [Fact]
    public void ToDetail_ShouldSkipMediaWithoutAUrl()
    {
        // Arrange
        var payload = CreateListing() with
        {
            Media = [new BridgeMediaPayload { Order = 1, MediaCategory = "Photo", MediaUrl = null }]
        };

        // Act
        var detail = BridgeListingMapper.ToDetail(payload, maxPhotos: 10);

        // Assert
        Assert.Empty(detail.Media);
    }

    /// <summary>Verifies that a photo cap of zero or less is rejected.</summary>
    /// <param name="maxPhotos">Cap supplied by the caller.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ToDetail_ShouldThrowArgumentOutOfRangeException_WhenMaxPhotosIsNotPositive(int maxPhotos)
    {
        // Arrange
        var payload = CreateListing();

        // Act
        var act = () => BridgeListingMapper.ToDetail(payload, maxPhotos);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    /// <summary>Verifies that a null payload is rejected.</summary>
    [Fact]
    public void ToDetail_ShouldThrowArgumentNullException_WhenPayloadIsNull()
    {
        // Arrange, Act
        var act = () => BridgeListingMapper.ToDetail(null!, maxPhotos: 10);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Builds a listing modelled on the most expensive one on the live kiosk, so the numbers in
    /// these assertions can be checked against a real page.
    /// </summary>
    /// <returns>A fully populated listing payload.</returns>
    private static BridgeListingPayload CreateListing() => new()
    {
        ListingKey = "6b26f42527d947fbab7b9e9b4b9ca26d",
        ListingId = "TB8412345",
        ListPrice = 37_500_000m,
        SubdivisionName = "MANDALAY POINT SUB 1ST ADD",
        UnparsedAddress = "1 Somewhere Drive",
        BedroomsTotal = 3,
        BathroomsFull = 3,
        BathroomsHalf = 1,
        LivingArea = 3434m,
        YearBuilt = 1974,
        GarageYn = true,
        WaterfrontYn = true,
        PropertyType = "Residential",
        PropertySubType = "Single Family Residence",
        PublicRemarks = "A description.",
        ListOfficeName = "COASTAL PROPERTIES GROUP INTERNATIONAL",
        PetsAllowed = ["Cats OK"],
        ExteriorFeatures = ["Balcony"],
        PoolFeatures = ["Heated"],
        InteriorFeatures = ["Elevator"],
        WaterfrontFeatures = ["Gulf/Ocean"],
        CommunityFeatures = ["Pool"],
        BridgeModificationTimestamp = new DateTimeOffset(2026, 7, 19, 23, 11, 41, TimeSpan.Zero)
    };

    /// <summary>
    /// Builds a media set that is deliberately out of order and contains a non-photo, so ordering
    /// and filtering are both exercised.
    /// </summary>
    /// <returns>Media attached to a listing.</returns>
    private static IReadOnlyList<BridgeMediaPayload> CreateMedia() =>
    [
        new() { Order = 3, MediaCategory = "Photo", MediaUrl = new Uri("https://cdn.example.com/photos/third.jpeg") },
        new() { Order = 1, MediaCategory = "Photo", MediaUrl = new Uri("https://cdn.example.com/photos/first.jpeg") },
        new() { Order = 0, MediaCategory = "Document", MediaUrl = new Uri("https://cdn.example.com/docs/brochure.pdf") },
        new() { Order = 2, MediaCategory = "Photo", MediaUrl = new Uri("https://cdn.example.com/photos/second.jpeg") }
    ];

    #endregion
}
