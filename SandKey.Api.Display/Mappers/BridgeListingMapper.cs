using SandKey.Api.Display.Payloads;
using SandKey.Api.Display.Responses;

namespace SandKey.Api.Display.Mappers;

/// <summary>
/// Projects a Bridge listing payload onto the response models the kiosk consumes.
/// </summary>
/// <remarks>
/// Replaces the legacy <c>StellarListingToListing</c> and <c>StellarListingsToListings</c>, which
/// were near-copies that had drifted: the collection mapper silently dropped waterfront features,
/// community features, pet policy, year built, garage, and the modification timestamp. Both
/// projections now come from one place, so they cannot disagree again.
/// </remarks>
internal static class BridgeListingMapper
{
    /// <summary>Media category the kiosk displays. Bridge also returns documents and virtual tours.</summary>
    private const string PHOTO_CATEGORY = "Photo";

    /// <summary>Path prefix photos are served under, handled by the CDN rather than by this API.</summary>
    private const string MEDIA_PATH_PREFIX = "/media";

    /// <summary>Projects a listing onto the grid summary.</summary>
    /// <param name="payload">Listing as returned by Bridge.</param>
    /// <returns>The summary shown in the results grid.</returns>
    public static ListingSummaryResponse ToSummary(BridgeListingPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return new ListingSummaryResponse
        {
            ListingKey = payload.ListingKey,
            ListPrice = payload.ListPrice ?? 0m,
            SubdivisionName = payload.SubdivisionName,
            PropertyType = payload.PropertyType,
            PropertySubType = payload.PropertySubType,
            BedroomsTotal = payload.BedroomsTotal ?? 0,
            BathroomsFull = payload.BathroomsFull ?? 0,
            LivingArea = payload.LivingArea ?? 0m,
            PrimaryPhoto = FirstPhotoOrDefault(payload)
        };
    }

    /// <summary>Projects a listing onto the full detail response.</summary>
    /// <param name="payload">Listing as returned by Bridge.</param>
    /// <param name="maxPhotos">Maximum photos to carry.</param>
    /// <returns>The listing in full.</returns>
    public static ListingResponse ToDetail(BridgeListingPayload payload, int maxPhotos)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxPhotos);

        return new ListingResponse
        {
            ListingKey = payload.ListingKey,
            ListingId = payload.ListingId,
            ListPrice = payload.ListPrice ?? 0m,
            SubdivisionName = payload.SubdivisionName,
            UnparsedAddress = payload.UnparsedAddress,
            PropertyType = payload.PropertyType,
            PropertySubType = payload.PropertySubType,
            BedroomsTotal = payload.BedroomsTotal ?? 0,
            BathroomsFull = payload.BathroomsFull ?? 0,
            BathroomsHalf = payload.BathroomsHalf ?? 0,
            LivingArea = payload.LivingArea ?? 0m,
            YearBuilt = payload.YearBuilt ?? 0,
            HasGarage = payload.GarageYn,
            IsWaterfront = payload.WaterfrontYn,
            PublicRemarks = payload.PublicRemarks,
            ListOfficeName = payload.ListOfficeName,
            PetsAllowed = payload.PetsAllowed,
            ExteriorFeatures = payload.ExteriorFeatures,
            PoolFeatures = payload.PoolFeatures,
            InteriorFeatures = payload.InteriorFeatures,
            WaterfrontFeatures = payload.WaterfrontFeatures,
            CommunityFeatures = payload.CommunityFeatures,
            Media = ToMedia(payload, maxPhotos),
            LastModified = payload.BridgeModificationTimestamp
        };
    }

    /// <summary>
    /// Selects the photos from a listing's media set, in display order.
    /// </summary>
    /// <param name="payload">Listing as returned by Bridge.</param>
    /// <param name="maxPhotos">Maximum photos to return.</param>
    /// <returns>Photo references, ordered, capped.</returns>
    private static List<MediaResponse> ToMedia(BridgeListingPayload payload, int maxPhotos) =>
        payload.Media
            .Where(media => media.MediaUrl is not null)
            .Where(media => string.Equals(media.MediaCategory, PHOTO_CATEGORY, StringComparison.Ordinal))
            .OrderBy(media => media.Order)
            .Take(maxPhotos)
            .Select(media => new MediaResponse
            {
                Order = media.Order,
                Url = $"{MEDIA_PATH_PREFIX}{media.MediaUrl!.AbsolutePath}"
            })
            .ToList();

    /// <summary>Returns the first photo on a listing, or null when it has none.</summary>
    /// <param name="payload">Listing as returned by Bridge.</param>
    /// <returns>The first photo in display order.</returns>
    private static MediaResponse? FirstPhotoOrDefault(BridgeListingPayload payload)
    {
        var photos = ToMedia(payload, maxPhotos: 1);

        return photos.Count > 0 ? photos[0] : null;
    }
}
