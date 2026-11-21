# Bank Connections - Developer Quick Start

## Prerequisites
- .NET 11.0 SDK
- Visual Studio 2025 or VS Code with C# extension

## Running the Application

### Option 1: Using .NET Aspire (Recommended)
```bash
cd src/Privatekonomi.AppHost
dotnet run
```
This starts both the API and Web projects with service discovery. Web still
uses its own database and Identity, not the personal API;
discovery does not share data.

### Option 2: Run Projects Separately

**Terminal 1 - Start API (HTTPS profile required for personal login):**
```bash
cd src/Privatekonomi.Api
dotnet run --launch-profile https
```

**Terminal 2 - Start Web:**
```bash
cd src/Privatekonomi.Web
dotnet run
```

## Accessing the Application

### Demoanvändare och beständiga konton

Webbprojektets `appsettings.Development.json` konfigurerar `DemoUser:Enabled`,
`DemoUser:Email` och `DemoUser:Password`. Uppgifterna är offentliga utvecklingsuppgifter
och får aldrig återanvändas för riktiga konton. Demoanvändaren skapas endast när
miljön är **Development**, utan administratörsbehörighet. `Storage:SeedTestData`
lägger till exempeldata när ett nytt demokonto skapas. Befintliga konton ändras
aldrig av initieringen, inte heller om demolösenordet ändras i konfigurationen.

Development använder SQLite (`privatekonomi-development.db`) så att konton och
exempeldata finns kvar efter omstart. InMemory är fortfarande tillgängligt för
isolerade tester men bevarar inte konton. Använd separata datalager för utveckling
och produktion; kopiera inte utvecklingsdatabasen till produktion.

Vid JSON-lagring sparas konton, lösenordshashar och övriga Identity-data omedelbart
i `identity.json` i `Storage:ConnectionString`-katalogen, separat från ekonomidatans
periodiska sparning. Skydda och säkerhetskopiera hela datakatalogen. Redan förlorade
konton från äldre JSON-versioner kan inte återskapas automatiskt. JSON-lagring är
avsedd för en enda skrivande process per katalog. Web och API får inte dela
JSON-katalog: processernas minnesbilder kan skriva över varandra. Behåll
datakatalog/databas på en beständig sökväg utanför publicerade
programfiler vid releaser. Säkerhetskopiera före schemaändringar och radera inte
databasen eller migrationshistoriken för att uppgradera.

Initieringsfel stoppar uppstarten i stället för att fortsätta med ett tomt eller
ofullständigt datalager. Om kontodata inte kan skrivas till JSON stoppas värden
och fler ändringar blockeras; den senaste fullständiga kontofilen behålls.
Åtgärda lagringsfelet innan appen startas igen.

- **Web UI**: http://localhost:5274 (http profile) or https://localhost:7117 (https profile)
- **API**: https://localhost:7023 (https profile); see console output for Aspire
- **Bank Connections Page**: http://localhost:5274/bank-connections

API-inloggning (`POST /api/session/login` med `email` och `password`) kräver
HTTPS och ett konto i API:s eget Identity-lager. API saknar registrering och
tvåfaktorsflöde; konton med tvåfaktor avvisas. Den utfärdade bearer-token används
för `GET /api/personal/transactions` och `/api/personal/transactions/{id}`.
Webs konto eller cookie ger inte automatiskt åtkomst till API. HTTP-profilerna
och Aspires standard-HTTP-endpoint räcker inte för dessa anrop. API måste
uppfatta begäran som HTTPS (`Request.IsHttps`); TLS-terminering i en proxy
räcker inte i sig om API ser anslutningen som HTTP.

**Alfa och produktionsdata:** Äldre API-controllers är spärrade som standard
och returnerar normalt 403, även med en personlig token. Tidigare versioner
utan spärr kan tillåta anonym läsning av exempelvis `GET /api/transactions`
utan användarfiltrering. Kontrollera den driftsatta versionen och isolera eller
uppdatera äldre installationer; HTTPS och personal API:s autentisering skyddar
inte automatiskt äldre rutter. Spärren påverkar även klienter och bank-OAuth.
Se [säkerhetsguiden](LEGACY_API_SECURITY.md).

`GET /api-info` är en publik endpoint som inte påverkas av spärren. Den visar
`version`, `buildNumber`, `sha`, `environment` och `storageProvider` i JSON.
Värden kan vara `null` om metadata saknas eller inte godkänns; SHA förkortas
till högst åtta tecken. Informationen identifierar ett bygge och dess
konfigurerade provider, inte en garanti för säkerhet eller dataisolering.

API har ett eget demo och egen seeding. Både bas- och Development-konfigurationen
använder InMemory och `Storage:SeedTestData=false`; `DemoUser` är inte aktiverat
som standard. Exempel för en isolerad Development-process med offentliga
testuppgifter:

```bash
DemoUser__Enabled=true DemoUser__Email=test@example.com \
DemoUser__Password='TestPassword123!' Storage__Provider=InMemory \
Storage__SeedTestData=true \
  dotnet run --project src/Privatekonomi.Api --launch-profile https
```

API skapar kontot bara i Development och bara om det inte redan finns.
Exempeldata kräver dessutom att `SeedTestData=true` och databasen är en ny
InMemory-databas. InMemory förlorar kontot och datan vid omstart; Webs konto och
data delas inte. Med demo och exempeldata aktiverade avvisas SQLite och JSON vid
uppstart. Ett demokonto utan exempeldata kan skapas där, men Development-demo
blockeras helt på SQL Server och MySQL/MariaDB; API hoppar även över
referensdata på dessa serverdatabaser på grund av samtidig initiering.
Lokala överstyrningar eller Aspire-parametrar kan påverka resultatet; kontrollera
effektiv konfiguration. Testuppgifterna får inte användas i produktion.

