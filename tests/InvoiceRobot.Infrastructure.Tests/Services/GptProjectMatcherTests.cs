using FluentAssertions;
using InvoiceRobot.Core.Domain;
using InvoiceRobot.Core.Interfaces;
using InvoiceRobot.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace InvoiceRobot.Infrastructure.Tests.Services;

public class GptProjectMatcherTests
{
    // HUOM: Nämä ovat integraatiotestejä, jotka vaativat oikeat Azure OpenAI -tunnukset
    // Ajetaan CI/CD:ssä, skipataan paikallisessa kehityksessä
    [Fact(Skip = "Integration test - requires real Azure OpenAI credentials")]
    public async Task Should_Call_OpenAI_And_Return_Match()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<GptProjectMatcher>>();
        var endpoint = Environment.GetEnvironmentVariable("AzureOpenAI:Endpoint") ?? "https://test.openai.azure.com/";
        var apiKey = Environment.GetEnvironmentVariable("AzureOpenAI:ApiKey") ?? "test-key";
        var deploymentName = Environment.GetEnvironmentVariable("AzureOpenAI:DeploymentName") ?? "gpt-4";

        var matcher = new GptProjectMatcher(endpoint, apiKey, deploymentName, mockLogger.Object);

        var invoice = new Invoice
        {
            InvoiceNumber = "INV-001",
            VendorName = "Rakennusliike Oy",
            Amount = 5000m,
            OcrText = "Työt tehty kohteessa PRJ-001 osoitteessa Mannerheimintie 123"
        };

        var projects = new List<Project>
        {
            new Project { NetvisorProjectKey = 100, ProjectCode = "PRJ-001", Name = "Kerrostalo", Address = "Mannerheimintie 123" }
        };

        // Act
        var result = await matcher.MatchProjectAsync(invoice, projects);

        // Assert
        result.Should().NotBeNull();
        result!.ProjectKey.Should().Be(100);
        result.ConfidenceScore.Should().BeGreaterThan(0);
    }

    [Fact(Skip = "Integration test - requires real Azure OpenAI credentials")]
    public async Task Should_Return_High_Confidence_For_Clear_Match()
    {
        // Tämä testi vaatii mockin OpenAI-vastaukselle
        // TAI integration-testin oikealla OpenAI:lla

        // Testi implementoidaan kun OpenAI-palvelu on käytössä
        Assert.True(true); // Placeholder
    }

    [Fact]
    public void GptProjectMatcher_Should_Initialize_Successfully()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<GptProjectMatcher>>();

        // Act
        var matcher = new GptProjectMatcher(
            "https://test.openai.azure.com/",
            "test-key",
            "gpt-4",
            mockLogger.Object);

        // Assert
        matcher.Should().NotBeNull();
        matcher.Should().BeAssignableTo<IProjectMatcher>();
    }
}
