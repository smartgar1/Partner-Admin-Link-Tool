using Microsoft.Extensions.Logging;
using PartnerAdminLinkTool.Core.Models;
using System.Text.Json;
using System.Text;

namespace PartnerAdminLinkTool.Core.Services;

/// <summary>
/// Implementation for linking Partner IDs to tenants using Azure Management API.
/// 
/// For beginners: This service handles the core business logic - actually linking
/// the Partner ID to customer tenants so Microsoft can track the partner's 
/// involvement with that customer's Azure consumption.
/// </summary>
public class PartnerLinkService : IPartnerLinkService
{
    private readonly ILogger<PartnerLinkService> _logger;
    private readonly IAuthenticationService _authenticationService;
    private readonly ITenantDiscoveryService _tenantDiscoveryService;

    public PartnerLinkService(
        ILogger<PartnerLinkService> logger,
        IAuthenticationService authenticationService,
        ITenantDiscoveryService tenantDiscoveryService)
    {
        _logger = logger;
        _authenticationService = authenticationService;
        _tenantDiscoveryService = tenantDiscoveryService;
    }

    /// <summary>
    /// Link a Partner ID to a specific tenant
    /// </summary>
    public async Task<PartnerLinkResult> LinkPartnerIdAsync(string partnerId, Tenant tenant)
    {
        try
        {
            _logger.LogInformation("Linking Partner ID {PartnerId} to tenant {TenantId}", 
                partnerId, tenant.Id);

            if (!_authenticationService.CurrentState.IsAuthenticated)
            {
                return PartnerLinkResult.Failure(tenant, partnerId, 
                    "User not authenticated", "Authentication is required to link Partner ID");
            }

            // Validate Partner ID format
            if (!IsValidPartnerId(partnerId))
            {
                return PartnerLinkResult.Failure(tenant, partnerId, 
                    "Invalid Partner ID format", "Partner ID should be a numeric Microsoft AI Cloud Partner Program (Associated PartnerID)");
            }

            // Always check the current partner link before attempting to link
            var (hasLink, existingPartnerId) = await _tenantDiscoveryService.CheckExistingPartnerLinkAsync(tenant.Id);
            if (hasLink)
            {
                if (existingPartnerId == partnerId)
                {
                    _logger.LogInformation("Tenant {TenantId} already linked to Partner ID {PartnerId}", tenant.Id, partnerId);
                    tenant.CurrentPartnerLink = existingPartnerId;
                    return PartnerLinkResult.Success(tenant, partnerId, "Partner ID is already linked to this tenant.");
                }
                else
                {
                    _logger.LogWarning("Tenant {TenantId} already linked to a different Partner ID: {ExistingPartnerId}", tenant.Id, existingPartnerId);
                    tenant.CurrentPartnerLink = existingPartnerId;
                    return PartnerLinkResult.Failure(tenant, partnerId, "Tenant already linked to a different Partner ID.", $"Existing Partner ID: {existingPartnerId}");
                }
            }

            // Get Azure Management access token (with actionable error info)
            var tokenResult = await _authenticationService.GetAzureManagementAccessTokenAsync(tenant.Id);
            if (!tokenResult.IsSuccess)
            {
                // Surface actionable error to user
                var details = tokenResult.ActionUrl != null
                    ? $"{tokenResult.ErrorMessage}\nAction required: {tokenResult.ActionUrl}"
                    : tokenResult.ErrorMessage;
                return PartnerLinkResult.Failure(tenant, partnerId, tokenResult.ErrorType ?? "token_error", details);
            }

            // Create the partner link using Azure Management API
            var result = await CreatePartnerLinkAsync(partnerId, tenant, tokenResult.AccessToken!);

            // If the link failed, always re-check the current partner link and update the result object
            if (!result.IsSuccess)
            {
                var (hasLinkAfter, partnerIdCurrentAfter) = await _tenantDiscoveryService.CheckExistingPartnerLinkAsync(tenant.Id);
                if (hasLinkAfter && !string.IsNullOrEmpty(partnerIdCurrentAfter))
                {
                    tenant.CurrentPartnerLink = partnerIdCurrentAfter;
                    // If the current partner ID matches the attempted one, treat as success
                    if (partnerIdCurrentAfter == partnerId)
                    {
                        return PartnerLinkResult.Success(tenant, partnerId, "Partner ID is already linked to this tenant.");
                    }
                    else
                    {
                        return PartnerLinkResult.Failure(tenant, partnerId, "Tenant already linked to a different Partner ID.", $"Existing Partner ID: {partnerIdCurrentAfter}");
                    }
                }
                else
                {
                    tenant.CurrentPartnerLink = null;
                }
            }

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully linked Partner ID {PartnerId} to tenant {TenantId}", partnerId, tenant.Id);
            }
            else
            {
                _logger.LogWarning("Failed to link Partner ID {PartnerId} to tenant {TenantId}: {Error}", partnerId, tenant.Id, result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while linking Partner ID {PartnerId} to tenant {TenantId}", 
                partnerId, tenant.Id);
            
            return PartnerLinkResult.Failure(tenant, partnerId, 
                $"Unexpected error: {ex.Message}", ex.ToString());
        }
    }

