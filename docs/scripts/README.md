# Installation och drift per plattform

Välj instruktionerna för den plattform där Privatekonomi ska byggas och köras. Skripten finns samlade i [`scripts/`](../../scripts/README.md); äldre skriptnamn i repositoryts rot fungerar fortfarande som kompatibilitetsstartare.

## macOS

För lokal utveckling på Apple Silicon (M-serien, inklusive M2) eller Intel:

1. Klona repositoryt och gå till dess rot.
2. Kör `bash scripts/macos/install.sh`.
3. Gå till plattformskatalogen med `cd scripts/macos` och starta med `bash start.sh`.

Se [macOS-installationsguiden](macos/installation.md) för förutsättningar, loggar och felsökning.

## Windows

Installera från repositoryts rot i PowerShell:

```powershell
.\local-app-install.ps1
```

För start och övriga Windows-kommandon, gå till `scripts\windows` med `Set-Location scripts\windows` och kör `.\start.ps1`. Starta utan `dotnet watch` med `-NoWatch`. Se [Windows-instruktionerna](windows/README.md).

## Raspberry Pi OS

Installationsflöde, uppdatering, start och felsökning för Raspberry Pi finns i [Raspberry Pi-guiden](raspberry-pi/RASPBERRY_PI_QUICKSTART.md). Alla Raspberry Pi-dokument är samlade i [Raspberry Pi-dokumentationen](raspberry-pi/RASPBERRY_PI_GUIDE.md).

Installera med den enda Raspberry Pi-installeraren i repositoryts rot: `bash ./raspberry-pi-install.sh`. För start, uppdatering och diagnostik går du till `~/privatekonomi/scripts/raspberry-pi` med `cd` och kör respektive skript därifrån.

## Linux och GitHub Codespaces

Installera i Linux/Codespaces med `bash ./app-install.sh`. För start, kör `cd scripts/codespaces` följt av `bash start.sh`. Se [Codespaces-instruktionerna](codespaces/README.md). OCR-språkdata kan hämtas med `bash scripts/linux/setup-tesseract.sh`.

## Skriptöversikt

Se [`scripts/README.md`](../../scripts/README.md) för katalogstrukturen och direkta skriptvägar.
