using FluentAssertions;
using InvoiceRobot.Core.Domain;
using InvoiceRobot.Core.Interfaces;
using InvoiceRobot.Infrastructure.Data;
using InvoiceRobot.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InvoiceRobot.Functions.Tests.Integration;

public class EndToEndTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public EndToEndTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Full_Invoice_Flow_Should_Work()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoiceRobotDbContext>();

        // 1. Synkronoi projektit (simuloi)
        var project = new Project
        {
            NetvisorProjectKey = 100,
            ProjectCode = "PRJ-001",
            Name = "Kerrostalo Mannerheimintie",
            Address = "Mannerheimintie 123, Helsinki",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        // 2. Hae lasku (simuloi InvoiceFetcher)
        var invoice = new Invoice
        {
            NetvisorInvoiceKey = 12345,
            InvoiceNumber = "INV-001",
            VendorName = "Rakennusliike Oy",
            Amount = 5000m,
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(14),
            Status = InvoiceStatus.Discovered,
            CreatedAt = DateTime.UtcNow
        };
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        // 3. OCR (simuloi)
        invoice.OcrText = "Työ tehty kohteessa PRJ-001 osoitteessa Mannerheimintie 123";
        invoice.OcrProcessedAt = DateTime.UtcNow;

        // 4. Heuristinen matcher
        var heuristicMatcher = scope.ServiceProvider.GetRequiredService<HeuristicProjectMatcher>();
        var projects = await context.Projects.ToListAsync();
        var heuristicResult = await heuristicMatcher.MatchProjectAsync(invoice, projects);

        heuristicResult.Should().NotBeNull();
        invoice.SuggestedProjectKey = heuristicResult!.ProjectKey;
        invoice.AiConfidenceScore = heuristicResult.ConfidenceScore;
        invoice.AiReasoning = heuristicResult.Reasoning;
        invoice.AiAnalyzedAt = DateTime.UtcNow;
        invoice.SuggestedProjectId = project.Id;

        // 5. Jos varmuus ≥ 0.9 → Päivitä automaattisesti
        if (heuristicResult.ConfidenceScore >= 0.9)
        {
            var accountingService = scope.ServiceProvider.GetRequiredService<IAccountingSystemService>();
            var success = await accountingService.UpdateInvoiceProjectAsync(
                invoice.NetvisorInvoiceKey,
                heuristicResult.ProjectKey);

            if (success)
            {
                invoice.Status = InvoiceStatus.MatchedAuto;
                invoice.FinalProjectKey = heuristicResult.ProjectKey;
                invoice.UpdatedToAccountingSystemAt = DateTime.UtcNow;
            }
        }

        await context.SaveChangesAsync();

        // Assert
        var savedInvoice = await context.Invoices.FindAsync(invoice.Id);
        savedInvoice.Should().NotBeNull();
        savedInvoice!.Status.Should().Be(InvoiceStatus.MatchedAuto);
        savedInvoice.FinalProjectKey.Should().Be(100);
        savedInvoice.UpdatedToAccountingSystemAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Low_Confidence_Should_Create_Approval_Request()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoiceRobotDbContext>();

        // 1. Luo projekti
        var project = new Project
        {
            NetvisorProjectKey = 200,
            ProjectCode = "PRJ-002",
            Name = "Toimistoremontti",
            Address = "Esimerkkikatu 10, Espoo",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        // 2. Luo lasku epäselvällä tekstillä
        var invoice = new Invoice
        {
            NetvisorInvoiceKey = 54321,
            InvoiceNumber = "INV-002",
            VendorName = "Tuntematonliike Oy",
            Amount = 3000m,
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(14),
            Status = InvoiceStatus.Discovered,
            CreatedAt = DateTime.UtcNow
        };
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        // 3. OCR - epäselvä teksti
        invoice.OcrText = "Materiaalit toimitettu";
        invoice.OcrProcessedAt = DateTime.UtcNow;

        // 4. Heuristinen matcher - alhainen varmuus
        var heuristicMatcher = scope.ServiceProvider.GetRequiredService<HeuristicProjectMatcher>();
        var projects = await context.Projects.ToListAsync();
        var heuristicResult = await heuristicMatcher.MatchProjectAsync(invoice, projects);

        // 5. Jos varmuus < 0.9 → Luo ApprovalRequest
        if (heuristicResult == null || heuristicResult.ConfidenceScore < 0.9)
        {
            invoice.Status = InvoiceStatus.PendingApproval;
            invoice.SuggestedProjectKey = heuristicResult?.ProjectKey;
            invoice.AiConfidenceScore = heuristicResult?.ConfidenceScore ?? 0.0;
            invoice.AiReasoning = heuristicResult?.Reasoning ?? "Ei vastaavuutta löytynyt";

            var approvalRequest = new ApprovalRequest
            {
                InvoiceId = invoice.Id,
                Token = Guid.NewGuid().ToString(),
                Status = ApprovalStatus.Pending,
                SuggestedProjectKey = heuristicResult?.ProjectKey,
                SuggestedProjectId = project.Id,
                ConfidenceScore = heuristicResult?.ConfidenceScore ?? 0.0,
                Reasoning = heuristicResult?.Reasoning ?? "Ei vastaavuutta löytynyt",
                SentAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            context.ApprovalRequests.Add(approvalRequest);
        }

        await context.SaveChangesAsync();

        // Assert
        var savedInvoice = await context.Invoices.FindAsync(invoice.Id);
        savedInvoice.Should().NotBeNull();
        savedInvoice!.Status.Should().Be(InvoiceStatus.PendingApproval);

        var approvalRequests = await context.ApprovalRequests
            .Where(ar => ar.InvoiceId == invoice.Id)
            .ToListAsync();
        approvalRequests.Should().HaveCount(1);
        approvalRequests[0].Status.Should().Be(ApprovalStatus.Pending);
        approvalRequests[0].Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Approval_Process_Should_Update_Invoice()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoiceRobotDbContext>();

        // 1. Luo projekti
        var project = new Project
        {
            NetvisorProjectKey = 300,
            ProjectCode = "PRJ-003",
            Name = "Saneeraus",
            Address = "Testikatu 5, Vantaa",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        // 2. Luo lasku
        var invoice = new Invoice
        {
            NetvisorInvoiceKey = 99999,
            InvoiceNumber = "INV-003",
            VendorName = "Hyväksyntätesti Oy",
            Amount = 2000m,
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(14),
            Status = InvoiceStatus.PendingApproval,
            SuggestedProjectKey = 300,
            CreatedAt = DateTime.UtcNow
        };
        context.Invoices.Add(invoice);

        // 3. Luo ApprovalRequest
        var token = Guid.NewGuid().ToString();
        var approvalRequest = new ApprovalRequest
        {
            InvoiceId = invoice.Id,
            Token = token,
            Status = ApprovalStatus.Pending,
            SuggestedProjectKey = 300,
            SuggestedProjectId = project.Id,
            ConfidenceScore = 0.7,
            Reasoning = "Osittainen vastaavuus",
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        context.ApprovalRequests.Add(approvalRequest);
        await context.SaveChangesAsync();

        // 4. Simuloi hyväksyntä
        var accountingService = scope.ServiceProvider.GetRequiredService<IAccountingSystemService>();
        var success = await accountingService.UpdateInvoiceProjectAsync(
            invoice.NetvisorInvoiceKey,
            300);

        if (success)
        {
            approvalRequest.Status = ApprovalStatus.Approved;
            approvalRequest.ApprovedProjectKey = 300;
            approvalRequest.RespondedAt = DateTime.UtcNow;
            approvalRequest.UpdatedAt = DateTime.UtcNow;

            invoice.Status = InvoiceStatus.Approved;
            invoice.FinalProjectKey = 300;
            invoice.UpdatedToAccountingSystemAt = DateTime.UtcNow;
            invoice.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

        // Assert
        var savedInvoice = await context.Invoices.FindAsync(invoice.Id);
        savedInvoice.Should().NotBeNull();
        savedInvoice!.Status.Should().Be(InvoiceStatus.Approved);
        savedInvoice.FinalProjectKey.Should().Be(300);
        savedInvoice.UpdatedToAccountingSystemAt.Should().NotBeNull();

        var savedApprovalRequest = await context.ApprovalRequests.FindAsync(approvalRequest.Id);
        savedApprovalRequest.Should().NotBeNull();
        savedApprovalRequest!.Status.Should().Be(ApprovalStatus.Approved);
        savedApprovalRequest.ApprovedProjectKey.Should().Be(300);
        savedApprovalRequest.RespondedAt.Should().NotBeNull();
    }
}

public class IntegrationTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; }

    public IntegrationTestFixture()
    {
        var services = new ServiceCollection();

        // Konfiguroi palvelut testausta varten
        services.AddDbContext<InvoiceRobotDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Lisää HeuristicProjectMatcher
        services.AddScoped<HeuristicProjectMatcher>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<HeuristicProjectMatcher>>();
            return new HeuristicProjectMatcher(logger);
        });

        // Mock IAccountingSystemService
        var mockAccountingService = new Mock<IAccountingSystemService>();
        mockAccountingService
            .Setup(x => x.UpdateInvoiceProjectAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(true);
        services.AddScoped<IAccountingSystemService>(_ => mockAccountingService.Object);

        // Lisää logging
        services.AddLogging(builder => builder.AddDebug());

        ServiceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