    /// <summary>
    /// Link a Partner ID to multiple tenants with progress reporting
    /// </summary>
    public async Task<List<PartnerLinkResult>> LinkPartnerIdToMultipleTenantsAsync(
        string partnerId, 
        List<Tenant> tenants, 
        IProgress<(int completed, int total, Tenant currentTenant)>? progressCallback = null)
    {
        var results = new List<PartnerLinkResult>();
        
        _logger.LogInformation("Starting bulk Partner ID linking for {TenantCount} tenants", tenants.Count);

        for (int i = 0; i < tenants.Count; i++)
        {
            var tenant = tenants[i];
            
            // Report progress
            progressCallback?.Report((i, tenants.Count, tenant));
            
            // Link to this tenant
            var result = await LinkPartnerIdAsync(partnerId, tenant);
            results.Add(result);
            
            // Small delay to avoid overwhelming the API
            await Task.Delay(1000);
        }

        // Report completion
        progressCallback?.Report((tenants.Count, tenants.Count, tenants.Last()));

        var successCount = results.Count(r => r.IsSuccess);
        _logger.LogInformation("Bulk linking completed: {SuccessCount}/{TotalCount} successful", 
            successCount, tenants.Count);

        return results;
    }

    /// <summary>
    /// Remove a Partner ID link from a specific tenant
    /// </summary>
    public async Task<PartnerLinkResult> UnlinkPartnerIdAsync(Tenant tenant)
    {
        try
        {
            _logger.LogInformation("Unlinking Partner ID from tenant {TenantId}", 
                tenant.Id);

            if (!_authenticationService.CurrentState.IsAuthenticated)
            {
                return PartnerLinkResult.Failure(tenant, "", 
                    "User not authenticated", "Authentication is required to unlink Partner ID");
            }

            // Get Azure Management access token
            var accessToken = await _authenticationService.GetAzureManagementAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                return PartnerLinkResult.Failure(tenant, "", 
                    "No Azure Management access token", "Unable to obtain access token for Azure Management API");
            }

            // Remove the partner link using Azure Management API
            var result = await RemovePartnerLinkAsync(tenant, accessToken);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully unlinked Partner ID from tenant {TenantId}", tenant.Id);
            }
            else
            {
                _logger.LogWarning("Failed to unlink Partner ID from tenant {TenantId}: {Error}", 
                    tenant.Id, result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while unlinking Partner ID from tenant {TenantId}", tenant.Id);
            
            return PartnerLinkResult.Failure(tenant, "", 
                $"Unexpected error: {ex.Message}", ex.ToString());
        }
    }

