using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using InvoiceRobot.Admin.Models;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Reflection;

namespace InvoiceRobot.Admin.Services;

/// <summary>
/// Service for deploying InvoiceRobot to Azure using ARM templates
/// </summary>
public class AzureDeploymentService
{
    private readonly ArmClient _armClient;
    private readonly ILogger _logger;

    public event EventHandler<DeploymentProgressEventArgs>? ProgressChanged;

    public AzureDeploymentService(ArmClient armClient, ILogger logger)
    {
        _armClient = armClient;
        _logger = logger;
    }

    /// <summary>
    /// Deploy InvoiceRobot infrastructure to Azure
    /// </summary>
    public async Task<DeploymentResult> DeployAsync(DeploymentConfig config)
    {
        try
        {
            ReportProgress(0, "Aloitetaan deployment...");

            // Get subscription
            var subscriptionId = config.SubscriptionId;
            var subscription = await _armClient.GetSubscriptionResource(
                new ResourceIdentifier($"/subscriptions/{subscriptionId}")).GetAsync();

            ReportProgress(10, "Subscription löydetty");

            // Get or create resource group
            var resourceGroups = subscription.Value.GetResourceGroups();

            ReportProgress(20, $"Luodaan resource group: {config.ResourceGroupName}");

            var resourceGroupData = new ResourceGroupData(config.Location)
            {
                Tags =
                {
                    ["Environment"] = config.Environment,
                    ["Application"] = "InvoiceRobot"
                }
            };

            var resourceGroup = await resourceGroups.CreateOrUpdateAsync(
                WaitUntil.Completed,
                config.ResourceGroupName,
                resourceGroupData);

            ReportProgress(30, "Resource group luotu");

            // Load ARM template
            ReportProgress(40, "Ladataan ARM template...");
            var armTemplate = LoadArmTemplate();

            // Deploy using ARM template deployment
            ReportProgress(50, "Aloitetaan Azure-deployment...");

            var deploymentName = $"invoicerobot-{DateTime.UtcNow:yyyyMMddHHmmss}";
            var deploymentCollection = resourceGroup.Value.GetArmDeployments();

            var deploymentContent = new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(armTemplate),
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    namePrefix = new { value = config.NamePrefix },
                    location = new { value = config.Location },
                    environment = new { value = config.Environment },
                    accountingProvider = new { value = config.AccountingProvider },
                    sqlServerPassword = new { value = config.SqlServerPassword },
                    emailPmDistributionList = new { value = config.EmailPmDistributionList ?? string.Empty }
                })
            });

            ReportProgress(60, "Luodaan resursseja...");

            var deploymentOperation = await deploymentCollection.CreateOrUpdateAsync(
                WaitUntil.Completed,
                deploymentName,
                deploymentContent);

            var deployment = deploymentOperation.Value;

            ReportProgress(90, "Deployment valmis, haetaan outputit...");

            // Get deployment outputs
            var properties = deployment.Data.Properties;
            var outputs = properties?.Outputs;

            string functionAppName = $"{config.NamePrefix}-func-{config.Environment}";
            string sqlServerFqdn = $"{config.NamePrefix}-sql-{config.Environment}.database.windows.net";
            string appInsightsConnString = string.Empty;
            string communicationServicesConnString = string.Empty;
            string openAIEndpoint = string.Empty;
            string openAIApiKey = string.Empty;
            string documentIntelligenceEndpoint = string.Empty;
            string documentIntelligenceApiKey = string.Empty;

            if (outputs != null)
            {
                // Deserialize outputs from BinaryData
                // ARM template outputs have structure: { "outputName": { "type": "...", "value": "..." } }
                using var outputsDoc = System.Text.Json.JsonDocument.Parse(outputs.ToString());
                var root = outputsDoc.RootElement;

                if (root.TryGetProperty("functionAppName", out var funcAppOutput) &&
                    funcAppOutput.TryGetProperty("value", out var funcAppValue))
                    functionAppName = funcAppValue.GetString() ?? functionAppName;

                if (root.TryGetProperty("sqlServerFqdn", out var sqlOutput) &&
                    sqlOutput.TryGetProperty("value", out var sqlValue))
                    sqlServerFqdn = sqlValue.GetString() ?? sqlServerFqdn;

                if (root.TryGetProperty("applicationInsightsConnectionString", out var appInsightsOutput) &&
                    appInsightsOutput.TryGetProperty("value", out var appInsightsValue))
                    appInsightsConnString = appInsightsValue.GetString() ?? string.Empty;

                if (root.TryGetProperty("communicationServicesConnectionString", out var commServicesOutput) &&
                    commServicesOutput.TryGetProperty("value", out var commServicesValue))
                    communicationServicesConnString = commServicesValue.GetString() ?? string.Empty;

                if (root.TryGetProperty("openAIEndpoint", out var openAIEndpointOutput) &&
                    openAIEndpointOutput.TryGetProperty("value", out var openAIEndpointValue))
                    openAIEndpoint = openAIEndpointValue.GetString() ?? string.Empty;

                if (root.TryGetProperty("openAIApiKey", out var openAIKeyOutput) &&
                    openAIKeyOutput.TryGetProperty("value", out var openAIKeyValue))
                    openAIApiKey = openAIKeyValue.GetString() ?? string.Empty;

                if (root.TryGetProperty("documentIntelligenceEndpoint", out var docIntEndpointOutput) &&
                    docIntEndpointOutput.TryGetProperty("value", out var docIntEndpointValue))
                    documentIntelligenceEndpoint = docIntEndpointValue.GetString() ?? string.Empty;

                if (root.TryGetProperty("documentIntelligenceApiKey", out var docIntKeyOutput) &&
                    docIntKeyOutput.TryGetProperty("value", out var docIntKeyValue))
                    documentIntelligenceApiKey = docIntKeyValue.GetString() ?? string.Empty;
            }

            ReportProgress(100, "Deployment onnistui!");

            return new DeploymentResult
            {
                Success = true,
                FunctionAppName = functionAppName,
                SqlServerFqdn = sqlServerFqdn,
                ApplicationInsightsConnectionString = appInsightsConnString,
                CommunicationServicesConnectionString = communicationServicesConnString,
                OpenAIEndpoint = openAIEndpoint,
                OpenAIApiKey = openAIApiKey,
                DocumentIntelligenceEndpoint = documentIntelligenceEndpoint,
                DocumentIntelligenceApiKey = documentIntelligenceApiKey
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Deployment epäonnistui");
            ReportProgress(0, $"Virhe: {ex.Message}");

            return new DeploymentResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private string LoadArmTemplate()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "InvoiceRobot.Admin.Bicep.main.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"ARM template not found: {resourceName}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private void ReportProgress(int percentComplete, string message)
    {
        _logger.LogInformation($"[{percentComplete}%] {message}");
        ProgressChanged?.Invoke(this, new DeploymentProgressEventArgs
        {
            PercentComplete = percentComplete,
            Message = message
        });
    }
}
