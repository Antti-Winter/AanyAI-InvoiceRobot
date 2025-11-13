using FluentAssertions;
using InvoiceRobot.Core.Domain;
using InvoiceRobot.Core.Interfaces;
using InvoiceRobot.Functions.Functions;
using InvoiceRobot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace InvoiceRobot.Functions.Tests.Functions;

public class InvoiceAnalyzerTests
{
    private readonly Mock<IOcrService> _mockOcrService;
    private readonly Mock<IProjectMatcher> _mockHeuristicMatcher;
    private readonly Mock<IProjectMatcher> _mockGptMatcher;
    private readonly Mock<IAccountingSystemService> _mockAccountingService;
    private readonly Mock<ILogger<InvoiceAnalyzer>> _mockLogger;
    private readonly InvoiceRobotDbContext _context;
    private readonly InvoiceAnalyzer _function;

    public InvoiceAnalyzerTests()
    {
        var options = new DbContextOptionsBuilder<InvoiceRobotDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new InvoiceRobotDbContext(options);

        _mockOcrService = new Mock<IOcrService>();
        _mockHeuristicMatcher = new Mock<IProjectMatcher>();
        _mockGptMatcher = new Mock<IProjectMatcher>();
        _mockAccountingService = new Mock<IAccountingSystemService>();
        _mockLogger = new Mock<ILogger<InvoiceAnalyzer>>();

        _function = new InvoiceAnalyzer(
            _context,
            _mockOcrService.Object,
            _mockHeuristicMatcher.Object,
            _mockGptMatcher.Object,
            _mockAccountingService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Should_Auto_Match_Invoice_With_High_Confidence()
    {
        // Arrange
        var project = new Project
        {
            NetvisorProjectKey = 100,
            ProjectCode = "PRJ-001",
            Name = "Test Project",
            IsActive = true
        };
        _context.Projects.Add(project);

        var invoice = new Invoice
        {
            NetvisorInvoiceKey = 12345,
            InvoiceNumber = "INV-001",
            VendorName = "Test Vendor",
            Amount = 1000m,
            Status = InvoiceStatus.Discovered
        };
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        _mockAccountingService
            .Setup(x => x.DownloadInvoicePdfAsync(12345))
            .ReturnsAsync(new byte[] { 1, 2, 3 });

        _mockOcrService
            .Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<byte[]>()))
            .ReturnsAsync("Test OCR text with PRJ-001");

        _mockHeuristicMatcher
            .Setup(x => x.MatchProjectAsync(It.IsAny<Invoice>(), It.IsAny<List<Project>>()))
            .ReturnsAsync(new ProjectMatchResult(100, 0.95, "High confidence match"));

        _mockAccountingService
            .Setup(x => x.UpdateInvoiceProjectAsync(12345, 100))
            .ReturnsAsync(true);

        // Act
        await _function.RunAsync(null);

        // Assert
        var updated = await _context.Invoices.FirstAsync();
        updated.Status.Should().Be(InvoiceStatus.MatchedAuto);
        updated.SuggestedProjectId.Should().NotBeNull();
        updated.OcrText.Should().NotBeNullOrEmpty();
        updated.AiConfidenceScore.Should().Be(0.95);
    }

