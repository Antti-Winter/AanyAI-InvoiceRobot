using InvoiceRobot.Core.Domain;

namespace InvoiceRobot.Core.Interfaces;

public interface IAccountingSystemService
{
    /// <summary>
    /// Hakee kaikki ostolaskut viimeisen 30 päivän ajalta
    /// </summary>
    Task<List<AccountingInvoiceDto>> GetPurchaseInvoicesAsync(int days = 30);

    /// <summary>
    /// Hakee projektin tiedot
    /// </summary>
    Task<AccountingProjectDto?> GetProjectAsync(int projectKey);

    /// <summary>
    /// Hakee kaikki aktiiviset projektit
    /// </summary>
    Task<List<AccountingProjectDto>> GetActiveProjectsAsync();

    /// <summary>
    /// Päivittää laskun projektikohdistuksen taloushallintojärjestelmässä
    /// </summary>
    Task<bool> UpdateInvoiceProjectAsync(int invoiceKey, int projectKey);

    /// <summary>
    /// Käynnistää hyväksymiskierroksen taloushallintojärjestelmässä
    /// </summary>
    Task<bool> StartApprovalCirculationAsync(int invoiceKey);

    /// <summary>
    /// Lataa laskun PDF:n
    /// </summary>
    Task<byte[]?> DownloadInvoicePdfAsync(int invoiceKey);
}

public record AccountingInvoiceDto(
    int InvoiceKey,
    string InvoiceNumber,
    string VendorName,
    decimal Amount,
    DateTime InvoiceDate,
    DateTime DueDate,
    int? ProjectKey
);

public record AccountingProjectDto(
    int ProjectKey,
    string ProjectCode,
    string Name,
    string? Address,
    bool IsActive
);
