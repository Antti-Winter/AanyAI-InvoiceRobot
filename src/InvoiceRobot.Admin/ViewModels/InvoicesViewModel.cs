using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoiceRobot.Admin.Models;
using InvoiceRobot.Admin.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace InvoiceRobot.Admin.ViewModels;

public partial class InvoicesViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;

    [ObservableProperty]
    private ObservableCollection<InvoiceDto> _invoices = new();

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _pageSize = 50;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private int _totalCount = 0;

    [ObservableProperty]
    private string? _statusFilter;

    [ObservableProperty]
    private string? _searchTerm;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string _connectionString = string.Empty;

    public InvoicesViewModel()
    {
        _databaseService = new DatabaseService(App.Logger!);
    }

    [RelayCommand]
    private async Task LoadInvoicesAsync()
    {
        if (string.IsNullOrEmpty(ConnectionString))
        {
            return;
        }

        IsLoading = true;

        try
        {
            _databaseService.SetConnectionString(ConnectionString);

            // Get total count
            TotalCount = await _databaseService.GetInvoiceCountAsync(StatusFilter, SearchTerm);
            TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

            // Get invoices for current page
            var invoices = await _databaseService.GetInvoicesAsync(
                CurrentPage,
                PageSize,
                StatusFilter,
                SearchTerm);

            Invoices.Clear();
            foreach (var invoice in invoices)
            {
                Invoices.Add(invoice);
            }
        }
        catch (Exception ex)
        {
            App.Logger?.LogError(ex, "Virhe laskujen latauksessa");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages && !IsLoading)
        {
            var nextPage = CurrentPage + 1;
            var previousPage = CurrentPage;

            try
            {
                CurrentPage = nextPage;
                await LoadInvoicesAsync();
            }
            catch
            {
                // Revert page if loading failed
                CurrentPage = previousPage;
                throw;
            }
        }
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (CurrentPage > 1 && !IsLoading)
        {
            var previousPage = CurrentPage - 1;
            var originalPage = CurrentPage;

            try
            {
                CurrentPage = previousPage;
                await LoadInvoicesAsync();
            }
            catch
            {
                // Revert page if loading failed
                CurrentPage = originalPage;
                throw;
            }
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadInvoicesAsync();
    }
}