    [Fact]
    public async Task Should_Create_Approval_Request_For_Low_Confidence()
    {
        // Arrange
        var project = new Project
        {
            NetvisorProjectKey = 200,
            ProjectCode = "PRJ-002",
            Name = "Another Project",
            IsActive = true
        };
        _context.Projects.Add(project);

        var invoice = new Invoice
        {
            NetvisorInvoiceKey = 54321,
            InvoiceNumber = "INV-002",
            VendorName = "Another Vendor",
            Amount = 2000m,
            Status = InvoiceStatus.Discovered
        };
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        _mockAccountingService
            .Setup(x => x.DownloadInvoicePdfAsync(54321))
            .ReturnsAsync(new byte[] { 4, 5, 6 });

        _mockOcrService
            .Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<byte[]>()))
            .ReturnsAsync("Test OCR text");

        _mockHeuristicMatcher
            .Setup(x => x.MatchProjectAsync(It.IsAny<Invoice>(), It.IsAny<List<Project>>()))
            .ReturnsAsync((ProjectMatchResult?)null);

        _mockGptMatcher
            .Setup(x => x.MatchProjectAsync(It.IsAny<Invoice>(), It.IsAny<List<Project>>()))
            .ReturnsAsync(new ProjectMatchResult(200, 0.7, "Medium confidence match"));

        // Act
        await _function.RunAsync(null);

        // Assert
        var updated = await _context.Invoices.FirstAsync();
        updated.Status.Should().Be(InvoiceStatus.PendingApproval);
        updated.AiConfidenceScore.Should().Be(0.7);

        var approval = await _context.ApprovalRequests.FirstOrDefaultAsync();
        approval.Should().NotBeNull();
        approval!.InvoiceId.Should().Be(updated.Id);
        approval.SuggestedProjectId.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Use_GPT_When_Heuristic_Fails()
    {
        // Arrange
        var project = new Project
        {
            NetvisorProjectKey = 300,
            ProjectCode = "PRJ-003",
            Name = "GPT Project",
            IsActive = true
        };
        _context.Projects.Add(project);

        var invoice = new Invoice
        {
            NetvisorInvoiceKey = 99999,
            InvoiceNumber = "INV-003",
            VendorName = "GPT Vendor",
            Amount = 3000m,
            Status = InvoiceStatus.Discovered
        };
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        _mockAccountingService
            .Setup(x => x.DownloadInvoicePdfAsync(99999))
            .ReturnsAsync(new byte[] { 7, 8, 9 });

        _mockOcrService
            .Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<byte[]>()))
            .ReturnsAsync("OCR text without clear project reference");

        _mockHeuristicMatcher
            .Setup(x => x.MatchProjectAsync(It.IsAny<Invoice>(), It.IsAny<List<Project>>()))
            .ReturnsAsync((ProjectMatchResult?)null);

        _mockGptMatcher
            .Setup(x => x.MatchProjectAsync(It.IsAny<Invoice>(), It.IsAny<List<Project>>()))
            .ReturnsAsync(new ProjectMatchResult(300, 0.92, "GPT identified project"));

        _mockAccountingService
            .Setup(x => x.UpdateInvoiceProjectAsync(99999, 300))
            .ReturnsAsync(true);

        // Act
        await _function.RunAsync(null);

        // Assert
        var updated = await _context.Invoices.FirstAsync();
        updated.Status.Should().Be(InvoiceStatus.MatchedAuto);
        updated.AiConfidenceScore.Should().Be(0.92);
        updated.AiReasoning.Should().Contain("GPT");

        // Verify GPT was called
        _mockGptMatcher.Verify(x => x.MatchProjectAsync(It.IsAny<Invoice>(), It.IsAny<List<Project>>()), Times.Once);
    }

    [Fact]
    public async Task Should_Skip_Invoices_Without_PDF_Data()
    {
        // Arrange
        var invoice = new Invoice
        {
            NetvisorInvoiceKey = 11111,
            InvoiceNumber = "INV-NO-PDF",
            VendorName = "No PDF Vendor",
            Amount = 500m,
            Status = InvoiceStatus.Discovered
        };
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        _mockAccountingService
            .Setup(x => x.DownloadInvoicePdfAsync(11111))
            .ReturnsAsync((byte[]?)null); // No PDF available

        // Act
        await _function.RunAsync(null);

        // Assert
        var updated = await _context.Invoices.FirstAsync();
        updated.Status.Should().Be(InvoiceStatus.Discovered); // Unchanged
        updated.OcrText.Should().BeNullOrEmpty();
    }
}
