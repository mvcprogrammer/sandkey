# Bridge Data Output contract — recorded from the legacy app

Captured 2026-09-21 from `Displays/Display.Core/Services/Bridge/Stellar/` at commit `c241254`,
which is the build running live on the EC2 kiosk host. `Factories/BridgeQueryFactory` must produce
equivalent requests, and the tests in `SandKey.Api.Display.Test` assert against the shapes below.

## Host

| | |
|---|---|
| Listings base address | `https://api.bridgedataoutput.com/api/` |
| Photo CDN | `https://dvvjkgh94f2v6.cloudfront.net` |
| Timeout | 3 seconds (`StellarClient.cs:12`, `PhotoClient.cs:11`) |

The access token is passed as an `access_token` **query-string parameter**, which Bridge requires.
It must never appear in a log line. The legacy app printed the fully-formed URI —
token included — to stdout at `StellarService.cs:24`.

## Collection request

```
v2/stellar/listings
  ?access_token={token}
  &offset={page * pageSize}
  &limit={pageSize}                        # always 9 in the kiosk
  &PropertyType=Residential                # or Residential%20Lease
  &sortBy=ListPrice
  &order=desc                              # or asc
  &fields={FIELDS}
  &MlsStatus=Active
  &PostalCode=33767
  &SubdivisionName.in={CONDO}              # omitted when condoId is unmapped (0)
```

Assembled in `StellarListingsRouteData.Uri` (`StellarListingsRouteData.cs:33`). Note the offset is
computed in the constructor as `skip * take` (`:24`), so the kiosk's `skip` route segment is a
**page index**, not a record offset.

`PropertyType` is emitted by `EnumConverter.ListingTypeToString` and already carries its own
leading `&`; the value for leases is pre-encoded as `Residential%20Lease`.

## Single-listing request

```
v2/stellar/listings/{listingKey}?access_token={token}&fields={FIELDS}
```

`StellarListingRouteData.cs:12`. The identifier is a `ListingKey` (32 hex characters), not a
`ListingId` (the MLS number, e.g. `TB8412345`).

## FIELDS

22 names, comma-joined, from `StellarListingsFilters.cs:5`:

```
ListPrice, SubdivisionName, BedroomsTotal, Media, BathroomsFull, BathroomsHalf,
LivingArea, PetsAllowed, ExteriorFeatures, PoolFeatures, InteriorFeatures,
WaterfrontFeatures, CommunityFeatures, YearBuilt, GarageYN, PublicRemarks,
PropertyType, PropertySubType, ListOfficeName, UnparsedAddress, ListingKey, ListingId
```

Two fields the app reads are **not** in this list but are returned anyway, so Bridge includes them
unconditionally. Verified against the live detail page, which renders a real
`BridgeModificationTimestamp` of `7/19/2026 11:11:41 PM`:

- `BridgeModificationTimestamp` — rendered as "Data last refreshed on ..."
- `WaterfrontYn`

Do not assume this holds. If a future Bridge change drops them, the detail page loses its
"last refreshed" line silently. Add them to the request explicitly.

## Condo filter

`CondoConverter.CondoNameById` maps the 18 image-map hotspot IDs to `SubdivisionName` values,
pre-encoded with `%20` for spaces. Three pairs/triples are **intentionally** non-unique, so those
hotspots return identical result sets:

| IDs | SubdivisionName |
|---|---|
| 107, 108, 109 | `SOUTH BEACH` |
| 113, 114 | `LIGHTHOUSE TOWERS` |
| 100–106, 110–112, 115–117 | one each |

Hotspot 115 is `HARBOUR LIGHT` while 101 is `HARBOUR LIGHT TOWERS` — different subdivisions,
not a typo.

## Response envelope

```jsonc
{
  "success": true,
  "status": 200,
  "bundle": [ /* listing objects, or a single object for the by-key route */ ],
  "total": 42
}
```

The legacy client deserialized this as `dynamic` and cast `response.bundle` with
`ToObject<IEnumerable<Bundle>>()`. `total` was parsed into `StellarListingsResponse.Total` but
never used — the kiosk's Prev/Next buttons page blindly with no upper bound, so pressing Next past
the end yields an empty grid. The new `PagedResponse<T>.totalCount` surfaces it.

## Media

Each listing carries a `Media` array of `{ Order, MediaUrl, MediaCategory }`. The app filters to
`MediaCategory == "Photo"`, orders by `Order`, and takes 10
(`Mappers/StellarListingsToListings.cs:14`).

`MediaUrl` is an absolute URL on the Bridge CDN. The legacy mapper rewrote it to
`/photo{AbsolutePath}` so it would be proxied by `Controllers/Photo.cs`. The replacement passes
the URL through unchanged and the kiosk loads the photo from the CDN directly, so no request
touches the application tier. A `/media/*` CloudFront behaviour in front of the CDN was tried
first and is not possible: the CDN is itself CloudFront, which refuses to be another
distribution's origin (2026-09-22).

## Observed live values (2026-09-21)

Reference points for the parity check in Phase 7:

| Page | Listings | First price | Note |
|---|---|---|---|
| `/Residential/` | 9 | $37,500,000 | default: sale, descending, page 0 |
| `/Residential/2/1/0` | 9 | $4,599,000 | page 1 — confirms the `skip * take` offset |
| `/Residential/1/0/0` | 9 | $175,000 | ascending |
| `/Residential/2/0/100` | 8 | $950,000 | Landmark Towers — a partial page |
| `/Rental/` | 9 | $20,000/Month | lease pricing suffix |
| `/Rental/2/0/117` | 2 | $5,500/Month | smallest non-empty set — good fixture |
| `/Residential/Details/{key}` | — | — | 10 thumbnails, all `MediaCategory == "Photo"` |

The `/Residential/` row records the legacy default. On 2026-09-22 the office asked for lowest price
first, so a request with no direction now sorts ascending and `/Residential/` matches the
`/Residential/1/0/0` row instead.

Golden HTML for these is in `sandkey-display-web/golden/pages/`.
