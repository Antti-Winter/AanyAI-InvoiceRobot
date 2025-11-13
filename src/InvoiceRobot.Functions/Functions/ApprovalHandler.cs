using InvoiceRobot.Core.Domain;
using InvoiceRobot.Core.Interfaces;
using InvoiceRobot.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvoiceRobot.Functions.Functions;

public class ApprovalHandler
{
    private readonly InvoiceRobotDbContext _context;
    private readonly IAccountingSystemService _accountingService;
    private readonly ILogger<ApprovalHandler> _logger;

    public ApprovalHandler(
        InvoiceRobotDbContext context,
        IAccountingSystemService accountingService,
        ILogger<ApprovalHandler> logger)
    {
        _context = context;
        _accountingService = accountingService;
        _logger = logger;
    }

    [Function("ApprovalHandler_Get")]
    public async Task<IActionResult> GetApprovalFormAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "approval")] HttpRequest req)
    {
        var token = req.Query["token"].ToString();

        if (string.IsNullOrEmpty(token))
        {
            return new BadRequestObjectResult("Token puuttuu");
        }

        _logger.LogInformation("Haetaan hyväksyntälomake tokenilla: {Token}", token);

        var approvalRequest = await _context.ApprovalRequests
            .Include(ar => ar.Invoice)
                .ThenInclude(i => i.SuggestedProject)
            .FirstOrDefaultAsync(ar => ar.Token == token);

        if (approvalRequest == null)
        {
            return new NotFoundObjectResult("Hyväksyntäpyyntöä ei löydy");
        }

        if (approvalRequest.Status != ApprovalStatus.Pending)
        {
            return new ContentResult
            {
                Content = "<html><body><h1>Tämä pyyntö on jo käsitelty</h1></body></html>",
                ContentType = "text/html",
                StatusCode = 200
            };
        }

        // Hae kaikki projektit
        var projects = await _context.Projects
            .Where(p => p.IsActive)
            .OrderBy(p => p.ProjectCode)
            .ToListAsync();

        var html = BuildApprovalForm(approvalRequest, projects);

        return new ContentResult
        {
            Content = html,
            ContentType = "text/html",
            StatusCode = 200
        };
    }

    [Function("ApprovalHandler_Post")]
    public async Task<IActionResult> PostApprovalAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "approval")] HttpRequest req)
    {
        var form = await req.ReadFormAsync();
        var token = form["token"].ToString();
        var action = form["action"].ToString();  // "approve" tai "reject"
        var projectKey = form["projectKey"].ToString();
        var rejectionReason = form["rejectionReason"].ToString();

        _logger.LogInformation("Käsitellään hyväksyntä: Token {Token}, Action {Action}", token, action);

        var approvalRequest = await _context.ApprovalRequests
            .Include(ar => ar.Invoice)
            .FirstOrDefaultAsync(ar => ar.Token == token);

        if (approvalRequest == null)
        {
            return new NotFoundObjectResult("Hyväksyntäpyyntöä ei löydy");
        }

        if (approvalRequest.Status != ApprovalStatus.Pending)
        {
            return new BadRequestObjectResult("Pyyntö on jo käsitelty");
        }

        if (action == "approve")
        {
            var selectedProjectKey = int.Parse(projectKey);

            // Päivitä taloushallintojärjestelmään
            var success = await _accountingService.UpdateInvoiceProjectAsync(
                approvalRequest.Invoice.NetvisorInvoiceKey,
                selectedProjectKey);

            if (!success)
            {
                _logger.LogError("Projektikohdistuksen päivitys epäonnistui");
                return new ObjectResult("Päivitys taloushallintoon epäonnistui") { StatusCode = 500 };
            }

            // Päivitä tietokanta
            approvalRequest.Status = ApprovalStatus.Approved;
            approvalRequest.ApprovedProjectKey = selectedProjectKey;
            approvalRequest.RespondedAt = DateTime.UtcNow;
            approvalRequest.UpdatedAt = DateTime.UtcNow;

            approvalRequest.Invoice.FinalProjectKey = selectedProjectKey;
            approvalRequest.Invoice.Status = InvoiceStatus.Approved;
            approvalRequest.Invoice.UpdatedToAccountingSystemAt = DateTime.UtcNow;
            approvalRequest.Invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Lasku {InvoiceNumber} hyväksytty projektille {ProjectKey}",
                approvalRequest.Invoice.InvoiceNumber,
                selectedProjectKey);

            return new ContentResult
            {
                Content = "<html><body><h1>Lasku hyväksytty!</h1><p>Projektikohdistus on päivitetty taloushallintoon.</p></body></html>",
                ContentType = "text/html",
                StatusCode = 200
            };
        }
        else if (action == "reject")
        {
            approvalRequest.Status = ApprovalStatus.Rejected;
            approvalRequest.RejectionReason = rejectionReason;
            approvalRequest.RespondedAt = DateTime.UtcNow;
            approvalRequest.UpdatedAt = DateTime.UtcNow;

            approvalRequest.Invoice.Status = InvoiceStatus.Rejected;
            approvalRequest.Invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Lasku {InvoiceNumber} hylätty", approvalRequest.Invoice.InvoiceNumber);

            return new ContentResult
            {
                Content = "<html><body><h1>Lasku hylätty</h1></body></html>",
                ContentType = "text/html",
                StatusCode = 200
            };
        }

        return new BadRequestObjectResult("Virheellinen action");
    }

    // Helper method for testing - public so tests can call it directly
    public async Task<IActionResult> PostApprovalAsync(string token, int projectKey, bool approved, string? rejectionReason)
    {
        _logger.LogInformation("Käsitellään hyväksyntä: Token {Token}, Approved {Approved}", token, approved);

        var approvalRequest = await _context.ApprovalRequests
            .Include(ar => ar.Invoice)
            .FirstOrDefaultAsync(ar => ar.Token == token);

        if (approvalRequest == null)
        {
            return new NotFoundObjectResult("Hyväksyntäpyyntöä ei löydy");
        }

        if (approvalRequest.Status != ApprovalStatus.Pending)
        {
            return new BadRequestObjectResult("Pyyntö on jo käsitelty");
        }

        if (approved)
        {
            // Päivitä taloushallintojärjestelmään
            var success = await _accountingService.UpdateInvoiceProjectAsync(
                approvalRequest.Invoice.NetvisorInvoiceKey,
                projectKey);

            if (!success)
            {
                _logger.LogError("Projektikohdistuksen päivitys epäonnistui");
                return new ObjectResult("Päivitys taloushallintoon epäonnistui") { StatusCode = 500 };
            }

            // Päivitä tietokanta
            approvalRequest.Status = ApprovalStatus.Approved;
            approvalRequest.ApprovedProjectKey = projectKey;
            approvalRequest.RespondedAt = DateTime.UtcNow;
            approvalRequest.UpdatedAt = DateTime.UtcNow;

            approvalRequest.Invoice.FinalProjectKey = projectKey;
            approvalRequest.Invoice.Status = InvoiceStatus.Approved;
            approvalRequest.Invoice.UpdatedToAccountingSystemAt = DateTime.UtcNow;
            approvalRequest.Invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Lasku {InvoiceNumber} hyväksytty projektille {ProjectKey}",
                approvalRequest.Invoice.InvoiceNumber,
                projectKey);

            return new ContentResult
            {
                Content = "<html><body><h1>Lasku hyväksytty!</h1><p>Projektikohdistus on päivitetty taloushallintoon.</p></body></html>",
                ContentType = "text/html",
                StatusCode = 200
            };
        }
        else
        {
            approvalRequest.Status = ApprovalStatus.Rejected;
            approvalRequest.RejectionReason = rejectionReason;
            approvalRequest.RespondedAt = DateTime.UtcNow;
            approvalRequest.UpdatedAt = DateTime.UtcNow;

            approvalRequest.Invoice.Status = InvoiceStatus.Rejected;
            approvalRequest.Invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Lasku {InvoiceNumber} hylätty", approvalRequest.Invoice.InvoiceNumber);

            return new ContentResult
            {
                Content = "<html><body><h1>Lasku hylätty</h1></body></html>",
                ContentType = "text/html",
                StatusCode = 200
            };
        }
    }

    private string BuildApprovalForm(ApprovalRequest approvalRequest, List<Project> projects)
    {
        var invoice = approvalRequest.Invoice;
        var suggestedProject = invoice.SuggestedProject;

        var projectOptions = string.Join("\n", projects.Select(p =>
            $"<option value=\"{p.NetvisorProjectKey}\" {(p.NetvisorProjectKey == invoice.SuggestedProjectKey ? "selected" : "")}>" +
            $"{p.ProjectCode} - {p.Name}</option>"
        ));

        return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Laskun hyväksyntä</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; }}
        .container {{ max-width: 800px; margin: 0 auto; }}
        .invoice-info {{ background: #f5f5f5; padding: 20px; margin: 20px 0; }}
        .ai-suggestion {{ background: #e3f2fd; padding: 15px; margin: 20px 0; border-left: 4px solid #2196F3; }}
        .form-group {{ margin: 15px 0; }}
        label {{ display: block; font-weight: bold; margin-bottom: 5px; }}
        select, textarea {{ width: 100%; padding: 8px; font-size: 14px; }}
        button {{ padding: 10px 20px; font-size: 16px; margin: 10px 5px; cursor: pointer; }}
        .btn-approve {{ background: #4CAF50; color: white; border: none; }}
        .btn-reject {{ background: #f44336; color: white; border: none; }}
    </style>
</head>
<body>
    <div class=""container"">
        <h1>Laskun projektikohdistuksen hyväksyntä</h1>

        <div class=""invoice-info"">
            <h2>Laskun tiedot</h2>
            <p><strong>Numero:</strong> {invoice.InvoiceNumber}</p>
            <p><strong>Toimittaja:</strong> {invoice.VendorName}</p>
            <p><strong>Summa:</strong> {invoice.Amount:N2} €</p>
            <p><strong>Päivämäärä:</strong> {invoice.InvoiceDate:yyyy-MM-dd}</p>
        </div>

        <div class=""ai-suggestion"">
            <h3>AI:n ehdotus</h3>
            <p><strong>Projekti:</strong> {suggestedProject?.ProjectCode} - {suggestedProject?.Name}</p>
            <p><strong>Varmuus:</strong> {invoice.AiConfidenceScore:P0}</p>
            <p><strong>Perustelu:</strong> {invoice.AiReasoning}</p>
        </div>

        <form method=""POST"" action=""/api/approval"">
            <input type=""hidden"" name=""token"" value=""{approvalRequest.Token}"" />

            <div class=""form-group"">
                <label for=""projectKey"">Valitse projekti:</label>
                <select id=""projectKey"" name=""projectKey"" required>
                    {projectOptions}
                </select>
            </div>

            <div class=""form-group"">
                <label for=""rejectionReason"">Hylkäyksen syy (jos hylkäät):</label>
                <textarea id=""rejectionReason"" name=""rejectionReason"" rows=""3""></textarea>
            </div>

            <button type=""submit"" name=""action"" value=""approve"" class=""btn-approve"">Hyväksy</button>
            <button type=""submit"" name=""action"" value=""reject"" class=""btn-reject"">Hylkää</button>
        </form>
    </div>
</body>
</html>";
    }
}
