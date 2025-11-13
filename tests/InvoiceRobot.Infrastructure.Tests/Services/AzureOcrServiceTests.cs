using FluentAssertions;
using InvoiceRobot.Core.Interfaces;
using InvoiceRobot.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace InvoiceRobot.Infrastructure.Tests.Services;

public class AzureOcrServiceTests
{
    // HUOM: Nämä ovat integraatiotestejä, jotka vaativat oikeat Azure-tunnukset
    // Ajetaan CI/CD:ssä, skipataan paikallisessa kehityksessä
    [Fact(Skip = "Integration test - requires real Azure Document Intelligence credentials")]
    public async Task ExtractTextFromPdfAsync_Should_Return_Text()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureOcrService>>();
        var endpoint = Environment.GetEnvironmentVariable("DocumentIntelligence:Endpoint") ?? "https://test.cognitiveservices.azure.com/";
        var apiKey = Environment.GetEnvironmentVariable("DocumentIntelligence:ApiKey") ?? "test-key";

        var service = new AzureOcrService(endpoint, apiKey, mockLogger.Object);

        // Mock PDF data (yksinkertainen)
        var pdfData = new byte[] { 37, 80, 68, 70 }; // %PDF header

        // Act
        var result = await service.ExtractTextFromPdfAsync(pdfData);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("lasku"); // Oletetaan että lasku-sana löytyy
    }

    [Fact(Skip = "Integration test - requires real Azure Document Intelligence credentials")]
    public async Task ExtractTextFromPdfAsync_Should_Throw_When_Invalid_Pdf()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureOcrService>>();
        var endpoint = Environment.GetEnvironmentVariable("DocumentIntelligence:Endpoint") ?? "https://test.cognitiveservices.azure.com/";
        var apiKey = Environment.GetEnvironmentVariable("DocumentIntelligence:ApiKey") ?? "test-key";

        var service = new AzureOcrService(endpoint, apiKey, mockLogger.Object);

        var invalidData = new byte[] { 1, 2, 3 };

        // Act
        var act = async () => await service.ExtractTextFromPdfAsync(invalidData);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public void AzureOcrService_Should_Initialize_Successfully()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureOcrService>>();

        // Act
        var service = new AzureOcrService(
            "https://test.cognitiveservices.azure.com/",
            "test-key",
            mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<IOcrService>();
    }
}
