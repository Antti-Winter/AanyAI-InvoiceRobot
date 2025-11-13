# InvoiceRobot

AI-avusteinen ostolaskujen automaattinen kohdistus projekteihin.

## Tarkoitus

Repositoryn tarkoitus on tuoda lähdekoodi asiakkaalle tarkistettavaksi, mutta ohjelmasta puuttuu olennaisia integraatioita, jotka estävät ohjelman käytön tuotannossa.

InvoiceRobot automatisoi ostolaskujen käsittelyn taloushallintojärjestelmissä (Netvisor/Procountor):

1. **Hakee ostolaskut** automaattisesti taloushallinnosta
2. **Tunnistaa oikean projektin** OCR:n ja GPT-4:n avulla
3. **Päivittää kohdistuksen** automaattisesti korkean varmuuden tapauksissa (≥90%)
4. **Pyytää hyväksynnän** projektipäälliköltä epävarmoissa tapauksissa (<90%)

Järjestelmä vähentää manuaalista työtä ja parantaa laskujen kohdistuksen tarkkuutta.

---

## Teknologiat

### Backend (Azure Functions)
- **.NET 8.0** (Isolated worker)
- **Azure Functions** - Serverless-arkkitehtuuri
- **Entity Framework Core** - Tietokantayhteydet
- **Azure SQL Database** - Tietovarasto

### AI-palvelut
- **Azure OpenAI (GPT-4o)** - Älykkäs projektin tunnistus
- **Azure Document Intelligence** - OCR-palvelu laskujen lukemiseen

### Integraatiot
- **AnyAI.AccountingSystem.Orchestrator** - Taloushallinto-integraatio (Netvisor/Procountor)
- **Azure Communication Services** - Sähköpostiviestit hyväksynnöille

### Admin-työkalu (WPF)
- **.NET 8.0 Windows Desktop**
- **WPF (Windows Presentation Foundation)**
- **Material Design Themes** - Moderni käyttöliittymä
- **MVVM-arkkitehtuuri** (CommunityToolkit.Mvvm)

---

## Arkkitehtuuri

```
┌─────────────────────────────────────────────────────────────┐
│                    Azure Functions                          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────┐   ┌──────────────┐   ┌───────────────┐    │
│  │ Invoice      │   │ Invoice      │   │ Approval      │    │  
│  │ Fetcher      │─▶ │ Analyzer     │─▶│ Handler       │    │
│  │ (Timer)      │   │ (Queue)      │   │ (HTTP)        │    │
│  └──────────────┘   └──────────────┘   └───────────────┘    │
│        │                   │                    │           │
│        ▼                   ▼                    ▼           │
│  ┌──────────────────────────────────────────────────────┐   │
│  │          Azure SQL Database                          │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
         │                  │                    │
         ▼                  ▼                    ▼
┌──────────────┐   ┌──────────────┐   ┌──────────────┐
│ Netvisor/    │   │ Azure OpenAI │   │ Azure Comm   │
│ Procountor   │   │ + Doc Intel  │   │ Services     │
└──────────────┘   └──────────────┘   └──────────────┘
```

---

## Projektirakenne

```
InvoiceRobot/
├── src/
│   ├── InvoiceRobot.Core/           # Domain-mallit ja rajapinnat
│   ├── InvoiceRobot.Infrastructure/ # EF Core, AI-palvelut, integraatiot
│   ├── InvoiceRobot.Functions/      # Azure Functions (InvoiceFetcher, InvoiceAnalyzer, ApprovalHandler)
│   └── InvoiceRobot.Admin/          # WPF Admin-työkalu (Deployment Wizard, konfiguraatio)
├── tests/
│   └── InvoiceRobot.Functions.Tests/
└── README.md
```

---

## Asennus

### Edellytykset

- **.NET 8.0 SDK** tai uudempi
- **Azure-tilaus** deployment-varten
- **Visual Studio 2022** tai **Visual Studio Code** (suositeltu)
- **Windows 10/11** (Admin-työkalua varten)

### 1. Kloonaa repository

```bash
git clone https://github.com/your-org/InvoiceRobot.git
cd InvoiceRobot
```

### 2. TÄRKEÄ: Asenna AnyAI.AccountingSystem.Orchestrator NuGet-paketti

