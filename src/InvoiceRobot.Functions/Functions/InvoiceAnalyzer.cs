using InvoiceRobot.Core.Domain;
using InvoiceRobot.Core.Interfaces;
using InvoiceRobot.Infrastructure.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvoiceRobot.Functions.Functions;

public class InvoiceAnalyzer
{
    private readonly InvoiceRobotDbContext _context;
    private readonly IOcrService _ocrService;
    private readonly IProjectMatcher _heuristicMatcher;
    private readonly IProjectMatcher _gptMatcher;
    private readonly IAccountingSystemService _accountingService;
    private readonly ILogger<InvoiceAnalyzer> _logger;

    public InvoiceAnalyzer(
        InvoiceRobotDbContext context,
        IOcrService ocrService,
        IProjectMatcher heuristicMatcher,
        IProjectMatcher gptMatcher,
        IAccountingSystemService accountingService,
        ILogger<InvoiceAnalyzer> logger)
    {
        _context = context;
        _ocrService = ocrService;
        _heuristicMatcher = heuristicMatcher;
        _gptMatcher = gptMatcher;
        _accountingService = accountingService;
        _logger = logger;
    }

    [Function("InvoiceAnalyzer")]
    public async Task RunAsync([TimerTrigger("0 */15 * * * *")] TimerInfo timerInfo)
    {
        _logger.LogInformation("InvoiceAnalyzer käynnistyy: {Time}", DateTime.UtcNow);

        try
        {
            // 1. Hae Discovered-tilassa olevat laskut
            var discoveredInvoices = await _context.Invoices
                .Where(i => i.Status == InvoiceStatus.Discovered)
                .ToListAsync();

            if (discoveredInvoices.Count == 0)
            {
                _logger.LogInformation("Ei löytynyt uusia laskuja analysoitavaksi");
                return;
            }

            _logger.LogInformation("Analysoidaan {Count} laskua", discoveredInvoices.Count);

            // Hae aktiiviset projektit
            var activeProjects = await _context.Projects
                .Where(p => p.IsActive)
                .ToListAsync();

            foreach (var invoice in discoveredInvoices)
            {
                await AnalyzeInvoiceAsync(invoice, activeProjects);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("InvoiceAnalyzer valmis");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Virhe InvoiceAnalyzer-funktiossa");
            throw;
        }
    }

    private async Task AnalyzeInvoiceAsync(Invoice invoice, List<Project> activeProjects)
    {
        _logger.LogInformation("Analysoidaan lasku {InvoiceNumber}", invoice.InvoiceNumber);

        try
        {
            // 2. Lataa PDF muistiin ja OCR → extrahoi teksti
            byte[]? pdfData = null;
            try
            {
                pdfData = await _accountingService.DownloadInvoicePdfAsync(invoice.NetvisorInvoiceKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PDF:n lataus epäonnistui laskulle {InvoiceNumber}", invoice.InvoiceNumber);
            }

            if (pdfData == null || pdfData.Length == 0)
            {
                _logger.LogWarning("Lasku {InvoiceNumber} ei sisällä PDF-dataa, ohitetaan", invoice.InvoiceNumber);
                return;
            }

            string ocrText;
            try
            {
                ocrText = await _ocrService.ExtractTextFromPdfAsync(pdfData);
                invoice.OcrText = ocrText;
                invoice.OcrProcessedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCR epäonnistui laskulle {InvoiceNumber}", invoice.InvoiceNumber);
                return;
            }

            // 3. Heuristinen matcher → nopea analyysi
            ProjectMatchResult? matchResult = null;
            string matchMethod = "";

            try
            {
                matchResult = await _heuristicMatcher.MatchProjectAsync(invoice, activeProjects);
                if (matchResult != null)
                {
                    matchMethod = "Heuristic";
                    _logger.LogInformation(
                        "Heuristinen matcher löysi projektin {ProjectKey} (confidence: {Confidence})",
                        matchResult.ProjectKey,
                        matchResult.ConfidenceScore);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Heuristinen matcher epäonnistui");
            }

            // 4. GPT-4 matcher → tarkempi analyysi (jos heuristinen ei löytänyt)
            if (matchResult == null)
            {
                try
                {
                    matchResult = await _gptMatcher.MatchProjectAsync(invoice, activeProjects);
                    if (matchResult != null)
                    {
                        matchMethod = "GPT-4";
                        _logger.LogInformation(
                            "GPT-4 matcher löysi projektin {ProjectKey} (confidence: {Confidence})",
                            matchResult.ProjectKey,
                            matchResult.ConfidenceScore);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GPT-4 matcher epäonnistui");
                }
            }

            if (matchResult == null)
            {
                _logger.LogWarning("Ei löydetty projektia laskulle {InvoiceNumber}", invoice.InvoiceNumber);
                invoice.Status = InvoiceStatus.AnalysisFailed;
                return;
            }

            // Tallenna AI-analyysin tulokset
            invoice.SuggestedProjectKey = matchResult.ProjectKey;
            invoice.AiConfidenceScore = matchResult.ConfidenceScore;
            invoice.AiReasoning = $"[{matchMethod}] {matchResult.Reasoning}";
            invoice.AiAnalyzedAt = DateTime.UtcNow;

            // Hae Project-entity EF Core navigationia varten
            var matchedProject = activeProjects.FirstOrDefault(p => p.NetvisorProjectKey == matchResult.ProjectKey);
            if (matchedProject != null)
            {
                invoice.SuggestedProjectId = matchedProject.Id;
            }

            // 5. Päätös confidence-scoren perusteella
            if (matchResult.ConfidenceScore >= 0.9)
            {
                // Automaattinen päivitys taloushallintoon
                try
                {
                    bool updateSuccess = await _accountingService.UpdateInvoiceProjectAsync(
                        invoice.NetvisorInvoiceKey,
                        matchResult.ProjectKey);

                    if (updateSuccess)
                    {
                        invoice.Status = InvoiceStatus.MatchedAuto;
                        invoice.UpdatedAt = DateTime.UtcNow;

                        _logger.LogInformation(
                            "Lasku {InvoiceNumber} päivitetty automaattisesti projektiin {ProjectKey}",
                            invoice.InvoiceNumber,
                            matchResult.ProjectKey);
                    }
                    else
                    {
                        _logger.LogError(
                            "Taloushallinnon päivitys epäonnistui laskulle {InvoiceNumber}",
                            invoice.InvoiceNumber);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Virhe päivitettäessä taloushallintoon");
                }
            }
            else
            {
                // Luo ApprovalRequest matalalla confidencella
                invoice.Status = InvoiceStatus.PendingApproval;
                invoice.UpdatedAt = DateTime.UtcNow;

                var approvalRequest = new ApprovalRequest
                {
                    InvoiceId = invoice.Id,
                    SuggestedProjectId = matchedProject?.Id,
                    ConfidenceScore = matchResult.ConfidenceScore,
                    Reasoning = matchResult.Reasoning,
                    Status = ApprovalStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ApprovalRequests.Add(approvalRequest);

                _logger.LogInformation(
                    "Luotiin hyväksyntäpyyntö laskulle {InvoiceNumber} (confidence: {Confidence})",
                    invoice.InvoiceNumber,
                    matchResult.ConfidenceScore);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Virhe analysoitaessa laskua {InvoiceNumber}", invoice.InvoiceNumber);
        }
    }
}
