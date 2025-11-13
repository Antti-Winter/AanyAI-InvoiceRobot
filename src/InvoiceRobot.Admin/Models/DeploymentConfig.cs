namespace InvoiceRobot.Admin.Models;

/// <summary>
/// Configuration for Azure deployment via Bicep
/// </summary>
public class DeploymentConfig
{
    public string SubscriptionId { get; set; } = string.Empty;
    public string ResourceGroupName { get; set; } = string.Empty;
    public string Location { get; set; } = "northeurope";
    public string NamePrefix { get; set; } = "invoicerobot";
    public string Environment { get; set; } = "dev";
    public string AccountingProvider { get; set; } = "Netvisor";
    public string SqlServerPassword { get; set; } = string.Empty;

    // Accounting system credentials
    public string? NetvisorCustomerId { get; set; }
    public string? NetvisorPartnerId { get; set; }
    public string? NetvisorPartnerKey { get; set; }
    public string? NetvisorOrganizationId { get; set; }

    public string? ProcountorApiKey { get; set; }
    public string? ProcountorCompanyId { get; set; }
    public string? ProcountorEnvironment { get; set; }

    // Azure AI credentials
    public string AzureOpenAIEndpoint { get; set; } = string.Empty;
    public string AzureOpenAIApiKey { get; set; } = string.Empty;
    public string AzureOpenAIDeploymentName { get; set; } = "gpt-4";

    public string DocumentIntelligenceEndpoint { get; set; } = string.Empty;
    public string DocumentIntelligenceApiKey { get; set; } = string.Empty;

    // Email configuration
    public string CommunicationServicesConnectionString { get; set; } = string.Empty;
    public string EmailSenderAddress { get; set; } = string.Empty;
    public string EmailPmDistributionList { get; set; } = string.Empty;
}
