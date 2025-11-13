namespace InvoiceRobot.Admin.Models;

/// <summary>
/// Application settings that can be configured after deployment
/// </summary>
public class AppSettings
{
    // Accounting system
    public string AccountingProvider { get; set; } = "Netvisor";

    public string? NetvisorCustomerId { get; set; }
    public string? NetvisorPartnerId { get; set; }
    public string? NetvisorPartnerKey { get; set; }
    public string? NetvisorOrganizationId { get; set; }

    public string? ProcountorApiKey { get; set; }
    public string? ProcountorCompanyId { get; set; }
    public string? ProcountorEnvironment { get; set; }

    // Azure AI
    public string AzureOpenAIEndpoint { get; set; } = string.Empty;
    public string AzureOpenAIApiKey { get; set; } = string.Empty;
    public string AzureOpenAIDeploymentName { get; set; } = "gpt-4";

    public string DocumentIntelligenceEndpoint { get; set; } = string.Empty;
    public string DocumentIntelligenceApiKey { get; set; } = string.Empty;

    // Email
    public string CommunicationServicesConnectionString { get; set; } = string.Empty;
    public string EmailSenderAddress { get; set; } = string.Empty;
    public string EmailPmDistributionList { get; set; } = string.Empty;

    // AI prompts (editable)
    public string HeuristicMatcherPrompt { get; set; } = string.Empty;
    public string GptMatcherPrompt { get; set; } = string.Empty;
}
