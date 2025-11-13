using InvoiceRobot.Core.Domain;
using InvoiceRobot.Core.Interfaces;
using InvoiceRobot.Infrastructure.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvoiceRobot.Functions.Functions;

public class InvoiceFetcher
{
    private readonly InvoiceRobotDbContext _context;
    private readonly IAccountingSystemService _accountingService;
    private readonly ILogger<InvoiceFetcher> _logger;

    public InvoiceFetcher(
        InvoiceRobotDbContext context,
        IAccountingSystemService accountingService,
        ILogger<InvoiceFetcher> logger)
    {
        _context = context;
        _accountingService = accountingService;
        _logger = logger;
    }

    [Function("InvoiceFetcher")]
    public async Task RunAsync([TimerTrigger("0 0 8 * * *")] TimerInfo timerInfo)
    {
        _logger.LogInformation("InvoiceFetcher käynnistyy: {Time}", DateTime.UtcNow);

        try
        {
            // 1. Synkronoi projektit
            await SyncProjectsAsync();

            // 2. Hae laskut (30 päivää)
            var invoices = await _accountingService.GetPurchaseInvoicesAsync(30);
            _logger.LogInformation("Haettiin {Count} laskua taloushallinnosta", invoices.Count);

            var newCount = 0;

            foreach (var invoiceDto in invoices)
            {
                // Tarkista onko jo olemassa
                var existing = await _context.Invoices
                    .FirstOrDefaultAsync(i => i.NetvisorInvoiceKey == invoiceDto.InvoiceKey);

                if (existing != null)
                {
                    _logger.LogDebug("Lasku {InvoiceNumber} on jo tietokannassa", invoiceDto.InvoiceNumber);
                    continue;
                }

                // Lataa PDF
                byte[]? pdfData = null;
                try
                {
                    pdfData = await _accountingService.DownloadInvoicePdfAsync(invoiceDto.InvoiceKey);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "PDF:n lataus epäonnistui laskulle {InvoiceKey}", invoiceDto.InvoiceKey);
                }

                // Luo uusi Invoice (PDF prosessoidaan myöhemmin muistissa, ei tallenneta)
                var invoice = new Invoice
                {
                    NetvisorInvoiceKey = invoiceDto.InvoiceKey,
                    InvoiceNumber = invoiceDto.InvoiceNumber,
                    VendorName = invoiceDto.VendorName,
                    Amount = invoiceDto.Amount,
                    InvoiceDate = invoiceDto.InvoiceDate,
                    DueDate = invoiceDto.DueDate,
                    Status = InvoiceStatus.Discovered,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Invoices.Add(invoice);
                newCount++;

                _logger.LogInformation("Lisätty uusi lasku: {InvoiceNumber}", invoice.InvoiceNumber);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "InvoiceFetcher valmis. Uusia laskuja: {NewCount} / {TotalCount}",
                newCount,
                invoices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Virhe InvoiceFetcher-funktiossa");
            throw;
        }
    }

    private async Task SyncProjectsAsync()
    {
        _logger.LogInformation("Synkronoidaan projektit");

        var accountingProjects = await _accountingService.GetActiveProjectsAsync();

        foreach (var projDto in accountingProjects)
        {
            var existing = await _context.Projects
                .FirstOrDefaultAsync(p => p.NetvisorProjectKey == projDto.ProjectKey);

            if (existing != null)
            {
                // Päivitä
                existing.ProjectCode = projDto.ProjectCode;
                existing.Name = projDto.Name;
                existing.Address = projDto.Address;
                existing.IsActive = projDto.IsActive;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Lisää uusi
                var project = new Project
                {
                    NetvisorProjectKey = projDto.ProjectKey,
                    ProjectCode = projDto.ProjectCode,
                    Name = projDto.Name,
                    Address = projDto.Address,
                    IsActive = projDto.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Projects.Add(project);
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Projektit synkronoitu: {Count}", accountingProjects.Count);
    }
}
