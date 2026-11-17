# Privatekonomi på macOS

Installationsflödet är avsett för lokal utveckling och körning via .NET Aspire på macOS. Det stöder både Apple Silicon (M-serien, inklusive M2) och Intel; på en M2 används .NETs ARM64-version. Det installerar inte appen som en bakgrundstjänst och ändrar inte systeminställningar utöver .NET-utvecklarcertifikatet och PATH i din shell-profil.

## Förutsättningar

- macOS med internetanslutning
- Klonat Privatekonomi-repository
- .NET 11 SDK och Aspire CLI (installeraren kontrollerar och installerar dessa vid behov)
- En terminal med `bash`

Homebrew krävs inte. Installeraren skapar en lokal byggmiljö och försöker även hämta svensk och engelsk OCR-språkdata. Om OCR-datan inte kan hämtas kan appen ändå starta, men kvitto-OCR är då inte tillgänglig.

## Installera

Kör från repositoryts rot:

```bash
cd scripts/macos
bash install.sh
```

Skriptet kontrollerar macOS och processorns arkitektur, installerar .NET 11 SDK och Aspire CLI vid behov, återställer NuGet-paket, skapar ett betrott HTTPS-utvecklarcertifikat och bygger lösningen. Certifikatet kan utlösa en bekräftelse från macOS Keychain. Installationsloggar sparas i `~/Library/Logs/Privatekonomi/`.

Om .NET installeras i `~/.dotnet` läggs sökvägen till i den aktuella shell-profilen. Öppna en ny terminal om kommandona inte hittas efter installationen.

## Starta

```bash
cd scripts/macos
bash start.sh
```

Aspire körs i förgrunden och visar Dashboard samt tjänsternas adresser. Avsluta med `Ctrl+C`. Startskriptet avslutar inte andra .NET-processer och försöker inte frigöra portar genom att döda andra program.

Som standard används `Development`, `Local` och SQLite enligt projektets utvecklingskonfiguration. Du kan ange miljövariabler före start om du vill ändra detta. Starta inte en andra instans om samma portar redan används; Aspire visar då vilket resurs- eller portfel som uppstod.

## Felsökning

- Kontrollera SDK: `dotnet --list-sdks` (en version som börjar med `11.` ska visas).
- Kontrollera Aspire CLI: `aspire --version`.
- Läs installationsloggen i `~/Library/Logs/Privatekonomi/`.
- Om HTTPS-certifikatet inte är betrott: kör `dotnet dev-certs https --trust` och godkänn macOS-dialogen.
- Om bygget misslyckas: kör `dotnet restore Privatekonomi.slnx` och `dotnet build Privatekonomi.slnx` från repositoryts rot och spara hela felutskriften.
