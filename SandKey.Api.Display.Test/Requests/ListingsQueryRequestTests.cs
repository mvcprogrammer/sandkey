using System.ComponentModel.DataAnnotations;
using SandKey.Api.Display.Enumerations;
using SandKey.Api.Display.Requests;

namespace SandKey.Api.Display.Test.Requests;

/// <summary>
/// Verifies the validation rules on a listings query, including the enum check that model binding
/// does not perform on its own.
/// </summary>
public sealed class ListingsQueryRequestTests
{
    #region Validate Tests

    /// <summary>Verifies that the kiosk's default query is accepted.</summary>
    [Fact]
    public void Validate_ShouldCompleteWithoutErrors_ForTheDefaultQuery()
    {
        // Arrange
        var request = new ListingsQueryRequest();

        // Act
        var results = Validate(request);

        // Assert
        Assert.Empty(results);
        Assert.Equal(ListingType.Sale, request.Type);
        Assert.Equal(SortDirection.Ascending, request.Order);
        Assert.Equal(9, request.PageSize);
    }

    /// <summary>
    /// Verifies that an undefined enum value is rejected. Model binding will turn
    /// <c>?type=99</c> into an undefined value without complaint, so the check has to be explicit.
    /// </summary>
    [Fact]
    public void Validate_ShouldReportAnError_ForAnUndefinedListingType()
    {
        // Arrange
        var request = new ListingsQueryRequest { Type = (ListingType)99 };

        // Act
        var results = Validate(request);

        // Assert
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(ListingsQueryRequest.Type)));
    }

    /// <summary>Verifies that an undefined sort direction is rejected.</summary>
    [Fact]
    public void Validate_ShouldReportAnError_ForAnUndefinedSortDirection()
    {
        // Arrange
        var request = new ListingsQueryRequest { Order = (SortDirection)42 };

        // Act
        var results = Validate(request);

        // Assert
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(ListingsQueryRequest.Order)));
    }

    /// <summary>
    /// Verifies that a negative page is rejected rather than silently clamped. The legacy
    /// controller clamped it, which hid the fact that the Prev button on the first page linked to
    /// page minus one.
    /// </summary>
    [Fact]
    public void Validate_ShouldReportAnError_ForANegativePage()
    {
        // Arrange
        var request = new ListingsQueryRequest { Page = -1 };

        // Act
        var results = Validate(request);

        // Assert
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(ListingsQueryRequest.Page)));
    }

    /// <summary>Verifies that a page size outside the permitted range is rejected.</summary>
    /// <param name="pageSize">Page size requested.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(51)]
    public void Validate_ShouldReportAnError_ForAPageSizeOutOfRange(int pageSize)
    {
        // Arrange
        var request = new ListingsQueryRequest { PageSize = pageSize };

        // Act
        var results = Validate(request);

        // Assert
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(ListingsQueryRequest.PageSize)));
    }

    #endregion

    #region Helper Methods

    /// <summary>Runs the full data-annotation pass the framework would run.</summary>
    /// <param name="request">Request to validate.</param>
    /// <returns>Every validation failure.</returns>
    private static List<ValidationResult> Validate(ListingsQueryRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);

        return results;
    }

    #endregion
}
