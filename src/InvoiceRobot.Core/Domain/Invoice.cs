using System.ComponentModel.DataAnnotations.Schema;

namespace InvoiceRobot.Core.Domain;

public class Invoice
{
    public int Id { get; set; }

    // Taloushallintojärjestelmän viittaus (provider-spesifinen tunniste)
    public int NetvisorInvoiceKey { get; set; }      // Unique (Netvisor/Procountor key)

    // Laskun perustiedot
    public string InvoiceNumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }

    // OCR-tulos (PDF prosessoidaan muistissa, ei tallenneta)
    public string? OcrText { get; set; }
    public DateTime? OcrProcessedAt { get; set; }

    // AI-analyysi
    public int? SuggestedProjectKey { get; set; }    // Netvisor ProjectKey (AI:n ehdotus)
    public int? SuggestedProjectId { get; set; }     // FK → Project.Id (EF Core navigation)
    public double? AiConfidenceScore { get; set; }   // 0.0 - 1.0
    public string? AiReasoning { get; set; }         // Miksi tämä projekti
    public DateTime? AiAnalyzedAt { get; set; }

    // Lopullinen tulos
    public int? FinalProjectKey { get; set; }        // Netvisor ProjectKey (päivitetty taloushallintoon)
    public int? FinalProjectId { get; set; }         // FK → Project.Id (EF Core navigation)
    public DateTime? UpdatedToAccountingSystemAt { get; set; }

    // Status
    public InvoiceStatus Status { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties (EF Core)
    [ForeignKey(nameof(SuggestedProjectId))]
    public Project? SuggestedProject { get; set; }

    [ForeignKey(nameof(FinalProjectId))]
    public Project? FinalProject { get; set; }

    public ApprovalRequest? ApprovalRequest { get; set; }
}
