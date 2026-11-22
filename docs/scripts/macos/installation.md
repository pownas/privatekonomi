# Privatekonomi på macOS

Installationsflödet är avsett för en privat macOS-installation på Apple Silicon (M-serien, inklusive M2) eller Intel. Web och API publiceras som separata .NET-processer och körs som per-användare LaunchAgents. Aspire Dashboard används inte i denna driftsättning.

## Förutsättningar

- macOS med internetanslutning
- Klonat Privatekonomi-repository
- .NET 11 SDK (installeraren kontrollerar och installerar denna vid behov)
- En terminal med `bash`

Homebrew krävs inte. Installeraren skapar en lokal byggmiljö och försöker även hämta svensk och engelsk OCR-språkdata. Om OCR-datan inte kan hämtas kan appen ändå starta, men kvitto-OCR är då inte tillgänglig.

För en steg-för-steg-guide om familjekonton, säker delning på hemnätverket, backup och systemadministratörsbehörighet, se [Kom igång och dela Privatekonomi hemma](../../FAMILY_HOME_SETUP.md).

## Installera

Kör från repositoryts rot:

```bash
bash setup-macos.sh install
```

Utan ett skriptnamn visas en lista över macOS-skripten. Tryck Enter för att välja `install.sh` som standard, eller skriv `start.sh` för att starta appen. Startaren visar dokumentations- och skriptkatalogen och kräver bekräftelse innan vald åtgärd körs.

Installationsskriptet hämtar grenarna från GitHub och låter dig välja vilken gren som ska installeras; standardvalet är `main`. Den valda grenen byggs och installeras, och används sedan även för uppdateringar från Drift-sidan. Du kan välja utan interaktiv fråga med `bash setup-macos.sh install --branch main` eller ange `PRIVATEKONOMI_BRANCH`; `--skip-interactive` använder `main`. Skriptet kontrollerar macOS och processorns arkitektur, hittar .NET 11 SDK från både den aktuella `PATH`-sökvägen och vanliga macOS-installationsplatser, och installerar SDK:t om det saknas. Därefter återställs NuGet-paket, lösningen byggs, Web/API publiceras och bakgrundstjänsterna aktiveras. Installationsskriptet visar vilken fas som körs, skriver byggutdata löpande och visar en statusrad var 15:e sekund vid tysta långkörande kommandon. Installations- och byggloggar sparas separat i `~/Library/Logs/Privatekonomi/`.

Om .NET installeras i `~/.dotnet` läggs sökvägen till i den aktuella shell-profilen. Öppna en ny terminal om kommandona inte hittas efter installationen.

## Starta och automatisk uppstart

```bash
bash setup-macos.sh start
```

## Avinstallera eller rensa installationen

Kör från repositoryts rot:

```bash
bash setup-macos.sh uninstall
```

Skriptet stoppar och avregistrerar Web-, API-, uppdaterings- och backup-LaunchAgents. Interaktivt kan du välja att ta bort enbart programfilerna (standard) eller även hela `~/Library/Application Support/Privatekonomi`, inklusive SQLite/JSON-data. Backuper i `~/Library/Application Support/Privatekonomi-Backups` och loggar i `~/Library/Logs/Privatekonomi` behålls som standard; de kan väljas bort separat. Källkodsklonen och .NET SDK lämnas kvar. Innan något tas bort krävs bekräftelsen `RADERA`. Om en tjänst inte går att stoppa avbryts rensningen före filradering och `launchctl`-felet visas.

För användning utan frågor krävs ett uttryckligt omfång och `--yes`:

```bash
bash scripts/macos/uninstall.sh --app-only --yes
bash scripts/macos/uninstall.sh --include-data --delete-backups-and-logs --yes
```

Vid installationen väljer du SQLite (standard) eller JSON-filer. Valet sparas i `$HOME/Library/Application Support/Privatekonomi/storage-provider`; det är en textfil, inte installationsmappen. Senare starter använder det sparade valet. Om du vill byta provider kan du köra `PRIVATEKONOMI_STORAGE_PROVIDER=JsonFile bash scripts/macos/start.sh` (eller `Sqlite`).

