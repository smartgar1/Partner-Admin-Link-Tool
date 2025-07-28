using Azure.Identity;
using Microsoft.Extensions.Logging;
using PartnerAdminLinkTool.Core.Models;

namespace PartnerAdminLinkTool.Core.Services;

/// <summary>
/// <summary>
/// Implementation for discovering Microsoft Entra tenants using Azure Management API.
/// Microsoft Graph dependency removed to avoid consent issues.
/// 
/// For beginners: This service connects to Azure Management API to find all the customer
/// tenants (organizations) that the signed-in user has access to. We'll then be able
/// to link our Partner ID to each of these tenants.
/// 
/// Note: Microsoft Graph was previously used for friendly tenant names, but removed
/// to avoid consent issues in organizations that block Microsoft Graph.
/// </summary>
public class TenantDiscoveryService : ITenantDiscoveryService
{
    private readonly ILogger<TenantDiscoveryService> _logger;
    private readonly IAuthenticationService _authenticationService;

    public TenantDiscoveryService(
        ILogger<TenantDiscoveryService> logger,
        IAuthenticationService authenticationService)
    {
        _logger = logger;
        _authenticationService = authenticationService;
    }

    /// <summary>
    /// Discover all tenants the current user has access to
    /// </summary>
    public async Task<List<Tenant>> DiscoverTenantsAsync()
    {
        return await DiscoverTenantsAsync(null);
    }

    /// <summary>
    /// Discover all tenants the current user has access to with user interaction for authentication failures
    /// </summary>
    public async Task<List<Tenant>> DiscoverTenantsAsync(Func<string, string, string, Task<bool>>? onAuthenticationFailure)
    {
        try
        {
            _logger.LogInformation("Starting tenant discovery (using Azure Management API /tenants endpoint)");

            if (!_authenticationService.CurrentState.IsAuthenticated)
            {
                _logger.LogWarning("User not authenticated, cannot discover tenants");
                return new List<Tenant>();
            }

            // Use Azure Management access token from authentication service
            var accessToken = await _authenticationService.GetAzureManagementAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogWarning("No Azure Management access token available");
                return new List<Tenant>();
            }

            var tenants = new List<Tenant>();
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var url = "https://management.azure.com/tenants?api-version=2020-01-01";
            while (!string.IsNullOrEmpty(url))
            {
                var response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to get tenants from Azure Management API: {StatusCode}", response.StatusCode);
                    _logger.LogError("Azure Management API response: {Content}", content);
                    break;
                }
                var contentStr = await response.Content.ReadAsStringAsync();
                var json = System.Text.Json.JsonDocument.Parse(contentStr);
                if (json.RootElement.TryGetProperty("value", out var valueArray))
                {
                    foreach (var tenantJson in valueArray.EnumerateArray())
                    {
                        var id = tenantJson.GetProperty("tenantId").GetString() ?? string.Empty;
                        tenants.Add(new Tenant
                        {
                            Id = id,
                            DisplayName = id,
                            Domain = "Unknown",
                            IsGuestUser = false,
                            UserRoles = new List<string>(),
                            HasPartnerLink = false,
                            CurrentPartnerLink = null
                        });
                    }
                }
                // Handle pagination
                url = json.RootElement.TryGetProperty("nextLink", out var nextLink) ? nextLink.GetString() : null;
            }

