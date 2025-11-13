using System.ComponentModel.DataAnnotations.Schema;

namespace InvoiceRobot.Core.Domain;

public class Project
{
    public int Id { get; set; }

    // Taloushallintojärjestelmän viittaus (provider-spesifinen tunniste)
    public int NetvisorProjectKey { get; set; }      // Unique (Netvisor/Procountor dimension key)

    public string ProjectCode { get; set; } = string.Empty;     // PRJ-001
    public string Name { get; set; } = string.Empty;            // "Kerrostalo Mannerheimintie"
    public string? Address { get; set; }                        // "Mannerheimintie 123, Helsinki"
    public string? ProjectManagerEmail { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties (EF Core)
    [InverseProperty(nameof(Invoice.SuggestedProject))]
    public ICollection<Invoice> SuggestedInvoices { get; set; } = new List<Invoice>();

    [InverseProperty(nameof(Invoice.FinalProject))]
    public ICollection<Invoice> FinalInvoices { get; set; } = new List<Invoice>();

    [InverseProperty(nameof(ApprovalRequest.SuggestedProject))]
    public ICollection<ApprovalRequest> SuggestedApprovalRequests { get; set; } = new List<ApprovalRequest>();

    [InverseProperty(nameof(ApprovalRequest.ApprovedProject))]
    public ICollection<ApprovalRequest> ApprovedApprovalRequests { get; set; } = new List<ApprovalRequest>();
}
