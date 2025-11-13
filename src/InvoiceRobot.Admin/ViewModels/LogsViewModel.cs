using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoiceRobot.Admin.Models;
using InvoiceRobot.Admin.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace InvoiceRobot.Admin.ViewModels;

public partial class LogsViewModel : ObservableObject
{
    private readonly LogsService _logsService;

    [ObservableProperty]
    private ObservableCollection<LogEntry> _logs = new();

    [ObservableProperty]
    private int _timeRangeHours = 24;

    [ObservableProperty]
    private string? _severityFilter;

    [ObservableProperty]
    private string? _operationNameFilter;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string _applicationInsightsConnectionString = string.Empty;

    public LogsViewModel()
    {
        _logsService = new LogsService(App.Logger!);
    }

    [RelayCommand]
    private async Task LoadLogsAsync()
    {
        if (string.IsNullOrEmpty(ApplicationInsightsConnectionString))
        {
            return;
        }

        IsLoading = true;

        try
        {
            _logsService.SetConnectionString(ApplicationInsightsConnectionString);

            var timeRange = TimeSpan.FromHours(TimeRangeHours);
            var logs = await _logsService.QueryLogsAsync(
                timeRange,
                SeverityFilter,
                OperationNameFilter);

            Logs.Clear();
            foreach (var log in logs)
            {
                Logs.Add(log);
            }
        }
        catch (Exception ex)
        {
            App.Logger?.LogError(ex, "Virhe lokien latauksessa");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadRecentErrorsAsync()
    {
        if (string.IsNullOrEmpty(ApplicationInsightsConnectionString))
        {
            return;
        }

        IsLoading = true;

        try
        {
            _logsService.SetConnectionString(ApplicationInsightsConnectionString);

            var logs = await _logsService.GetRecentErrorsAsync();

            Logs.Clear();
            foreach (var log in logs)
            {
                Logs.Add(log);
            }
        }
        catch (Exception ex)
        {
            App.Logger?.LogError(ex, "Virhe virheiden latauksessa");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
