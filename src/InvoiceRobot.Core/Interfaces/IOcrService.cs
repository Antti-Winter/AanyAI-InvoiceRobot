namespace InvoiceRobot.Core.Interfaces;

public interface IOcrService
{
    /// <summary>
    /// Suorittaa OCR:n PDF-dokumentille
    /// </summary>
    Task<string> ExtractTextFromPdfAsync(byte[] pdfData);
}
