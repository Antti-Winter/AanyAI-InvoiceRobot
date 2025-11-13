namespace InvoiceRobot.Admin.Models;

/// <summary>
/// DTO for displaying Application Insights logs
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string SeverityLevel { get; set; } = string.Empty;
    public string OperationName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public Dictionary<string, string> CustomDimensions { get; set; } = new();
}
