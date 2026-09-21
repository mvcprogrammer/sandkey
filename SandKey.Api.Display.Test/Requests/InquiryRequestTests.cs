using System.ComponentModel.DataAnnotations;
using SandKey.Api.Display.Requests;

namespace SandKey.Api.Display.Test.Requests;

/// <summary>
/// Verifies the validation rules on an inquiry, which decide whether the visitor is emailed or
/// the office is asked to call back.
/// </summary>
public sealed class InquiryRequestTests
{
    #region Validate Tests

    /// <summary>Verifies that an email-only inquiry is accepted.</summary>
    [Fact]
    public void Validate_ShouldCompleteWithoutErrors_ForAnEmailInquiry()
    {
        // Arrange
        var request = new InquiryRequest { ListingKey = "key", EmailAddress = "visitor@example.com" };

        // Act
        var results = Validate(request);

        // Assert
        Assert.Empty(results);
    }

    /// <summary>Verifies that a phone-only inquiry is accepted.</summary>
    [Fact]
    public void Validate_ShouldCompleteWithoutErrors_ForAPhoneInquiry()
    {
        // Arrange
        var request = new InquiryRequest { ListingKey = "key", PhoneNumber = "727-555-0100" };

        // Act
        var results = Validate(request);

        // Assert
        Assert.Empty(results);
    }

    /// <summary>
    /// Verifies that exactly one contact method is required. Neither leaves nothing to answer,
    /// and both leaves it ambiguous which of the two very different emails to send.
    /// </summary>
    /// <param name="emailAddress">Address supplied, if any.</param>
    /// <param name="phoneNumber">Number supplied, if any.</param>
    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData("visitor@example.com", "727-555-0100")]
    public void Validate_ShouldReportAnError_WhenTheContactMethodIsNotExactlyOne(
        string? emailAddress,
        string? phoneNumber)
    {
        // Arrange
        var request = new InquiryRequest
        {
            ListingKey = "key",
            EmailAddress = emailAddress,
            PhoneNumber = phoneNumber
        };

        // Act
        var results = Validate(request);

        // Assert
        Assert.Contains(results, result =>
            result.ErrorMessage!.Contains("either an email address or a phone number", StringComparison.Ordinal));
    }

    /// <summary>Verifies that a malformed address is rejected.</summary>
    [Fact]
    public void Validate_ShouldReportAnError_ForAMalformedEmailAddress()
    {
        // Arrange
        var request = new InquiryRequest { ListingKey = "key", EmailAddress = "not-an-address" };

        // Act
        var results = Validate(request);

        // Assert
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(InquiryRequest.EmailAddress)));
    }

    /// <summary>Verifies that a missing listing key is rejected.</summary>
    /// <param name="listingKey">Key supplied by the caller.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldReportAnError_WhenTheListingKeyIsMissing(string listingKey)
    {
        // Arrange
        var request = new InquiryRequest { ListingKey = listingKey, EmailAddress = "visitor@example.com" };

        // Act
        var results = Validate(request);

        // Assert
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(InquiryRequest.ListingKey)));
    }

    #endregion

    #region Helper Methods

    /// <summary>Runs the full data-annotation pass the framework would run.</summary>
    /// <param name="request">Request to validate.</param>
    /// <returns>Every validation failure.</returns>
    private static List<ValidationResult> Validate(InquiryRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);

        return results;
    }

    #endregion
}
