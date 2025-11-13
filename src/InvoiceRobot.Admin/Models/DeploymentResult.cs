namespace InvoiceRobot.Admin.Models;

/// <summary>
/// Result of a deployment operation
/// </summary>
public class DeploymentResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    // Basic infrastructure
    public string? FunctionAppName { get; set; }
    public string? SqlServerFqdn { get; set; }
    public string? ApplicationInsightsConnectionString { get; set; }

    // AI services
    public string? OpenAIEndpoint { get; set; }
    public string? OpenAIApiKey { get; set; }
    public string? DocumentIntelligenceEndpoint { get; set; }
    public string? DocumentIntelligenceApiKey { get; set; }

    // Communication
    public string? CommunicationServicesConnectionString { get; set; }
}
