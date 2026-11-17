# Skript

Plattformsspecifika installations-, start- och underhållsskript finns här:

- `macos/` – lokal installation och start via .NET Aspire på macOS.
- `windows/` – lokal installation och start via PowerShell.
- `raspberry-pi/` – installation, start, uppdatering och diagnostik för Raspberry Pi.
- `codespaces/` – utvecklingsmiljö för GitHub Codespaces.
- `linux/` – Linux-hjälpskript, till exempel OCR-språkdata.

Rootens enda startare per miljö är `setup-macos.sh`, `setup-windows.ps1`, `setup-raspberry-pi.sh` och `setup-codespaces.sh`. De visar dokumentation och installerarsökväg och kräver ett aktivt godkännande innan installationen startar.

Övriga plattformsskript körs från respektive underkatalog, till exempel `cd scripts/raspberry-pi`. Dokumentationen finns i [`../docs/scripts/README.md`](../docs/scripts/README.md).