**HUOM!** `AnyAI.AccountingSystem.Orchestrator` NuGet-paketti **ei sisälly projektiin** ja se täytyy asentaa erikseen ennen kääntämistä.

#### Vaihtoehto A: Visual Studio
1. Avaa Solution Visual Studiossa
2. Tools → NuGet Package Manager → Manage NuGet Packages for Solution
3. Etsi: `AnyAI.AccountingSystem.Orchestrator`
4. Asenna paketti projekteihin:
   - `InvoiceRobot.Infrastructure`
   - `InvoiceRobot.Functions`

#### Vaihtoehto B: .NET CLI
```bash
dotnet add src/InvoiceRobot.Infrastructure/InvoiceRobot.Infrastructure.csproj package AnyAI.AccountingSystem.Orchestrator
dotnet add src/InvoiceRobot.Functions/InvoiceRobot.Functions.csproj package AnyAI.AccountingSystem.Orchestrator
```

#### Vaihtoehto C: Manuaalinen lisäys .csproj-tiedostoon
Lisää molempiin projekteihin:
```xml
<ItemGroup>
  <PackageReference Include="AnyAI.AccountingSystem.Orchestrator" Version="X.Y.Z" />
</ItemGroup>
```

### 3. Restore ja Build

```bash
dotnet restore
dotnet build
```

### 4. Suorita testit

```bash
dotnet test
```

---

## Deployment Admin-työkalulla

InvoiceRobot sisältää **WPF-pohjaisen Admin-työkalun**, joka automatisoi täydellisen Azure-deploymentin.

### 1. Käynnistä Admin-työkalu

```bash
cd src/InvoiceRobot.Admin
dotnet run
```

Tai Visual Studiossa: Aseta `InvoiceRobot.Admin` startup-projektiksi ja paina F5.

### 2. Deployment Wizard (5 vaihetta)

#### Vaihe 1: Tervetuloa
- Yleiskatsaus deployment-prosessista

#### Vaihe 2: Azure Subscription
- Kirjaudu Azure-tilillesi
- Valitse subscription

#### Vaihe 3: Configuration
- **Resource Group Name**: esim. `rg-invoicerobot-prod`
- **Location**: esim. `northeurope`
- **Name Prefix**: esim. `invoicerobot`
- **Environment**: `dev`, `test` tai `prod`
- **Accounting Provider**: Valitse `Netvisor` tai `Procountor`
- **SQL Server Password**: Vahva salasana (min. 8 merkkiä)
- **PM Distribution List**: Projektipäälliköiden sähköpostit (pilkulla eroteltu)

#### Vaihe 4: Review
- Tarkista kaikki asetukset
- Näet mitä resursseja luodaan:
  - Storage Account
  - App Service Plan (Consumption)
  - Function App (.NET 8.0 Isolated)
  - SQL Server + Database
  - Application Insights
  - Azure OpenAI (Sweden Central) + GPT-4o deployment
  - Document Intelligence (Form Recognizer)
  - Communication Services (Email)

#### Vaihe 5: Deployment
- Wizard luo **automaattisesti kaikki resurssit**
- Konfiguroi Function App-asetukset
- Näet edistymisen reaaliajassa
- Deployment kestää noin 10-15 minuuttia

### 3. Deployment onnistui!

Admin-työkalu näyttää:
- Function App URL
- SQL Server FQDN
- Kaikki tarvittavat yhteystiedot

Järjestelmä on nyt valmis tuotantokäyttöön!

---

## Manuaalinen Deployment (Bicep)

Jos et halua käyttää Admin-työkalua, voit käyttää suoraan Bicep-templateja:

```bash
# 1. Kirjaudu Azure:en
az login

# 2. Luo Resource Group
az group create --name rg-invoicerobot-prod --location northeurope

# 3. Deploy Bicep template
az deployment group create \
  --resource-group rg-invoicerobot-prod \
  --template-file src/InvoiceRobot.Admin/Bicep/main.bicep \
  --parameters \
    namePrefix=invoicerobot \
    environment=prod \
    accountingProvider=Netvisor \
    sqlServerPassword='YourStrongPassword!' \
    emailPmDistributionList='pm1@example.com,pm2@example.com'
```

---

## Konfiguraatio

