using PartnerAdminLinkTool.Core.Models;
using Xunit;

namespace PartnerAdminLinkTool.Tests.Models;

/// <summary>
/// Unit tests for the Tenant model.
/// 
/// For beginners: These tests verify that our data models work correctly.
/// We test things like creating objects, setting properties, and any 
/// business logic within the models.
/// </summary>
public class TenantTests
{
    [Fact]
    public void Tenant_DefaultConstructor_SetsDefaultValues()
    {
        // Arrange & Act
        var tenant = new Tenant();

        // Assert
        Assert.NotNull(tenant.Id);
        Assert.NotNull(tenant.DisplayName);
        Assert.NotNull(tenant.Domain);
        Assert.NotNull(tenant.UserRoles);
        Assert.False(tenant.IsGuestUser);
        Assert.False(tenant.HasPartnerLink);
        Assert.Null(tenant.CurrentPartnerLink);
    }

    [Fact]
    public void Tenant_SetProperties_ReturnsCorrectValues()
    {
        // Arrange
        var tenant = new Tenant();
        var expectedId = "12345678-1234-1234-1234-123456789abc";
        var expectedDisplayName = "Contoso Corporation";
        var expectedDomain = "contoso.onmicrosoft.com";

        // Act
        tenant.Id = expectedId;
        tenant.DisplayName = expectedDisplayName;
        tenant.Domain = expectedDomain;
        tenant.IsGuestUser = true;
        tenant.HasPartnerLink = true;
        tenant.CurrentPartnerLink = "1234567";
        tenant.UserRoles.Add("Global Administrator");

        // Assert
        Assert.Equal(expectedId, tenant.Id);
        Assert.Equal(expectedDisplayName, tenant.DisplayName);
        Assert.Equal(expectedDomain, tenant.Domain);
        Assert.True(tenant.IsGuestUser);
        Assert.True(tenant.HasPartnerLink);
        Assert.Equal("1234567", tenant.CurrentPartnerLink);
        Assert.Contains("Global Administrator", tenant.UserRoles);
    }
}