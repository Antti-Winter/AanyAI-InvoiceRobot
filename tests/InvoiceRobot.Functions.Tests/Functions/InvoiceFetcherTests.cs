using FluentAssertions;
using InvoiceRobot.Core.Domain;
using InvoiceRobot.Core.Interfaces;
using InvoiceRobot.Functions.Functions;
using InvoiceRobot.Infrastructure.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace InvoiceRobot.Functions.Tests.Functions;

public class InvoiceFetcherTests
{
    private readonly Mock<IAccountingSystemService> _mockAccountingService;
    private readonly Mock<ILogger<InvoiceFetcher>> _mockLogger;
    private readonly InvoiceRobotDbContext _context;
    private readonly InvoiceFetcher _function;

    public InvoiceFetcherTests()
    {
        var options = new DbContextOptionsBuilder<InvoiceRobotDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new InvoiceRobotDbContext(options);

        _mockAccountingService = new Mock<IAccountingSystemService>();
        _mockLogger = new Mock<ILogger<InvoiceFetcher>>();

        _function = new InvoiceFetcher(
            _context,
            _mockAccountingService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Should_Fetch_And_Save_New_Invoices()
    {
        // Arrange
        var mockInvoices = new List<AccountingInvoiceDto>
        {
            new AccountingInvoiceDto(
                InvoiceKey: 12345,
                InvoiceNumber: "INV-001",
                VendorName: "Test Vendor",
                Amount: 1500m,
                InvoiceDate: DateTime.UtcNow,
                DueDate: DateTime.UtcNow.AddDays(14),
                ProjectKey: null
            )
        };

        _mockAccountingService
            .Setup(x => x.GetPurchaseInvoicesAsync(30))
            .ReturnsAsync(mockInvoices);

        _mockAccountingService
            .Setup(x => x.GetActiveProjectsAsync())
            .ReturnsAsync(new List<AccountingProjectDto>());

        // Act
        await _function.RunAsync(null);

        // Assert
        var saved = await _context.Invoices.FirstOrDefaultAsync();
        saved.Should().NotBeNull();
        saved!.NetvisorInvoiceKey.Should().Be(12345);
        saved.InvoiceNumber.Should().Be("INV-001");
        saved.Status.Should().Be(InvoiceStatus.Discovered);
    }

    [Fact]
    public async Task Should_Not_Duplicate_Existing_Invoices()
    {
        // Arrange
        var existingInvoice = new Invoice
        {
            NetvisorInvoiceKey = 12345,
            InvoiceNumber = "INV-001",
            VendorName = "Test Vendor",
            Amount = 1500m,
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(14),
            Status = InvoiceStatus.Discovered,
            CreatedAt = DateTime.UtcNow
        };
        _context.Invoices.Add(existingInvoice);
        await _context.SaveChangesAsync();

        var mockInvoices = new List<AccountingInvoiceDto>
        {
            new AccountingInvoiceDto(12345, "INV-001", "Test Vendor", 1500m, DateTime.UtcNow, DateTime.UtcNow.AddDays(14), null)
        };

        _mockAccountingService
            .Setup(x => x.GetPurchaseInvoicesAsync(30))
            .ReturnsAsync(mockInvoices);

        _mockAccountingService
            .Setup(x => x.GetActiveProjectsAsync())
            .ReturnsAsync(new List<AccountingProjectDto>());

        // Act
        await _function.RunAsync(null);

        // Assert
        var invoiceCount = await _context.Invoices.CountAsync();
        invoiceCount.Should().Be(1); // Ei duplikaatteja
    }

    [Fact]
    public async Task Should_Sync_Projects_From_Accounting_System()
    {
        // Arrange
        var mockProjects = new List<AccountingProjectDto>
        {
            new AccountingProjectDto(
                ProjectKey: 100,
                ProjectCode: "PRJ-001",
                Name: "Test Project",
                Address: "Test Address",
                IsActive: true
            )
        };

        _mockAccountingService
            .Setup(x => x.GetActiveProjectsAsync())
            .ReturnsAsync(mockProjects);

        _mockAccountingService
            .Setup(x => x.GetPurchaseInvoicesAsync(30))
            .ReturnsAsync(new List<AccountingInvoiceDto>());

        // Act
        await _function.RunAsync(null);

        // Assert
        var project = await _context.Projects.FirstOrDefaultAsync();
        project.Should().NotBeNull();
        project!.NetvisorProjectKey.Should().Be(100);
        project.ProjectCode.Should().Be("PRJ-001");
    }
}
