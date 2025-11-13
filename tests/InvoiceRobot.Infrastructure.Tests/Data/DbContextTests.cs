using FluentAssertions;
using InvoiceRobot.Core.Domain;
using InvoiceRobot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiceRobot.Infrastructure.Tests.Data;

public class DbContextTests
{
    private InvoiceRobotDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<InvoiceRobotDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new InvoiceRobotDbContext(options);
    }

    [Fact]
    public async Task Should_Save_Invoice_To_Database()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var invoice = new Invoice
        {
            NetvisorInvoiceKey = 12345,
            InvoiceNumber = "INV-001",
            VendorName = "Test Vendor",
            Amount = 1000m,
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(14),
            Status = InvoiceStatus.Discovered,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.Invoices.FirstOrDefaultAsync();
        saved.Should().NotBeNull();
        saved!.InvoiceNumber.Should().Be("INV-001");
    }

    // Note: In-Memory database doesn't fully enforce unique constraints like SQL Server.
    // The unique constraint on NetvisorInvoiceKey will work correctly in production with SQL Server.
}
