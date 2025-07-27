using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PartnerAdminLinkTool.Core.Services;
using PartnerAdminLinkTool.UI.Services;
using Spectre.Console;

namespace PartnerAdminLinkTool.UI;

/// <summary>
/// Main entry point for the Partner Admin Link Tool application.
/// 
/// For beginners: This is a console-based version of our application that provides
/// a text-based user interface. It demonstrates all the core functionality while
/// being cross-platform compatible.
/// 
/// Note: A WPF (Windows-only) GUI version can be created later for Windows users.
/// </summary>
class Program
{
    /// <summary>
    /// Application entry point
    /// </summary>
    static async Task Main(string[] args)
    {
        // Display a welcome banner
        AnsiConsole.Write(
            new FigletText("PAL Tool")
                .LeftJustified()
                .Color(Color.Blue));

        AnsiConsole.MarkupLine("[bold blue]Microsoft Partner Admin Link (PAL) Tool[/]");
        AnsiConsole.MarkupLine("[grey]Link your Partner ID to all accessible Microsoft Entra tenants[/]");
        AnsiConsole.WriteLine();

        // Build the host with dependency injection
        var host = CreateHostBuilder(args).Build();

        try
        {
            // Get the console UI service and run the application
            var consoleUI = host.Services.GetRequiredService<IConsoleUserInterface>();
            await consoleUI.RunAsync();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Fatal error: {ex.Message}[/]");
            Environment.Exit(1);
        }
        finally
        {
            host.Dispose();
        }
    }

    /// <summary>
    /// Configure the host and dependency injection container
    /// </summary>
    static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(services, context.Configuration);
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });
    }

    /// <summary>
    /// Configure all services for dependency injection
    /// </summary>
    static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register configuration
        services.AddSingleton(configuration);

        // Register Core Services
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<ITenantDiscoveryService, TenantDiscoveryService>();
        services.AddSingleton<IPartnerLinkService, PartnerLinkService>();

        // Register UI Services
        services.AddTransient<IConsoleUserInterface, ConsoleUserInterface>();
    }
}