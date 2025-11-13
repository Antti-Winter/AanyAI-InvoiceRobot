using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoiceRobot.Admin.Models;
using InvoiceRobot.Admin.Services;
using System.Collections.ObjectModel;

namespace InvoiceRobot.Admin.ViewModels;

public partial class DeploymentWizardViewModel : ObservableObject, IDisposable
{
    private readonly AzureDeploymentService _deploymentService;
    private bool _disposed;

    [ObservableProperty]
    private int _currentStep = 1;

    [ObservableProperty]
    private DeploymentConfig _config = new();

    [ObservableProperty]
    private ObservableCollection<string> _deploymentSteps = new();

    [ObservableProperty]
    private int _deploymentProgress = 0;

    [ObservableProperty]
    private bool _isDeploying = false;

    [ObservableProperty]
    private bool _deploymentSucceeded = false;

    [ObservableProperty]
    private string? _deploymentError = null;

    public DeploymentWizardViewModel()
    {
        _deploymentService = new AzureDeploymentService(
            App.ArmClient!,
            App.Logger!);

        _deploymentService.ProgressChanged += OnDeploymentProgress;
    }

    [RelayCommand]
    private void NextStep()
    {
        if (CurrentStep < 5)
            CurrentStep++;
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStep > 1)
            CurrentStep--;
    }

    [RelayCommand]
    private async Task DeployAsync()
    {
        IsDeploying = true;
        DeploymentProgress = 0;
        DeploymentSteps.Clear();
        DeploymentSucceeded = false;
        DeploymentError = null;

        try
        {
            var result = await _deploymentService.DeployAsync(Config);

            if (result.Success)
            {
                DeploymentSteps.Add("✅ Deployment onnistui!");
                DeploymentSucceeded = true;
            }
            else
            {
                DeploymentSteps.Add($"❌ Virhe: {result.ErrorMessage}");
                DeploymentError = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            DeploymentSteps.Add($"❌ Virhe: {ex.Message}");
            DeploymentError = ex.Message;
        }
        finally
        {
            IsDeploying = false;
        }
    }

    [RelayCommand]
    private void Finish()
    {
        // Reset wizard to initial state
        CurrentStep = 1;
        DeploymentProgress = 0;
        DeploymentSteps.Clear();
        DeploymentSucceeded = false;
        DeploymentError = null;
        Config = new DeploymentConfig();
    }

    private void OnDeploymentProgress(object? sender, DeploymentProgressEventArgs e)
    {
        DeploymentProgress = e.PercentComplete;
        DeploymentSteps.Add(e.Message);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _deploymentService.ProgressChanged -= OnDeploymentProgress;
        _disposed = true;
    }
}
