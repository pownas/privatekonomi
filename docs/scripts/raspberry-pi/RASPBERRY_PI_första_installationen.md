# Raspberry Pi Installation och Konfiguration

För nästa steg efter installationen – familjekonton, säker delning på hemnätverket, backup och SQLite-systemadministratör – se [Kom igång och dela Privatekonomi hemma](../../FAMILY_HOME_SETUP.md).

## 🚀 Automatisk Installation (Rekommenderat)

**Enklaste sättet:** Använd det automatiserade installationsskriptet som hanterar hela processen:

```bash
# Klona repositoryt och kör den godkännandestyrda root-startaren
git clone https://github.com/pownas/Privatekonomi.git Privatekonomi-setup
cd Privatekonomi-setup
bash setup-raspberry-pi.sh
```

Installationsskriptet hanterar:
- ✅ Kontroll av systemkrav och Raspberry Pi-miljö
- ✅ Installation av .NET 11 SDK
- ✅ Skapande av NuGet.Config om det saknas
- ✅ Konfiguration av PATH och miljövariabler
- ✅ Kloning/uppdatering av Privatekonomi-projekt
- ✅ Återställning av NuGet-paket och Aspire-beroenden
- ✅ **Publicering för linux-arm64 med self-contained binärer (NYTT)**
- ✅ Val av lagringsalternativ (SQLite/JsonFile)
- ✅ Automatisk skapande av appsettings.Production.json
- ✅ Skapande av datakatalog och backup-katalog
- ✅ Installation av Entity Framework CLI-verktyg
- ✅ Konfiguration av utvecklingscertifikat
- ✅ Byggning av applikationen
- ✅ **Nginx reverse proxy-konfiguration (NYTT)**
- ✅ **SSL/HTTPS med Let's Encrypt eller self-signed certifikat (NYTT)**
- ✅ Swap-optimering för system med lågt minne (valfri)
- ✅ Valfri systemd-tjänst för automatisk start
- ✅ Brandväggskonfiguration med UFW (valfri, inkluderar HTTP/HTTPS-portar)
- ✅ Automatiska dagliga backuper med cron (valfri)
- ✅ Statisk IP-konfiguration (valfri)
- ✅ Verifiering och användningsinstruktioner

**Kommandoradsalternativ:**
```bash
# Full interaktiv installation
bash setup-raspberry-pi.sh

# Automatisk installation utan interaktiva frågor
bash setup-raspberry-pi.sh --skip-interactive

# Anpassad installation (hoppa över vissa steg)
bash setup-raspberry-pi.sh --no-service --no-firewall --no-backup

# Installera utan publicering (använd dotnet run istället)
bash setup-raspberry-pi.sh --no-publish

# Installera utan Nginx reverse proxy
bash setup-raspberry-pi.sh --no-nginx

# Installera utan SSL/HTTPS
bash setup-raspberry-pi.sh --no-ssl

# Konfigurera endast SSL (för befintlig installation)
bash setup-raspberry-pi.sh --configure-ssl

# Visa hjälp
bash setup-raspberry-pi.sh --help
```

**Efter installation:**
```bash
cd ~/privatekonomi/scripts/raspberry-pi
bash start.sh
```

**Åtkomst till tjänsterna:**

Efter installation kommer följande tjänster att vara tillgängliga:

### Utan Nginx (Direktåtkomst)

| Tjänst | Port | Lokal åtkomst | Nätverksåtkomst |
|--------|------|---------------|-----------------|
| **Aspire Dashboard** | 17127 | `http://localhost:17127` | `http://[raspberry-pi-ip]:17127` |
| **Web App** | 5274 | `http://localhost:5274` | `http://[raspberry-pi-ip]:5274` |
| **API (Swagger)** | 5277 | `http://localhost:5277` | `http://[raspberry-pi-ip]:5277` |

### Med Nginx Reverse Proxy (Rekommenderat för produktion)

| Tjänst | URL | Beskrivning |
|--------|-----|-------------|
| **Web App** | `https://[domain-eller-ip]/` | Huvudapplikation via Nginx |
| **API** | `https://[domain-eller-ip]/api/` | API-endpoints via Nginx |
| **Aspire Dashboard** | `https://[domain-eller-ip]/aspire/` | Övervakningsdashboard (valfri) |
| **Health Check** | `https://[domain-eller-ip]/health` | Hälsokontroll endpoint |

