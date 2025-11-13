namespace InvoiceRobot.Admin.Models;

/// <summary>
/// DTO for displaying invoices in the Admin UI
/// </summary>
public class InvoiceDto
{
    public int Id { get; set; }
    public int NetvisorInvoiceKey { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = string.Empty;

    public string? ProjectCode { get; set; }
    public string? ProjectName { get; set; }

    public double? AiConfidenceScore { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
