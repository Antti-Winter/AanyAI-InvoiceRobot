using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using InvoiceRobot.Admin.Models;
using Microsoft.Extensions.Logging;

namespace InvoiceRobot.Admin.Services;

/// <summary>
/// Service for managing Function App configuration
/// </summary>
public class ConfigurationService
{
    private readonly ArmClient _armClient;
    private readonly ILogger _logger;

    public ConfigurationService(ArmClient armClient, ILogger logger)
    {
        _armClient = armClient;
        _logger = logger;
    }

    /// <summary>
    /// Load current configuration from Function App
    /// </summary>
    public async Task<AppSettings?> LoadSettingsAsync(string subscriptionId, string resourceGroupName, string functionAppName)
    {
        try
        {
            var functionAppId = WebSiteResource.CreateResourceIdentifier(
                subscriptionId,
                resourceGroupName,
                functionAppName);

            var functionApp = await _armClient.GetWebSiteResource(functionAppId).GetAsync();

            // Get app settings
            var appSettings = await functionApp.Value.GetApplicationSettingsAsync();

            var settings = new AppSettings();

            // Parse settings from app settings
            if (appSettings.Value.Properties.TryGetValue("AccountingProvider", out var accountingProvider))
                settings.AccountingProvider = accountingProvider;

            // Netvisor settings
            if (appSettings.Value.Properties.TryGetValue("Netvisor:CustomerID", out var netvisorCustomerId))
                settings.NetvisorCustomerId = netvisorCustomerId;
            if (appSettings.Value.Properties.TryGetValue("Netvisor:PartnerID", out var netvisorPartnerId))
                settings.NetvisorPartnerId = netvisorPartnerId;
            if (appSettings.Value.Properties.TryGetValue("Netvisor:PartnerKey", out var netvisorPartnerKey))
                settings.NetvisorPartnerKey = netvisorPartnerKey;
            if (appSettings.Value.Properties.TryGetValue("Netvisor:OrganizationID", out var netvisorOrgId))
                settings.NetvisorOrganizationId = netvisorOrgId;

            // Procountor settings
            if (appSettings.Value.Properties.TryGetValue("Procountor:ApiKey", out var procountorApiKey))
                settings.ProcountorApiKey = procountorApiKey;
            if (appSettings.Value.Properties.TryGetValue("Procountor:CompanyId", out var procountorCompanyId))
                settings.ProcountorCompanyId = procountorCompanyId;
            if (appSettings.Value.Properties.TryGetValue("Procountor:Environment", out var procountorEnv))
                settings.ProcountorEnvironment = procountorEnv;

            // Azure AI settings
            if (appSettings.Value.Properties.TryGetValue("AzureOpenAI:Endpoint", out var openAIEndpoint))
                settings.AzureOpenAIEndpoint = openAIEndpoint;
            if (appSettings.Value.Properties.TryGetValue("AzureOpenAI:ApiKey", out var openAIKey))
                settings.AzureOpenAIApiKey = openAIKey;
            if (appSettings.Value.Properties.TryGetValue("AzureOpenAI:DeploymentName", out var openAIDeployment))
                settings.AzureOpenAIDeploymentName = openAIDeployment;

            if (appSettings.Value.Properties.TryGetValue("DocumentIntelligence:Endpoint", out var docIntelEndpoint))
                settings.DocumentIntelligenceEndpoint = docIntelEndpoint;
            if (appSettings.Value.Properties.TryGetValue("DocumentIntelligence:ApiKey", out var docIntelKey))
                settings.DocumentIntelligenceApiKey = docIntelKey;

            // Email settings
            if (appSettings.Value.Properties.TryGetValue("CommunicationServices:ConnectionString", out var commSvcConn))
                settings.CommunicationServicesConnectionString = commSvcConn;
            if (appSettings.Value.Properties.TryGetValue("Email:SenderAddress", out var emailSender))
                settings.EmailSenderAddress = emailSender;
            if (appSettings.Value.Properties.TryGetValue("Email:PmDistributionList", out var emailPmList))
                settings.EmailPmDistributionList = emailPmList;

            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Virhe asetusten latauksessa");
            return null;
        }
    }

    /// <summary>
    /// Save configuration to Function App
    /// </summary>
    public async Task<bool> SaveSettingsAsync(
        string subscriptionId,
        string resourceGroupName,
        string functionAppName,
        AppSettings settings)
    {
        try
        {
            var functionAppId = WebSiteResource.CreateResourceIdentifier(
                subscriptionId,
                resourceGroupName,
                functionAppName);

            var functionApp = await _armClient.GetWebSiteResource(functionAppId).GetAsync();

            // Get current app settings
            var currentSettings = await functionApp.Value.GetApplicationSettingsAsync();
            var updatedSettings = new Dictionary<string, string>(currentSettings.Value.Properties);

            // Update settings
            updatedSettings["AccountingProvider"] = settings.AccountingProvider;

            // Netvisor
            if (!string.IsNullOrEmpty(settings.NetvisorCustomerId))
                updatedSettings["Netvisor:CustomerID"] = settings.NetvisorCustomerId;
            if (!string.IsNullOrEmpty(settings.NetvisorPartnerId))
                updatedSettings["Netvisor:PartnerID"] = settings.NetvisorPartnerId;
            if (!string.IsNullOrEmpty(settings.NetvisorPartnerKey))
                updatedSettings["Netvisor:PartnerKey"] = settings.NetvisorPartnerKey;
            if (!string.IsNullOrEmpty(settings.NetvisorOrganizationId))
                updatedSettings["Netvisor:OrganizationID"] = settings.NetvisorOrganizationId;

            // Procountor
            if (!string.IsNullOrEmpty(settings.ProcountorApiKey))
                updatedSettings["Procountor:ApiKey"] = settings.ProcountorApiKey;
            if (!string.IsNullOrEmpty(settings.ProcountorCompanyId))
                updatedSettings["Procountor:CompanyId"] = settings.ProcountorCompanyId;
            if (!string.IsNullOrEmpty(settings.ProcountorEnvironment))
                updatedSettings["Procountor:Environment"] = settings.ProcountorEnvironment;

            // Azure AI
            updatedSettings["AzureOpenAI:Endpoint"] = settings.AzureOpenAIEndpoint;
            updatedSettings["AzureOpenAI:ApiKey"] = settings.AzureOpenAIApiKey;
            updatedSettings["AzureOpenAI:DeploymentName"] = settings.AzureOpenAIDeploymentName;

            updatedSettings["DocumentIntelligence:Endpoint"] = settings.DocumentIntelligenceEndpoint;
            updatedSettings["DocumentIntelligence:ApiKey"] = settings.DocumentIntelligenceApiKey;

            // Email
            updatedSettings["CommunicationServices:ConnectionString"] = settings.CommunicationServicesConnectionString;
            updatedSettings["Email:SenderAddress"] = settings.EmailSenderAddress;
            updatedSettings["Email:PmDistributionList"] = settings.EmailPmDistributionList;

            // Update Function App settings using Azure Resource Manager API
            _logger.LogInformation($"Päivitetään {updatedSettings.Count} asetusta...");

            // Update settings directly in the currentSettings object
            foreach (var kvp in updatedSettings)
            {
                currentSettings.Value.Properties[kvp.Key] = kvp.Value;
            }

            await functionApp.Value.UpdateApplicationSettingsAsync(currentSettings.Value);

            _logger.LogInformation("Asetukset tallennettu onnistuneesti");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Virhe asetusten tallennuksessa");
            return false;
        }
    }
}
