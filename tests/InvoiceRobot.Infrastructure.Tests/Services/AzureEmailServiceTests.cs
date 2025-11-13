using FluentAssertions;
using InvoiceRobot.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace InvoiceRobot.Infrastructure.Tests.Services;

public class AzureEmailServiceTests
{
    // Unit test - testaa vain että service voidaan luoda
    [Fact]
    public void AzureEmailService_Should_Initialize_Successfully()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureEmailService>>();
        var connectionString = "endpoint=https://test.communication.azure.com/;accesskey=dGVzdGtleQ==";
        var senderAddress = "DoNotReply@test.com";

        // Act
        var act = () => new AzureEmailService(connectionString, senderAddress, mockLogger.Object);

        // Assert
        act.Should().NotThrow();
    }

    // HUOM: Tämä on integraatiotesti, joka vaatii oikeat Azure Communication Services -tunnukset
    // Ajetaan CI/CD:ssä, skipataan paikallisessa kehityksessä
    [Fact(Skip = "Integration test - requires real Azure Communication Services credentials")]
    public async Task SendApprovalRequestAsync_Should_Send_Email()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureEmailService>>();
        var connectionString = Environment.GetEnvironmentVariable("CommunicationServices:ConnectionString")
            ?? "endpoint=https://test.communication.azure.com/;accesskey=dGVzdGtleQ==";
        var senderAddress = Environment.GetEnvironmentVariable("Email:SenderAddress")
            ?? "DoNotReply@test.com";

        var service = new AzureEmailService(connectionString, senderAddress, mockLogger.Object);

        var to = "pm@company.com";
        var subject = "Hyväksyntäpyyntö: Lasku INV-001";
        var htmlBody = "<p>Hyväksy lasku</p>";

        // Act
        await service.SendApprovalRequestAsync(to, subject, htmlBody);

        // Assert - Ei poikkeusta
    }
}
