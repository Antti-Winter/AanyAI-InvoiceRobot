using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using InvoiceRobot.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace InvoiceRobot.Infrastructure.Services;

public class AzureOcrService : IOcrService
{
    private readonly DocumentAnalysisClient _client;
    private readonly ILogger<AzureOcrService> _logger;

    public AzureOcrService(string endpoint, string apiKey, ILogger<AzureOcrService> logger)
    {
        _client = new DocumentAnalysisClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        _logger = logger;
    }

    public async Task<string> ExtractTextFromPdfAsync(byte[] pdfData)
    {
        try
        {
            _logger.LogInformation("Aloitetaan OCR, PDF-koko: {Size} tavua", pdfData.Length);

            using var stream = new MemoryStream(pdfData);

            // Käytä "prebuilt-read" mallia tekstin erottamiseen
            var operation = await _client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-read",
                stream);

            var result = operation.Value;

            // Yhdistä kaikki sivut yhteen tekstiin
            var extractedText = string.Join("\n\n", result.Pages.Select(page =>
                string.Join("\n", page.Lines.Select(line => line.Content))
            ));

            _logger.LogInformation("OCR valmis, merkkejä: {Length}", extractedText.Length);

            return extractedText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR epäonnistui");
            throw;
        }
    }
}
