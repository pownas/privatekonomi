# Release Pipeline - Snabbreferens

Snabbguide för vanliga deployment-uppgifter i Privatekonomi release pipeline.

## Skapa en Ny Release

### Via Kommandoraden
```bash
# Kontrollera att releasen utgår från en granskad commit/tagg
git status --short
git log -1 --oneline

# Skapa version tag (semantic versioning)
git tag -a v1.2.3 -m "Release 1.2.3: Beskrivning av ändringar"

# Pusha till GitHub (startar pipelinen automatiskt)
git push origin v1.2.3
```

### Via GitHub Web
1. Gå till repository → **Releases** → **Draft a new release**
2. Skriv tag: `v1.2.3`
3. Target: `main`
4. Titel: `Release 1.2.3`
5. Klicka **Publish release**

## Övervaka Deployment

### GitHub Actions
```
Repository → Actions → Release and Deploy to Web Hosting
```

Status-indikatorer:
- 🟡 **Gul prick:** Pågående
- ✅ **Grön check:** Lyckades
- ❌ **Röd X:** Misslyckades

### Deployment-steg
1. **Build** (~5-10 min)
   - Bygger .NET applikation
   - Kör tester
   - Skapar publiceringspaket

2. **Deploy** (~2-5 min)
   - Laddar upp via SFTP
   - Deployas till server

3. **Create Release** (~1-2 min)
   - Skapar GitHub Release
   - Genererar release notes
   - Bifogar installationspaket

## GitHub Secrets (En gång setup)

Gå till: **Settings** → **Secrets and variables** → **Actions**

### Required Secrets
```
SFTP_HOST           = ftp.example.com
SFTP_USERNAME       = your_username
SFTP_PASSWORD       = your_password
SFTP_PORT           = porten för FTPS-servern
SFTP_WEB_DIR        = sökväg för Web-publiceringen
SFTP_API_DIR        = sökväg för API-publiceringen
PRODUCTION_URL      = https://privatekonomi.example.com
```

Workflowen publicerar Web och API separat. Konfigurera även `MYSQL_CONNECTION_STRING`
om deploymentens MySQL-steg används. Exakta workflowvillkor och paketversioner
finns i [release-deploy.yml](../.github/workflows/release-deploy.yml); använd inte
en gemensam mål-katalog för Web och API.

## Verifiera Deployment

### 1. På GitHub
```
Actions → Senaste workflow → Kontrollera gröna checkmarks
```

### 2. På Servern
```bash
# SSH till servern
ssh username@host

# Kontrollera filer
cd /var/www/privatekonomi/
ls -la
cat DEPLOYMENT_INFO.txt

# Starta applikationen (om inte redan igång)
./Privatekonomi.Web
```

### 3. I Webbläsaren
```
http://your_host:5000
eller
https://your_domain.com
```

## Vanliga Kommandon

### Lokalt: Bygga och Testa
```bash
# Full build
dotnet build --configuration Release

# Kör tester
dotnet test --configuration Release

# Publicera lokalt (test)
dotnet publish src/Privatekonomi.Web/Privatekonomi.Web.csproj \
  --configuration Release \
  --output ./publish \
  --runtime linux-x64
```

### Server: Hantera Applikationen
```bash
# Visa status
systemctl status privatekonomi

# Stoppa
systemctl stop privatekonomi

# Starta
systemctl start privatekonomi

# Starta om
systemctl restart privatekonomi

# Visa loggar
journalctl -u privatekonomi -n 50 -f
```

### SFTP: Manuell Uppladdning (nödsituation)
```bash
# Anslut med FTPS
lftp -u username ftps://host:21

# Anslut med SFTP
sftp -P 22 username@host

# Ladda upp fil
put local_file.dll /var/www/privatekonomi/local_file.dll

# Ladda upp katalog
mirror -R ./publish/ /var/www/privatekonomi/
```

## Felsökning

### Build Misslyckas
```bash
# Kör lokalt för att se fel
dotnet build --configuration Release

# Kontrollera tester
dotnet test --configuration Release --verbosity detailed
```

### SFTP Anslutning Misslyckas
```bash
# Testa anslutning manuellt
lftp -u username -e "ls; quit" ftps://host:21

# Eller med SFTP
sftp -P 22 username@host
```

