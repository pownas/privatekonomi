# Raspberry Pi Installation Test Guide

Detta dokument beskriver hur du kan testa Raspberry Pi-installationsskriptet.

## Förutsättningar för test

### På en riktig Raspberry Pi:
- Raspberry Pi OS (Debian-baserat)  
- Internetanslutning
- Minst 1GB RAM (rekommenderat)
- Minst 4GB ledigt diskutrymme

### På andra Linux-system (för test):
- Ubuntu, Debian eller annan Linux-distribution
- curl och git installerat
- Internetanslutning

## Testning av installationsskriptet

### 1. Lokal test (om du har klonat repot):

```bash
cd /workspaces/Privatekonomi
bash setup-raspberry-pi.sh --help

# Testrun (skippa systemd service och firewall)
bash setup-raspberry-pi.sh --no-service --no-firewall
```

### 2. Test från en ren klon:

```bash
git clone https://github.com/pownas/Privatekonomi.git Privatekonomi-setup
cd Privatekonomi-setup
bash setup-raspberry-pi.sh
```

### 3. Test på Raspberry Pi:

```bash
# SSH till din Raspberry Pi
ssh pi@<raspberry-pi-ip>

# Hämta repot och starta installeraren med godkännande
git clone https://github.com/pownas/Privatekonomi.git Privatekonomi-setup
cd Privatekonomi-setup
bash setup-raspberry-pi.sh

# Efter installation
cd ~/privatekonomi/scripts/raspberry-pi
bash start.sh
```

## Vad skriptet gör

1. **Systemkontroll**: Verifierar Raspberry Pi-miljö och systemkrav
2. **Dependencies**: Installerar git, curl om de saknas
3. **NuGet Config**: Skapar NuGet.Config om den saknas
4. **.NET-installation**: Laddar ner och installerar .NET 11 SDK
5. **Projekt Setup**: Klonar/uppdaterar Privatekonomi repository
6. **NuGet-återställning**: Återställer projektets paket
7. **Storage Config**: Interaktivt val av SQLite eller JsonFile
8. **appsettings.json**: Skapar Production-konfiguration automatiskt
9. **EF Tools**: Installerar Entity Framework CLI-verktyg
10. **Certificates**: Konfigurerar utvecklingscertifikat
11. **Build**: Bygger hela projektet
12. **Swap Optimization**: (Valfritt) Ökar swap för bättre prestanda
13. **Systemd Service**: (Valfritt) Skapar automatisk start-tjänst
14. **Firewall**: (Valfritt) Konfigurerar UFW för portar 17127 och 5274
15. **Backup Script**: (Valfritt) Skapar dagligt backup-script med cron
16. **Static IP**: (Valfritt) Konfigurerar statisk IP-adress
17. **Verification**: Testar att allt fungerar

## Förväntad output

Skriptet visar färgad output för varje steg:
- 🔵 **[INFO]**: Allmän information
- 🟢 **[SUCCESS]**: Framgångsrika operationer  
- 🟡 **[WARNING]**: Varningar (inte kritiska)
- 🔴 **[ERROR]**: Fel som stoppar installationen

## Efter installation

Applikationen kommer att vara tillgänglig på:
- **Lokalt**: `http://localhost:17127`
- **Från andra enheter**: `http://[raspberry-pi-ip]:17127`

### Starta manuellt:
```bash
cd ~/privatekonomi/scripts/raspberry-pi
bash start.sh
```

### Med systemd (om installerat):
```bash
sudo systemctl start privatekonomi
sudo systemctl status privatekonomi
```

## Felsökning

### Problem med .NET installation:
```bash
# Kontrollera .NET
~/.dotnet/dotnet --version

# Lägg till i PATH
export PATH="$PATH:$HOME/.dotnet"
```

### Problem med EF tools:
```bash
# Kontrollera EF tools
~/.dotnet/tools/dotnet-ef --version

# Lägg till tools i PATH  
export PATH="$PATH:$HOME/.dotnet/tools"
```

### Problem med port access:
```bash
# Kontrollera att port lyssnar
ss -lntp | grep 17127

# Kontrollera Raspberry Pi IP
hostname -I
```

### Logs för systemd service:
```bash
journalctl -u privatekonomi -f
```

## Säkerhetsmässigt

Installationsskriptet:
- Kör ALDRIG som root (använder sudo endast vid behov)
- Installerar allt i användarens hemkatalog
- Skapar inga säkerhetsrisker
- Endast port 17127 exponeras (valfritt)

## Test checklist

- [ ] Skriptet körs utan fel
- [ ] .NET 9 installeras korrekt
- [ ] NuGet.Config skapas (om den saknades)
- [ ] EF tools fungerar (`dotnet-ef --version`)
- [ ] Workloads återställs framgångsrikt
- [ ] Lagringsval (SQLite/JsonFile) presenteras
- [ ] appsettings.Production.json skapas korrekt
- [ ] Datakatalog skapas (~/ privatekonomi-data)
- [ ] Backup-katalog skapas (~/privatekonomi-backups)
- [ ] Projektet byggs framgångsrikt
- [ ] Raspberry Pi-startskriptet fungerar
- [ ] Applikationen är åtkomlig via webbläsare
- [ ] Aspire Dashboard visar tjänster korrekt (port 17127)
- [ ] Web app är åtkomlig (port 5274)
- [ ] Systemd service startar (om installerat)
- [ ] Port 17127 och 5274 är åtkomliga från andra enheter
- [ ] Backup-script skapas (om valt)
- [ ] Cron-jobb schemaläggs (om valt)
- [ ] Statisk IP konfigureras (om valt)
- [ ] UFW brandvägg konfigureras korrekt (om valt)
- [ ] Swap-optimering fungerar (om valt)