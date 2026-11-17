# Skript

Plattformsspecifika installations-, start- och underhållsskript finns här:

- `macos/` – lokal installation och start via .NET Aspire på macOS.
- `windows/` – lokal installation och start via PowerShell.
- `raspberry-pi/` – installation, start, uppdatering och diagnostik för Raspberry Pi.
- `codespaces/` – utvecklingsmiljö för GitHub Codespaces.
- `linux/` – Linux-hjälpskript, till exempel OCR-språkdata.

Användarguiden och körinstruktionerna finns i [`../docs/scripts/README.md`](../docs/scripts/README.md).

Det finns högst en installerare i repositoryts rot per miljö: `app-install.sh` för Linux/Codespaces, `local-app-install.ps1` för Windows och `raspberry-pi-install.sh` för Raspberry Pi. macOS installeras direkt via `scripts/macos/install.sh`.

Övriga plattformsskript körs från respektive underkatalog, till exempel `cd scripts/raspberry-pi`. Övriga äldre root-skript är bara kompatibilitetsstartare och ska inte användas i nya instruktioner.
