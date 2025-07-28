using Microsoft.Extensions.Logging;
using PartnerAdminLinkTool.Core.Models;
using PartnerAdminLinkTool.Core.Services;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PartnerAdminLinkTool.UI.Services
{
    /// <summary>
    /// Console-based user interface for the Partner Admin Link Tool.
    /// </summary>
    public class ConsoleUserInterface : IConsoleUserInterface
    {
        private readonly ILogger<ConsoleUserInterface> _logger;
        private readonly IAuthenticationService _authenticationService;
        private readonly ITenantDiscoveryService _tenantDiscoveryService;
        private readonly IPartnerLinkService _partnerLinkService;
        // No Graph API dependencies needed; all operations use Azure Management API
        private static List<(Tenant tenant, string previousPartnerId, string newPartnerId, PartnerLinkResult result)> _lastLinkingResults = new();
        private bool _skipAllAuthenticationFailures = false;
        private readonly SemaphoreSlim _authPromptSemaphore = new(1, 1);

        public ConsoleUserInterface(
            ILogger<ConsoleUserInterface> logger,
            IAuthenticationService authenticationService,
            ITenantDiscoveryService tenantDiscoveryService,
            IPartnerLinkService partnerLinkService)
        {
            _logger = logger;
            _authenticationService = authenticationService;
            _tenantDiscoveryService = tenantDiscoveryService;
            _partnerLinkService = partnerLinkService;
        }

        public async Task RunAsync()
        {
            try
            {
                _logger.LogInformation("Starting console user interface");
                
                // Reset authentication failure skip flag at start of session
                _skipAllAuthenticationFailures = false;
                
                await TrySignInSilentlyAsync();
                bool continueRunning = true;
                while (continueRunning)
                {
                    continueRunning = await ShowMainMenuAsync();
                }
                AnsiConsole.MarkupLine("[green]Thank you for using the Partner Admin Link Tool![/]");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in console UI");
                AnsiConsole.MarkupLine($"[red]An error occurred: {ex.Message}[/]");
            }
        }

        private async Task TrySignInSilentlyAsync()
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Checking for cached credentials...", async ctx =>
                {
                    var state = await _authenticationService.TrySignInSilentlyAsync();
                    if (state.IsAuthenticated)
                    {
                        AnsiConsole.MarkupLine($"[green]✓[/] Signed in as [cyan]{state.UserPrincipalName}[/]");
                    }
                });
        }

        private void ShowAuthenticationStatus()
        {
            var state = _authenticationService.CurrentState;
            var panel = new Panel(
                new Markup($"[green]✓ Signed in as:[/] [cyan]{state.UserPrincipalName}[/]\n" +
                          $"[green]Home Tenant:[/] [yellow]{state.HomeTenantId}[/]\n" +
                          $"[green]Last Auth:[/] [grey]{state.LastAuthenticationTime:yyyy-MM-dd HH:mm:ss} UTC[/]"))
                .Header("Authentication Status")
                .BorderColor(Color.Green);
            AnsiConsole.Write(panel);
        }

        private async Task SignInInteractiveAsync()
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Signing in...", async ctx =>
                {
                    // Only use Azure Management API for authentication
                    var state = await _authenticationService.SignInInteractiveAsync();
                    if (state.IsAuthenticated)
                    {
                        AnsiConsole.MarkupLine($"[green]✓ Successfully signed in as {state.UserPrincipalName}[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]✗ Sign in failed[/]");
                    }
                });
        }

        private async Task SignInWithDeviceCodeAsync()
        {
            AnsiConsole.MarkupLine("[yellow]Starting device code authentication...[/]");
            // Only use Azure Management API for device code authentication
            var state = await _authenticationService.SignInWithDeviceCodeAsync((userCode, verificationUrl) =>
            {
                var panel = new Panel(
                    new Markup($"[bold yellow]Device Code Authentication[/]\n\n" +
                              $"1. Go to: [link]{verificationUrl}[/]\n" +
                              $"2. Enter code: [bold cyan]{userCode}[/]\n" +
                              $"3. Sign in with your Microsoft account\n\n" +
                              $"[grey]Waiting for you to complete authentication...[/]"))
                    .Header("Authentication Required")
                    .BorderColor(Color.Yellow);
                AnsiConsole.Write(panel);
                return Task.CompletedTask;
            });
            if (state.IsAuthenticated)
            {
                AnsiConsole.MarkupLine($"[green]✓ Successfully signed in as {state.UserPrincipalName}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]✗ Device code sign in failed[/]");
            }
        }

        private async Task<bool> ShowMainMenuAsync()
        {
            AnsiConsole.WriteLine();
            var isAuthenticated = _authenticationService.CurrentState.IsAuthenticated;
            if (isAuthenticated)
            {
                ShowAuthenticationStatus();
            }
            var menuOptions = new List<string>();
            if (!isAuthenticated)
            {
                menuOptions.Add("🔐 Sign In (Interactive)");
                menuOptions.Add("📱 Sign In (Device Code)");
            }
            else
            {
                menuOptions.Add("🔍 Discover Tenants");
                menuOptions.Add("🔗 Link Partner ID to All Tenants");
                menuOptions.Add("📋 Show Current Status");
                menuOptions.Add("🚪 Sign Out");
            }
            menuOptions.Add("❓ About Partner Admin Link (PAL)");
            menuOptions.Add("❌ Exit");
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Select an option:[/]")
                    .AddChoices(menuOptions));
            return await HandleMenuChoiceAsync(choice);
        }

        private async Task<bool> HandleMenuChoiceAsync(string choice)
        {
            try
            {
                switch (choice)
                {
                    case "🔐 Sign In (Interactive)":
                        await SignInInteractiveAsync();
                        break;
                    case "📱 Sign In (Device Code)":
                        await SignInWithDeviceCodeAsync();
                        break;
                    case "🔍 Discover Tenants":
                        await DiscoverTenantsAsync();
                        break;
                    case "🔗 Link Partner ID to All Tenants":
                        await LinkPartnerIdToAllTenantsAsync();
                        break;
                    case "📋 Show Current Status":
                        await ShowCurrentStatusAsync();
                        break;
                    case "🚪 Sign Out":
                        await SignOutAsync();
                        break;
                    case "❓ About Partner Admin Link (PAL)":
                        ShowAboutPAL();
                        break;
                    case "❌ Exit":
                        return false;
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error handling menu choice: {ex.Message}[/]");
            }
            return true;
        }

        private async Task DiscoverTenantsAsync()
        {
            List<Tenant> tenants = new();
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Discovering tenants...", async ctx =>
                {
                    tenants = await _tenantDiscoveryService.DiscoverTenantsAsync(OnAuthenticationFailureWithTimeoutAsync);
                });
            if (tenants.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No tenants found.[/]");
                return;
            }
            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold blue]Discovered Tenants[/]");
            table.AddColumn("[grey]Tenant ID[/]");
            foreach (var tenant in tenants)
            {
                table.AddRow($"[grey]{tenant.Id}[/]");
            }
            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"\n[green]Found {tenants.Count} accessible tenant(s)[/]");
        }

        private async Task LinkPartnerIdToAllTenantsAsync()
        {
            // Prompt for Partner ID before tenant discovery
            var partnerId = AnsiConsole.Ask<string>("Enter the Partner ID (Associated PartnerID) to link to all tenants:", "");
            if (string.IsNullOrWhiteSpace(partnerId))
            {
                AnsiConsole.MarkupLine("[red]Partner ID is required.[/]");
                return;
            }

            // Validate Partner ID
            if (!partnerId.All(char.IsDigit) || partnerId.Length < 6 || partnerId.Length > 10)
            {
                AnsiConsole.MarkupLine("[red]Partner ID must be a 6-10 digit number.[/]");
                return;
            }

            // Ask about overwriting existing partner links
            var forceOverwrite = AnsiConsole.Confirm(
                "Do you want to overwrite existing partner links? " +
                "[yellow](If 'No', tenants with different Partner IDs will be skipped)[/]", 
                false);
            
            if (forceOverwrite)
            {
                AnsiConsole.MarkupLine("[yellow]⚠️  Overwrite mode enabled: Existing partner links will be replaced.[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[blue]ℹ️  Safe mode: Tenants with different Partner IDs will be skipped.[/]");
            }

            // Discover tenants (after Partner ID input)
            var tenants = await _tenantDiscoveryService.DiscoverTenantsAsync(OnAuthenticationFailureWithTimeoutAsync);
            if (tenants.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No tenants found to link Partner ID to.[/]");
                return;
            }
            // Always fetch and display current Partner ID for each tenant before linking, with robust interactive fallback
            var results = new List<PartnerLinkResult>();
            var linkingResults = new List<(Tenant tenant, string previousPartnerId, string newPartnerId, PartnerLinkResult result)>();
            await AnsiConsole.Progress()
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new RemainingTimeColumn(),
                    new SpinnerColumn())
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[green]Linking Partner ID to tenants[/]");
                    task.MaxValue = tenants.Count;
                    int completed = 0;
                    foreach (var tenant in tenants)
                    {
                        task.Description = $"[green]Processing[/] [cyan]{tenant.Id}[/]";
                        string previousPartnerId = tenant.CurrentPartnerLink ?? string.Empty;
                        string newPartnerId = partnerId;
                        PartnerLinkResult result;
                    // Use partner link information already gathered during tenant discovery
                    // This avoids authentication prompts during progress display which causes UI conflicts
                    AnsiConsole.MarkupLine($"[green]Current Partner ID for {tenant.Id}: {tenant.CurrentPartnerLink ?? "None"}[/]");

                    if (!string.IsNullOrEmpty(previousPartnerId) && previousPartnerId == partnerId)
                        {
                            result = new PartnerLinkResult
                            {
                                Tenant = tenant,
                                IsSuccess = true,
                                Details = "Already linked to this Partner ID.",
                                ErrorMessage = null
                            };
                        }
                        else
                        {
                            result = new PartnerLinkResult { Tenant = tenant, IsSuccess = false, ErrorMessage = "Unknown error" };
                            bool linkAttempted = false;
                            int linkAttempts = 0;
                            while (!linkAttempted && linkAttempts < 2)
                            {
                                try
                                {
                                    var linkResults = await _partnerLinkService.LinkPartnerIdToMultipleTenantsAsync(
                                        partnerId, new List<Tenant> { tenant }, null, forceOverwrite).ConfigureAwait(false);
                                    result = linkResults != null && linkResults.Count > 0
                                        ? linkResults[0]
                                        : new PartnerLinkResult { Tenant = tenant, IsSuccess = false, ErrorMessage = "Unknown error" };
                                    // If PartnerIdAlreadyLinked error, the Partner ID should have been retrieved by PartnerLinkService
                                    if (!result.IsSuccess && !string.IsNullOrEmpty(result.ErrorMessage) && 
                                        (result.ErrorMessage.Contains("PartnerIdAlreadyLinked") || result.ErrorMessage.Contains("conflict")))
                                    {
                                        // The PartnerLinkService should have retrieved the Partner ID and set it in tenant.CurrentPartnerLink
                                        var retrievedPartnerId = tenant.CurrentPartnerLink;
                                        if (!string.IsNullOrEmpty(retrievedPartnerId))
                                        {
                                            previousPartnerId = retrievedPartnerId;
                                            AnsiConsole.MarkupLine($"[blue]Retrieved Partner ID {retrievedPartnerId} from PartnerLinkService for tenant {tenant.Id}[/]");
                                            if (retrievedPartnerId == partnerId)
                                            {
                                                result.IsSuccess = true;
                                                result.Details = "Already linked to this Partner ID.";
                                            }
                                            else
                                            {
                                                result.IsSuccess = false;
                                                result.Details = $"Already linked to a different Partner ID: {retrievedPartnerId}.";
                                            }
                                        }
                                        else
                                        {
                                            // Enhanced fallback: try to extract Partner ID from error message directly
                                            var errorMessage = result.ErrorMessage;
                                            if (!string.IsNullOrEmpty(errorMessage))
                                            {
                                                var patterns = new[]
                                                {
                                                    @"partnerId['""]?:?\s*['""]?(\d{6,10})",        
                                                    @"partner[_ ]?id[^\d]*(\d{6,10})",              
                                                    @"AI[_ ]?Cloud[_ ]?Partner[_ ]?Program[_ ]?ID[^\d]*(\d{6,10})",
                                                    @"Associated[_ ]?PartnerID[^\d]*(\d{6,10})",                  
                                                    @"already\s+linked[^\d]*(\d{6,10})",            
                                                    @"existing[_ ]?partner[^\d]*(\d{6,10})",        
                                                    @"current[_ ]?partner[^\d]*(\d{6,10})",         
                                                    @"conflict.*?(\d{6,10})",                       
                                                    @"management[_ ]?partner[^\d]*(\d{6,10})",      
                                                    @"\bpartner\b[^\d]*(\d{6,10})",                
                                                    @"\b(\d{6,10})\b"                              
                                                };
                                                
                                                foreach (var pattern in patterns)
                                                {
                                                    var match = System.Text.RegularExpressions.Regex.Match(errorMessage, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                                    if (match.Success)
                                                    {
                                                        var extractedId = match.Groups[1].Value;
                                                        if (extractedId.Length >= 6 && extractedId.Length <= 10 && extractedId.All(char.IsDigit))
                                                        {
                                                            previousPartnerId = extractedId;
                                                            tenant.CurrentPartnerLink = extractedId;
                                                            AnsiConsole.MarkupLine($"[blue]Extracted Partner ID {extractedId} from error message for tenant {tenant.Id}[/]");
                                                            if (extractedId == partnerId)
                                                            {
                                                                result.IsSuccess = true;
                                                                result.Details = "Already linked to this Partner ID.";
                                                            }
                                                            else
                                                            {
                                                                result.IsSuccess = false;
                                                                result.Details = $"Already linked to a different Partner ID: {extractedId}.";
                                                            }
                                                            break;
                                                        }
                                                    }
                                                }
                                            }
                                            
                                            // Final fallback: try to re-fetch current Partner ID only if extraction failed
                                            if (previousPartnerId == "Unknown" || string.IsNullOrEmpty(previousPartnerId))
                                            {
                                                try
                                                {
                                                    var (hasLinkAfter, partnerIdCurrentAfter) = await _tenantDiscoveryService.CheckExistingPartnerLinkAsync(tenant.Id);
                                                    if (!string.IsNullOrEmpty(partnerIdCurrentAfter))
                                                    {
                                                        previousPartnerId = partnerIdCurrentAfter;
                                                        tenant.CurrentPartnerLink = partnerIdCurrentAfter;
                                                        AnsiConsole.MarkupLine($"[blue]Re-fetched Partner ID {partnerIdCurrentAfter} for tenant {tenant.Id}[/]");
                                                        if (partnerIdCurrentAfter == partnerId)
                                                        {
                                                            result.IsSuccess = true;
                                                            result.Details = "Already linked to this Partner ID.";
                                                        }
                                                        else
                                                        {
                                                            result.IsSuccess = false;
                                                            result.Details = $"Already linked to a different Partner ID: {partnerIdCurrentAfter}.";
                                                        }
                                                    }
                                                    else
                                                    {
                                                        previousPartnerId = "Unknown";
                                                        result.IsSuccess = false;
                                                        result.Details = "Already linked to a Partner ID (actual ID unknown).";
                                                    }
                                                }
                                                catch (Exception)
                                                {
                                                    previousPartnerId = "Unknown";
                                                    result.IsSuccess = false;
                                                    result.Details = "Already linked to a Partner ID (actual ID unknown).";
                                                }
                                            }
                                        }
                                    }
                                    else if (result.IsSuccess)
                                    {
                                        if (string.IsNullOrEmpty(previousPartnerId))
                                        {
                                            result.Details = "Partner ID set for the first time.";
                                        }
                                        else if (previousPartnerId != partnerId)
                                        {
                                            result.Details = $"Partner ID updated from {previousPartnerId} to {partnerId}.";
                                        }
                                    }
                                    // Handle consent and MFA errors for user-friendly output
                                    else if (!result.IsSuccess && !string.IsNullOrEmpty(result.ErrorMessage))
                                    {
                                        if (result.ErrorMessage.Contains("consent required") || result.ErrorMessage.Contains("consent_required"))
                                        {
                                            result.Details = "Consent required. Admin must grant consent.";
                                        }
                                        else if (result.ErrorMessage.Contains("AADSTS50076") || result.ErrorMessage.Contains("AADSTS50079") || 
                                                result.ErrorMessage.Contains("multi-factor authentication") || result.ErrorMessage.Contains("mfa_required"))
                                        {
                                            result.Details = "Multi-factor authentication required. Automatic MFA prompt failed - please complete MFA and try again.";
                                        }
                                        else if (result.ErrorMessage.Contains("ui_required") || result.ErrorMessage.Contains("interaction_required"))
                                        {
                                            result.Details = "User interaction required. Please sign in interactively and try again.";
                                        }
                                    }
                                    linkAttempted = true;
                                }
                                catch (Exception ex)
                                {
                                    // If linking fails due to consent/MFA/ui required, prompt for interactive login and retry once
                                    if (ex.Message.Contains("consent_required") || ex.Message.Contains("mfa_required") || ex.Message.Contains("ui_required") || 
                                        ex.Message.Contains("AADSTS50076") || ex.Message.Contains("AADSTS50079") || ex is Microsoft.Identity.Client.MsalUiRequiredException)
                                    {
                                        AnsiConsole.MarkupLine($"[yellow]Additional authentication required for tenant [cyan]{tenant.Id}[/] during linking. Attempting MFA authentication...[/]");
                                        if (!(_authenticationService is PartnerAdminLinkTool.Core.Services.AuthenticationService concreteAuthService))
                                            throw new InvalidOperationException("AuthenticationService must be of type PartnerAdminLinkTool.Core.Services.AuthenticationService");
                                        
                                        // Use the new MFA handling method
                                        var tokenResult = await concreteAuthService.HandleMfaRequiredAsync(tenant.Id, $"MFA required for linking to tenant {tenant.Id}");
                                        if (tokenResult == null || !tokenResult.IsSuccess)
                                        {
                                            AnsiConsole.MarkupLine($"[red]MFA authentication failed for tenant [cyan]{tenant.Id}[/] during linking: {tokenResult?.ErrorMessage ?? "Unknown error"}[/]");
                                            result = new PartnerLinkResult
                                            {
                                                Tenant = tenant,
                                                IsSuccess = false,
                                                ErrorMessage = $"MFA authentication failed: {tokenResult?.ErrorMessage ?? "Unknown error"}",
                                                Details = "Multi-factor authentication required but failed. Please try again."
                                            };
                                            break;
                                        }
                                        AnsiConsole.MarkupLine($"[green]MFA authentication successful for tenant [cyan]{tenant.Id}[/] during linking[/]");
                                        linkAttempts++;
                                    }
                                    else
                                    {
                                        AnsiConsole.MarkupLine($"[red]Error linking Partner ID for tenant {tenant.Id}: {ex.Message}[/]");
                                        result = new PartnerLinkResult
                                        {
                                            Tenant = tenant,
                                            IsSuccess = false,
                                            ErrorMessage = ex.Message,
                                            Details = "Failed to link Partner ID due to unexpected error."
                                        };
                                        break;
                                    }
                                }
                            }
                        }
                        
                        // Final fallback: if previousPartnerId is still "Unknown" but tenant.CurrentPartnerLink was set during linking, use it
                        if ((previousPartnerId == "Unknown" || string.IsNullOrEmpty(previousPartnerId)) && !string.IsNullOrEmpty(tenant.CurrentPartnerLink))
                        {
                            previousPartnerId = tenant.CurrentPartnerLink;
                            AnsiConsole.MarkupLine($"[blue]Using Partner ID {previousPartnerId} from tenant.CurrentPartnerLink for {tenant.Id}[/]");
                        }
                        
                        // If we still don't have a previous Partner ID but the result indicates it's already linked, try one more extraction
                        if ((previousPartnerId == "Unknown" || string.IsNullOrEmpty(previousPartnerId)) && !result.IsSuccess && 
                            !string.IsNullOrEmpty(result.Details) && result.Details.Contains("Already linked"))
                        {
                            var detailsPatterns = new[]
                            {
                                @"Already linked to.*?(\d{6,10})",
                                @"different Partner ID[:\s]*(\d{6,10})",
                                @"existing.*?(\d{6,10})"
                            };
                            
                            foreach (var pattern in detailsPatterns)
                            {
                                var match = System.Text.RegularExpressions.Regex.Match(result.Details, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                if (match.Success)
                                {
                                    var extractedId = match.Groups[1].Value;
                                    if (extractedId.Length >= 6 && extractedId.Length <= 10 && extractedId.All(char.IsDigit))
                                    {
                                        previousPartnerId = extractedId;
                                        AnsiConsole.MarkupLine($"[blue]Extracted Partner ID {extractedId} from result details for {tenant.Id}[/]");
                                        break;
                                    }
                                }
                            }
                        }
                        
                        linkingResults.Add((tenant, previousPartnerId, newPartnerId, result));
                        task.Value = ++completed;
                    }
                    results = linkingResults.Select(lr => lr.result).ToList();
                    _lastLinkingResults = linkingResults;
                });
            ShowLinkingResultsWithActions(results, linkingResults);
            AnsiConsole.MarkupLine("[green]Partner ID linking process completed.[/]");
        }

        private void ShowLinkingResultsWithActions(List<PartnerLinkResult> results, List<(Tenant tenant, string previousPartnerId, string newPartnerId, PartnerLinkResult result)> linkingResults)
        {
            var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold blue]Partner ID Linking Results[/]");
        table.AddColumn("[grey]Tenant ID[/]");
        table.AddColumn("[grey]Previous Partner ID[/]");
        table.AddColumn("[grey]New Partner ID[/]");
        table.AddColumn("[grey]Status[/]");
        table.AddColumn("[grey]Details[/]");
        foreach (var (tenant, previousPartnerId, newPartnerId, result) in linkingResults)
        {
            var status = result.IsSuccess ? "[green]✓ Success[/]" : "[red]✗ Failed[/]";
            var details = result.Details ?? result.ErrorMessage ?? "";
            var prevIdDisplay = string.IsNullOrWhiteSpace(previousPartnerId) ? "[grey]Unknown[/]" : $"[yellow]{previousPartnerId}[/]";
            table.AddRow(
                $"[grey]{tenant.Id}[/]",
                prevIdDisplay,
                $"[yellow]{newPartnerId}[/]",
                status,
                $"[grey]{details}[/]"
            );
        }
        AnsiConsole.Write(table);
        }

        private async Task ShowCurrentStatusAsync()
        {
            ShowAuthenticationStatus();
            AnsiConsole.WriteLine();
            await DiscoverTenantsWithPALStatusAsync();
        }

        private async Task DiscoverTenantsWithPALStatusAsync()
        {
            List<Tenant> tenants = new();
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Discovering tenants and fetching PAL status...", async ctx =>
                {
                    tenants = await _tenantDiscoveryService.DiscoverTenantsAsync(OnAuthenticationFailureWithTimeoutAsync);
                    
                    // Fetch current partner links for each tenant
                    foreach (var tenant in tenants)
                    {
                        try
                        {
                            ctx.Status($"Checking PAL status for {tenant.Id}...");
                            var (hasLink, partnerId) = await _tenantDiscoveryService.CheckExistingPartnerLinkAsync(tenant.Id);
                            tenant.HasPartnerLink = hasLink;
                            tenant.CurrentPartnerLink = partnerId;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to check partner link for tenant {TenantId}", tenant.Id);
                            tenant.HasPartnerLink = false;
                            tenant.CurrentPartnerLink = null;
                        }
                    }
                });

            if (tenants.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No tenants found.[/]");
                return;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold blue]Discovered Tenants with PAL Status[/]");
            table.AddColumn("[grey]Tenant ID[/]");
            table.AddColumn("[grey]Current PAL Tag[/]");
            
            foreach (var tenant in tenants)
            {
                var palTag = string.IsNullOrEmpty(tenant.CurrentPartnerLink) 
                    ? "[red]None[/]" 
                    : $"[green]{tenant.CurrentPartnerLink}[/]";
                    
                table.AddRow($"[grey]{tenant.Id}[/]", palTag);
            }
            
            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"\n[green]Found {tenants.Count} accessible tenant(s)[/]");
        }

        private async Task SignOutAsync()
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Signing out...", async ctx =>
                {
                    await _authenticationService.SignOutAsync();
                });
            AnsiConsole.MarkupLine("[green]✓ Signed out successfully[/]");
        }

        /// <summary>
        /// Callback method to handle authentication failures during tenant discovery
        /// </summary>
        /// <param name="tenantId">The tenant ID that failed authentication</param>
        /// <param name="errorType">The type of authentication error</param>
        /// <param name="errorMessage">The detailed error message</param>
        /// <returns>True to skip the tenant, false to retry</returns>
        private async Task<bool> OnAuthenticationFailureAsync(string tenantId, string errorType, string errorMessage)
        {
            // If user previously chose to skip all, return immediately
            if (_skipAllAuthenticationFailures)
            {
                return true;
            }
            
            // Use semaphore to prevent multiple authentication prompts at the same time
            await _authPromptSemaphore.WaitAsync();
            try
            {
                // Check again after acquiring the lock in case another thread changed it
                if (_skipAllAuthenticationFailures)
                {
                    return true;
                }
                
                // Avoid interactive prompts during progress displays - they cause conflicts
                // Instead, automatically default to skipping with a clear notification
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[yellow]⚠️ Authentication failure for tenant {tenantId}[/]");
                AnsiConsole.MarkupLine($"[red]Error:[/] {errorType} - {errorMessage}");
                AnsiConsole.MarkupLine("[yellow]📝 Note: Automatically skipping this tenant to avoid conflicts with progress display.[/]");
                AnsiConsole.MarkupLine("[grey]   To handle authentication failures interactively, run tenant discovery separately.[/]");
                AnsiConsole.WriteLine();
                
                // For now, always skip authentication failures during bulk operations
                // This prevents the UI conflict while still allowing the operation to continue
                return true;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error in authentication failure handler: {ex.Message}[/]");
                AnsiConsole.MarkupLine("[yellow]Defaulting to skip this tenant.[/]");
                return true; // Default to skip on error
            }
            finally
            {
                _authPromptSemaphore.Release();
            }
        }

        /// <summary>
        /// Wrapper to convert timeout callback to authentication failure callback format
        /// </summary>
        private async Task<bool> OnAuthenticationFailureWithTimeoutAsync(string tenantId, string errorType, string errorMessage)
        {
            // If it's a timeout, use the timeout callback
            if (errorType == "timeout")
            {
                return await OnAuthenticationTimeoutAsync(tenantId);
            }
            
            // Otherwise, use the regular authentication failure callback
            return await OnAuthenticationFailureAsync(tenantId, errorType, errorMessage);
        }

        /// <summary>
        /// Callback method to handle authentication timeouts during tenant discovery
        /// </summary>
        /// <param name="tenantId">The tenant ID that is taking time to authenticate</param>
        /// <returns>True to skip the tenant, false to continue waiting</returns>
        private async Task<bool> OnAuthenticationTimeoutAsync(string tenantId)
        {
            await Task.CompletedTask; // Make this method async
            
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[yellow]⏱️ Authentication In Progress[/]");
            AnsiConsole.MarkupLine($"[cyan]Tenant ID:[/] {tenantId}");
            AnsiConsole.MarkupLine($"[yellow]Authentication is taking longer than expected (5+ seconds)[/]");
            AnsiConsole.MarkupLine("[grey]This usually means MFA (Multi-Factor Authentication) is required.[/]");
            AnsiConsole.MarkupLine("[grey]You might have a browser window or authentication prompt waiting for your attention.[/]");
            AnsiConsole.WriteLine();
            
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"What would you like to do for tenant [cyan]{tenantId}[/]?")
                    .AddChoices(new[] {
                        "Continue waiting (check for auth prompts)",
                        "Skip this tenant and continue with others"
                    }));
            
            bool shouldSkip = choice == "Skip this tenant and continue with others";
            
            if (shouldSkip)
            {
                AnsiConsole.MarkupLine($"[yellow]⏭️ Skipping tenant [cyan]{tenantId}[/][/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]⏳ Continuing to wait for authentication for tenant [cyan]{tenantId}[/][/]");
                AnsiConsole.MarkupLine("[dim]Check for browser windows, mobile notifications, or other authentication prompts...[/]");
            }
            
            return shouldSkip;
        }

        private static void ShowAboutPAL()
        {
            var content = new Markup(
                "[bold yellow]What is Partner Admin Link (PAL)?[/]\n\n" +
                "Partner Admin Link (PAL) enables Microsoft to identify and recognize partners " +
                "who drive Azure customer success. Microsoft can attribute influenced and Azure " +
                "consumed revenue to your organization based on the account's permissions (Azure role) " +
                "and scope (subscription, resource group, resource).\n\n" +
                "[bold cyan]Key Benefits:[/]\n" +
                "• [green]Revenue Attribution:[/] Get credited for influenced revenue from Azure consumption\n" +
                "• [green]Partner Recognition:[/] Microsoft can identify your customer engagements\n" +
                "• [green]Performance Insights:[/] Track your impact through Partner Center My Insights dashboard\n\n" +
                "[bold cyan]Requirements:[/]\n" +
                "• Valid Microsoft AI Cloud Partner Program ID (Associated PartnerID)\n" +
                "• Access to customer's Azure environment (guest user, directory account, or service principal)\n" +
                "• Appropriate Azure role-based access control (Azure RBAC) permissions\n\n" +
                "[bold red]Important:[/]\n" +
                "PAL only adds your Associated PartnerID to existing credentials. It doesn't change " +
                "permissions or provide additional access to customer data. Tracking is automated " +
                "and doesn't require partner input.\n\n" +
                "[bold cyan]Reporting:[/]\n" +
                "View influenced revenue reporting in Partner Center at the My Insights dashboard. " +
                "Select 'Partner Admin Link' as the partner association type.\n\n" +
                "[link=https://learn.microsoft.com/partner-center/membership/link-partner-id-for-azure-performance-pal-dpor?WT.mc_id=AZ-MVP-5004796]About Partner Admin Link (PAL)[/]");
            var panel = new Panel(content)
                .Header("About Partner Admin Link (PAL)")
                .BorderColor(Color.Blue);
            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
            Console.ReadKey();
        }
    }
}
