@description('Projektin nimi-prefiksi')
param namePrefix string = 'invoicerobot'

@description('Sijainti')
param location string = resourceGroup().location

@description('Ympäristö (dev, staging, prod)')
param environment string = 'dev'

@description('Taloushallintojärjestelmä (Netvisor tai Procountor)')
@allowed([
  'Netvisor'
  'Procountor'
])
param accountingProvider string = 'Netvisor'

@description('SQL Server admin salasana')
@secure()
param sqlServerPassword string

@description('Sähköpostilista projektipäälliköille')
param emailPmDistributionList string = ''

// Variables
var functionAppName = '${namePrefix}-func-${environment}'
var appServicePlanName = '${namePrefix}-asp-${environment}'
var storageAccountName = replace('${namePrefix}st${environment}', '-', '')
var sqlServerName = '${namePrefix}-sql-${environment}'
var sqlDatabaseName = 'InvoiceRobotDb'
var appInsightsName = '${namePrefix}-appi-${environment}'
var communicationServicesName = '${namePrefix}-acs-${environment}'
var openAIName = '${namePrefix}-openai-${environment}'
var documentIntelligenceName = '${namePrefix}-docint-${environment}'

// Storage Account
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
  }
}

// App Service Plan (Consumption)
resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
    reserved: true
  }
}

// Function App
resource functionApp 'Microsoft.Web/sites@2023-01-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${az.environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
        {
          name: 'SqlConnectionString'
          value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabaseName};Persist Security Info=False;User ID=${sqlServer.properties.administratorLogin};Password=${sqlServerPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
        }
        {
          name: 'AccountingProvider'
          value: accountingProvider
        }
        {
          name: 'AzureOpenAIEndpoint'
          value: openAI.properties.endpoint
        }
        {
          name: 'AzureOpenAIApiKey'
          value: openAI.listKeys().key1
        }
        {
          name: 'AzureOpenAIDeploymentName'
          value: 'gpt-4o'
        }
        {
          name: 'DocumentIntelligenceEndpoint'
          value: documentIntelligence.properties.endpoint
        }
        {
          name: 'DocumentIntelligenceApiKey'
          value: documentIntelligence.listKeys().key1
        }
        {
          name: 'CommunicationServicesConnectionString'
          value: communicationServices.listKeys().primaryConnectionString
        }
        {
          name: 'EmailPmDistributionList'
          value: emailPmDistributionList
        }
      ]
    }
    httpsOnly: true
  }
}

// SQL Server
resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: 'sqladmin'
    administratorLoginPassword: sqlServerPassword
    version: '12.0'
  }
}

// SQL Database
resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
}

// Application Insights
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

// Communication Services
resource communicationServices 'Microsoft.Communication/communicationServices@2023-04-01' = {
  name: communicationServicesName
  location: 'global'
  properties: {
    dataLocation: 'Europe'
  }
}

// Azure OpenAI - Sweden Central (best support for OpenAI in Europe)
resource openAI 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: openAIName
  location: 'swedencentral'
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: openAIName
    publicNetworkAccess: 'Enabled'
  }
}

// OpenAI GPT-4o Deployment (latest, faster, cheaper than GPT-4)
resource openAIDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: openAI
  name: 'gpt-4o'
  sku: {
    name: 'GlobalStandard'
    capacity: 1
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-05-13'
    }
  }
}

// Document Intelligence (Form Recognizer)
resource documentIntelligence 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: documentIntelligenceName
  location: location
  kind: 'FormRecognizer'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: documentIntelligenceName
    publicNetworkAccess: 'Enabled'
  }
}

// Outputs
output functionAppName string = functionApp.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output applicationInsightsConnectionString string = applicationInsights.properties.ConnectionString
output communicationServicesConnectionString string = communicationServices.listKeys().primaryConnectionString
output openAIEndpoint string = openAI.properties.endpoint
output openAIApiKey string = openAI.listKeys().key1
output documentIntelligenceEndpoint string = documentIntelligence.properties.endpoint
output documentIntelligenceApiKey string = documentIntelligence.listKeys().key1