**Fördelar med Nginx:**
- ✅ SSL/HTTPS-kryptering med Let's Encrypt (gratis certifikat)
- ✅ Enkel domänbaserad åtkomst
- ✅ Säkerhetsheaders (X-Frame-Options, CSP, etc.)
- ✅ Bättre prestanda med caching och buffering
- ✅ Centraliserad loggning och övervakning
- ✅ Reverse proxy för alla tjänster på port 80/443

**Hitta din Raspberry Pi IP-adress:**
```bash
hostname -I
# Exempel output: 192.168.1.100
```

**Kontrollera att portarna lyssnar:**

```bash
# Direktåtkomst (utan Nginx)
ss -lntp | grep '17127\|5274\|5277'
# Ska visa att alla tre portar lyssnar på 0.0.0.0 (alla nätverksinterfaces)

# Med Nginx
ss -lntp | grep ':80\|:443'
# Ska visa att Nginx lyssnar på port 80 och 443

# Kontrollera Nginx status
sudo systemctl status nginx

# Kontrollera Nginx-konfiguration
sudo nginx -t
```

**SSL/HTTPS-alternativ:**

1. **Let's Encrypt (Rekommenderat för produktionsmiljö)**
   - Gratis SSL-certifikat
   - Automatisk förnyelse
   - Kräver domännamn som pekar på din Raspberry Pi
    - Installeras automatiskt vid `bash setup-raspberry-pi.sh` om Nginx är aktiverat
    - Eller kör: `bash setup-raspberry-pi.sh --configure-ssl`

2. **Self-signed Certificate (För lokal användning)**
   - Fungerar utan domännamn
   - Webbläsare visar säkerhetsvarning (normalt)
   - Bra för privat hemmanätverk
   - Välj "self-signed" under SSL-konfigurationen

**Publicerade binärer:**

Installationsskriptet publicerar applikationen med följande optimeringar:
- `--runtime linux-arm64`: Optimerad för Raspberry Pi ARM64-arkitektur
- `--self-contained`: Inkluderar alla .NET-beroenden (ingen .NET runtime krävs)
- Snabbare uppstart jämfört med `dotnet run`
- Mindre resursanvändning
- Publicerade binärer sparas i: `~/Privatekonomi/publish/`

**Hoppa över publicering:**

För snabbare utveckling/testning kan du hoppa över publicering:
```bash
bash setup-raspberry-pi.sh --no-publish
# Systemd-tjänsten använder då "dotnet run" istället
```

## 📋 Manuell Installation (För referens)

### Installera Entity Framework på Raspberry Pi: 

```terminal
username@raspberrypi:~/Privatekonomi $ dotnet tool install --global dotnet-ef
Tools directory '/home/username/.dotnet/tools' is not currently on the PATH environment variable.
If you are using bash, you can add it to your profile by running the following command:

cat << \EOF >> ~/.bash_profile
# Add .NET Core SDK tools
export PATH="$PATH:/home/username/.dotnet/tools"
EOF

You can add it to the current session by running the following command:

export PATH="$PATH:/home/username/.dotnet/tools"

You can invoke the tool using the following command: dotnet-ef
Tool 'dotnet-ef' (version '9.0.10') was successfully installed.
username@raspberrypi:~/Privatekonomi $ cat << \EOF >> ~/.bash_profile
# Add .NET Core SDK tools
export PATH="$PATH:/home/username/.dotnet/tools"
EOF
username@raspberrypi:~/Privatekonomi $ export PATH="$PATH:/home/username/.dotnet/tools"


username@raspberrypi:~/Privatekonomi $ dotnet tool install --global dotnet-ef
Tool 'dotnet-ef' is already installed.
```

# Installera dev-cert
`dotnet dev-certs https --clean`

`dotnet dev-certs https --trust`

# Starta applikationen
```
cd "src/Privatekonomi.AppHost"
dotnet run
```



# Nå webbsidan via andra enheter på nätverket

