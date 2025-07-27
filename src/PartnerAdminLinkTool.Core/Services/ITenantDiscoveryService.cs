using PartnerAdminLinkTool.Core.Models;

namespace PartnerAdminLinkTool.Core.Services;

/// <summary>
/// Interface for discovering Microsoft Entra tenants the user has access to.
/// 
/// For beginners: This service finds all the customer tenants (organizations)
/// that the signed-in user can access. Each tenant represents a separate
/// customer environment where we might want to link our Partner ID.
/// </summary>
public interface ITenantDiscoveryService
{
    /// <summary>
    /// Discover all tenants the current user has access to
    /// </summary>
    /// <returns>List of accessible tenants</returns>
    Task<List<Tenant>> DiscoverTenantsAsync();

    /// <summary>
    /// Check if a specific tenant has a Partner ID already linked
    /// </summary>
    /// <param name="tenantId">The tenant to check</param>
    /// <returns>Partner link information for the tenant</returns>
    Task<(bool hasLink, string? partnerId)> CheckExistingPartnerLinkAsync(string tenantId);

    /// <summary>
    /// Get detailed information about a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID to get details for</param>
    /// <returns>Detailed tenant information</returns>
    Task<Tenant?> GetTenantDetailsAsync(string tenantId);
}