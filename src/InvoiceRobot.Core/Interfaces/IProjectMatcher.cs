using InvoiceRobot.Core.Domain;

namespace InvoiceRobot.Core.Interfaces;

public interface IProjectMatcher
{
    /// <summary>
    /// Tunnistaa laskun projektiin
    /// </summary>
    Task<ProjectMatchResult?> MatchProjectAsync(Invoice invoice, List<Project> projects);
}

public record ProjectMatchResult(
    int ProjectKey,
    double ConfidenceScore,
    string Reasoning
);
