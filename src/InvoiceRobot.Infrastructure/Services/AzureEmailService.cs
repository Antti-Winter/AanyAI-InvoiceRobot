using Azure;
using Azure.Communication.Email;
using InvoiceRobot.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace InvoiceRobot.Infrastructure.Services;

public class AzureEmailService : IEmailService
{
    private readonly EmailClient _client;
    private readonly string _senderAddress;
    private readonly ILogger<AzureEmailService> _logger;

    public AzureEmailService(
        string connectionString,
        string senderAddress,
        ILogger<AzureEmailService> logger)
    {
        _client = new EmailClient(connectionString);
        _senderAddress = senderAddress;
        _logger = logger;
    }

    public async Task SendApprovalRequestAsync(string to, string subject, string htmlBody)
    {
        try
        {
            _logger.LogInformation("Lähetetään sähköposti: {To}, {Subject}", to, subject);

            var emailMessage = new EmailMessage(
                senderAddress: _senderAddress,
                recipientAddress: to,
                content: new EmailContent(subject)
                {
                    Html = htmlBody
                });

            var operation = await _client.SendAsync(
                WaitUntil.Started,
                emailMessage);

            _logger.LogInformation("Sähköposti lähetetty, Message ID: {MessageId}", operation.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sähköpostin lähetys epäonnistui");
            throw;
        }
    }
}
