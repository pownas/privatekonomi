# Linux och GitHub Codespaces

Välj installation från repositoryts rot:

```bash
bash setup-codespaces.sh install
```

Utan ett skriptnamn visas tillgängliga Codespaces-skript; Enter väljer `install.sh` som standard. Startaren visar dokumentations- och skriptkatalogen och frågar innan vald åtgärd körs.

Starta via root-startaren:

```bash
bash setup-codespaces.sh start
```

AppHost startas i förgrunden. Avsluta med `Ctrl+C`. OCR-språkdata körs med `cd scripts/linux` och `bash setup-tesseract.sh`.
