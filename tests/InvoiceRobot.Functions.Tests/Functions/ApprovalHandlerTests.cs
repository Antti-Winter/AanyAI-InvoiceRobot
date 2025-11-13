using FluentAssertions;
using InvoiceRobot.Core.Domain;
using InvoiceRobot.Core.Interfaces;
using InvoiceRobot.Functions.Functions;
using InvoiceRobot.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace InvoiceRobot.Functions.Tests.Functions;

public class ApprovalHandlerTests
{
    private readonly InvoiceRobotDbContext _context;
    private readonly Mock<IAccountingSystemService> _mockAccountingService;
    private readonly Mock<ILogger<ApprovalHandler>> _mockLogger;
    private readonly ApprovalHandler _function;

    public ApprovalHandlerTests()
    {
        var options = new DbContextOptionsBuilder<InvoiceRobotDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new InvoiceRobotDbContext(options);

        _mockAccountingService = new Mock<IAccountingSystemService>();
        _mockLogger = new Mock<ILogger<ApprovalHandler>>();

        _function = new ApprovalHandler(
            _context,
            _mockAccountingService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GET_Should_Return_Approval_Form()
    {
        // Arrange
        var token = Guid.NewGuid().ToString();
        var invoice = new Invoice
        {
            NetvisorInvoiceKey = 123,
            InvoiceNumber = "INV-001",
            VendorName = "Test Vendor",
            Amount = 1000m,
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            SuggestedProjectKey = 100,
            Status = InvoiceStatus.PendingApproval,
            CreatedAt = DateTime.UtcNow
        };
        _context.Invoices.Add(invoice);

        var approvalRequest = new ApprovalRequest
        {
            InvoiceId = invoice.Id,
            Token = token,
            Status = ApprovalStatus.Pending,
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.ApprovalRequests.Add(approvalRequest);
        await _context.SaveChangesAsync();

        var mockRequest = new Mock<HttpRequest>();
        mockRequest.Setup(r => r.Query["token"]).Returns(token);

        // Act
        var response = await _function.GetApprovalFormAsync(mockRequest.Object);

        // Assert
        response.Should().BeOfType<ContentResult>();
        var contentResult = (ContentResult)response;
        contentResult.StatusCode.Should().Be(200);
        contentResult.ContentType.Should().Be("text/html");
        contentResult.Content.Should().Contain("INV-001");
    }

    [Fact]
    public async Task POST_Approve_Should_Update_Accounting_System()
    {
        // Arrange
        var token = Guid.NewGuid().ToString();
        var invoice = new Invoice
        {
            NetvisorInvoiceKey = 123,
            InvoiceNumber = "INV-001",
            VendorName = "Test Vendor",
            Amount = 1000m,
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            SuggestedProjectKey = 100,
            Status = InvoiceStatus.PendingApproval,
            CreatedAt = DateTime.UtcNow
        };
        _context.Invoices.Add(invoice);

        var approvalRequest = new ApprovalRequest
        {
            InvoiceId = invoice.Id,
            Token = token,
            Status = ApprovalStatus.Pending,
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.ApprovalRequests.Add(approvalRequest);
        await _context.SaveChangesAsync();

        _mockAccountingService
            .Setup(x => x.UpdateInvoiceProjectAsync(123, 100))
            .ReturnsAsync(true);

        // Act
        var response = await _function.PostApprovalAsync(token, 100, approved: true, null);

        // Assert
        response.Should().BeOfType<ContentResult>();
        var contentResult = (ContentResult)response;
        contentResult.StatusCode.Should().Be(200);

        var updatedInvoice = await _context.Invoices.FindAsync(invoice.Id);
        updatedInvoice!.Status.Should().Be(InvoiceStatus.Approved);
        updatedInvoice.FinalProjectKey.Should().Be(100);

        var updatedRequest = await _context.ApprovalRequests.FindAsync(approvalRequest.Id);
        updatedRequest!.Status.Should().Be(ApprovalStatus.Approved);

        _mockAccountingService.Verify(x => x.UpdateInvoiceProjectAsync(123, 100), Times.Once);
    }
}
