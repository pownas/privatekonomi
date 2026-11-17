# Linux och GitHub Codespaces

Kör den bekräftelse-gated installeraren från repositoryts rot:

```bash
bash setup-codespaces.sh
```

Startaren visar dokumentationen och implementationsskriptet `scripts/codespaces/install.sh` och frågar innan installationen startar.

Starta från Codespaces-skriptkatalogen:

```bash
cd scripts/codespaces
bash start.sh
```

AppHost startas i förgrunden. Avsluta med `Ctrl+C`. OCR-språkdata körs med `cd scripts/linux` och `bash setup-tesseract.sh`.