Installationsroten är `$HOME/Library/Application Support/Privatekonomi/`. Web och API publiceras separat under `releases/<commit>/Web` och `releases/<commit>/Api`; länken `current` pekar på den aktiva releasen. En förenklad struktur ser ut så här:

```text
$HOME/Library/Application Support/Privatekonomi/
├── current -> releases/<commit>
├── releases/<commit>/
│   ├── Web/
│   └── Api/
├── storage-provider
├── privatekonomi.db  (när SQLite används)
└── json/              (när JSON-filer används)
```

SQLite-databasen ligger alltså i `$HOME/Library/Application Support/Privatekonomi/privatekonomi.db`. Med JSON lagras Web-data i `json/`; API använder minneslagring och dess data försvinner vid omstart. Om du byter provider migreras inte befintliga data.

Providerbyte migrerar inte data eller konton. Med JSON använder Web en egen
JSON-katalog och API InMemory, vars data försvinner vid omstart. JSON är
processlokalt: låt aldrig Web och API eller flera instanser skriva till samma
JSON-katalog. Kontrollera tjänsternas faktiska konfiguration före start.
Installationsvalet SQLite konfigurerar däremot samma databasfil för Web och API;
för isolering, konfigurera separata databaser och secrets per tjänst och miljö.
Se [lagringsguiden](../../STORAGE_GUIDE.md).

## Öppna SQLite och ge ditt konto systemadministratörsbehörighet

macOS har normalt SQLite-kommandoradsverktyget `sqlite3` inbyggt; kontrollera med:

```bash
sqlite3 --version
```

Appen behöver ingen separat SQLite-serverinstallation. Om `sqlite3` saknas och
Homebrew redan finns kan kommandoradsverktyget installeras med `brew install sqlite`.

För att ge ett befintligt konto systemadministratörsbehörighet i SQLite:

1. Stoppa Web och API så att de inte skriver till databasen medan den ändras:

   ```bash
   launchctl bootout "gui/$(id -u)/com.privatekonomi.web"
   launchctl bootout "gui/$(id -u)/com.privatekonomi.api"
   ```

2. Ta en verifierbar SQLite-backup:

   ```bash
   DB="$HOME/Library/Application Support/Privatekonomi/privatekonomi.db"
   BACKUP_DIR="$HOME/Library/Application Support/Privatekonomi-Backups"
   mkdir -p "$BACKUP_DIR"
   BACKUP="$BACKUP_DIR/privatekonomi-before-admin-$(date +%Y%m%d-%H%M%S).db"
   sqlite3 "$DB" ".backup '$BACKUP'"
   ```

3. Hämta konto-ID:t och kontrollera den nuvarande behörigheten:

   ```bash
   DB="$HOME/Library/Application Support/Privatekonomi/privatekonomi.db"
   sqlite3 -header -column "$DB" \
     "SELECT Id, UserName, Email, IsSystemAdmin FROM AspNetUsers;"
   ```

4. Byt ut `<USER_ID>` mot ID:t för ditt eget konto och uppdatera enbart den raden:

   ```bash
   DB="$HOME/Library/Application Support/Privatekonomi/privatekonomi.db"
   sqlite3 "$DB" \
     "UPDATE AspNetUsers SET IsSystemAdmin = 1 WHERE Id = '<USER_ID>';"
   sqlite3 "$DB" \
     "SELECT Id, UserName, Email, IsSystemAdmin FROM AspNetUsers WHERE Id = '<USER_ID>';"
   ```

   Kontrollera att `IsSystemAdmin` visar `1`. Appens systemadministratörsbehörighet
   baseras på den här kolumnen i `AspNetUsers`, inte på en Identity-roll. Ändra inte
   databasen utan backup och lämna alltid villkoret `WHERE Id = ...` kvar.

5. Starta om tjänsterna och logga ut/in för att uppdatera gränssnittet:

   ```bash
   bash setup-macos.sh start
   ```