Fråga: 
Hur öppnar jag porten: 17127 från min raspberry pi till andra enheter på nätverket för min dotnet applikation? 

--------- 

Kort svar: På Raspberry Pi behöver du normalt inte “öppna” portar lokalt. Det viktiga är att din .NET-app lyssnar på rätt nätverksinterface (inte bara localhost) och att ingen brandvägg blockerar. Gör så här:

1) Kontrollera att appen lyssnar på 0.0.0.0:17127
- Starta appen temporärt så att den lyssnar på alla nätverkskort:
  - ASP.NET Core:
    - Engångskörning:
      ```
      ASPNETCORE_URLS=http://0.0.0.0:17127 dotnet MyApp.dll
      ```
      eller
      ```
      dotnet run --urls "http://0.0.0.0:17127"
      ```
    - I kod (Program.cs) - **IMPLEMENTERAT**:
      ```csharp
      builder.WebHost.UseUrls("http://0.0.0.0:17127");
      ```
    - Via konfig (appsettings.Production.json) - **IMPLEMENTERAT**:
      ```json
      {
        "Kestrel": {
          "Endpoints": {
            "Http": {
              "Url": "http://0.0.0.0:17127"
            }
          }
        }
      }
      ```
      Denna fil skapas automatiskt av installationsskriptet för AppHost, Web och API.
  - Egen TCP/UDP-server: bind till 0.0.0.0 (eller Pi:ns LAN-IP) på port 17127.
- Verifiera att portarna lyssnar:
  ```bash
  ss -lntp | grep '17127\|5274\|5277'    # Kontrollera alla tre portar
  # Ska visa 0.0.0.0:XXXX (inte 127.0.0.1:XXXX)
  ```

2) Brandvägg (om ufw är aktiverad)
- Öppna TCP-portar för alla tjänster:
  ```bash
  sudo ufw status
  sudo ufw allow 17127/tcp              # Aspire Dashboard
  sudo ufw allow 5274/tcp               # Web App  
  sudo ufw allow 5277/tcp               # API
  sudo ufw reload
  sudo ufw status
  ```

2) Testa från annan enhet på samma nät
- Byt 192.168.x.y mot Pi:ns IP:
  ```
  # Testa Aspire Dashboard
  curl http://192.168.x.y:17127/
  
  # Testa Web App
  curl http://192.168.x.y:5274/
  
  # Testa API
  curl http://192.168.x.y:5277/health
  
  # Eller med netcat
  nc -vz 192.168.x.y 17127
  nc -vz 192.168.x.y 5274
  nc -vz 192.168.x.y 5277
  ```
- Kontrollera att alla tjänster lyssnar på alla interfaces (inte bara 127.0.0.1):
  ```
  ss -lntp | grep '17127\|5274\|5277'
  ```
  Ska visa 0.0.0.0:17127, 0.0.0.0:5274, 0.0.0.0:5277 eller [::]:17127 etc. (inte 127.0.0.1).

3) Köra som systemd-tjänst (rekommenderat)
- Automatiskt konfigurerat av installationsskriptet med alla nödvändiga miljövariabler.

```ini name=/etc/systemd/system/privatekonomi.service
[Unit]
Description=Privatekonomi Financial Management Application
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
WorkingDirectory=/home/pi/Privatekonomi/src/Privatekonomi.AppHost
ExecStart=/usr/bin/dotnet run
# Miljövariabler för Raspberry Pi-konfiguration
Environment=PRIVATEKONOMI_RASPBERRY_PI=true
Environment=ASPNETCORE_URLS=http://0.0.0.0:17127
Environment=DOTNET_DASHBOARD_URLS=http://0.0.0.0:17127
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=PRIVATEKONOMI_ENVIRONMENT=RaspberryPi
Restart=always
RestartSec=10
User=pi
Group=pi
# Säkrare filbehörigheter
UMask=0027

[Install]
WantedBy=multi-user.target
```

- Aktivera och starta:
  ```bash
  sudo systemctl daemon-reload
  sudo systemctl enable privatekonomi
  sudo systemctl start privatekonomi
  sudo systemctl status privatekonomi
  ```

