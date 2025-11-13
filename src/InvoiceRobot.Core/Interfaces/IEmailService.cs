namespace InvoiceRobot.Core.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Lähettää hyväksyntäpyynnön PM:lle
    /// </summary>
    Task SendApprovalRequestAsync(string to, string subject, string htmlBody);
}
