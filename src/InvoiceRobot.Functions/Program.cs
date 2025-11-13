using InvoiceRobot.Core.Interfaces;
using InvoiceRobot.Functions.Functions;
using InvoiceRobot.Infrastructure.Data;
using InvoiceRobot.Infrastructure.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// DbContext
builder.Services.AddDbContext<InvoiceRobotDbContext>(options =>
{
    var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString")
        ?? throw new InvalidOperationException("SqlConnectionString missing");
    options.UseSqlServer(connectionString);
});

// Services
builder.Services.AddScoped<IAccountSystemOrchestrator, MockAccountSystemOrchestrator>();
builder.Services.AddScoped<IAccountingSystemService, AccountingSystemService>();

// OCR Service
builder.Services.AddScoped<IOcrService>(sp =>
{
    var endpoint = Environment.GetEnvironmentVariable("DocumentIntelligence:Endpoint")!;
    var apiKey = Environment.GetEnvironmentVariable("DocumentIntelligence:ApiKey")!;
    var logger = sp.GetRequiredService<ILogger<AzureOcrService>>();
    return new AzureOcrService(endpoint, apiKey, logger);
});

// Project Matchers - Register both for InvoiceAnalyzer
builder.Services.AddScoped<HeuristicProjectMatcher>();
builder.Services.AddScoped<GptProjectMatcher>(sp =>
{
    var endpoint = Environment.GetEnvironmentVariable("AzureOpenAI:Endpoint")!;
    var apiKey = Environment.GetEnvironmentVariable("AzureOpenAI:ApiKey")!;
    var deployment = Environment.GetEnvironmentVariable("AzureOpenAI:DeploymentName") ?? "gpt-4";
    var logger = sp.GetRequiredService<ILogger<GptProjectMatcher>>();
    return new GptProjectMatcher(endpoint, apiKey, deployment, logger);
});

// InvoiceAnalyzer Function - Manual registration with both matchers
builder.Services.AddScoped<InvoiceAnalyzer>(sp =>
{
    var context = sp.GetRequiredService<InvoiceRobotDbContext>();
    var ocrService = sp.GetRequiredService<IOcrService>();
    var heuristicMatcher = sp.GetRequiredService<HeuristicProjectMatcher>();
    var gptMatcher = sp.GetRequiredService<GptProjectMatcher>();
    var accountingService = sp.GetRequiredService<IAccountingSystemService>();
    var logger = sp.GetRequiredService<ILogger<InvoiceAnalyzer>>();

    return new InvoiceAnalyzer(context, ocrService, heuristicMatcher, gptMatcher, accountingService, logger);
});

// Email Service
builder.Services.AddScoped<IEmailService>(sp =>
{
    var connectionString = Environment.GetEnvironmentVariable("CommunicationServices:ConnectionString")!;
    var senderAddress = Environment.GetEnvironmentVariable("Email:SenderAddress")!;
    var logger = sp.GetRequiredService<ILogger<AzureEmailService>>();
    return new AzureEmailService(connectionString, senderAddress, logger);
});

builder.Build().Run();
