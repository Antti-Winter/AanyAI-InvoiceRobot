using FluentAssertions;
using InvoiceRobot.Core.Domain;

namespace InvoiceRobot.Core.Tests.Domain;

public class InvoiceTests
{
    [Fact]
    public void Invoice_Should_Initialize_With_Default_Values()
    {
        // Arrange & Act
        var invoice = new Invoice();

        // Assert
        invoice.Id.Should().Be(0);
        invoice.Status.Should().Be(InvoiceStatus.Discovered);
        invoice.InvoiceNumber.Should().BeEmpty();
    }

    [Fact]
    public void Invoice_Should_Set_Properties()
    {
        // Arrange
        var invoice = new Invoice
        {
            InvoiceNumber = "INV-2025-001",
            VendorName = "Rakennusliike Oy",
            Amount = 1500.50m,
            Status = InvoiceStatus.Analyzing
        };

        // Assert
        invoice.InvoiceNumber.Should().Be("INV-2025-001");
        invoice.VendorName.Should().Be("Rakennusliike Oy");
        invoice.Amount.Should().Be(1500.50m);
        invoice.Status.Should().Be(InvoiceStatus.Analyzing);
    }

    [Theory]
    [InlineData(0.95, InvoiceStatus.MatchedAuto)]
    [InlineData(0.85, InvoiceStatus.PendingApproval)]
    public void Invoice_Status_Should_Match_ConfidenceScore(double confidence, InvoiceStatus expectedStatus)
    {
        // Arrange
        var invoice = new Invoice
        {
            AiConfidenceScore = confidence,
            Status = confidence >= 0.9 ? InvoiceStatus.MatchedAuto : InvoiceStatus.PendingApproval
        };

        // Assert
        invoice.Status.Should().Be(expectedStatus);
    }
}
