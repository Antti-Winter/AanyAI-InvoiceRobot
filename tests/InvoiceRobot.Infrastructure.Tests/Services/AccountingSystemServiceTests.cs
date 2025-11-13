using FluentAssertions;
using InvoiceRobot.Core.Interfaces;
using InvoiceRobot.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace InvoiceRobot.Infrastructure.Tests.Services;

public class AccountingSystemServiceTests
{
    private readonly Mock<IAccountSystemOrchestrator> _mockOrchestrator;
    private readonly Mock<ILogger<AccountingSystemService>> _mockLogger;
    private readonly AccountingSystemService _service;

    public AccountingSystemServiceTests()
    {
        _mockOrchestrator = new Mock<IAccountSystemOrchestrator>();
        _mockLogger = new Mock<ILogger<AccountingSystemService>>();
        _service = new AccountingSystemService(_mockOrchestrator.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetPurchaseInvoicesAsync_Should_Return_Invoices()
    {
        // Arrange
        var mockInvoices = new List<OrchestratorInvoice>
        {
            new OrchestratorInvoice
            {
                InvoiceKey = 1,
                InvoiceNumber = "INV-001",
                VendorName = "Test Vendor",
                Amount = 1000m,
                InvoiceDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(14)
            }
        };

        _mockOrchestrator
            .Setup(x => x.GetPurchaseInvoicesAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(mockInvoices);

        // Act
        var result = await _service.GetPurchaseInvoicesAsync(30);

        // Assert
        result.Should().HaveCount(1);
        result[0].InvoiceNumber.Should().Be("INV-001");
    }

    [Fact]
    public async Task UpdateInvoiceProjectAsync_Should_Return_True_When_Successful()
    {
        // Arrange
        _mockOrchestrator
            .Setup(x => x.UpdateInvoiceProjectAsync(123, 456))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateInvoiceProjectAsync(123, 456);

        // Assert
        result.Should().BeTrue();
        _mockOrchestrator.Verify(x => x.UpdateInvoiceProjectAsync(123, 456), Times.Once);
    }
}
