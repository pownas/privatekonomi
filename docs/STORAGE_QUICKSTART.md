# Snabbguide - Lagringsmetoder

## Snabbstart

Web och API har separata processer och lagringsinställningar; Web använder inte
personal API.
Web har som standard JsonFile och SQLite i Development, medan API har InMemory.
Exemplen nedan är alternativa konfigurationer per tjänst, inte
instruktioner om att dela ett datalager. Använd olika databaser, JSON-kataloger
och hemligheter för Web/API och för mock, test och produktion.

### Utveckling (valfri flyktig lagring)

API-exempeldata kräver en ny InMemory-databas, Development och ett aktiverat
API-demokonto med e-post och lösenord. API:s standard har `SeedTestData=false`.
Se [API-demovillkoren](STORAGE_GUIDE.md#isolera-mock-test-och-produktion).
Web använder fortfarande Core och sin egen Storage-konfiguration direkt.
API- och Web-data delas inte automatiskt; ta inte bort Web-lagringen förrän
Web-flödena är migrerade till API:t.

```json
{
  "Storage": {
    "Provider": "InMemory",
    "SeedTestData": true
  }
}
```

### Produktion (persistent data)

Exemplet är en relativ SQLite-sökväg och beror på processens arbetskatalog.
Ange i drift en beständig absolut sökväg för respektive process, håll Web och
API åtskilda och säkerhetskopiera före schema- eller providerbyte.

```json
{
  "Storage": {
    "Provider": "Sqlite",
    "ConnectionString": "Data Source=/var/lib/privatekonomi/api-prod.db",
    "SeedTestData": false
  }
}
```

### Raspberry Pi (lokal disk)
```json
{
  "Storage": {
    "Provider": "Sqlite",
    "ConnectionString": "Data Source=/var/lib/privatekonomi/web.db",
    "SeedTestData": false
  }
}
```

### Storskalig produktion (SQL Server)
```json
{
  "Storage": {
    "Provider": "SqlServer",
    "ConnectionString": "Server=localhost;Database=Privatekonomi;Trusted_Connection=True;MultipleActiveResultSets=true",
    "SeedTestData": false
  }
}
```

### Azure SQL Database
```json
{
  "Storage": {
    "Provider": "SqlServer",
    "ConnectionString": "Server=tcp:yourserver.database.windows.net,1433;Initial Catalog=Privatekonomi;User ID=yourusername;Password=yourpassword;MultipleActiveResultSets=False;Encrypt=True;",
    "SeedTestData": false
  }
}
```

### Backup/Portabilitet (JsonFile)
```json
{
  "Storage": {
    "Provider": "JsonFile",
    "ConnectionString": "./data",
    "SeedTestData": false
  }
}
```

**Varning:** JsonFile använder processlokalt InMemory med periodisk persistens.
Web, API och andra instanser får aldrig dela JSON-katalog: de kan skriva över
varandras data. Ekonomidata sparas var femte minut och vid ordnat avslut;
Identity-data sparas separat vid ändring.

## Växla lagringsmetod

1. Öppna `appsettings.json` (eller `appsettings.Development.json`)
2. Ändra `Provider` till önskad lagringsmetod
3. Ange `ConnectionString` om nödvändigt
4. Sätt `SeedTestData` till `true` eller `false`
5. Starta om applikationen

Providerbyte flyttar inte data eller konton. Behåll original och backup; det finns
ingen utlovad generell migrering genom byte av konfiguration.

## Vanliga scenarier

### Scenario 1: Utveckling till produktion

**Före (utveckling):**
```json
{
  "Storage": {
    "Provider": "InMemory",
    "SeedTestData": true
  }
}
```

**Efter (produktion):**
```json
{
  "Storage": {
    "Provider": "Sqlite",
    "ConnectionString": "Data Source=privatekonomi.db",
    "SeedTestData": false
  }
}
```

### Scenario 2: Ändra sökväg

En ny sökväg pekar på ett annat datalager och flyttar inte filerna. För SQLite
föredras lokal disk framför delning av databasfilen via ett nätverksfilsystem.

### Scenario 3: Test och produktion

Använd separata databaser och secrets. Flytta inte testdata till produktion som
rutin; import/export är inte en garanti för att alla data och konton följer med.

## Felsökning

### Problem: Data försvinner vid omstart
**Lösning:** Byt från InMemory till Sqlite

### Problem: "Database is locked"
**Lösning:** Stäng av alla instanser av applikationen

### Problem: Kan inte hitta databas-fil
**Lösning:** Kontrollera att sökvägen i ConnectionString är korrekt

## Mer information

Se [STORAGE_GUIDE.md](STORAGE_GUIDE.md) för detaljerad dokumentation.
