using System.Windows;
using Azure.Identity;
using Azure.ResourceManager;
using Microsoft.Extensions.Logging;

namespace InvoiceRobot.Admin;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static ArmClient? ArmClient { get; private set; }
    public static ILogger? Logger { get; private set; }
    private static ILoggerFactory? _loggerFactory;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Initialize logger (must be before anything else)
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            Logger = _loggerFactory.CreateLogger<App>();

            // Initialize Azure authentication using DefaultAzureCredential
            // This supports multiple authentication methods:
            // 1. Environment variables
            // 2. Managed Identity
            // 3. Visual Studio
            // 4. Azure CLI (az login)
            // 5. Azure PowerShell
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeVisualStudioCredential = false,
                ExcludeVisualStudioCodeCredential = false,
                ExcludeAzureCliCredential = false,
                ExcludeAzurePowerShellCredential = false,
                ExcludeInteractiveBrowserCredential = true // Don't pop up browser automatically
            });

            // Create ArmClient for Azure Resource Manager operations
            ArmClient = new ArmClient(credential);

            Logger.LogInformation("InvoiceRobot Admin started successfully");
            Logger.LogInformation("Azure authentication initialized");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Virhe Azure-autentikoinnissa:\n\n{ex.Message}\n\nVarmista että olet kirjautunut Azureen (az login tai kirjaudu Visual Studioon).\n\nSovellus jatkaa ilman Azure-yhteyttä.",
                "Varoitus",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            Logger?.LogError(ex, "Virhe Azure-autentikoinnissa");
            // EI suljeta sovellusta - jatketaan ilman Azure-yhteyttä
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _loggerFactory?.Dispose();
        base.OnExit(e);
    }
}

