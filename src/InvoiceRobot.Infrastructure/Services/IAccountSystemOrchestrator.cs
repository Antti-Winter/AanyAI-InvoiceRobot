// MOCK-INTERFACE testausta varten
// Korvaa tämä oikealla AnyAI.AccountSystem.Orchestrator NuGet-paketilla kun saatavilla

namespace InvoiceRobot.Infrastructure.Services;

public interface IAccountSystemOrchestrator
{
    Task<List<OrchestratorInvoice>> GetPurchaseInvoicesAsync(DateTime startDate);
    Task<OrchestratorProject?> GetProjectAsync(int projectKey);
    Task<List<OrchestratorProject>> GetActiveProjectsAsync();
    Task<bool> UpdateInvoiceProjectAsync(int invoiceKey, int projectKey);
    Task<byte[]?> DownloadInvoicePdfAsync(int invoiceKey);
}

public class OrchestratorInvoice
{
    public int InvoiceKey { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public int? ProjectKey { get; set; }
}

public class OrchestratorProject
{
    public int ProjectKey { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsActive { get; set; }
}
