# Installation och drift per plattform

Välj instruktionerna för plattformen där Privatekonomi ska byggas och köras. Skripten finns samlade i [`scripts/`](../../scripts/README.md). Repositoryts rot har en startare per miljö: `setup-macos.sh`, `setup-windows.ps1`, `setup-raspberry-pi.sh` och `setup-codespaces.sh`. Startarna visar tillgängliga skript och låter dig skriva vilket som ska köras; tryck Enter för att välja `install` som standard. Alla åtgärder kräver bekräftelse innan de körs. Du kan också ange skriptnamnet direkt.

## macOS

För lokal utveckling på Apple Silicon (M-serien, inklusive M2) eller Intel:

```bash
bash setup-macos.sh install
```

Starta appen med `bash setup-macos.sh start`. Utan ett skriptnamn visas valmenyn. Se [macOS-installationsguiden](macos/installation.md) för förutsättningar, loggar och felsökning.

## Windows

Kör från repositoryts rot i PowerShell:

```powershell
.\setup-windows.ps1
```

För start och övriga Windows-kommandon, kör `Set-Location scripts\windows` och sedan `.\start.ps1`. Starta utan hot reload med `-NoWatch`. Se [Windows-instruktionerna](windows/README.md).

## Raspberry Pi OS

Installationsflöde, uppdatering, start och felsökning finns i [Raspberry Pi-snabbstarten](raspberry-pi/RASPBERRY_PI_QUICKSTART.md) och [Raspberry Pi-guiden](raspberry-pi/RASPBERRY_PI_GUIDE.md).

Kör `bash setup-raspberry-pi.sh` från repositoryts rot och skriv namnet på skriptet som ska köras, till exempel `install.sh`, `start.sh`, `update.sh` eller `debug.sh`. Tryck Enter för att välja installation som standard. Skript kan också väljas direkt, exempelvis `bash setup-raspberry-pi.sh update`.

## Linux och GitHub Codespaces

Kör `bash setup-codespaces.sh install` från repositoryts rot och starta sedan med `bash setup-codespaces.sh start`. Utan skriptnamn visas valmenyn. Se [Codespaces-instruktionerna](codespaces/README.md). OCR-språkdata körs från `scripts/linux/`.

## Skriptöversikt

Se [`scripts/README.md`](../../scripts/README.md) för katalogstrukturen.
