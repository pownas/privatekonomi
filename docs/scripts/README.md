# Installation och drift per plattform

Välj instruktionerna för den plattform där Privatekonomi ska byggas och köras. Skripten finns samlade i [`scripts/`](../../scripts/README.md); äldre skriptnamn i repositoryts rot fungerar fortfarande som kompatibilitetsstartare.

## macOS

För lokal utveckling på Apple Silicon (M-serien, inklusive M2) eller Intel:

1. Klona repositoryt och gå till dess rot.
2. Kör `bash scripts/macos/install.sh`.
3. Starta med `bash scripts/macos/start.sh`.

Se [macOS-installationsguiden](macos/installation.md) för förutsättningar, loggar och felsökning.

## Windows

Kör från repositoryts rot i PowerShell:

```powershell
.\scripts\windows\install.ps1
.\scripts\windows\start.ps1
```

Starta utan `dotnet watch` med `-NoWatch`. Se [Windows-instruktionerna](windows/README.md).

## Raspberry Pi OS

Installationsflöde, uppdatering, start och felsökning för Raspberry Pi finns i [Raspberry Pi-guiden](raspberry-pi/RASPBERRY_PI_QUICKSTART.md). Alla Raspberry Pi-dokument är samlade i [Raspberry Pi-dokumentationen](raspberry-pi/RASPBERRY_PI_GUIDE.md).

Skriptens källfiler finns under `scripts/raspberry-pi/`. Rootkommandon som `./raspberry-pi-install.sh` behålls för bakåtkompatibilitet.

## Linux och GitHub Codespaces

`./app-install.sh` och `./app-start.sh` är kompatibilitetsstartare för Codespaces-/Linux-utvecklingsflödet. Se [Codespaces-instruktionerna](codespaces/README.md). OCR-språkdata kan hämtas med `bash scripts/linux/setup-tesseract.sh`.

## Skriptöversikt

Se [`scripts/README.md`](../../scripts/README.md) för katalogstrukturen och direkta skriptvägar.
