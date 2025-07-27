using PartnerAdminLinkTool.Core.Models;
using Xunit;

namespace PartnerAdminLinkTool.Tests.Models;

/// <summary>
/// Unit tests for the PartnerLinkResult model.
/// 
/// For beginners: These tests ensure our result objects work correctly,
/// including the static factory methods that create success/failure results.
/// </summary>
public class PartnerLinkResultTests
{
    [Fact]
    public void PartnerLinkResult_DefaultConstructor_SetsDefaultValues()
    {
        // Arrange & Act
        var result = new PartnerLinkResult();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Tenant);
        Assert.NotNull(result.PartnerId);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.Details);
        Assert.True(result.Timestamp > DateTime.MinValue);
    }

    [Fact]
    public void Success_ValidInput_ReturnsSuccessResult()
    {
        // Arrange
        var tenant = new Tenant { Id = "test-id", DisplayName = "Test Tenant" };
        var partnerId = "1234567";
        var details = "Operation completed successfully";

        // Act
        var result = PartnerLinkResult.Success(tenant, partnerId, details);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(tenant, result.Tenant);
        Assert.Equal(partnerId, result.PartnerId);
        Assert.Equal(details, result.Details);
        Assert.Null(result.ErrorMessage);
        Assert.True(result.Timestamp > DateTime.MinValue);
    }

    [Fact]
    public void Failure_ValidInput_ReturnsFailureResult()
    {
        // Arrange
        var tenant = new Tenant { Id = "test-id", DisplayName = "Test Tenant" };
        var partnerId = "1234567";
        var errorMessage = "Operation failed";
        var details = "Additional error details";

        // Act
        var result = PartnerLinkResult.Failure(tenant, partnerId, errorMessage, details);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(tenant, result.Tenant);
        Assert.Equal(partnerId, result.PartnerId);
        Assert.Equal(errorMessage, result.ErrorMessage);
        Assert.Equal(details, result.Details);
        Assert.True(result.Timestamp > DateTime.MinValue);
    }

    [Fact]
    public void Success_WithoutDetails_ReturnsSuccessResult()
    {
        // Arrange
        var tenant = new Tenant { Id = "test-id", DisplayName = "Test Tenant" };
        var partnerId = "1234567";

        // Act
        var result = PartnerLinkResult.Success(tenant, partnerId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(tenant, result.Tenant);
        Assert.Equal(partnerId, result.PartnerId);
        Assert.Null(result.Details);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Failure_WithoutDetails_ReturnsFailureResult()
    {
        // Arrange
        var tenant = new Tenant { Id = "test-id", DisplayName = "Test Tenant" };
        var partnerId = "1234567";
        var errorMessage = "Operation failed";

        // Act
        var result = PartnerLinkResult.Failure(tenant, partnerId, errorMessage);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(tenant, result.Tenant);
        Assert.Equal(partnerId, result.PartnerId);
        Assert.Equal(errorMessage, result.ErrorMessage);
        Assert.Null(result.Details);
    }
}