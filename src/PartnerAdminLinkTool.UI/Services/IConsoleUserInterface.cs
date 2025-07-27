namespace PartnerAdminLinkTool.UI.Services;

/// <summary>
/// Interface for the console user interface service.
/// 
/// For beginners: This interface defines the contract for our console-based
/// user interface. By using an interface, we can easily swap out different
/// UI implementations (console, GUI, web, etc.) without changing our core logic.
/// </summary>
public interface IConsoleUserInterface
{
    /// <summary>
    /// Run the main application loop
    /// </summary>
    Task RunAsync();
}