### Applikation Startar Inte
```bash
# Kontrollera .NET runtime
dotnet --list-runtimes

# Kontrollera fil-behörigheter
ls -la /var/www/privatekonomi/

# Testa starta manuellt
cd /var/www/privatekonomi/
./Privatekonomi.Web

# Kontrollera loggar
journalctl -u privatekonomi -n 100 --no-pager
```

## Rollback (Återställning)

### Metod 1: Skapa release från tidigare version
```bash
# Hitta tidigare fungerande version
git tag -l

# Skapa ny release från den
git tag -a v1.2.4 v1.2.2^{}
git push origin v1.2.4
```

### Metod 2: Manuell återställning
```bash
# Ladda ner tidigare release från GitHub
wget https://github.com/user/repo/releases/download/v1.2.2/privatekonomi-v1.2.2-linux-x64.tar.gz

# Extrahera
tar -xzf privatekonomi-v1.2.2-linux-x64.tar.gz

# Ladda upp via SFTP (se ovan)
```

### Metod 3: Databas-återställning
```bash
# SQLite backup
cp backup/privatekonomi-20250109.db privatekonomi.db

# Starta om applikationen
systemctl restart privatekonomi
```

## Backup

### Skapa Backup
```bash
# Backup databas
cp /var/www/privatekonomi/privatekonomi.db ~/backups/privatekonomi-$(date +%Y%m%d).db

# Backup konfiguration
cp /var/www/privatekonomi/appsettings.json ~/backups/appsettings-$(date +%Y%m%d).json

# Backup hela applikationen
tar -czf ~/backups/privatekonomi-full-$(date +%Y%m%d).tar.gz /var/www/privatekonomi/
```

### Återställ Backup
```bash
# Återställ databas
cp ~/backups/privatekonomi-20250109.db /var/www/privatekonomi/privatekonomi.db

# Starta om
systemctl restart privatekonomi
```

## Versioning (Semantic Versioning)

Format: `vMAJOR.MINOR.PATCH`

- **MAJOR:** Breaking changes (v1.0.0 → v2.0.0)
- **MINOR:** Nya funktioner, bakåtkompatibla (v1.0.0 → v1.1.0)
- **PATCH:** Buggfixar, bakåtkompatibla (v1.0.0 → v1.0.1)

Exempel:
```bash
# Större release med breaking changes
git tag -a v2.0.0 -m "Release 2.0.0: Ny autentiseringsmodul"

# Ny funktion
git tag -a v1.5.0 -m "Release 1.5.0: Lägg till export-funktion"

# Buggfix
git tag -a v1.4.1 -m "Release 1.4.1: Fix transaction delete bug"
```

## Pre-release Versioner

För beta/alpha releases:
```bash
# Beta release
git tag -a v2.0.0-beta.1 -m "Release 2.0.0 Beta 1"

# Alpha release
git tag -a v2.0.0-alpha.1 -m "Release 2.0.0 Alpha 1"

# Release candidate
git tag -a v2.0.0-rc.1 -m "Release 2.0.0 RC 1"
```

**Varning:** Workflowens `v*.*.*`-taggfilter matchar även beta-, alfa- och
release-candidate-taggar med samma punktstruktur. Sådana taggar kan därför
starta deployment. Pusha inte en pre-release-tagg innan du verifierat aktuell
workflow och avsett målmiljö.

## Säkerhetschecklist

Innan varje release:

- [ ] Kör alla tester lokalt
- [ ] Kontrollera säkerhetssårbarheter: `dotnet list package --vulnerable`
- [ ] Uppdatera dependencies om nödvändigt
- [ ] Granska kod-ändringar
- [ ] Testa i staging-miljö (om tillgänglig)
- [ ] Backup av produktionsdatabas
- [ ] Säkerställ att rollback-plan finns
- [ ] Verifiera att GitHub Secrets är aktuella
- [ ] Kontrollera diskutrymme på server
- [ ] Informera användare om eventuell downtime

## Kontakt och Support

- **Dokumentation:** [docs/DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md)
- **Issues:** [GitHub Issues](https://github.com/pownas/Privatekonomi/issues)
- **Felsökning:** [docs/ISSUE_EXAMPLES.md](ISSUE_EXAMPLES.md)

---

**Version:** 1.0.0  
**Senast uppdaterad:** 2025-11-09
