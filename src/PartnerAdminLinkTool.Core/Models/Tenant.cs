namespace PartnerAdminLinkTool.Core.Models;

/// <summary>
/// Represents a Microsoft Entra tenant that a user has access to.
/// 
/// For beginners: This is a "model" class - it just holds data about a tenant.
/// Think of it like a container for information about each customer's Azure/Microsoft 365 environment.
/// </summary>
public class Tenant
{
    /// <summary>
    /// Unique identifier for the tenant (also called Directory ID)
    /// Example: "12345678-1234-1234-1234-123456789abc"
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the tenant (usually the organization name)
    /// Example: "Contoso Corporation"
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Primary domain name for the tenant
    /// Example: "contoso.onmicrosoft.com" or "contoso.com"
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Whether the current user is a guest in this tenant or a member
    /// </summary>
    public bool IsGuestUser { get; set; }

    /// <summary>
    /// The roles/permissions the user has in this tenant
    /// </summary>
    public List<string> UserRoles { get; set; } = new();

    /// <summary>
    /// Whether a Partner ID is already linked to this tenant
    /// </summary>
    public bool HasPartnerLink { get; set; }

    /// <summary>
    /// The currently linked Partner ID (if any)
    /// </summary>
    public string? CurrentPartnerLink { get; set; }
}