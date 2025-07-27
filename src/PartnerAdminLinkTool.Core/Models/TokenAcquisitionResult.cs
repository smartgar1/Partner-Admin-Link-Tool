namespace PartnerAdminLinkTool.Core.Models;

/// <summary>
/// Represents the result of attempting to acquire an access token.
/// </summary>
public class TokenAcquisitionResult
{
    /// <summary>
    /// True if the token was acquired successfully.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// The access token, if successful.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// The error type (e.g., consent_required, mfa_required, etc.), if failed.
    /// </summary>
    public string? ErrorType { get; set; }

    /// <summary>
    /// A user-friendly error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// An actionable URL (e.g., admin consent URL), if applicable.
    /// </summary>
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Create a success result.
    /// </summary>
    public static TokenAcquisitionResult Success(string accessToken) => new TokenAcquisitionResult
    {
        IsSuccess = true,
        AccessToken = accessToken
    };

    /// <summary>
    /// Create a failure result.
    /// </summary>
    public static TokenAcquisitionResult Failure(string errorType, string errorMessage, string? actionUrl = null) => new TokenAcquisitionResult
    {
        IsSuccess = false,
        ErrorType = errorType,
        ErrorMessage = errorMessage,
        ActionUrl = actionUrl
    };
}
