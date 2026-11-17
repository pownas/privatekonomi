# Privatekonomi på macOS

Installationsflödet är avsett för lokal utveckling och körning via .NET Aspire på macOS. Det stöder både Apple Silicon (M-serien, inklusive M2) och Intel; på en M2 används .NETs ARM64-version. Appen körs i terminalen och installeras inte som bakgrundstjänst; en separat LaunchAgent schemalägger databasbackup.

## Förutsättningar

- macOS med internetanslutning
- Klonat Privatekonomi-repository
- .NET 11 SDK och Aspire CLI (installeraren kontrollerar och installerar dessa vid behov)
- En terminal med `bash`

Homebrew krävs inte. Installeraren skapar en lokal byggmiljö och försöker även hämta svensk och engelsk OCR-språkdata. Om OCR-datan inte kan hämtas kan appen ändå starta, men kvitto-OCR är då inte tillgänglig.

## Installera

Kör från repositoryts rot:

```bash
bash setup-macos.sh install
```

Utan ett skriptnamn visas en lista över macOS-skripten. Tryck Enter för att välja `install.sh` som standard, eller skriv `start.sh` för att starta appen. Startaren visar dokumentations- och skriptkatalogen och kräver bekräftelse innan vald åtgärd körs.

Skriptet kontrollerar macOS och processorns arkitektur, hittar .NET 11 SDK från både den aktuella `PATH`-sökvägen och vanliga macOS-installationsplatser, och installerar SDK:t om det saknas. Det installerar även Aspire CLI vid behov, återställer NuGet-paket, skapar ett betrott HTTPS-utvecklarcertifikat och bygger lösningen. Certifikatet kan utlösa en bekräftelse från macOS Keychain. Installationsloggar sparas i `~/Library/Logs/Privatekonomi/`.

Om .NET installeras i `~/.dotnet` läggs sökvägen till i den aktuella shell-profilen. Öppna en ny terminal om kommandona inte hittas efter installationen.

## Starta

```bash
bash setup-macos.sh start
```

Startskriptet frågar vilken lagring som ska användas: SQLite (standard) eller JSON-filer. SQLite-databasen eller JSON-datafilerna sparas under `~/Library/Application Support/Privatekonomi/`. Vid start installeras och aktiveras en macOS LaunchAgent som försöker skapa en verifierad backup varje natt kl. 02:30. Aspire körs interaktivt i förgrunden så AppHost förblir ansluten till Dashboard; avsluta med `Ctrl+C`. Webappen finns på `http://localhost:5274` och API:t på `http://localhost:5277`. Startskriptet använder dessa fasta portar och binder appen till nätverksgränssnitten, så andra enheter i hemnätverket kan öppna `http://<Macens-IP-adress>:5274`. IP-adressen visas vid start när den kan identifieras. Startskriptet avslutar inte andra .NET-processer och försöker inte frigöra portar genom att döda andra program.

Som standard används `Development`, `Local` och SQLite enligt projektets utvecklingskonfiguration. Du kan ange miljövariabler före start om du vill ändra detta. Starta inte en andra instans om samma portar redan används; Aspire visar då vilket resurs- eller portfel som uppstod.

LAN-åtkomst är avsedd för ett betrott hemnätverk. macOS-brandväggen kan behöva tillåta inkommande anslutningar. LAN-adressen använder HTTP; öppna inte portar mot internet. Aspire Dashboard exponeras inte på LAN.

Backuperna sparas separat i `~/Library/Application Support/Privatekonomi-Backups/`, och äldre backuper än 60 dagar tas bort efter att en ny backup verifierats. Loggar finns i `~/Library/Logs/Privatekonomi/backup.log` och `backup-error.log`. Du kan köra en backup manuellt med `bash "$HOME/Library/Application Support/Privatekonomi/backup.sh"`. LaunchAgent kräver att användaren är inloggad; om datorn sover kl. 02:30 kör macOS normalt den schemalagda uppgiften när den väcks. Backupen ligger på samma Mac och skyddar därför inte mot diskfel eller förlust av datorn; kopiera backupmappen till en separat disk eller säker molnlagring för sådant skydd.

För återställning av SQLite: stoppa appen och ersätt `privatekonomi.db` med önskad fil från backupmappen. För JSON: stoppa appen, packa upp önskad `.tar.gz`-backup till `~/Library/Application Support/Privatekonomi/json/` och starta om. PostgreSQL är inte tillgängligt som val ännu eftersom projektet saknar PostgreSQL EF Core-provider och serverkonfiguration. Välj SQLite eller JSON-filer tills PostgreSQL-stöd har implementerats.

## Felsökning

- Kontrollera SDK: `dotnet --list-sdks` (en version som börjar med `11.` ska visas).
- Kontrollera Aspire CLI: `aspire --version`.
- Läs installationsloggen i `~/Library/Logs/Privatekonomi/`.
- Om HTTPS-certifikatet inte är betrott: kör `dotnet dev-certs https --trust` och godkänn macOS-dialogen.
- Om bygget misslyckas: kör `dotnet restore Privatekonomi.slnx` och `dotnet build Privatekonomi.slnx` från repositoryts rot och spara hela felutskriften.
