# Guide för lagringsmetoder

## Alfa: Web och API

Web och API är separata processer med varsin `Storage`-konfiguration och
Identity-installation. Web använder Core-tjänster direkt mot
sitt eget datalager, inte `/api/personal`. API:s personliga endpoints läser
API-processens datalager. Aspire startar båda, men gör inte deras data eller
inloggningar gemensamma.

Som standard använder Web `JsonFile` (`./data`), men i Development SQLite
(`privatekonomi-development.db`). API använder `InMemory` i både bas- och
Development-konfigurationen. Samma provider eller databasnamn betyder inte att
processernas minnesdata delas. Om båda pekar på samma relationsdatabas används
visserligen samma fysiska tabeller, men detta gör inte Web till en API-klient.
Föredra separata databaser och konton för tjänsterna och håll mock/test och
produktion åtskilda.

> **Säkerhetsgräns för alfa-API:** Äldre API-controllers är nu spärrade som
> standard och svarar normalt 403, även med en personlig bearer-token. I äldre
> versioner utan spärren kan exempelvis anonyma anrop till
> `GET /api/transactions` läsa transaktioner utan användarfiltrering. HTTPS
> och skyddet för `/api/personal` skyddar inte automatiskt äldre rutter.
> Kontrollera vilken version som faktiskt körs och isolera äldre installationer
> bakom en tillförlitlig åtkomstgräns eller uppdatera dem. Spärren påverkar
> befintliga klienter och bank-OAuth; se [säkerhetsguiden](LEGACY_API_SECURITY.md).

## Isolera mock, test och produktion

Använd olika datalager för varje kombination av tjänst och miljö, exempelvis:

| Miljö | Web | API |
|---|---|---|
| Lokal mock/Development | separat SQLite-fil för demo eller InMemory | separat InMemory eller SQLite-fil |
| Test | egen tillfällig databas per testkörning | egen tillfällig databas per testkörning |
| Produktion | egen beständig databas | egen beständig databas |

Använd olika absoluta sökvägar, databasnamn, databasanvändare och hemligheter.
Ge varje process endast åtkomst till sitt eget lager. Bankmockar och exempeldata
är inte produktionsdata: koppla aldrig test eller mockar till produktionsdatabasen,
bankkonton eller produktionshemligheter. Kontrollera effektiv konfiguration och
miljö före start. Håll `Storage:SeedTestData` avstängd i produktion.
Web skapar referensdata vid start; i Development skapas ett nytt demokonto och
dess exempeldata endast om `DemoUser:Enabled` respektive `Storage:SeedTestData`
är aktiverade. Befintliga demokonton ändras inte eller seedas på nytt.

**API-demo:** API:s bas- och Development-konfiguration använder InMemory och
`SeedTestData=false`; demo är inte aktiverat som standard. Ett API-demokonto
kräver Development, `DemoUser:Enabled=true` samt API:ts egna `DemoUser:Email`
och `DemoUser:Password`. Det skapas bara om kontot inte redan finns.
API-exempeldata kräver dessutom `Storage:SeedTestData=true` och en **ny**
InMemory-databas. Med exempeldata aktiverade avvisas SQLite och JsonFile vid
uppstart; de kan däremot användas för ett demokonto utan exempeldata.
Aktiverad API-demo i Development blockeras på SQL Server och MySQL/MariaDB på
grund av okoordinerad samtidig initiering. API hoppar också över seedning av
referensdata på dessa serverdatabaser; för InMemory, JsonFile och SQLite körs
referensdatainitieringen. API-kontot finns inte automatiskt i Webs lager.
Använd aldrig demouppgifter i produktion.

## Konfiguration

Varje process läser sina egna `appsettings.json` och `appsettings.{Environment}.json`.
I Development och Local läggs även `appsettings.local.json` och projektets User
Secrets till efter standardkonfigurationen. Därefter läses miljövariabler med
prefixet `PRIVATEKONOMI_ASPIRE_`, som Aspire använder för vissa överstyrningar.
Vanliga miljövariabler använder `__` för nästlade nycklar; ange dem separat för
varje process, till exempel i respektive tjänsts servicekonfiguration:

```text
ASPNETCORE_ENVIRONMENT=Production
Storage__Provider=Sqlite
Storage__ConnectionString=Data Source=/var/lib/privatekonomi/web-prod.db
Storage__SeedTestData=false
```

För API, använd exempelvis `/var/lib/privatekonomi/api-prod.db` i stället.
Relativa sökvägar utgår från processens arbetskatalog. Välj en beständig katalog
utanför publicerade filer och web root, och säkerställ katalogbehörighet. Sätt inte
en gemensam `Storage__ConnectionString` i en föräldraprocess som startar båda.
Aspires parametrar och särskilda macOS-överstyrningar beskrivs i
[Aspire-guiden](ASPIRE_GUIDE.md); kontrollera den faktiska konfigurationen per tjänst.

