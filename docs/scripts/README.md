# Installation och drift per plattform

Välj instruktionerna för plattformen där Privatekonomi ska byggas och köras. Skripten finns samlade i [`scripts/`](../../scripts/README.md). Repositoryts rot har en bekräftelse-startare per miljö: `setup-macos.sh`, `setup-windows.ps1`, `setup-raspberry-pi.sh` och `setup-codespaces.sh`. Varje startare visar dokumentations- och implementationssökvägen innan den frågar om godkännande.

## macOS

För lokal utveckling på Apple Silicon (M-serien, inklusive M2) eller Intel:

```bash
bash setup-macos.sh
```

Efter installationen: `cd scripts/macos` och kör `bash start.sh`. Se [macOS-installationsguiden](macos/installation.md) för förutsättningar, loggar och felsökning.

## Windows

Kör från repositoryts rot i PowerShell:

```powershell
.\setup-windows.ps1
```

För start och övriga Windows-kommandon, kör `Set-Location scripts\windows` och sedan `.\start.ps1`. Starta utan hot reload med `-NoWatch`. Se [Windows-instruktionerna](windows/README.md).

## Raspberry Pi OS

Installationsflöde, uppdatering, start och felsökning finns i [Raspberry Pi-snabbstarten](raspberry-pi/RASPBERRY_PI_QUICKSTART.md) och [Raspberry Pi-guiden](raspberry-pi/RASPBERRY_PI_GUIDE.md).

Kör `bash setup-raspberry-pi.sh` från repositoryts rot. Startaren visar dokumentationen och installationsskriptets sökväg och frågar innan installationen startar. För start, uppdatering och diagnostik går du till `~/privatekonomi/scripts/raspberry-pi` med `cd` och kör respektive skript därifrån.

## Linux och GitHub Codespaces

Kör `bash setup-codespaces.sh` från repositoryts rot. För start, kör `cd scripts/codespaces` följt av `bash start.sh`. Se [Codespaces-instruktionerna](codespaces/README.md). OCR-språkdata körs från `scripts/linux/`.

## Skriptöversikt

Se [`scripts/README.md`](../../scripts/README.md) för katalogstrukturen.
