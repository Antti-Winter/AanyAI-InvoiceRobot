using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoiceRobot.Admin.Models;
using InvoiceRobot.Admin.Services;

namespace InvoiceRobot.Admin.ViewModels;

public partial class ConfigurationViewModel : ObservableObject
{
    private readonly ConfigurationService _configurationService;

    [ObservableProperty]
    private AppSettings _settings = new();

    [ObservableProperty]
    private string _subscriptionId = string.Empty;

    [ObservableProperty]
    private string _resourceGroupName = string.Empty;

    [ObservableProperty]
    private string _functionAppName = string.Empty;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private bool _isSaving = false;

    [ObservableProperty]
    private string? _statusMessage;

    public ConfigurationViewModel()
    {
        _configurationService = new ConfigurationService(
            App.ArmClient!,
            App.Logger!);
    }

    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        if (string.IsNullOrEmpty(SubscriptionId) ||
            string.IsNullOrEmpty(ResourceGroupName) ||
            string.IsNullOrEmpty(FunctionAppName))
        {
            StatusMessage = "❌ Täytä kaikki kentät";
            return;
        }

        IsLoading = true;
        StatusMessage = "Ladataan asetuksia...";

        try
        {
            var settings = await _configurationService.LoadSettingsAsync(
                SubscriptionId,
                ResourceGroupName,
                FunctionAppName);

            if (settings != null)
            {
                Settings = settings;
                StatusMessage = "✅ Asetukset ladattu";
            }
            else
            {
                StatusMessage = "❌ Asetusten lataus epäonnistui";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Virhe: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        if (string.IsNullOrEmpty(SubscriptionId) ||
            string.IsNullOrEmpty(ResourceGroupName) ||
            string.IsNullOrEmpty(FunctionAppName))
        {
            StatusMessage = "❌ Täytä kaikki kentät";
            return;
        }

        IsSaving = true;
        StatusMessage = "Tallennetaan asetuksia...";

        try
        {
            var success = await _configurationService.SaveSettingsAsync(
                SubscriptionId,
                ResourceGroupName,
                FunctionAppName,
                Settings);

            if (success)
            {
                StatusMessage = "✅ Asetukset tallennettu";
            }
            else
            {
                StatusMessage = "❌ Asetusten tallennus epäonnistui";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Virhe: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }
}
