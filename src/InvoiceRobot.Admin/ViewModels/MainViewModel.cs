using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace InvoiceRobot.Admin.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    // Singleton ViewModels to preserve state across navigation
    private readonly DeploymentWizardViewModel _deploymentViewModel;
    private readonly ConfigurationViewModel _configurationViewModel;
    private readonly InvoicesViewModel _invoicesViewModel;
    private readonly LogsViewModel _logsViewModel;

    [ObservableProperty]
    private string _title = "InvoiceRobot Admin";

    [ObservableProperty]
    private object? _currentView;

    public MainViewModel()
    {
        // Create ViewModels once and reuse them
        _deploymentViewModel = new DeploymentWizardViewModel();
        _configurationViewModel = new ConfigurationViewModel();
        _invoicesViewModel = new InvoicesViewModel();
        _logsViewModel = new LogsViewModel();

        // Set initial view
        CurrentView = _deploymentViewModel;
    }

    [RelayCommand]
    private void NavigateToDeployment()
    {
        CurrentView = _deploymentViewModel;
    }

    [RelayCommand]
    private void NavigateToConfiguration()
    {
        CurrentView = _configurationViewModel;
    }

    [RelayCommand]
    private void NavigateToInvoices()
    {
        CurrentView = _invoicesViewModel;
    }

    [RelayCommand]
    private void NavigateToLogs()
    {
        CurrentView = _logsViewModel;
    }

    public void Dispose()
    {
        // Dispose ViewModels that implement IDisposable
        if (_deploymentViewModel is IDisposable d1) d1.Dispose();
        if (_configurationViewModel is IDisposable d2) d2.Dispose();
        if (_invoicesViewModel is IDisposable d3) d3.Dispose();
        if (_logsViewModel is IDisposable d4) d4.Dispose();
    }
}
