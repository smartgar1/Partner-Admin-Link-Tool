namespace PartnerAdminLinkTool.Core.Models;

/// <summary>
/// Represents the result of attempting to link a Partner ID to a tenant.
/// 
/// For beginners: This class helps us track whether our operations succeed or fail,
/// and provides useful information about what happened.
/// </summary>
public class PartnerLinkResult
{
    /// <summary>
    /// The error type (e.g., consent_required, mfa_required, etc.), if failed.
    /// </summary>
    public string? ErrorType { get; set; }
    /// <summary>
    /// Whether the Partner ID was successfully linked
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// The tenant this result relates to
    /// </summary>
    public Tenant Tenant { get; set; } = new();

    /// <summary>
    /// The Partner ID that was linked (or attempted to be linked)
    /// </summary>
    public string PartnerId { get; set; } = string.Empty;

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional details about the operation
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// When the operation was performed
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Create a successful result
    /// </summary>
    public static PartnerLinkResult Success(Tenant tenant, string partnerId, string? details = null)
    {
        return new PartnerLinkResult
        {
            IsSuccess = true,
            Tenant = tenant,
            PartnerId = partnerId,
            Details = details
        };
    }

    /// <summary>
    /// Create a failed result
    /// </summary>
    public static PartnerLinkResult Failure(Tenant tenant, string partnerId, string errorMessage, string? details = null)
    {
        // If errorMessage looks like a known error type, set ErrorType accordingly
        string errorType = errorMessage;
        // If errorMessage contains a colon, treat left as errorType, right as message
        if (errorMessage != null && errorMessage.Contains(":"))
        {
            var parts = errorMessage.Split(":", 2);
            errorType = parts[0].Trim();
            errorMessage = parts[1].Trim();
        }
        return new PartnerLinkResult
        {
            IsSuccess = false,
            Tenant = tenant,
            PartnerId = partnerId,
            ErrorType = errorType,
            ErrorMessage = errorMessage,
            Details = details
        };
    }
}