Lösenord och anslutningssträngar med credentials hör hemma i en säker
hemlighetshanterare eller tjänstens skyddade miljövariabler, inte i versionshanterade
filer. Lokalt kan respektive projekts User Secrets användas i Development/Local,
till exempel `dotnet user-secrets set "Storage:ConnectionString" "..." --project
src/Privatekonomi.Api`. User Secrets är inte krypterade och är inte en
produktionslösning. Ge test och produktion olika secrets; skydda även databasfiler,
JSON-filer, säkerhetskopior och loggar.

## Providers

| `Storage:Provider` | Funktion |
|---|---|
| `InMemory` | EF-databas i processens minne. All data, även konton, försvinner vid avslut. Separata processer delar inte minne. |
| `Sqlite` | Filbaserad relationsdatabas; tom anslutningssträng använder `Data Source=privatekonomi.db`. |
| `SqlServer` | Relationsdatabas; anslutningssträng krävs. |
| `MySQL`, `MariaDB` | Relationsdatabas; anslutningssträng krävs. Se [MySQL-guiden](MYSQL_DEPLOYMENT_GUIDE.md). |
| `JsonFile` | Implementerad filpersistens över en processlokal InMemory-databas. |

Kontrollera stavningen och den effektiva providerinställningen. `SeedTestData`
är en inställning för initiering, inte ett byte eller en kopiering av datalager.

### SQLite

```json
{
  "Storage": {
    "Provider": "Sqlite",
    "ConnectionString": "Data Source=/var/lib/privatekonomi/web-prod.db",
    "SeedTestData": false
  }
}
```

SQLite passar mindre installationer, men har begränsad samtidig skrivning. Föredra
lokal disk framför att dela en SQLite-fil via nätverksfilsystem. `Mode=Memory`
ger inte en beständig diskbackup. För större installationer kan en serverbaserad
provider vara lämpligare.

### SQL Server

```json
{
  "Storage": {
    "Provider": "SqlServer",
    "ConnectionString": "Server=localhost;Database=PrivatekonomiWeb;Integrated Security=true",
    "SeedTestData": false
  }
}
```

Ange en annan databas för API och olika databaser för utveckling, test och
produktion. Lägg autentiseringsuppgifter i en hemlighetshanterare.

### JsonFile

`Storage:ConnectionString` är en katalog, exempelvis `./data` (även standard om
den är tom). Varje process laddar filerna till sitt eget minne vid start.
Ekonomidata sparas periodiskt var femte minut och vid ordnat avslut; en krasch
eller skrivfel kan medföra dataförlust. Identity-data, inklusive lösenordshashar,
sparas separat i `identity.json` vid ändringar. Ekonomidata omfattar bland annat
`categories.json` och månatliga filer som `transactions-2025-01.json`.

> **Varning för dataförlust:** JSON-persistensen är processlokal. Web, API och
> andra instanser får **inte dela samma JSON-katalog**, inte ens om de använder
> samma konfiguration. Deras minnesbilder synkroniseras inte och separata
> skrivningar kan skriva över varandras data. Kontrollera särskilt befintliga
> Raspberry Pi-installationer innan båda processerna startas.

Använd en separat katalog per process. Filerna innehåller känsliga
personuppgifter; lägg dem inte i Git.
Säkerhetskopiera hela katalogen efter ordnat stopp och skydda kopian.
JSON-formatet är ingen garanti för fullständig export eller migrering.

## Byte av provider och säkerhetskopiering

Att ändra `Provider` eller `ConnectionString` flyttar inte data eller konton.
Import/export täcker inte nödvändigtvis alla entiteter och Identity; det finns
ingen utlovad generell migrering mellan providers eller miljöer. Behåll
originalet, planera och verifiera eventuell separat överföring innan bytet.
Flytta inte testdata till produktion som rutin.

För SQLite: använd exempelvis `sqlite3 /sökväg/web-prod.db ".backup
'/säker/backup/web-prod.db'"` för en konsekvent backup, eller stoppa alla
skrivande processer före filkopiering. Testa återställning till en separat plats
och säkerhetskopiera även API:s databas om den är beständig. För JSON: stoppa
processen ordnat och kopiera hela katalogen. Backuper kan innehålla kontodata.

## Schema och felsökning

`EnsureCreated` skapar en ny databas men uppdaterar inte ett befintligt schema
generellt. Web och API kör särskilda additiva initialiserare för vissa tabeller
och kolumner. De ersätter inte en allmän migrationsstrategi. Säkerhetskopiera
innan uppdatering och kör inte hela EF-migrationskedjan blint mot äldre databaser
som skapats med `EnsureCreated`.

- **Data saknas efter omstart:** kontrollera provider och process; InMemory är
  flyktigt. För JSON, kontrollera loggar och rätt katalog.
- **Database is locked:** kontrollera samtidiga skribenter och filsystemet.
- **Permission denied:** kontrollera tjänstekontots rättigheter till katalog och fil.
- **Fel databas:** kontrollera miljö, överstyrningar och arbetskatalog.

Se även [utvecklarguiden](DEVELOPER_QUICKSTART.md) och
[snabbguiden](STORAGE_QUICKSTART.md).
