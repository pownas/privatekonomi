# Skript

Plattformsspecifika installations-, start- och underhållsskript finns här:

- `macos/` – lokal installation och start via .NET Aspire på macOS.
- `windows/` – lokal installation och start via PowerShell.
- `raspberry-pi/` – installation, start, uppdatering och diagnostik för Raspberry Pi.
- `codespaces/` – utvecklingsmiljö för GitHub Codespaces.
- `linux/` – Linux-hjälpskript, till exempel OCR-språkdata.

Rootens enda startare per miljö är `setup-macos.sh`, `setup-windows.ps1`, `setup-raspberry-pi.sh` och `setup-codespaces.sh`. De visar dokumentations- och skriptkatalogen och listar alla tillgängliga skript för miljön. Skriv ett skriptnamn vid prompten; Enter väljer `install` som standard. Alternativt kan skriptet anges direkt, till exempel `bash setup-raspberry-pi.sh update` eller `.\setup-windows.ps1 start`. Startaren ber om bekräftelse innan det valda skriptet körs.

Skript kan fortfarande köras direkt från respektive underkatalog. En gemensam intern väljare för Bash-startarna finns i `common/action-dispatch.sh`. Dokumentationen finns i [`../docs/scripts/README.md`](../docs/scripts/README.md).
