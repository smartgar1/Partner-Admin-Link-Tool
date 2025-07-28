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
    /// Discover all tenants the current user has access to with user interaction for authentication failures
    /// </summary>
    /// <param name="onAuthenticationFailure">Callback function when authentication fails during tenant enrichment. Returns true to skip tenant, false to retry</param>
    /// <returns>List of accessible tenants</returns>
    Task<List<Tenant>> DiscoverTenantsAsync(Func<string, string, string, Task<bool>>? onAuthenticationFailure);

    /// <summary>
    /// Check if a specific tenant has a Partner ID already linked
    /// </summary>
    /// <param name="tenantId">The tenant to check</param>
    /// <returns>Partner link information for the tenant</returns>
    Task<(bool hasLink, string? partnerId)> CheckExistingPartnerLinkAsync(string tenantId);

    /// <summary>
    /// Check if a specific tenant has a Partner ID already linked with user interaction for authentication failures
    /// </summary>
    /// <param name="tenantId">The tenant to check</param>
    /// <param name="onAuthenticationFailure">Callback function when authentication fails. Returns true to skip tenant, false to retry</param>
    /// <returns>Partner link information for the tenant, or null if skipped</returns>
    Task<(bool hasLink, string? partnerId, bool skipped)> CheckExistingPartnerLinkAsync(string tenantId, Func<string, string, string, Task<bool>>? onAuthenticationFailure);

    /// <summary>
    /// Check if a specific tenant has a Partner ID already linked with delayed user interaction for authentication issues
    /// </summary>
    /// <param name="tenantId">The tenant to check</param>
    /// <param name="onAuthenticationTimeout">Callback function when authentication takes too long. Returns true to skip tenant, false to wait longer</param>
    /// <param name="timeoutSeconds">Number of seconds to wait before showing timeout prompt</param>
    /// <returns>Partner link information for the tenant, or null if skipped</returns>
    Task<(bool hasLink, string? partnerId, bool skipped)> CheckExistingPartnerLinkWithTimeoutAsync(string tenantId, Func<string, Task<bool>>? onAuthenticationTimeout, int timeoutSeconds = 5);

    /// <summary>
    /// Get detailed information about a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID to get details for</param>
    /// <returns>Detailed tenant information</returns>
    Task<Tenant?> GetTenantDetailsAsync(string tenantId);
}