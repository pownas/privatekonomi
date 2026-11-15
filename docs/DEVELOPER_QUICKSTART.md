# Bank Connections - Developer Quick Start

## Prerequisites
- .NET 9.0 SDK
- Visual Studio 2022 or VS Code with C# extension

## Running the Application

### Option 1: Using .NET Aspire (Recommended)
```bash
cd src/Privatekonomi.AppHost
dotnet run
```
This starts both the API and Web projects with service discovery.

### Option 2: Run Projects Separately

**Terminal 1 - Start API:**
```bash
cd src/Privatekonomi.Api
dotnet run
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
avsedd för en enda skrivande process; använd en gemensam relationsdatabas för flera
instanser. Behåll datakatalog/databas på en beständig sökväg utanför publicerade
programfiler vid releaser. Säkerhetskopiera före schemaändringar och radera inte
databasen eller migrationshistoriken för att uppgradera.

Initieringsfel stoppar uppstarten i stället för att fortsätta med ett tomt eller
ofullständigt datalager. Åtgärda felet innan appen startas igen.

- **Web UI**: https://localhost:5001 or http://localhost:5000
- **API**: https://localhost:5101 (or check console output)
- **Bank Connections Page**: https://localhost:5001/bank-connections

## Testing the Bank Connections Feature

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

Webbprojektet använder SQLite i Development. API-projektet använder fortfarande
InMemory som standard; konfigurera samma beständiga relationsdatabas i båda
projekten om de ska dela data.

### Using a Real Database

För SQL Server, konfigurera `Storage` i båda projektens konfiguration:

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
Make sure the API's CORS policy includes the Web app's URL. Check `Program.cs` in the API project.

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
dotnet test tests/Privatekonomi.Tests

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
