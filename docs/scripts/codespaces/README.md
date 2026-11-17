# Linux och GitHub Codespaces

De befintliga rootkommandona `./app-install.sh` och `./app-start.sh` finns kvar för bakåtkompatibilitet. De vidarebefordrar till `scripts/codespaces/`.

Från repositoryts rot:

```bash
./app-install.sh
./app-start.sh
```

AppHost startas i förgrunden. Avsluta med `Ctrl+C`. OCR-språkdata kan hämtas separat med:

```bash
bash scripts/linux/setup-tesseract.sh
```
