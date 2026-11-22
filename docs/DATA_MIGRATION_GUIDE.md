# Migrering mellan datalagringsytor

Denna guide beskriver hur data flyttas mellan SQLite, SQL Server, MySQL,
MariaDB, PostgreSQL och JSON-snapshot.

## Förutsättningar

- Privatekonomi är stoppat på källan och målet när migreringen körs.
- Källans databas är tillgänglig och innehåller data.
- Måldatabasen är ny eller tom. Befintliga användardata skrivs inte över.
- Schema och tabeller finns på målplattformen; migreringen använder samma EF-modell.
- Säkerhetskopiera både källan och målet före starten.

## Smidig migrering i gränssnittet

1. Öppna **Datahantering** (`/data-management` eller `/settings/data-management`).
2. Öppna avsnittet **Flytta mellan databaser**.
3. Välj källprovider och målprovider.
4. Ange källans anslutningssträng eller JSON-mapp.
5. Ange målets anslutningssträng eller JSON-mapp.
6. Kontrollera att målet är tomt och välj **Starta migrering**.
7. Läs resultatmeddelandet och migreringsantal per entitet.

Migrationsavsnittet visas endast för systemadministratörer. Innan start krävs
både en lyckad förhandskontroll av källan och målet samt en explicit
bekräftelse på att säkerhetskopior är gjorda. En ändrad provider eller
anslutning återställer förhandskontrollen och kräver kontroll på nytt.

Autentiserade API-klienter kan använda samma funktion direkt:

```text
POST /api/data-migration
Content-Type: application/json
Authorization: ******
```

Använd först `POST /api/data-migration/preview` med samma begäran för att
kontrollera käll- och målstatus. Migreringen startas först med
`POST /api/data-migration`. Båda anropen kräver systemadministratör.

Exempel:

```json
{
  "sourceProvider": "Sqlite",
  "sourceConnectionString": "Data Source=/var/lib/privatekonomi/privatekonomi.db",
  "targetProvider": "PostgreSQL",
  "targetConnectionString": "Host=localhost;Database=privatekonomi;Username=privatekonomi;******"
}
```

## Kopplade providers

| Käll-/målprovider | Anslutningssträng eller käll-/målkatalog |
|---|---|
| `Sqlite` | `Data Source=/absolut/väg/privatekonomi.db` |
| `SqlServer` | `Server=host;Database=Privatekonomi;Integrated Security=true` |
| `MySQL` | MySQL Anslutningssträng för Pomelo EF |
| `MariaDB` | MariaDB Anslutningssträng för Pomelo EF |
| `PostgreSQL` | `Host=host;Database=privatekonomi;Username=...;****** |
| `JsonFile` | Mapp i `sourceConnectionString` eller `targetConnectionString` |

Vid JSON-export skapas `privatekonomi-migration.json` i den angivna tomma
mappen. JSON-filen innehåller hela EF-modellen och Identity-data, inklusive
lösenordshashar. Skydda den som en känslig fullständig backup.

Snapshotet har versionsnummer och SHA-256-integritetskontroll. Vid import
avvisas filen om formatet inte stöds eller om innehållet har ändrats.

## Windows till SQLite eller omvänt

Stoppa Privatekonomi på Windows och säkerhetskopiera SQL Server-databasen.
Flytta till en ny eller tom SQLite-databas via migreringsgränssnittet:

```json
{
  "sourceProvider": "SqlServer",
  "sourceConnectionString": "Server=localhost;Database=Privatekonomi;Integrated Security=true;TrustServerCertificate=true",
  "targetProvider": "Sqlite",
  "targetConnectionString": "Data Source=C:\\Privatekonomi\\privatekonomi.db"
}
```

Använd motsatta provider för att flytta tillbaka. Konfigurera målprocessen med:

```json
"Storage": {
  "Provider": "Sqlite",
  "ConnectionString": "Data Source=C:\\Privatekonomi\\privatekonomi.db",
  "SeedTestData": false
}
```

## macOS till eller från SQLite, MySQL/MariaDB eller PostgreSQL

Använd ett absolut `Data Source` för SQLite, exempelvis:

```text
Data Source=/Users/anvandare/Library/Application Support/Privatekonomi/privatekonomi.db
```

För MariaDB/MySQL eller PostgreSQL används respektive provider och anslutningssträng.
Måldatabasen ska vara skapad men tom på användardata.

Efter flytten:

```bash
Storage__Provider=Sqlite
Storage__ConnectionString="Data Source=/Users/anvandare/Library/Application Support/Privatekonomi/privatekonomi.db"
Storage__SeedTestData=false
```

## Raspberry Pi och SQLite

Stoppa tjänsterna innan migreringen:

```bash
sudo systemctl stop privatekonomi-web privatekonomi-api
```

Säkerhetskopiera befintlig databas och katalog. Migrera till en absolut,
skrivbar SQLite-sökväg, till exempel:

```text
Data Source=/var/lib/privatekonomi/privatekonomi.db
```

Kontrollera katalogens rättigheter:

```bash
sudo mkdir -p /var/lib/privatekonomi
sudo chown privatekonomi:privatekonomi /var/lib/privatekonomi
sudo systemctl start privatekonomi-web privatekonomi-api
```

## Säkerhet och felsökning

- Använd aldrig ett befintligt produktionsmål som är avsett att behålla data.
- JSON-filer och migrations-API:n får inte exponeras utan autentisering.
- `Måldatabasen är inte tom` betyder att migreringen avbröts utan överskrivning.
- Kontrollera anslutningssträng, nätverk, DNS, port och databasbehörigheter.
- Kontrollera providerspecifika hostmiljövariabler med `Storage__Provider` och
  `Storage__ConnectionString`.
- Verifiera alltid ett migrerat konto och flera entitetstyper efter omstart.
