using System.ComponentModel.DataAnnotations;
using SandKey.Api.Display.Enumerations;

namespace SandKey.Api.Display.Requests;

/// <summary>
/// Filters applied to a listings search. Part 4 requires filtering to be expressed through the
/// query string rather than through the path, which is how the legacy kiosk did it.
/// </summary>
public sealed record ListingsQueryRequest : IValidatableObject
{
    /// <summary>Sale or lease. Defaults to sale.</summary>
    public ListingType Type { get; init; } = ListingType.Sale;

    /// <summary>Field to order by. Defaults to list price.</summary>
    public SortField Sort { get; init; } = SortField.ListPrice;

    /// <summary>Direction to order in. Defaults to ascending, lowest price first, at the office's request.</summary>
    public SortDirection Order { get; init; } = SortDirection.Ascending;

    /// <summary>Zero-based page index.</summary>
    [Range(0, 1000)]
    public int Page { get; init; }

    /// <summary>Items per page. The kiosk grid fits nine.</summary>
    [Range(1, 50)]
    public int PageSize { get; init; } = 9;

    /// <summary>
    /// Rejects enum values outside the defined set. Model binding will happily turn
    /// <c>?type=99</c> into an undefined <see cref="ListingType"/>, so the check has to be
    /// explicit; doing it here makes it a 400 rather than a failure further down.
    /// </summary>
    /// <param name="validationContext">Context supplied by the validation framework.</param>
    /// <returns>One result per undefined enum value.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enum.IsDefined(Type))
        {
            yield return new ValidationResult("Unknown listing type.", [nameof(Type)]);
        }

        if (!Enum.IsDefined(Sort))
        {
            yield return new ValidationResult("Unknown sort field.", [nameof(Sort)]);
        }

        if (!Enum.IsDefined(Order))
        {
            yield return new ValidationResult("Unknown sort direction.", [nameof(Order)]);
        }
    }
}
