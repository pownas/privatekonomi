# Linux och GitHub Codespaces

`./app-install.sh` finns kvar som enda root-installerare för Linux/Codespaces och vidarebefordrar till `scripts/codespaces/`.

Installera från repositoryts rot:

```bash
bash ./app-install.sh
```

Starta från Codespaces-skriptkatalogen:

```bash
cd scripts/codespaces
bash start.sh
```

AppHost startas i förgrunden. Avsluta med `Ctrl+C`. OCR-språkdata kan hämtas separat med:

```bash
bash scripts/linux/setup-tesseract.sh