### Azure Function App Settings

Deployment luo automaattisesti seuraavat asetukset Function App:iin:

```
AzureWebJobsStorage              # Storage Account connection string
SqlConnectionString              # SQL Database connection string
AccountingProvider               # Netvisor tai Procountor
NetvisorCustomerId               # Netvisor Customer ID (konfiguroitava)
NetvisorCustomerKey              # Netvisor API Key (konfiguroitava)
NetvisorOrganizationId           # Netvisor Partner ID (konfiguroitava)
AzureOpenAIEndpoint              # Azure OpenAI endpoint (automaattinen)
AzureOpenAIApiKey                # Azure OpenAI API key (automaattinen)
AzureOpenAIDeploymentName        # gpt-4o (automaattinen)
DocumentIntelligenceEndpoint     # Document Intelligence endpoint (automaattinen)
DocumentIntelligenceApiKey       # Document Intelligence key (automaattinen)
CommunicationServicesConnectionString  # Email service (automaattinen)
EmailSenderAddress               # Lähettäjän sähköpostiosoite (konfiguroitava)
EmailPmDistributionList          # PM-lista (automaattinen)
```

### Post-Deployment konfiguraatio

Admin-työkalun **Configuration**-näkymässä voit konfiguroida:
1. **Netvisor/Procountor API-avaimet**
2. **Sähköpostin lähettäjäosoite**
3. **GPT-4 promptit** (heuristinen + AI matcher)

---

## Käyttö

### 1. Automaattinen suoritus

**InvoiceFetcher** suoritetaan automaattisesti **päivittäin klo 09:00** (Azure Timer Trigger).

Voit muuttaa aikataulua Function App:n `host.json`-tiedostossa:
```json
{
  "schedule": "0 0 9 * * *"  // Cron: päivittäin klo 09:00
}
```

### 2. Manuaalinen käynnistys

Admin-työkalun **Invoices**-näkymässä voit:
- Tarkastella laskujen tilaa
- Nähdä AI:n varmuusprosentit
- Tarkistaa kohdistukset
- Käynnistää prosessin manuaalisesti

### 3. Hyväksyntäprosessi

Kun lasku vaatii hyväksynnän (AI confidence < 90%):
1. Projektipäällikkö saa sähköpostin
2. Sähköposti sisältää laskun tiedot ja ehdotetun projektin
3. PM klikkaa "Hyväksy" tai "Hylkää" -linkkiä
4. Järjestelmä päivittää laskun kohdistuksen automaattisesti

---

## Admin-työkalu

### Ominaisuudet

1. **Deployment Wizard**
   - Täysin automaattinen Azure-deployment
   - 5-vaiheinen wizard
   - Ei vaadi Bicep-osaamista

2. **Configuration**
   - Hallitse Function App -asetuksia
   - Päivitä API-avaimet
   - Muokkaa GPT-4 prompteja

3. **Invoices**
   - Näytä kaikki laskut
   - Suodata tilan mukaan (Pending, Approved, Processed)
   - Tarkista AI:n varmuusprosentit

4. **Logs**
   - Application Insights -lokit
   - Reaaliaikainen seuranta
   - Virheanalyysi

---

## Kehitys

### Lokaalinen kehitysympäristö

```bash
# 1. Kopioi local.settings.json template
cp src/InvoiceRobot.Functions/local.settings.example.json src/InvoiceRobot.Functions/local.settings.json

# 2. Täytä asetukset
# - Azure OpenAI endpoint ja API key
# - Document Intelligence endpoint ja API key
# - SQL Connection String
# - Netvisor/Procountor API-avaimet

# 3. Käynnistä Functions lokaalisti
cd src/InvoiceRobot.Functions
func start
```

### Testien ajaminen

```bash
# Kaikki testit
dotnet test

# Vain yksikkötestit
dotnet test --filter Category=Unit

# Vain integraatiotestit
dotnet test --filter Category=Integration
```



## Lisenssi

Proprietary - Kaikki oikeudet pidätetään.

---

## Yhteystiedot

**Projekti**: InvoiceRobot
**Kehittäjä**: Winter IT Oy / Antti Winter
**Sähköposti**: [antti@winterit.fi]

---

**Päivitetty**: 2025-11-13
**Versio**: 1.0
