using InvoiceRobot.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace InvoiceRobot.Infrastructure.Services;

public class AccountingSystemService : IAccountingSystemService
{
    private readonly IAccountSystemOrchestrator _orchestrator;
    private readonly ILogger<AccountingSystemService> _logger;

    public AccountingSystemService(
        IAccountSystemOrchestrator orchestrator,
        ILogger<AccountingSystemService> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task<List<AccountingInvoiceDto>> GetPurchaseInvoicesAsync(int days = 30)
    {
        try
        {
            _logger.LogInformation("Haetaan ostolaskut viimeiseltä {Days} päivältä", days);

            var startDate = DateTime.UtcNow.AddDays(-days);
            var invoices = await _orchestrator.GetPurchaseInvoicesAsync(startDate);

            var result = invoices.Select(inv => new AccountingInvoiceDto(
                inv.InvoiceKey,
                inv.InvoiceNumber,
                inv.VendorName,
                inv.Amount,
                inv.InvoiceDate,
                inv.DueDate,
                inv.ProjectKey
            )).ToList();

            _logger.LogInformation("Haettiin {Count} laskua", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Virhe laskujen haussa");
            throw;
        }
    }

    public async Task<AccountingProjectDto?> GetProjectAsync(int projectKey)
    {
        try
        {
            var project = await _orchestrator.GetProjectAsync(projectKey);
            if (project == null) return null;

            return new AccountingProjectDto(
                project.ProjectKey,
                project.ProjectCode,
                project.Name,
                project.Address,
                project.IsActive
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Virhe projektin {ProjectKey} haussa", projectKey);
            throw;
        }
    }

    public async Task<List<AccountingProjectDto>> GetActiveProjectsAsync()
    {
        try
        {
            _logger.LogInformation("Haetaan aktiiviset projektit");

            var projects = await _orchestrator.GetActiveProjectsAsync();

            var result = projects.Select(p => new AccountingProjectDto(
                p.ProjectKey,
                p.ProjectCode,
                p.Name,
                p.Address,
                p.IsActive
            )).ToList();

            _logger.LogInformation("Haettiin {Count} projektia", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Virhe projektien haussa");
            throw;
        }
    }

    public async Task<bool> UpdateInvoiceProjectAsync(int invoiceKey, int projectKey)
    {
        try
        {
            _logger.LogInformation(
                "Päivitetään lasku {InvoiceKey} projektiin {ProjectKey}",
                invoiceKey,
                projectKey);

            var success = await _orchestrator.UpdateInvoiceProjectAsync(invoiceKey, projectKey);

            if (success)
            {
                _logger.LogInformation("Projektikohdistus päivitetty onnistuneesti");
            }
            else
            {
                _logger.LogWarning("Projektikohdistuksen päivitys epäonnistui");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Virhe projektikohdistuksen päivityksessä");
            throw;
        }
    }

    public Task<bool> StartApprovalCirculationAsync(int invoiceKey)
    {
        // TODO: Implementoi kun AnyAI.AccountSystem.Orchestrator tukee hyväksymiskierroksen käynnistystä
        _logger.LogWarning(
            "StartApprovalCirculationAsync ei ole vielä tuettu taloushallintojärjestelmässä. InvoiceKey: {InvoiceKey}",
            invoiceKey);

        throw new NotImplementedException(
            "Hyväksymiskierroksen käynnistys ei ole vielä toteutettu AnyAI.AccountSystem.Orchestrator-kirjastossa.");
    }

    public async Task<byte[]?> DownloadInvoicePdfAsync(int invoiceKey)
    {
        try
        {
            _logger.LogInformation("Ladataan PDF lasku {InvoiceKey}", invoiceKey);

            var pdfData = await _orchestrator.DownloadInvoicePdfAsync(invoiceKey);

            if (pdfData == null || pdfData.Length == 0)
            {
                _logger.LogWarning("PDF ei saatavilla laskulle {InvoiceKey}", invoiceKey);
                return null;
            }

            _logger.LogInformation("PDF ladattu, koko {Size} tavua", pdfData.Length);
            return pdfData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Virhe PDF:n latauksessa");
            throw;
        }
    }
}