            // Enrich tenants with partner link information, allowing user to skip on authentication failure
            var enrichedTenants = new List<Tenant>();
            foreach (var tenant in tenants)
            {
                // Microsoft Graph tenant enrichment removed to avoid consent issues
                // Tenant display names will show as tenant IDs, which is acceptable for PAL functionality
                _logger.LogDebug("Tenant enrichment with Microsoft Graph disabled to avoid consent issues");

                // Retrieve and set the previous Partner ID for this tenant with timeout support
                try
                {
                    (bool hasLink, string? partnerId, bool skipped) result;
                    
                    if (onAuthenticationFailure != null)
                    {
                        // Use timeout-based approach - convert the 3-parameter callback to 1-parameter
                        result = await CheckExistingPartnerLinkWithTimeoutAsync(tenant.Id, async (tenantId) =>
                        {
                            return await onAuthenticationFailure(tenantId, "timeout", "Authentication is taking longer than expected. This might be due to MFA requirements.");
                        }, 5);
                    }
                    else
                    {
                        // Use original method for backward compatibility
                        var originalResult = await CheckExistingPartnerLinkAsync(tenant.Id);
                        result = (originalResult.hasLink, originalResult.partnerId, false);
                    }
                    
                    var (hasLink, partnerId, skipped) = result;
                    if (skipped)
                    {
                        _logger.LogInformation("Skipping tenant {TenantId} during discovery due to user choice", tenant.Id);
                        continue; // Skip adding this tenant to the final list
                    }
                    
                    if (hasLink && !string.IsNullOrEmpty(partnerId))
                    {
                        tenant.CurrentPartnerLink = partnerId;
                        tenant.HasPartnerLink = true;
                    }
                    else
                    {
                        tenant.CurrentPartnerLink = null;
                        tenant.HasPartnerLink = false;
                    }
                    
                    enrichedTenants.Add(tenant);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to retrieve previous Partner ID for tenant {tenant.Id}");
                    // Still add the tenant, just without partner link information
                    tenant.CurrentPartnerLink = null;
                    tenant.HasPartnerLink = false;
                    enrichedTenants.Add(tenant);
                }
            }

            if (enrichedTenants.Count == 0)
            {
                _logger.LogWarning("No tenants discovered or all tenants were skipped. This may indicate you do not have access to any tenants, your account is not properly configured, or you chose to skip all tenants.");
            }

