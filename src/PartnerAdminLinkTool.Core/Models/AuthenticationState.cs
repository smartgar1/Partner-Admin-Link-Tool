namespace PartnerAdminLinkTool.Core.Models;

/// <summary>
/// Represents the current authentication state of the user.
/// 
/// For beginners: This helps us track whether the user is signed in,
/// which account they're using, and what permissions they have.
/// </summary>
public class AuthenticationState
{
    /// <summary>
    /// Whether the user is currently authenticated
    /// </summary>
    public bool IsAuthenticated { get; set; }

    /// <summary>
    /// The user's email address / username
    /// </summary>
    public string? UserPrincipalName { get; set; }

    /// <summary>
    /// The user's display name
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// The ID of the user's home tenant
    /// </summary>
    public string? HomeTenantId { get; set; }

    /// <summary>
    /// The name of the user's home tenant
    /// </summary>
    public string? HomeTenantName { get; set; }

    /// <summary>
    /// When the authentication was last refreshed
    /// </summary>
    public DateTime? LastAuthenticationTime { get; set; }

    /// <summary>
    /// When the access token expires
    /// </summary>
    public DateTime? TokenExpiresAt { get; set; }

    /// <summary>
    /// Create an unauthenticated state
    /// </summary>
    public static AuthenticationState Unauthenticated => new() { IsAuthenticated = false };
}