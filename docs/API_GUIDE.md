# API-guide

## Aktuell status

API:t har ännu inte ersatt Web-appens direkta dataåtkomst. Web använder egna Core-tjänster, Identity och lagring; API har separat Identity och datalager. En användare eller databas i den ena processen innebär inte automatiskt samma konto eller data i den andra.

API:ets nu aktiva personliga dataflöde är begränsat till inloggning och läsning av personliga transaktioner. Äldre controllers är spärrade som standard och ska inte användas som reservlösning. Se [säkerhetsguiden för äldre API:er](LEGACY_API_SECURITY.md).

## Aktiva endpoints

### `GET /api-info`

Publik endpoint för versionsmetadata: `version`, `buildNumber`, `sha`, `environment` och `storageProvider`. Fält kan vara `null` när metadata saknas eller inte godkänns. `storageProvider` visar konfigurationen, inte om lagret innehåller mock-, test- eller produktionsdata.

### API-inloggning och personliga transaktioner

API-inloggning kräver HTTPS och ett konto i API:ts eget Identity-lager. API har inte ett separat registreringsflöde; Web-kontot skapas inte automatiskt i API. Inloggning med tvåfaktorsskyddat konto stöds inte.

1. Skicka `POST /api/session/login` med `email` och `password` över HTTPS.
2. Läs `accessToken` från det lyckade svaret och skicka det i
   `Authorization`-headern med bearer-autentisering.
3. Använd token endast för:
   - `GET /api/personal/transactions?limit=50&beforeId=...`
   - `GET /api/personal/transactions/{id}`

Listan returnerar en sida med transaktioner och `nextBeforeId` när fler poster finns. `limit` måste vara 1–100. Endpoints returnerar bara icke-hushållskopplade transaktioner för den autentiserade API-användaren; ett ogiltigt, låst eller utgånget konto ger inte åtkomst. Överför inte token till vanlig Web-autentisering eller andra endpoints.

## Inaktiva äldre routes

Äldre API-controllers returnerar normalt HTTP 403 även om anropet har en personlig API-token. Detta omfattar bland annat konton, transaktioner, bankkopplingar och notifieringar. Bankens OAuth-callback är också spärrad. Äldre route-exempel i feature- och implementationdokument är inte instruktioner för ett fungerande API-flöde.

## Lagring och miljö

Lagringsprovider och anslutningssträng konfigureras på API-processen för API-data. Web har fortfarande en separat lagring eftersom dess funktioner inte är migrerade till API:t; den separationen är en tillfällig arkitekturbegränsning, inte färdig måldesign. Se [lagringsguiden](STORAGE_GUIDE.md) för providers, testdata, isolering och backup.

Önskad miljöindelning är testin-memory/mock för isolerade automatiska tester,
Development med en separat SQL Server-databas och Production med beständig
SQLite-lagring. Aspire-parametrarna `api-data-profile`,
`api-sqlite-connection-string` och `api-sqlserver-connection-string` väljer
profilerna `Mock`, `Sqlite` respektive `SqlServer` vid processstart.
`web-api-base-url` styr adressen Web använder för API-anrop.

Dessa är konfigurationsval, inte automatik som väljer eller
migrerar data: den aktuella API Development-konfigurationen är fortfarande
InMemory om den inte överstyrts. Innan produktionsdata läggs in ska API:ts och
Webs verkliga provider, connection string och databas vara verifierade var för
sig. Kör aldrig testseeding mot en beständig databas.
