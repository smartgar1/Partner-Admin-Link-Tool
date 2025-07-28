using PartnerAdminLinkTool.Core.Models;

namespace PartnerAdminLinkTool.Core.Services;

/// <summary>
/// Interface for linking Partner IDs to tenants using Partner Admin Link (PAL).
/// 
/// For beginners: This service handles the core business logic of our application -
/// actually linking the Partner ID to customer tenants so that Microsoft can
/// track the partner's involvement with that customer's Azure consumption.
/// </summary>
public interface IPartnerLinkService
{
    /// <summary>
    /// Link a Partner ID to a specific tenant
    /// </summary>
    /// <param name="partnerId">The Microsoft AI Cloud Partner Program (Associated PartnerID) to link</param>
    /// <param name="tenant">The tenant to link the Partner ID to</param>
    /// <param name="forceOverwrite">If true, will overwrite existing partner links. If false, will fail if a different partner ID is already linked.</param>
    /// <returns>Result of the link operation</returns>
    Task<PartnerLinkResult> LinkPartnerIdAsync(string partnerId, Tenant tenant, bool forceOverwrite = false);

    /// <summary>
    /// Link a Partner ID to multiple tenants
    /// </summary>
    /// <param name="partnerId">The Microsoft AI Cloud Partner Program (Associated PartnerID) to link</param>
    /// <param name="tenants">The tenants to link the Partner ID to</param>
    /// <param name="progressCallback">Optional callback to report progress</param>
    /// <param name="forceOverwrite">If true, will overwrite existing partner links. If false, will fail if a different partner ID is already linked.</param>
    /// <returns>Results of all link operations</returns>
    Task<List<PartnerLinkResult>> LinkPartnerIdToMultipleTenantsAsync(
        string partnerId, 
        List<Tenant> tenants, 
        IProgress<(int completed, int total, Tenant currentTenant)>? progressCallback = null,
        bool forceOverwrite = false);

    /// <summary>
    /// Remove a Partner ID link from a specific tenant
    /// </summary>
    /// <param name="tenant">The tenant to remove the Partner ID link from</param>
    /// <returns>Result of the unlink operation</returns>
    Task<PartnerLinkResult> UnlinkPartnerIdAsync(Tenant tenant);

    /// <summary>
    /// Validate that a Partner ID is valid and accessible
    /// </summary>
    /// <param name="partnerId">The Partner ID to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    Task<bool> ValidatePartnerIdAsync(string partnerId);
}