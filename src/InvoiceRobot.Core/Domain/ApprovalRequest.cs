using System.ComponentModel.DataAnnotations.Schema;

namespace InvoiceRobot.Core.Domain;

public class ApprovalRequest
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public string Token { get; set; } = string.Empty;           // Unique token URL:issa
    public DateTime SentAt { get; set; }
    public DateTime? RespondedAt { get; set; }

    public ApprovalStatus Status { get; set; }
    public int? ApprovedProjectKey { get; set; }                // Netvisor ProjectKey (PM:n valinta)
    public int? ApprovedProjectId { get; set; }                 // FK → Project.Id (EF Core navigation)
    public string? RejectionReason { get; set; }

    // AI-analyysin tulos (kopio Invoice-entitystä pysyvää tallennusta varten)
    public int? SuggestedProjectKey { get; set; }               // Netvisor ProjectKey (AI:n ehdotus)
    public int? SuggestedProjectId { get; set; }                // FK → Project.Id (EF Core navigation)
    public double? ConfidenceScore { get; set; }                // 0.0 - 1.0
    public string? Reasoning { get; set; }                      // AI:n selitys

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties (EF Core)
    [ForeignKey(nameof(SuggestedProjectId))]
    public Project? SuggestedProject { get; set; }

    [ForeignKey(nameof(ApprovedProjectId))]
    public Project? ApprovedProject { get; set; }
}

public enum ApprovalStatus
{
    Pending = 0,
    Approved = 10,
    Rejected = 20,
    Expired = 30
}
