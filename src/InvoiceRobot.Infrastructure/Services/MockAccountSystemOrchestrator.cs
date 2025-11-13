// MOCK-TOTEUTUS testausta varten
// Korvaa tämä oikealla AnyAI.AccountSystem.Orchestrator NuGet-paketilla kun saatavilla

namespace InvoiceRobot.Infrastructure.Services;

public class MockAccountSystemOrchestrator : IAccountSystemOrchestrator
{
    public Task<List<OrchestratorInvoice>> GetPurchaseInvoicesAsync(DateTime startDate)
    {
        // Palauta tyhjä lista
        return Task.FromResult(new List<OrchestratorInvoice>());
    }

    public Task<OrchestratorProject?> GetProjectAsync(int projectKey)
    {
        return Task.FromResult<OrchestratorProject?>(null);
    }

    public Task<List<OrchestratorProject>> GetActiveProjectsAsync()
    {
        // Palauta tyhjä lista
        return Task.FromResult(new List<OrchestratorProject>());
    }

    public Task<bool> UpdateInvoiceProjectAsync(int invoiceKey, int projectKey)
    {
        return Task.FromResult(true);
    }

    public Task<byte[]?> DownloadInvoicePdfAsync(int invoiceKey)
    {
        return Task.FromResult<byte[]?>(null);
    }
}