4) Vanliga felsökningssteg
- **Tjänsterna lyssnar bara på localhost:** Kontrollera att miljövariabeln `PRIVATEKONOMI_RASPBERRY_PI=true` är satt
- **Brandväggen blockerar:** Verifiera UFW-regler med `sudo ufw status`
- **Fel IP används vid test:** Kontrollera Pi:ns IP med `hostname -I`
- **Tjänsten startar inte:** Kolla systemd-loggar med `journalctl -u privatekonomi -n 50`
- **Port redan används:** Stänga eventuell annan process med `sudo lsof -i :17127` (eller :5274, :5277)

**Felsökningsverktyg:**
```bash
# Kontrollera vilka portar som lyssnar
sudo netstat -tulpn | grep LISTEN

# Kontrollera specifik tjänst
ss -lntp | grep '17127\|5274\|5277'

# Testa lokalt
curl http://localhost:17127/
curl http://localhost:5274/
curl http://localhost:5277/health

# Kontrollera Pi:ns IP
hostname -I

# Visa systemd-loggar
journalctl -u privatekonomi -f
```

## Implementerade Lösningar för Aspire AppHost

### Automatisk konfiguration via miljövariabler

Privatekonomi detekterar automatiskt Raspberry Pi-miljön och konfigurerar alla tjänster att lyssna på alla nätverksinterfaces när `PRIVATEKONOMI_RASPBERRY_PI=true`.

**I Program.cs (AppHost):**
```csharp
var isRaspberryPi = Environment.GetEnvironmentVariable("PRIVATEKONOMI_RASPBERRY_PI") == "true";
var webUrls = isRaspberryPi ? "http://0.0.0.0:5274" : null;
var apiUrls = isRaspberryPi ? "http://0.0.0.0:5277" : null;
```

**Miljövariabler som sätts automatiskt:**
```bash
PRIVATEKONOMI_RASPBERRY_PI=true
ASPNETCORE_URLS=http://0.0.0.0:17127          # Aspire Dashboard
DOTNET_DASHBOARD_URLS=http://0.0.0.0:17127    # Aspire Dashboard
ASPNETCORE_ENVIRONMENT=Production
PRIVATEKONOMI_ENVIRONMENT=RaspberryPi
```

### Startup-skript

Ett dedikerat startup-skript `scripts/raspberry-pi/start.sh` hanterar automatiskt:
- Sätter alla nödvändiga miljövariabler
- Konfigurerar URL:er för alla tjänster
- Startar Aspire AppHost med rätt inställningar

#### Användning av startup-skriptet:
```bash
# Från repository-roten
cd ~/Privatekonomi
cd ~/privatekonomi/scripts/raspberry-pi && bash start.sh
```

#### Manuell start med miljövariabler:
```bash
cd ~/Privatekonomi/src/Privatekonomi.AppHost
export PRIVATEKONOMI_RASPBERRY_PI=true
export ASPNETCORE_URLS="http://0.0.0.0:17127"
export DOTNET_DASHBOARD_URLS="http://0.0.0.0:17127"
export ASPNETCORE_ENVIRONMENT=Production
export PRIVATEKONOMI_ENVIRONMENT=RaspberryPi
dotnet run
```

### Brandväggskonfiguration (UFW)

Om du valt att konfigurera brandväggen under installationen, öppnas automatiskt:

```bash
sudo ufw allow ssh                    # SSH-åtkomst
sudo ufw allow 17127/tcp              # Aspire Dashboard
sudo ufw allow 5274/tcp               # Web App
sudo ufw allow 5277/tcp               # API
sudo ufw enable
```

**Kontrollera brandväggsstatus:**
```bash
sudo ufw status
```

### Systemd-tjänst (Valfri)

Om du valt att skapa en systemd-tjänst under installationen:

```bash
# Starta tjänsten
sudo systemctl start privatekonomi

# Stoppa tjänsten
sudo systemctl stop privatekonomi

# Kontrollera status
sudo systemctl status privatekonomi

# Visa loggar
journalctl -u privatekonomi -f
```

**Systemd-tjänsten konfigurerar automatiskt:**
- Alla miljövariabler för Raspberry Pi
- Automatisk omstart vid fel
- Startar automatiskt vid systemuppstart



