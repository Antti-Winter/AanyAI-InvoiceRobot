namespace InvoiceRobot.Core.Domain;

public enum InvoiceStatus
{
    Discovered = 0,                    // Haettu taloushallinnosta
    Analyzing = 10,                    // OCR + AI analyysi käynnissä
    MatchedAuto = 20,                  // AI tunnisti (≥0.9), päivitetty automaattisesti
    PendingApproval = 30,              // Odottaa PM:n hyväksyntää (<0.9)
    Approved = 40,                     // PM hyväksynyt
    UpdatedToAccountingSystem = 50,    // Päivitetty taloushallintoon
    InApprovalCirculation = 60,        // Hyväksymiskierros käynnissä taloushallinnossa
    Completed = 70,                    // Valmis
    Rejected = 80,                     // PM hylännyt
    AnalysisFailed = 90,               // Analyysi epäonnistui
    Error = 100                        // Virhe prosessoinnissa
}