            _logger.LogInformation("Discovered {TenantCount} tenants (after user selections)", enrichedTenants.Count);
            return enrichedTenants;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover tenants");
            _logger.LogError("Tenant discovery failed using Azure Management API /tenants endpoint.");
            return new List<Tenant>();
        }
    }

    /// <summary>
    /// Check if a specific tenant has a Partner ID already linked
    /// </summary>
    public async Task<(bool hasLink, string? partnerId)> CheckExistingPartnerLinkAsync(string tenantId)
    {
        var result = await CheckExistingPartnerLinkAsync(tenantId, null);
        return (result.hasLink, result.partnerId);
    }

    /// <summary>
    /// Check if a specific tenant has a Partner ID already linked with user interaction for authentication failures
    /// </summary>
    public async Task<(bool hasLink, string? partnerId, bool skipped)> CheckExistingPartnerLinkAsync(string tenantId, Func<string, string, string, Task<bool>>? onAuthenticationFailure)
    {
        try
        {
            _logger.LogDebug("Checking existing partner link for tenant: {TenantId}", tenantId);

            // Get Azure Management access token for this tenant
            // Disable automatic interactive auth when we have a callback to let user decide
            var allowInteractiveAuth = onAuthenticationFailure == null;
            var tokenResult = await _authenticationService.GetAzureManagementAccessTokenAsync(tenantId, allowInteractiveAuth);
            if (!tokenResult.IsSuccess || string.IsNullOrEmpty(tokenResult.AccessToken))
            {
                _logger.LogWarning("No Azure Management access token available for tenant {TenantId}. Error: {ErrorType} - {ErrorMessage}", tenantId, tokenResult.ErrorType, tokenResult.ErrorMessage);
                
                // If we have a callback and this is an authentication-related failure, ask the user
                if (onAuthenticationFailure != null && IsAuthenticationFailure(tokenResult.ErrorType))
                {
                    bool shouldSkip = await onAuthenticationFailure(tenantId, tokenResult.ErrorType ?? "unknown", tokenResult.ErrorMessage ?? "Unknown error");
                    if (shouldSkip)
                    {
                        _logger.LogInformation("User chose to skip tenant {TenantId} due to authentication failure", tenantId);
                        return (false, null, true);
                    }
                    
                    // User chose to retry - attempt once more with interactive auth allowed
                    _logger.LogInformation("User chose to retry authentication for tenant {TenantId}", tenantId);
                    var retryTokenResult = await _authenticationService.GetAzureManagementAccessTokenAsync(tenantId, true);
                    if (!retryTokenResult.IsSuccess || string.IsNullOrEmpty(retryTokenResult.AccessToken))
                    {
                        _logger.LogWarning("Retry attempt failed for tenant {TenantId}. Error: {ErrorType} - {ErrorMessage}", tenantId, retryTokenResult.ErrorType, retryTokenResult.ErrorMessage);
                        return (false, null, false);
                    }
                    tokenResult = retryTokenResult;
                }
                else
                {
                    return (false, null, false);
                }
            }

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);

            // Call the Azure ManagementPartner API for this tenant
            // The correct endpoint is tenant-specific: https://management.azure.com/providers/Microsoft.ManagementPartner/partners?api-version=2018-02-01
            // But the context is determined by the token's tenant
            var requestUri = $"https://management.azure.com/providers/Microsoft.ManagementPartner/partners?api-version=2018-02-01";
            var response = await httpClient.GetAsync(requestUri);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Partner link response for tenant {TenantId}: {Content}", tenantId, content);
                
                var json = System.Text.Json.JsonDocument.Parse(content);
                string? partnerId = null;
                
                // The response may be an object or an array, so check both
                if (json.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    // Single partner response
                    if (json.RootElement.TryGetProperty("properties", out var propsProp))
                    {
                        if (propsProp.TryGetProperty("partnerId", out var partnerIdProp))
                        {
                            partnerId = partnerIdProp.GetString();
                        }
                    }
                    // Also check directly at root level
                    if (string.IsNullOrEmpty(partnerId) && json.RootElement.TryGetProperty("partnerId", out var partnerIdPropRoot))
                    {
                        partnerId = partnerIdPropRoot.GetString();
                    }
                }
                else if (json.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    // Array of partners response - take the first one
                    foreach (var item in json.RootElement.EnumerateArray())
                    {
                        if (item.TryGetProperty("properties", out var propsProp))
                        {
                            if (propsProp.TryGetProperty("partnerId", out var partnerIdProp))
                            {
                                partnerId = partnerIdProp.GetString();
                                if (!string.IsNullOrEmpty(partnerId))
                                    break;
                            }
                        }
                        // Also check directly at item level
                        if (string.IsNullOrEmpty(partnerId) && item.TryGetProperty("partnerId", out var partnerIdPropItem))
                        {
                            partnerId = partnerIdPropItem.GetString();
                            if (!string.IsNullOrEmpty(partnerId))
                                break;
                        }
                    }
                }
                
                if (!string.IsNullOrEmpty(partnerId))
                {
                    _logger.LogDebug("Found Partner ID {PartnerId} for tenant {TenantId}", partnerId, tenantId);
                    return (true, partnerId, false);
                }
                else
                {
                    _logger.LogDebug("No Partner ID found in response for tenant {TenantId}", tenantId);
                    return (false, null, false);
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // No partner link exists
                return (false, null, false);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to get partner link for tenant {TenantId}: {StatusCode} {Content}", tenantId, response.StatusCode, errorContent);
            }

            return (false, null, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check existing partner link for tenant: {TenantId}", tenantId);
            return (false, null, false);
        }
    }

    /// <summary>
    /// Check if a specific tenant has a Partner ID already linked with delayed user interaction for authentication issues
    /// </summary>
    public async Task<(bool hasLink, string? partnerId, bool skipped)> CheckExistingPartnerLinkWithTimeoutAsync(string tenantId, Func<string, Task<bool>>? onAuthenticationTimeout, int timeoutSeconds = 5)
    {
        try
        {
            _logger.LogDebug("Checking existing partner link for tenant with timeout: {TenantId}", tenantId);

            // Create a cancellation token that will trigger the timeout
            using var cancellationTokenSource = new CancellationTokenSource();
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), cancellationTokenSource.Token);
            
            // Start the authentication attempt with interactive auth allowed
            var authTask = _authenticationService.GetAzureManagementAccessTokenAsync(tenantId, true);
            
            // Wait for either authentication to complete or timeout
            var completedTask = await Task.WhenAny(authTask, timeoutTask);
            
            if (completedTask == timeoutTask && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                // Timeout occurred - ask user if they want to skip
                if (onAuthenticationTimeout != null)
                {
                    bool shouldSkip = await onAuthenticationTimeout(tenantId);
                    if (shouldSkip)
                    {
                        _logger.LogInformation("User chose to skip tenant {TenantId} due to authentication timeout", tenantId);
                        cancellationTokenSource.Cancel(); // Cancel the timeout task
                        return (false, null, true);
                    }
                    
                    // User chose to wait - continue waiting for authentication
                    _logger.LogInformation("User chose to continue waiting for authentication for tenant {TenantId}", tenantId);
                }
                
                // Wait for authentication to complete
                cancellationTokenSource.Cancel(); // Cancel the timeout task since we're continuing
            }
            else
            {
                // Authentication completed before timeout
                cancellationTokenSource.Cancel(); // Cancel the timeout task
            }
            
            // Get the authentication result
            var tokenResult = await authTask;
            if (!tokenResult.IsSuccess || string.IsNullOrEmpty(tokenResult.AccessToken))
            {
                _logger.LogWarning("No Azure Management access token available for tenant {TenantId}. Error: {ErrorType} - {ErrorMessage}", tenantId, tokenResult.ErrorType, tokenResult.ErrorMessage);
                return (false, null, false);
            }

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);

            // Call the Azure ManagementPartner API for this tenant
            var requestUri = $"https://management.azure.com/providers/Microsoft.ManagementPartner/partners?api-version=2018-02-01";
            var response = await httpClient.GetAsync(requestUri);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Partner link response for tenant {TenantId}: {Content}", tenantId, content);
                
                var json = System.Text.Json.JsonDocument.Parse(content);
                string? partnerId = null;
                
                // The response may be an object or an array, so check both
                if (json.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    // Single partner response
                    if (json.RootElement.TryGetProperty("properties", out var propsProp))
                    {
                        if (propsProp.TryGetProperty("partnerId", out var partnerIdProp))
                        {
                            partnerId = partnerIdProp.GetString();
                        }
                    }
                    // Also check directly at root level
                    if (string.IsNullOrEmpty(partnerId) && json.RootElement.TryGetProperty("partnerId", out var partnerIdPropRoot))
                    {
                        partnerId = partnerIdPropRoot.GetString();
                    }
                }
                else if (json.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    // Array of partners response - take the first one
                    foreach (var item in json.RootElement.EnumerateArray())
                    {
                        if (item.TryGetProperty("properties", out var propsProp))
                        {
                            if (propsProp.TryGetProperty("partnerId", out var partnerIdProp))
                            {
                                partnerId = partnerIdProp.GetString();
                                if (!string.IsNullOrEmpty(partnerId))
                                    break;
                            }
                        }
                        // Also check directly at item level
                        if (string.IsNullOrEmpty(partnerId) && item.TryGetProperty("partnerId", out var partnerIdPropItem))
                        {
                            partnerId = partnerIdPropItem.GetString();
                            if (!string.IsNullOrEmpty(partnerId))
                                break;
                        }
                    }
                }
                
                if (!string.IsNullOrEmpty(partnerId))
                {
                    _logger.LogDebug("Found Partner ID {PartnerId} for tenant {TenantId}", partnerId, tenantId);
                    return (true, partnerId, false);
                }
                else
                {
                    _logger.LogDebug("No Partner ID found in response for tenant {TenantId}", tenantId);
                    return (false, null, false);
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // No partner link exists
                return (false, null, false);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to get partner link for tenant {TenantId}: {StatusCode} {Content}", tenantId, response.StatusCode, errorContent);
            }

            return (false, null, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check existing partner link for tenant: {TenantId}", tenantId);
            return (false, null, false);
        }
    }

    /// <summary>
    /// Determines if an error type represents an authentication failure that could benefit from user interaction
    /// </summary>
    private static bool IsAuthenticationFailure(string? errorType)
    {
        if (string.IsNullOrEmpty(errorType))
            return false;
            
        var authFailureTypes = new[] { "mfa_required", "consent_required", "basic_action", "ui_required" };
        return authFailureTypes.Contains(errorType.ToLowerInvariant());
    }

    /// <summary>
    /// Get detailed information about a specific tenant
    /// </summary>
    public async Task<Tenant?> GetTenantDetailsAsync(string tenantId)
    {
        try
        {
            _logger.LogDebug("Getting details for tenant: {TenantId}", tenantId);

            // For demonstration purposes, return a sample tenant
            // In a real implementation, this would call Microsoft Graph API
            await Task.Delay(100); // Simulate API call

            return new Tenant
            {
                Id = tenantId,
                DisplayName = "Sample Organization",
                Domain = "sample.onmicrosoft.com",
                IsGuestUser = false,
                UserRoles = new List<string> { "User" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tenant details for: {TenantId}", tenantId);
            return null;
        }
    }
}