    /// <summary>
    /// Validate that a Partner ID is valid and accessible
    /// </summary>
    public Task<bool> ValidatePartnerIdAsync(string partnerId)
    {
        try
        {
            _logger.LogDebug("Validating Partner ID: {PartnerId}", partnerId);

            // Basic format validation
            if (!IsValidPartnerId(partnerId))
            {
                return Task.FromResult(false);
            }

            // Could add more sophisticated validation here, such as:
            // - Checking if the Partner ID exists in Microsoft Partner Center
            // - Verifying the user has access to this Partner ID
            // For now, we'll just do basic validation

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while validating Partner ID: {PartnerId}", partnerId);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Create a partner link using Azure Management API
    /// </summary>
    private async Task<PartnerLinkResult> CreatePartnerLinkAsync(string partnerId, Tenant tenant, string accessToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            // Azure Management API endpoint for creating partner links
            var requestUri = $"https://management.azure.com/providers/Microsoft.ManagementPartner/partners/{partnerId}?api-version=2018-02-01";

            // Create the request payload
            var payload = new
            {
                properties = new
                {
                    partnerId = partnerId
                }
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Make the PUT request to create the partner link
            var response = await httpClient.PutAsync(requestUri, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return PartnerLinkResult.Success(tenant, partnerId, 
                    $"Partner link created successfully. Response: {responseContent}");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                string? previousPartnerId = null;
                string? errorType = null;
                
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(errorContent);
                    var root = doc.RootElement;
                    
                    // First try to get error code from the standard Azure error format
                    if (root.TryGetProperty("error", out var errorObj))
                    {
                        if (errorObj.TryGetProperty("code", out var codeProp))
                        {
                            errorType = codeProp.GetString();
                            _logger.LogDebug("Error code extracted: {ErrorCode}", errorType);
                        }
                    }
                }
                catch (Exception ex) 
                { 
                    _logger.LogDebug("Error parsing JSON response: {Error}", ex.Message);
                }

                // If we get a PartnerIdAlreadyLinked error, we need to retrieve the existing Partner ID separately
                if (!string.IsNullOrEmpty(errorType) && 
                    (errorType.Equals("PartnerIdAlreadyLinked", StringComparison.OrdinalIgnoreCase) || 
                     errorType.Contains("conflict", StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogDebug("PartnerIdAlreadyLinked error detected, retrieving existing Partner ID for tenant {TenantId}", tenant.Id);
                    
                    try
                    {
                        // Retrieve the existing Partner ID using the GET partners endpoint
                        var getUri = "https://management.azure.com/providers/Microsoft.ManagementPartner/partners?api-version=2018-02-01";
                        var getResponse = await httpClient.GetAsync(getUri);
                        
                        if (getResponse.IsSuccessStatusCode)
                        {
                            var getContent = await getResponse.Content.ReadAsStringAsync();
                            _logger.LogDebug("GET partners response: {Content}", getContent);
                            
                            var getJson = System.Text.Json.JsonDocument.Parse(getContent);
                            
                            // Parse the response to extract the existing Partner ID
                            if (getJson.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                            {
                                // Single partner response
                                if (getJson.RootElement.TryGetProperty("properties", out var propsProp))
                                {
                                    if (propsProp.TryGetProperty("partnerId", out var partnerIdProp))
                                    {
                                        previousPartnerId = partnerIdProp.GetString();
                                    }
                                }
                                // Also check directly at root level
                                if (string.IsNullOrEmpty(previousPartnerId) && getJson.RootElement.TryGetProperty("partnerId", out var partnerIdPropRoot))
                                {
                                    previousPartnerId = partnerIdPropRoot.GetString();
                                }
                            }
                            else if (getJson.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array && getJson.RootElement.GetArrayLength() > 0)
                            {
                                // Array of partners response - take the first one
                                var firstItem = getJson.RootElement[0];
                                if (firstItem.TryGetProperty("properties", out var propsProp))
                                {
                                    if (propsProp.TryGetProperty("partnerId", out var partnerIdProp))
                                    {
                                        previousPartnerId = partnerIdProp.GetString();
                                    }
                                }
                                // Also check directly at item level
                                if (string.IsNullOrEmpty(previousPartnerId) && firstItem.TryGetProperty("partnerId", out var partnerIdPropItem))
                                {
                                    previousPartnerId = partnerIdPropItem.GetString();
                                }
                            }
                            
                            if (!string.IsNullOrEmpty(previousPartnerId))
                            {
                                _logger.LogInformation("Retrieved existing Partner ID {PartnerId} for tenant {TenantId}", previousPartnerId, tenant.Id);
                            }
                            else
                            {
                                _logger.LogWarning("No Partner ID found in GET response despite PartnerIdAlreadyLinked error");
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Failed to retrieve existing Partner ID: GET request failed with status {StatusCode}", getResponse.StatusCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Exception while retrieving existing Partner ID for tenant {TenantId}", tenant.Id);
                    }
                }

                if (!string.IsNullOrEmpty(previousPartnerId))
                {
                    tenant.CurrentPartnerLink = previousPartnerId;
                    _logger.LogInformation("Set tenant.CurrentPartnerLink to {PartnerId} for tenant {TenantId}", previousPartnerId, tenant.Id);
                }

                // Add previousPartnerId to details if found
                var details = errorContent;
                if (!string.IsNullOrEmpty(previousPartnerId))
                {
                    details = $"Previous Partner ID: {previousPartnerId}\n{errorContent}";
                }

                return PartnerLinkResult.Failure(tenant, partnerId, 
                    errorType ?? $"API request failed with status {response.StatusCode}", details);
            }
        }
        catch (Exception ex)
        {
            return PartnerLinkResult.Failure(tenant, partnerId, 
                $"HTTP request failed: {ex.Message}", ex.ToString());
        }
    }

    /// <summary>
    /// Remove a partner link using Azure Management API
    /// </summary>
    private async Task<PartnerLinkResult> RemovePartnerLinkAsync(Tenant tenant, string accessToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            // First, get the current partner link to find the partner ID
            var getUri = "https://management.azure.com/providers/Microsoft.ManagementPartner/partners?api-version=2018-02-01";
            var getResponse = await httpClient.GetAsync(getUri);

            if (!getResponse.IsSuccessStatusCode)
            {
                return PartnerLinkResult.Failure(tenant, "", 
                    "Unable to retrieve current partner link", await getResponse.Content.ReadAsStringAsync());
            }

            var getContent = await getResponse.Content.ReadAsStringAsync();
            
            // Parse the response to get the partner ID (simplified parsing)
            // In a real implementation, you'd properly parse the JSON
            if (string.IsNullOrEmpty(getContent) || !getContent.Contains("partnerId"))
            {
                return PartnerLinkResult.Failure(tenant, "", 
                    "No partner link found to remove", "Tenant does not have an existing partner link");
            }

            // For now, assume we can extract the partner ID
            var partnerId = "existing-partner-id"; // Simplified

            // Azure Management API endpoint for deleting partner links
            var deleteUri = $"https://management.azure.com/providers/Microsoft.ManagementPartner/partners/{partnerId}?api-version=2018-02-01";

            // Make the DELETE request to remove the partner link
            var deleteResponse = await httpClient.DeleteAsync(deleteUri);

            if (deleteResponse.IsSuccessStatusCode)
            {
                return PartnerLinkResult.Success(tenant, partnerId, 
                    "Partner link removed successfully");
            }
            else
            {
                var errorContent = await deleteResponse.Content.ReadAsStringAsync();
                return PartnerLinkResult.Failure(tenant, partnerId, 
                    $"Delete request failed with status {deleteResponse.StatusCode}", errorContent);
            }
        }
        catch (Exception ex)
        {
            return PartnerLinkResult.Failure(tenant, "", 
                $"HTTP request failed: {ex.Message}", ex.ToString());
        }
    }

    /// <summary>
    /// Validate Partner ID format
    /// </summary>
    private static bool IsValidPartnerId(string partnerId)
    {
        // Partner ID should be a numeric string (Associated PartnerID)
        return !string.IsNullOrWhiteSpace(partnerId) && 
               partnerId.All(char.IsDigit) && 
               partnerId.Length >= 6 && 
               partnerId.Length <= 10;
    }

    /// <summary>
    /// Extract Partner ID from error message or content using multiple regex patterns
    /// </summary>
    private string? ExtractPartnerIdFromMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return null;

        // Try multiple regex patterns to extract Partner ID
        var patterns = new[]
        {
            @"partnerId['""]?:?\s*['""]?(\d+)",        // "partnerId": "1234567" or partnerId: 1234567
            @"partner[_ ]?id[^\d]*(\d+)",              // Partner ID: 1234567, partner_id 1234567, etc.
            @"AI[_ ]?Cloud[_ ]?Partner[_ ]?Program[_ ]?ID[^\d]*(\d+)", // AI Cloud Partner Program ID 1234567
            @"Associated[_ ]?PartnerID[^\d]*(\d+)",    // Associated PartnerID 1234567
            @"already\s+linked[^\d]*(\d+)",            // already linked to 1234567
            @"existing[_ ]?partner[^\d]*(\d+)",        // existing partner 1234567
            @"current[_ ]?partner[^\d]*(\d+)",         // current partner 1234567
            @"conflict.*?(\d{6,10})",                  // conflict ... 1234567 (for partner ID already linked errors)
            @"management[_ ]?partner[^\d]*(\d+)",      // management partner 1234567
            @"\bpartner\b[^\d]*(\d{6,10})",           // partner 1234567 (6-10 digit number)
            @"\b(\d{6,10})\b"                          // any 6-10 digit number (last resort)
        };

        foreach (var pattern in patterns)
        {
            var idMatch = System.Text.RegularExpressions.Regex.Match(message, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (idMatch.Success)
            {
                var extractedId = idMatch.Groups[1].Value;
                // Validate extracted ID is reasonable length for a Partner ID
                if (extractedId.Length >= 6 && extractedId.Length <= 10 && extractedId.All(char.IsDigit))
                {
                    _logger.LogDebug("Extracted Partner ID {PartnerId} using pattern: {Pattern}", extractedId, pattern);
                    return extractedId;
                }
                else
                {
                    _logger.LogDebug("Skipped extracted value {ExtractedValue} - invalid Partner ID format", extractedId);
                }
            }
        }

        return null;
    }
}