Web och API körs i bakgrunden och övervakas av `launchd`. De startas automatiskt när macOS-användaren loggar in och startas om om en process oväntat avslutas. Det går inte att starta en personlig LaunchAgent före inloggning; datorn måste vara på och användarkontot inloggat. Startkommandot uppdaterar tjänstekonfigurationen och startar om tjänsterna. Loggar finns under `~/Library/Logs/Privatekonomi/`.

Webappen finns på `http://localhost:5274` och API:t på `http://localhost:5277`. Tjänsterna lyssnar på nätverksgränssnitten, så enheter på samma hemnätverk kan använda `http://<Macens-IP-adress>:5274`. macOS-brandväggen kan behöva tillåta inkommande anslutningar för .NET. LAN-anslutningen använder HTTP och är endast avsedd för ett betrott privat nätverk. Öppna inte portarna mot internet; HTTP krypterar inte trafik. Aspire Dashboard startas inte av dessa tjänster.

API:s personliga inloggning och transaktionsanrop kräver HTTPS som API självt
uppfattar som HTTPS; ovanstående HTTP-adress kan inte användas för dem.

Installeraren skapar separata `appsettings.Production.json` för Web och API med portar, lagringsprovider, datakatalog och utan testdata. Filerna innehåller lokala sökvägar och ignoreras av Git.

## Uppdatera från webbgränssnittet

En systemadministratör kan öppna **Drift** i webbappen och begära installation av en gren från Privatekonomis GitHub-repository eller starta om Web/API. Begäran verifieras av serverns admin-policy och antiforgery-skydd och utförs sedan av en separat per-användare bakgrundstjänst. Uppdateringen hämtar källkod, bygger och publicerar en separat release, stoppar tjänsterna, skapar en verifierad databasbackup, växlar release och kontrollerar API/Webs lokala hälsosvar. Om den nya versionen inte startar återställs den tidigare releasen automatiskt. Status och logg visas på Drift-sidan; detaljerade loggar finns i `~/Library/Logs/Privatekonomi/update.log`.

Uppdatering kräver att den ursprungliga Git-klonen fortfarande finns kvar på samma sökväg, internetanslutning och tillräckligt med ledigt diskutrymme för en ny publicering och backup. Om den ursprungliga källkoden flyttas, installera om tjänsterna genom att köra `bash scripts/macos/start.sh` från den nya klonen.

Verifierade nattliga backuper schemaläggs kl. 02:30 och sparas separat i `~/Library/Application Support/Privatekonomi-Backups/`; backuper äldre än 60 dagar tas bort efter att en ny backup verifierats. Du kan köra en backup manuellt med `bash "$HOME/Library/Application Support/Privatekonomi/backup.sh"`. Backupjobbet kräver att användaren är inloggad; om datorn sover kl. 02:30 kör macOS normalt den schemalagda uppgiften när den väcks. Backupen ligger på samma Mac och skyddar därför inte mot diskfel eller förlust av datorn; kopiera backupmappen till en separat disk eller säker molnlagring för sådant skydd.

För återställning av SQLite: stoppa appen och ersätt `privatekonomi.db` med önskad fil från backupmappen. För JSON: stoppa appen, packa upp önskad `.tar.gz`-backup till `~/Library/Application Support/Privatekonomi/json/` och starta om. PostgreSQL kan nu användas som lagringsprovider och migreras med anvisningarna i [migrationsguiden](../../DATA_MIGRATION_GUIDE.md).

## Felsökning

- Kontrollera SDK: `dotnet --list-sdks` (en version som börjar med `11.` ska visas).
- Läs installationsloggen i `~/Library/Logs/Privatekonomi/`.
- Kontrollera tjänsterna och starta om dem med `bash scripts/macos/start.sh`.
- Om appen inte svarar: läs `web-error.log`, `api-error.log` och `update-error.log` i `~/Library/Logs/Privatekonomi/`.
- Om bygget misslyckas: kör `dotnet restore Privatekonomi.slnx` och `dotnet build Privatekonomi.slnx` från repositoryts rot och spara hela felutskriften.
