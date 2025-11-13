using Azure;
using Azure.AI.OpenAI;
using InvoiceRobot.Core.Domain;
using InvoiceRobot.Core.Interfaces;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.Text.Json;

namespace InvoiceRobot.Infrastructure.Services;

public class GptProjectMatcher : IProjectMatcher
{
    private readonly ChatClient _client;
    private readonly ILogger<GptProjectMatcher> _logger;

    public GptProjectMatcher(
        string endpoint,
        string apiKey,
        string deploymentName,
        ILogger<GptProjectMatcher> logger)
    {
        var azureClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        _client = azureClient.GetChatClient(deploymentName);
        _logger = logger;
    }

    public async Task<ProjectMatchResult?> MatchProjectAsync(Invoice invoice, List<Project> projects)
    {
        try
        {
            _logger.LogInformation("Aloitetaan GPT-4 analyysi laskulle {InvoiceNumber}", invoice.InvoiceNumber);

            var systemPrompt = BuildSystemPrompt();
            var userPrompt = BuildUserPrompt(invoice, projects);

            var options = new ChatCompletionOptions
            {
                Temperature = 0.3f,  // Matala lämpötila → johdonmukaisuus
                MaxOutputTokenCount = 500,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            var response = await _client.CompleteChatAsync(messages, options);
            var content = response.Value.Content[0].Text;

            _logger.LogDebug("GPT-4 vastaus: {Content}", content);

            // Parse JSON-vastaus
            var result = JsonSerializer.Deserialize<GptMatchResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null || result.ProjectKey == null)
            {
                _logger.LogWarning("GPT-4 ei pystynyt tunnistamaan projektia");
                return null;
            }

            var matchResult = new ProjectMatchResult(
                result.ProjectKey.Value,
                result.Confidence,
                result.Reasoning ?? "GPT-4 tunnisti projektin"
            );

            _logger.LogInformation(
                "GPT-4 tunnisti: Projekti {ProjectKey}, Varmuus: {Confidence}",
                matchResult.ProjectKey,
                matchResult.ConfidenceScore);

            return matchResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GPT-4 analyysi epäonnistui");
            throw;
        }
    }

    private string BuildSystemPrompt()
    {
        return @"Olet rakennusalan projektikohdistus-asiantuntija.
Tehtäväsi on tunnistaa mikä projekti liittyy ostolaskuun.

Analysoi laskun sisältö huolellisesti ja etsi:
- Projektiviittauksia (projektinumerot, -koodit)
- Osoitteita
- Rakennuskohteen nimiä
- Muita tunnisteita

Anna vastauksesi JSON-muodossa:
{
  ""projectKey"": 100,
  ""confidence"": 0.95,
  ""reasoning"": ""Selitys miksi tämä projekti valittiin""
}

Jos et pysty tunnistamaan projektia, palauta:
{
  ""projectKey"": null,
  ""confidence"": 0,
  ""reasoning"": ""Ei riittäviä tunnisteita""
}";
    }

    private string BuildUserPrompt(Invoice invoice, List<Project> projects)
    {
        var projectList = string.Join("\n", projects.Select(p =>
            $"- ProjectKey: {p.NetvisorProjectKey}, Koodi: {p.ProjectCode}, Nimi: {p.Name}, Osoite: {p.Address ?? "N/A"}"
        ));

        return $@"Analysoi seuraava ostolasku ja tunnista siihen liittyvä projekti.

Laskun tiedot:
- Numero: {invoice.InvoiceNumber}
- Toimittaja: {invoice.VendorName}
- Summa: {invoice.Amount:N2} €
- Päivämäärä: {invoice.InvoiceDate:yyyy-MM-dd}

OCR-teksti:
{invoice.OcrText}

Käytettävissä olevat projektit:
{projectList}

Anna vastauksesi JSON-muodossa.";
    }

    private class GptMatchResponse
    {
        public int? ProjectKey { get; set; }
        public double Confidence { get; set; }
        public string? Reasoning { get; set; }
    }
}