## Testing the Bank Connections Feature

The steps below describe the bank connections UI, but its API-backed operations
are blocked when routed to the API service. Bank OAuth callback is also blocked,
so authorization cannot complete. See [legacy API security](LEGACY_API_SECURITY.md);
do not expect this walkthrough to complete successfully until the flow is secured.

### 1. View Bank Connections
Navigate to `/bank-connections` to see the bank connections management page.

### 2. Add a New Connection
- Click "Lägg till bank" button
- Select a bank from the dropdown
- Optionally enter an account ID
- Toggle auto-sync if desired
- Click "Anslut"

**Note**: Since the bank API services are mock implementations, the OAuth flow will not complete in development. This is expected behavior.

### 3. Edit a Connection
- Click the edit icon (pencil) on any connection
- Modify the auto-sync setting or status
- Click "Spara"

### 4. Sync Transactions
- Click the sync icon on any connection
- The system will attempt to fetch accounts and import transactions

### 5. Delete a Connection
- Click the delete icon (trash can)
- Confirm the deletion in the dialog

## Database

Webbprojektet använder SQLite i Development. API använder InMemory som standard
och har ett separat datalager. Samma provider eller databasnamn delar inte
InMemory mellan processer. Använd separata databaser och användaruppgifter för
Web och API samt för mock, test och produktion.

### Using a Real Database

För SQL Server, konfigurera `Storage` separat för varje projekt, exempelvis
följande för Web och en annan databas för API:

```json
{
  "Storage": {
    "Provider": "SqlServer",
    "ConnectionString": "Server=localhost;Database=PrivatekonomyDb;Integrated Security=true;TrustServerCertificate=true",
    "SeedTestData": false
  }
}
```

Lagringsleverantören registreras redan i `Program.cs`; inga kodändringar behövs.
Uppstarten skapar nya databaser och lägger till saknade kontoprofilkolumner och
passkey-tabellen i äldre databaser utan att återskapa konton. Befintliga
lösenordshashar, säkerhetsstämplar och behörigheter behålls. Dessa additiva
uppdateringar kan köras flera gånger, även på databaser utan migrationshistorik.
Kör inte en ny `InitialCreate` eller hela migrationskedjan på en befintlig databas
skapad med `EnsureCreated`; schemaändringar måste vara kompatibla med dess
befintliga schema. Se även [lagringsguiden](STORAGE_GUIDE.md).

## Debugging

### Visual Studio
1. Set `Privatekonomi.AppHost` as the startup project
2. Press F5 to start debugging

### VS Code
1. Open the workspace
2. Use the "Launch Aspire" debug configuration
3. Or run API and Web projects separately

## Troubleshooting

### Port Already in Use
If you get a port conflict error, change the port in `Properties/launchSettings.json`

### Aspire AppHost Issues
If Aspire fails to start (DCP errors), run the projects directly:
```bash
# Terminal 1
dotnet run --project src/Privatekonomi.Api

# Terminal 2  
dotnet run --project src/Privatekonomi.Web
```

### CORS Errors
For browser-based API clients, check the API's CORS policy in `Program.cs`.
Web does not call the personal API.

### Null Reference Warnings
The project has nullable reference types enabled. Some warnings are expected and don't prevent the application from running.

## Code Structure

```
src/
├── Privatekonomi.Core/
│   ├── Models/
│   │   ├── BankConnection.cs       # Connection entity
│   │   └── AuditLog.cs             # Audit log entity
│   ├── Services/
│   │   ├── IBankConnectionService.cs
│   │   ├── BankConnectionService.cs
│   │   ├── IAuditLogService.cs
│   │   └── AuditLogService.cs
│   └── Data/
│       └── PrivatekonomyContext.cs # EF Core DbContext
├── Privatekonomi.Api/
│   └── Controllers/
│       └── BankConnectionsController.cs # REST API endpoints
├── Privatekonomi.Web/
│   └── Components/
│       ├── Pages/
│       │   └── BankConnections.razor    # Main page
│       └── Dialogs/
│           ├── AddBankConnectionDialog.razor
│           └── EditBankConnectionDialog.razor
└── Privatekonomi.AppHost/
    └── Program.cs                       # Aspire orchestration
```

## Making Changes

### Adding a New Field to BankConnection

1. Update the model in `Privatekonomi.Core/Models/BankConnection.cs`
2. Update the database context configuration in `PrivatekonomyContext.cs`
3. Update the UI in `BankConnections.razor`
4. Update API DTOs if needed
5. Create a migration (if using real database)

### Adding Audit Logging to Other Services

```csharp
public class MyService
{
    private readonly IAuditLogService? _auditLogService;
    
    public MyService(IAuditLogService? auditLogService = null)
    {
        _auditLogService = auditLogService;
    }
    
    public async Task DoSomething()
    {
        // Your code here
        
        await _auditLogService?.LogAsync(
            "ActionName",
            "EntityType",
            entityId: 123,
            details: "Additional info")!;
    }
}
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Privatekonomi.Core.Tests

# Run Playwright E2E tests
cd tests/playwright
npm install
npm test
```

## Additional Resources

- [Full Implementation Documentation](BANK_CONNECTIONS_GUI.md)
- [PSD2 API Guide](PSD2_API_GUIDE.md)
- [MudBlazor Documentation](https://mudblazor.com/)
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
