# Raspberry Pi Installationsguide för Privatekonomi

Denna guide beskriver hur du installerar och kör Privatekonomi på en Raspberry Pi för att ha din privatekonomi-applikation körande lokalt hemma med persistent datalagring.

## Innehållsförteckning

1. [Förutsättningar](#förutsättningar)
2. [Hårdvarukrav](#hårdvarukrav)
3. [Installation av Raspberry Pi OS](#installation-av-raspberry-pi-os)
4. [Installation av .NET 9](#installation-av-net-9)
5. [Installation av Privatekonomi](#installation-av-privatekonomi)
6. [Konfiguration för Persistent Lagring](#konfiguration-för-persistent-lagring)
7. [Automatisk Start vid Omstart](#automatisk-start-vid-omstart)
8. [Åtkomst från Andra Enheter](#åtkomst-från-andra-enheter)
9. [Backup och Återställning](#backup-och-återställning)
10. [Felsökning](#felsökning)

## Förutsättningar

- Raspberry Pi 4 Model B (rekommenderat minst 4GB RAM)
- MicroSD-kort (minst 32GB, rekommenderat Class 10 eller bättre)
- Strömförsörjning för Raspberry Pi (officiell adapter rekommenderas)
- Internetanslutning (WiFi eller Ethernet)
- Dator för initial konfiguration

## Hårdvarukrav

### Minimum
- **Raspberry Pi:** 3 Model B+ eller senare
- **RAM:** 2GB (4GB rekommenderat)
- **Lagring:** 16GB microSD-kort
- **Nätverk:** WiFi eller Ethernet

### Rekommenderat
- **Raspberry Pi:** 4 Model B eller 5
- **RAM:** 4GB eller 8GB
- **Lagring:** 32GB eller större microSD-kort (Class 10 eller U3)
- **Nätverk:** Gigabit Ethernet för bästa prestanda
- **Extern lagring:** USB-hårddisk eller NAS för backuper

## Installation av Raspberry Pi OS

### 1. Ladda ner Raspberry Pi Imager

Ladda ner och installera Raspberry Pi Imager från [raspberrypi.com/software](https://www.raspberrypi.com/software/)

### 2. Installera OS

1. Sätt i microSD-kortet i din dator
2. Öppna Raspberry Pi Imager
3. Välj:
   - **OS:** Raspberry Pi OS (64-bit) Desktop eller Lite
   - **Storage:** Ditt microSD-kort
4. Klicka på kugghjulsikonen (⚙️) för avancerade inställningar:
   - Aktivera SSH
   - Ställ in användarnamn och lösenord
   - Konfigurera WiFi (valfritt)
   - Ställ in tidzon
5. Klicka "Write" och vänta på att processen slutförs

### 3. Första Uppstart

1. Sätt i microSD-kortet i Raspberry Pi
2. Anslut nätverk (Ethernet eller WiFi)
3. Anslut ström
4. Vänta några minuter på att systemet startar

### 4. Anslut via SSH (om du använder Lite version)

Använd Windows PowerShell (stöd från windows 10), putty eller bash för att ansluta via SSH till din pi. 

```bash
ssh pi@raspberrypi.local
# Eller använd IP-adressen om .local inte fungerar
ssh pi@192.168.1.XXX
```

Där `pi` är användarnamnet du konfigurerat för din raspberry pi. 

Standard användarnamn och lösenord är det du konfigurerade i Imager.

(Det kan vara så att man missade aktivera SSH i installationen, då behöver det aktiveras via Rasperry pi start -> Preferences -> Control center -> Interfaces -> SSH)

## Installation av .NET 9

### 1. Uppdatera Raspberry Pi-systemet

Uppdatera din Raspberry pi till senaste versionerna med kommandona: 

```bash
# Anslut och kntrollera om det finns uppdateringar
sudo apt update
# Uppdatera till senaste versionerna
sudo apt upgrade -y
```

### 2. Installera .NET 9 SDK

Installera nu .NET 9 eller nyare.

```bash
# Ladda ner och installera .NET 9
# OBS: Verifiera alltid nedladdade skript innan körning
curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
# Granska skriptet om du vill: cat /tmp/dotnet-install.sh
bash /tmp/dotnet-install.sh --channel 9.0

# Lägg till .NET i PATH
echo 'export DOTNET_ROOT=$HOME/.dotnet' >> ~/.bashrc
echo 'export PATH=$PATH:$HOME/.dotnet' >> ~/.bashrc
source ~/.bashrc
```

### 3. Verifiera installation

```bash
dotnet --version
# Ska visa 9.0.x
```

## Installation av Privatekonomi

### 1. Klona repositoriet

```bash
cd ~
git clone https://github.com/pownas/Privatekonomi.git
cd Privatekonomi
```

### 2. Bygg applikationen

```bash
dotnet build -c Release
```

Man kan också behöva skapa en NuGet.Config om det är första gången via: 

```bash
nano /home/[PIUSERNAME]/.nuget/NuGet/NuGet.Config
```
Där `[PIUSERNAME]` är ditt användarnamn på din raspberry pi. 

Klistra sedan in denna standard NuGet.Config XML-kod: 

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

Spara och avsluta nano:  
Tryck CTRL+O (spara - Write Out), Enter, och sedan CTRL+X (avsluta).

Behöver sedan återställa NuGet-paket och Aspire-beroenden: 
```bash
dotnet restore
```

### 3. Skapa datakatalog

```bash
mkdir -p ~/privatekonomi-data
```

## Konfiguration för Persistent Lagring

### Alternativ 1: SQLite (Rekommenderat för Raspberry Pi)

Skapa en konfigurationsfil för produktion:

```bash
cd ~/Privatekonomi/src/Privatekonomi.Web
nano appsettings.Production.json
```

Lägg in följande innehåll:

```json
{
  "Storage": {
    "Provider": "Sqlite",
    "ConnectionString": "Data Source=/home/pi/privatekonomi-data/privatekonomi.db",
    "SeedTestData": false
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Urls": "http://0.0.0.0:5274",
  "AllowedHosts": "*"
}
```

**Fördelar:**
- Enkel konfiguration
- Låg resursanvändning
- Perfekt för hemmabruk
- Snabb prestanda

### Alternativ 2: JsonFile (Med automatisk sparning var 5:e minut)

```json
{
  "Storage": {
    "Provider": "JsonFile",
    "ConnectionString": "/home/pi/privatekonomi-data",
    "SeedTestData": false
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Urls": "http://0.0.0.0:5274",
  "AllowedHosts": "*"
}
```

**Fördelar:**
- Lätt att backa upp (kopiera JSON-filer)
- Lätt att inspektera data manuellt
- Automatisk sparning var 5:e minut
- Kan läsas och redigeras i textredigerare

### Alternativ 3: Extern lagring på NAS

Om du har en NAS kan du montera den och använda den för lagring:

```bash
# Skapa monteringspunkt
sudo mkdir -p /mnt/nas

# För SMB/CIFS: Skapa en säker credentials-fil först
sudo mkdir -p /etc/smbcredentials
sudo nano /etc/smbcredentials/nas.cred
# Lägg till i filen:
# username=dinUser
# password=dittLösenord
sudo chmod 600 /etc/smbcredentials/nas.cred

# Montera NAS (exempel för NFS)
sudo mount -t nfs 192.168.1.100:/volume1/privatekonomi /mnt/nas

# Eller för SMB/CIFS med credentials-fil
sudo mount -t cifs //192.168.1.100/privatekonomi /mnt/nas -o credentials=/etc/smbcredentials/nas.cred

# Gör monteringen permanent (lägg till i /etc/fstab)
sudo nano /etc/fstab
# För NFS, lägg till:
# 192.168.1.100:/volume1/privatekonomi /mnt/nas nfs defaults 0 0
# För CIFS, lägg till:
# //192.168.1.100/privatekonomi /mnt/nas cifs credentials=/etc/smbcredentials/nas.cred 0 0
```

Använd sedan `/mnt/nas/privatekonomi.db` som ConnectionString.

## Automatisk Start vid Omstart

### 1. Skapa systemd service

```bash
sudo nano /etc/systemd/system/privatekonomi.service
```

Lägg in följande innehåll:

```ini
[Unit]
Description=Privatekonomi Web Application
After=network.target

[Service]
Type=notify
User=pi
WorkingDirectory=/home/pi/Privatekonomi/src/Privatekonomi.Web
Environment="ASPNETCORE_ENVIRONMENT=Production"
Environment="DOTNET_ROOT=/home/pi/.dotnet"
ExecStart=/home/pi/.dotnet/dotnet run --no-build -c Release
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=privatekonomi
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target
```

### 2. Aktivera och starta service

```bash
# Ladda om systemd
sudo systemctl daemon-reload

# Aktivera service att starta vid boot
sudo systemctl enable privatekonomi

# Starta service nu
sudo systemctl start privatekonomi

# Kontrollera status
sudo systemctl status privatekonomi
```

### 3. Hantera service

```bash
# Stoppa applikationen
sudo systemctl stop privatekonomi

# Starta om applikationen
sudo systemctl restart privatekonomi

# Se loggar
sudo journalctl -u privatekonomi -f
```

## Åtkomst från Andra Enheter

Privatekonomi är automatiskt konfigurerat för att vara tillgängligt från alla enheter på ditt lokala nätverk.

### 1. Hitta Raspberry Pi:s IP-adress

```bash
hostname -I
# Exempel output: 192.168.1.100
```

### 2. Åtkomst från olika enheter

#### Desktop (Windows/Mac/Linux)
Öppna webbläsaren och navigera till:

```
http://192.168.1.100:5274
```

#### Smartphone/Surfplatta (iOS/Android)
1. Anslut till samma WiFi-nätverk som Raspberry Pi
2. Öppna webbläsare (Safari/Chrome)
3. Navigera till: `http://192.168.1.100:5274`
4. **Rekommenderat:** Installera som PWA (Progressive Web App)
   - iOS: Dela → "Lägg till på hemskärmen"
   - Android: Meny → "Lägg till på startskärmen"

#### Via Nginx Reverse Proxy (Rekommenderat)
Om du har konfigurerat Nginx:

```
http://192.168.1.100          # HTTP
https://192.168.1.100         # HTTPS (om SSL konfigurerat)
```

**Fördelar med Nginx:**
- Enklare URL (ingen port behövs)
- SSL/HTTPS-stöd
- Bättre säkerhet
- Enhetlig åtkomstpunkt

### 3. Statisk IP-adress (Rekommenderat)

För att undvika att IP-adressen ändras:

**Alternativ A: DHCP-reservation i router (Enklast)**
1. Logga in på din router (vanligtvis `192.168.1.1`)
2. Hitta DHCP-inställningar
3. Skapa reservation för Raspberry Pi MAC-adress
4. Tilldela önskad IP (t.ex. `192.168.1.100`)

**Alternativ B: Statisk IP på Raspberry Pi**
```bash
# Under installation
./raspberry-pi-install.sh
# Välj att konfigurera statisk IP

# Eller manuellt
sudo nano /etc/dhcpcd.conf
```

Lägg till i slutet av filen:
```
interface eth0
static ip_address=192.168.1.100/24
static routers=192.168.1.1
static domain_name_servers=192.168.1.1 8.8.8.8
```

Starta om nätverkstjänsten:
```bash
sudo systemctl restart dhcpcd
```

### 4. DNS-namn (Valfritt)

**mDNS (fungerar automatiskt på Mac/Linux):**
```
http://raspberrypi.local:5274
```

**Anpassat hostname:**
```bash
sudo raspi-config
# System Options → Hostname → "privatekonomi"
sudo reboot
```

Sedan:
```
http://privatekonomi.local:5274
```

**Router DNS-konfiguration:**
Konfigurera i din router för att använda eget namn:
```
http://privatekonomi:5274
```

### 5. Nginx Reverse Proxy (Rekommenderat för produktion)

För bästa användarupplevelse, konfigurera Nginx:

```bash
./raspberry-pi-install.sh
# Välj att installera och konfigurera Nginx
```

**Åtkomst efter Nginx-konfiguration:**
```
http://192.168.1.100       # Web App
http://192.168.1.100/api/  # API
```

**Med SSL:**
```
https://192.168.1.100
```

Se [Nginx & SSL Guide](RASPBERRY_PI_NGINX_SSL.md) för detaljerad konfiguration.

### 6. Felsökning Nätverksåtkomst

Om du inte kan nå applikationen från andra enheter:

**Snabbdiagnostik:**
```bash
cd ~/Privatekonomi
./raspberry-pi-debug.sh
```

**Vanliga problem:**
- ❌ Portar lyssnar på `127.0.0.1` istället för `0.0.0.0`
  - Lösning: Kör om installation med `./raspberry-pi-install.sh`
  
- ❌ Brandväggen blockerar
  - Lösning: `sudo ufw allow 5274/tcp`
  
- ❌ WiFi-isolering aktiverad på router
  - Lösning: Inaktivera AP/WiFi-isolering i router-inställningar
  
- ❌ Enheter på olika nätverk
  - Lösning: Anslut alla enheter till samma WiFi

**Omfattande felsökningsguider:**
- [Network Troubleshooting Guide](RASPBERRY_PI_NETWORK_TROUBLESHOOTING.md) - Detaljerad felsökning
- [Device Testing Guide](RASPBERRY_PI_DEVICE_TESTING.md) - Testa från olika enheter
- [Network Access Guide](RASPBERRY_PI_NETWORK_ACCESS.md) - Nätverkskonfiguration

## Backup och Återställning

### Automatisk Backup med Cron

#### 1. Skapa backup-script

```bash
mkdir -p ~/scripts
nano ~/scripts/backup-privatekonomi.sh
```

Lägg in följande:

```bash
#!/bin/bash

# Backup directory
BACKUP_DIR=~/privatekonomi-backups
DATA_DIR=~/privatekonomi-data
DATE=$(date +%Y%m%d_%H%M%S)

# Skapa backup directory om det inte finns
mkdir -p $BACKUP_DIR

# För SQLite
if [ -f "$DATA_DIR/privatekonomi.db" ]; then
    cp "$DATA_DIR/privatekonomi.db" "$BACKUP_DIR/privatekonomi_$DATE.db"
    echo "SQLite backup skapad: $BACKUP_DIR/privatekonomi_$DATE.db"
fi

# För JsonFile
if [ -d "$DATA_DIR" ] && [ "$(ls -A $DATA_DIR/*.json 2>/dev/null)" ]; then
    tar -czf "$BACKUP_DIR/privatekonomi_json_$DATE.tar.gz" -C "$DATA_DIR" .
    echo "JSON backup skapad: $BACKUP_DIR/privatekonomi_json_$DATE.tar.gz"
fi

# Ta bort backuper äldre än 750 dagar (ca 2 år)
find $BACKUP_DIR -name "privatekonomi_*" -type f -mtime +750 -delete

echo "Backup klar: $(date)"
```

Gör scriptet körbart:

```bash
chmod +x ~/scripts/backup-privatekonomi.sh
```

#### 2. Schemalägg med cron

```bash
crontab -e
```

Lägg till (kör backup varje dag kl 02:00):

```
0 2 * * * /home/pi/scripts/backup-privatekonomi.sh >> /home/pi/backup.log 2>&1
```

### Manuel Backup via UI

1. Öppna Privatekonomi i webbläsaren
2. Navigera till "Datahantering" i menyn
3. Klicka på "Exportera Full Backup"
4. Spara filen på en säker plats (extern disk, molntjänst, etc.)

### Återställning från Backup

#### Via UI (Rekommenderat)

1. Öppna Privatekonomi i webbläsaren
2. Navigera till "Datahantering"
3. Välj backup-fil under "Importera Data"
4. Välj om du vill sammanfoga eller ersätta befintlig data
5. Klicka "Importera Backup"

#### Manuellt (SQLite)

```bash
# Stoppa applikationen
sudo systemctl stop privatekonomi

# Återställ databasen
cp ~/privatekonomi-backups/privatekonomi_YYYYMMDD_HHMMSS.db ~/privatekonomi-data/privatekonomi.db

# Starta applikationen
sudo systemctl start privatekonomi
```

#### Manuellt (JsonFile)

```bash
# Stoppa applikationen
sudo systemctl stop privatekonomi

# Återställ JSON-filer
tar -xzf ~/privatekonomi-backups/privatekonomi_json_YYYYMMDD_HHMMSS.tar.gz -C ~/privatekonomi-data

# Starta applikationen
sudo systemctl start privatekonomi
```

## Felsökning

### Applikationen startar inte

**Kontrollera status:**
```bash
sudo systemctl status privatekonomi
sudo journalctl -u privatekonomi -n 50
```

**Vanliga problem:**
- Port 5274 redan används: Ändra port i appsettings.json
- .NET inte installerat korrekt: Verifiera med `dotnet --version`
- Behörighetsproblem: Kontrollera att användaren 'pi' äger datakataloger

### Kan inte komma åt från andra enheter

**Kontrollera:**
1. Raspberry Pi:s IP-adress: `hostname -I`
2. Brandvägg (om aktiverad): `sudo ufw status`
3. Port är öppen: `netstat -tuln | grep 5274`

**Tillåt trafik genom brandvägg:**
```bash
sudo ufw allow 5274/tcp
```

### Långsam prestanda

**Optimeringar:**
1. **Öka swap-storlek** (för Pi med mindre RAM):
   ```bash
   sudo dphys-swapfile swapoff
   sudo nano /etc/dphys-swapfile
   # Ändra CONF_SWAPSIZE till 2048
   sudo dphys-swapfile setup
   sudo dphys-swapfile swapon
   ```

2. **Använd extern USB-lagring** istället för microSD för databas

3. **Övervaka resurser:**
   ```bash
   htop
   ```

### Databasfel

**För SQLite:**
```bash
# Kontrollera databas-integritet
sqlite3 ~/privatekonomi-data/privatekonomi.db "PRAGMA integrity_check;"

# Reparera om nödvändigt
sqlite3 ~/privatekonomi-data/privatekonomi.db "VACUUM;"
```

### Loggar

**Se realtidsloggar:**
```bash
sudo journalctl -u privatekonomi -f
```

**Se loggar från senaste timmen:**
```bash
sudo journalctl -u privatekonomi --since "1 hour ago"
```

**Exportera loggar:**
```bash
sudo journalctl -u privatekonomi > ~/privatekonomi-logs.txt
```

### CSV Import "Connection refused" fel

Om du får ett felmeddelande "Connection refused (localhost:7023)" när du försöker importera CSV-filer:

**Orsak:** Web-applikationen kan inte nå API-tjänsten.

**Lösning:**
1. Kontrollera att både Web och API tjänsterna körs:
   ```bash
   # Kontrollera vilka processer som lyssnar på port 5277 (API)
   sudo netstat -tuln | grep 5277
   
   # Kontrollera vilka processer som lyssnar på port 5274 (Web)
   sudo netstat -tuln | grep 5274
   ```

2. Verifiera API-konfigurationen i `appsettings.Production.json`:
   ```json
   {
     "ApiSettings": {
       "BaseUrl": "http://localhost:5277"
     }
   }
   ```

3. Starta om båda tjänsterna:
   ```bash
   cd ~/Privatekonomi
   ./raspberry-pi-start.sh
   ```

**OBS:** Sedan version 0.0.4+ är detta problem automatiskt åtgärdat genom korrekt konfiguration av API-URL:er.

## Säkerhet

### 1. Uppdatera regelbundet

```bash
sudo apt update && sudo apt upgrade -y
```

### 2. Använd brandvägg

```bash
sudo apt install ufw
sudo ufw enable
sudo ufw allow ssh
sudo ufw allow 5274/tcp
```

### 3. Använd HTTPS (Rekommenderat för produktion)

För att använda HTTPS med ett självsignerat certifikat:

```bash
# Skapa certifikat
cd ~/Privatekonomi/src/Privatekonomi.Web
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

Uppdatera `appsettings.Production.json`:
```json
{
  "Urls": "https://0.0.0.0:5275;http://0.0.0.0:5274"
}
```

### 4. Använd starka lösenord

Se till att du använder starka lösenord för:
- Raspberry Pi användarkonto (minst 12 tecken, blanda stora/små bokstäver, siffror och specialtecken)
- Privatekonomi-applikationen (minst 8 tecken enligt policy)
- NAS/extern lagring (följ samma rekommendation som för Pi-kontot)

**Exempel på starka lösenord:**
- Använd en lösenordshanterare (rekommenderat)
- Kombinera ord med siffror och symboler: `Kaffe42!Solsken#2024`
- Undvik vanliga ord, födelsedatum eller enkla sekvenser

## Prestandarekommendationer

### Raspberry Pi 3
- Använd SQLite (inte JsonFile)
- Begränsa testdata
- Använd extern USB-lagring för databas
- Begränsa antalet samtidiga användare till 1-2

### Raspberry Pi 4 (4GB+)
- SQLite eller JsonFile fungerar bra
- Kan hantera 3-5 samtidiga användare
- Behöver inte extern lagring (men rekommenderas för backuper)

### Raspberry Pi 5
- Alla lagringsalternativ fungerar utmärkt
- Kan hantera 5+ samtidiga användare
- Snabb prestanda även med stor datamängd

## Support och Ytterligare Hjälp

- **GitHub Issues:** [github.com/pownas/Privatekonomi/issues](https://github.com/pownas/Privatekonomi/issues)
- **Dokumentation:** Se projektets README och wiki
- **Raspberry Pi Forum:** [raspberrypi.com/forums](https://www.raspberrypi.com/forums)

## Checklista för Installation

- [ ] Raspberry Pi OS installerat
- [ ] .NET 9 installerat och verifierat
- [ ] Privatekonomi klonad och byggd
- [ ] Datakatalog skapad
- [ ] appsettings.Production.json konfigurerad
- [ ] Systemd service skapad och aktiverad
- [ ] Applikationen startar och är åtkomlig
- [ ] Statisk IP konfigurerad
- [ ] Automatisk backup konfigurerad
- [ ] Första backup-export genomförd och sparad säkert
- [ ] Brandvägg konfigurerad (om tillämpligt)

## Sammanfattning

Du har nu en fullt fungerande Privatekonomi-installation på din Raspberry Pi med:
- ✅ Persistent datalagring (SQLite eller JsonFile)
- ✅ Automatisk start vid omstart
- ✅ Automatisk sparning var 5:e minut (för JsonFile)
- ✅ Åtkomst från alla enheter i nätverket
- ✅ Automatiska backuper
- ✅ Enkel import/export via webgränssnittet

Din ekonomidata är nu säker, tillgänglig och körande lokalt på din egen hårdvara!
