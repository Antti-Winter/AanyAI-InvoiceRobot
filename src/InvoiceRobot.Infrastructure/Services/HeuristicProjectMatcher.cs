using InvoiceRobot.Core.Domain;
using InvoiceRobot.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace InvoiceRobot.Infrastructure.Services;

public class HeuristicProjectMatcher : IProjectMatcher
{
    private readonly ILogger<HeuristicProjectMatcher> _logger;

    public HeuristicProjectMatcher(ILogger<HeuristicProjectMatcher> logger)
    {
        _logger = logger;
    }

    public Task<ProjectMatchResult?> MatchProjectAsync(Invoice invoice, List<Project> projects)
    {
        if (string.IsNullOrEmpty(invoice.OcrText))
        {
            _logger.LogWarning("OCR-teksti puuttuu laskulta {InvoiceNumber}", invoice.InvoiceNumber);
            return Task.FromResult<ProjectMatchResult?>(null);
        }

        var ocrText = invoice.OcrText.ToLower();
        var scores = new Dictionary<int, (double Score, List<string> Matches)>();

        foreach (var project in projects.Where(p => p.IsActive))
        {
            var matchReasons = new List<string>();
            double score = 0;

            // 1. Projektikoodin täsmällinen match (korkein prioriteetti)
            if (ContainsWord(ocrText, project.ProjectCode.ToLower()))
            {
                score += 1.0;
                matchReasons.Add($"Projektikoodi '{project.ProjectCode}' löytyy tekstistä");
            }

            // 2. Osoite (keskitaso)
            if (!string.IsNullOrEmpty(project.Address))
            {
                var addressSegments = project.Address.Split(',', ';').Select(p => p.Trim());
                bool addressMatched = false;

                foreach (var segment in addressSegments)
                {
                    // Tarkista täysi segmentti
                    if (segment.Length > 5 && ocrText.Contains(segment.ToLower()))
                    {
                        score += 0.7;
                        matchReasons.Add($"Osoite '{segment}' löytyy tekstistä");
                        addressMatched = true;
                        break;
                    }

                    // Jos ei täsmää, tarkista yksittäiset merkittävät sanat
                    if (!addressMatched)
                    {
                        var words = segment.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Where(w => w.Length > 5); // Vain merkittävät sanat

                        foreach (var word in words)
                        {
                            if (ContainsWord(ocrText, word.ToLower()))
                            {
                                score += 0.7;
                                matchReasons.Add($"Osoite '{segment}' löytyy tekstistä");
                                addressMatched = true;
                                break;
                            }
                        }
                    }

                    if (addressMatched) break;
                }
            }

            // 3. Projektin nimi (keskitaso)
            var nameParts = project.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(p => p.Length > 4);
            foreach (var part in nameParts)
            {
                if (ContainsWord(ocrText, part.ToLower()))
                {
                    score += 0.5;
                    matchReasons.Add($"Projektin nimen osa '{part}' löytyy tekstistä");
                }
            }

            if (score > 0)
            {
                scores[project.NetvisorProjectKey] = (score, matchReasons);
            }
        }

        if (scores.Count == 0)
        {
            _logger.LogInformation("Ei heuristisia osumia laskulle {InvoiceNumber}", invoice.InvoiceNumber);
            return Task.FromResult<ProjectMatchResult?>(null);
        }

        // Valitse korkein score
        var best = scores.OrderByDescending(s => s.Value.Score).First();
        var normalizedScore = Math.Min(best.Value.Score, 1.0); // Cap 1.0

        var result = new ProjectMatchResult(
            best.Key,
            normalizedScore,
            string.Join("; ", best.Value.Matches)
        );

        _logger.LogInformation(
            "Heuristinen osuma: Lasku {InvoiceNumber} → Projekti {ProjectKey}, Score: {Score}",
            invoice.InvoiceNumber,
            result.ProjectKey,
            result.ConfidenceScore);

        return Task.FromResult<ProjectMatchResult?>(result);
    }

    private bool ContainsWord(string text, string word)
    {
        // Tarkista että sana on kokonaan (ei osa toista sanaa)
        var pattern = $@"\b{Regex.Escape(word)}\b";
        return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase);
    }
}
