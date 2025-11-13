using FluentAssertions;
using InvoiceRobot.Core.Domain;
using InvoiceRobot.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace InvoiceRobot.Infrastructure.Tests.Services;

public class HeuristicProjectMatcherTests
{
    private readonly HeuristicProjectMatcher _matcher;
    private readonly Mock<ILogger<HeuristicProjectMatcher>> _mockLogger;

    public HeuristicProjectMatcherTests()
    {
        _mockLogger = new Mock<ILogger<HeuristicProjectMatcher>>();
        _matcher = new HeuristicProjectMatcher(_mockLogger.Object);
    }

    [Fact]
    public async Task Should_Match_By_ProjectCode_In_OcrText()
    {
        // Arrange
        var invoice = new Invoice
        {
            InvoiceNumber = "INV-001",
            VendorName = "Rakennusliike Oy",
            OcrText = "Työ tehty kohteessa PRJ-001 osoitteessa Mannerheimintie 123"
        };

        var projects = new List<Project>
        {
            new Project { Id = 1, NetvisorProjectKey = 100, ProjectCode = "PRJ-001", Name = "Kerrostalo", IsActive = true },
            new Project { Id = 2, NetvisorProjectKey = 200, ProjectCode = "PRJ-002", Name = "Rivitalo", IsActive = true }
        };

        // Act
        var result = await _matcher.MatchProjectAsync(invoice, projects);

        // Assert
        result.Should().NotBeNull();
        result!.ProjectKey.Should().Be(100);
        result.ConfidenceScore.Should().BeGreaterThanOrEqualTo(0.8);
        result.Reasoning.Should().Contain("PRJ-001");
    }

    [Fact]
    public async Task Should_Match_By_Address()
    {
        // Arrange
        var invoice = new Invoice
        {
            InvoiceNumber = "INV-002",
            VendorName = "Sähköasennus Oy",
            OcrText = "Asennustyö osoitteessa Mannerheimintie 123, Helsinki"
        };

        var projects = new List<Project>
        {
            new Project { Id = 1, NetvisorProjectKey = 100, ProjectCode = "PRJ-001", Name = "Kerrostalo", Address = "Mannerheimintie 123, Helsinki", IsActive = true },
            new Project { Id = 2, NetvisorProjectKey = 200, ProjectCode = "PRJ-002", Name = "Rivitalo", Address = "Kalevankatu 45, Tampere", IsActive = true }
        };

        // Act
        var result = await _matcher.MatchProjectAsync(invoice, projects);

        // Assert
        result.Should().NotBeNull();
        result!.ProjectKey.Should().Be(100);
        result.Reasoning.Should().Contain("Mannerheimintie 123");
    }

    [Fact]
    public async Task Should_Return_Null_When_No_Match()
    {
        // Arrange
        var invoice = new Invoice
        {
            InvoiceNumber = "INV-003",
            VendorName = "Toimittaja Oy",
            OcrText = "Ei mitään projektiin liittyvää"
        };

        var projects = new List<Project>
        {
            new Project { Id = 1, NetvisorProjectKey = 100, ProjectCode = "PRJ-001", Name = "Kerrostalo", IsActive = true }
        };

        // Act
        var result = await _matcher.MatchProjectAsync(invoice, projects);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("PRJ-001", 1.0)]  // Täsmällinen match
    [InlineData("Mannerheimintie", 0.7)]  // Osoite
    [InlineData("kerrostalo", 0.5)]  // Projektin nimi
    public async Task Should_Calculate_Correct_Confidence_Score(string searchTerm, double expectedMinScore)
    {
        // Arrange
        var invoice = new Invoice
        {
            InvoiceNumber = "INV-004",
            VendorName = "Test Oy",
            OcrText = $"Työ liittyen kohteeseen {searchTerm}"
        };

        var projects = new List<Project>
        {
            new Project
            {
                Id = 1,
                NetvisorProjectKey = 100,
                ProjectCode = "PRJ-001",
                Name = "Kerrostalo Mannerheimintie",
                Address = "Mannerheimintie 123",
                IsActive = true
            }
        };

        // Act
        var result = await _matcher.MatchProjectAsync(invoice, projects);

        // Assert
        result.Should().NotBeNull();
        result!.ConfidenceScore.Should().BeGreaterThanOrEqualTo(expectedMinScore);
    }
}
