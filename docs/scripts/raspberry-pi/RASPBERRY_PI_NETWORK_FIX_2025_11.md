# Raspberry Pi Nätverksåtkomst - Åtgärdad Bugg (November 2025)

## Sammanfattning

**Problem:** Privatekonomi-applikationen på Raspberry Pi var inte nåbar från andra enheter på nätverket, trots att alla portar skulle vara öppna.

**Orsak:** Tjänsterna (AppHost, Web, API) lyssnade på `127.0.0.1` (localhost) istället för `0.0.0.0` (alla nätverksinterfaces).

**Lösning:** Implementerade explicit nätverkskonfiguration i Program.cs-filerna och förbättrade installationsskriptet.

## Tekniska Detaljer

### Rotorsaken

Aspire (.NET Aspire orchestration framework) konfigurerar som standard tjänster att lyssna på localhost av säkerhetsskäl. Detta är korrekt för utvecklingsmiljöer men inte för produktionsmiljöer som Raspberry Pi där man vill ha nätverksåtkomst.

Även om `appsettings.Production.json` hade korrekt `"Urls": "http://0.0.0.0:PORT"`, så ignorerades eller överskrevs denna konfiguration av Aspire vid uppstart.

### Implementerade Ändringar

#### 1. AppHost/Program.cs
```csharp
// Raspberry Pi configuration - listen on all network interfaces
var isRaspberryPi = Environment.GetEnvironmentVariable("PRIVATEKONOMI_RASPBERRY_PI") == "true";

// Configure Aspire Dashboard to listen on all network interfaces when on Raspberry Pi
if (isRaspberryPi)
{
    // Ensure DOTNET_DASHBOARD_URLS is set to listen on 0.0.0.0 for network access
    var dashboardUrl = Environment.GetEnvironmentVariable("DOTNET_DASHBOARD_URLS");
    if (string.IsNullOrEmpty(dashboardUrl) || dashboardUrl.Contains("localhost") || dashboardUrl.Contains("127.0.0.1"))
    {
        Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_URLS", "http://0.0.0.0:17127");
    }
}

// Pass PRIVATEKONOMI_RASPBERRY_PI flag to Web and Api projects
if (isRaspberryPi)
{
    apiBuilder = apiBuilder
        .WithHttpEndpoint(port: 5277, name: "http")
        .WithEnvironment("PRIVATEKONOMI_RASPBERRY_PI", "true");
    
    webBuilder = webBuilder
        .WithHttpEndpoint(port: 5274, name: "http")
        .WithEnvironment("PRIVATEKONOMI_RASPBERRY_PI", "true");
}
```

**Vad detta gör:**
- Detekterar Raspberry Pi-miljö via `PRIVATEKONOMI_RASPBERRY_PI` miljövariabel
- Konfigurerar Aspire Dashboard att lyssna på `0.0.0.0:17127`
- Skickar vidare Raspberry Pi-flaggan till Web och Api projekten

#### 2. Web/Program.cs & Api/Program.cs
```csharp
// Raspberry Pi configuration - ensure we listen on all network interfaces
var isRaspberryPi = Environment.GetEnvironmentVariable("PRIVATEKONOMI_RASPBERRY_PI") == "true";
if (isRaspberryPi)
{
    // Explicitly configure Kestrel to listen on 0.0.0.0 for network access
    // This overrides any localhost-only bindings from Aspire
    var port = Environment.GetEnvironmentVariable("PORT") ?? "5274"; // or "5277" for API
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}
```

**Vad detta gör:**
- Läser `PRIVATEKONOMI_RASPBERRY_PI` miljövariabel
- Använder `UseUrls()` för att explicit konfigurera Kestrel att lyssna på `0.0.0.0`
- Överskriver alla localhost-bindningar från Aspire

#### 3. raspberry-pi-install.sh

**Förbättrad systemd-tjänst:**
```bash
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=PRIVATEKONOMI_ENVIRONMENT=RaspberryPi
Environment=PRIVATEKONOMI_STORAGE_PROVIDER=Sqlite
Environment=PRIVATEKONOMI_RASPBERRY_PI=true
Environment=DOTNET_DASHBOARD_URLS=http://0.0.0.0:17127
Environment=DOTNET_ROOT=$HOME/.dotnet
Environment=PATH=$HOME/.dotnet:$HOME/.dotnet/tools:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin
```

**Vad detta gör:**
- Sätter alla nödvändiga miljövariabler
- Inkluderar komplett PATH med .dotnet och .dotnet/tools
- Expanderar `$HOME` korrekt vid skapande av tjänsten

**Förbättrad Nginx-konfiguration:**
```nginx
# Define upstream servers for better error handling
upstream privatekonomi_web {
    server localhost:5274;
    keepalive 32;
}

upstream privatekonomi_api {
    server localhost:5277;
    keepalive 32;
}

upstream privatekonomi_dashboard {
    server localhost:17127;
    keepalive 32;
}

server {
    # ... 
    location / {
        proxy_pass http://privatekonomi_web;
        # Better error handling
        proxy_intercept_errors on;
        error_page 502 503 504 /50x.html;
    }
}
```

**Vad detta gör:**
- Använder upstream-block för bättre anslutningshantering
- Keepalive-anslutningar för bättre prestanda
- Timeout-inställningar och error handling för 502/503/504 fel

## Testning och Verifiering

### 1. Kontrollera att portar lyssnar på rätt adress
```bash
ss -lntp | grep -E "17127|5274|5277"
```

**Förväntat resultat:**
```
LISTEN 0  511  0.0.0.0:17127  0.0.0.0:*
LISTEN 0  511  0.0.0.0:5274   0.0.0.0:*
LISTEN 0  511  0.0.0.0:5277   0.0.0.0:*
```

**Fel resultat (före fix):**
```
LISTEN 0  511  127.0.0.1:17127  0.0.0.0:*  # Endast localhost!
LISTEN 0  511  127.0.0.1:5274   0.0.0.0:*
LISTEN 0  511  127.0.0.1:5277   0.0.0.0:*
```

### 2. Testa lokal åtkomst
```bash
curl http://localhost:5274
curl http://localhost:5277
curl http://localhost:17127
```

Alla ska returnera HTTP 200 eller omdirigera.

### 3. Testa nätverksåtkomst från Raspberry Pi
```bash
MY_IP=$(hostname -I | awk '{print $1}')
curl http://$MY_IP:5274
curl http://$MY_IP:5277
curl http://$MY_IP:17127
```

Alla ska returnera HTTP 200 eller omdirigera.

### 4. Testa från annan enhet
Från en dator eller mobil på samma nätverk:
```bash
# Ersätt med din Raspberry Pi IP
curl http://192.168.50.111:5274
```

Eller öppna i webbläsare: `http://192.168.50.111:5274`

### 5. Kör diagnostikskript
```bash
cd ~/Privatekonomi
./raspberry-pi-debug.sh
```

Alla porttester ska visa gröna checkmarks (✓).

## Användning

### För nya installationer
```bash
git clone https://github.com/pownas/Privatekonomi.git
cd Privatekonomi
./raspberry-pi-install.sh
```

Installationsskriptet konfigurerar automatiskt allt korrekt.

### För befintliga installationer
```bash
cd ~/Privatekonomi
git pull origin main
dotnet build --configuration Release

# Kör om installationen för att uppdatera konfiguration
./raspberry-pi-install.sh
```

Välj att uppdatera befintlig installation när du får frågan.

### Manuell start (för test)
```bash
cd ~/Privatekonomi
./raspberry-pi-start.sh
```

Detta sätter alla miljövariabler korrekt och startar applikationen.

### Automatisk start med systemd
```bash
sudo systemctl start privatekonomi
sudo systemctl enable privatekonomi  # Starta vid boot
```

## Vanliga Frågor

### Q: Varför inte bara använda appsettings.Production.json?
A: Aspire-projektet (AppHost) överskriver URL-konfiguration vid uppstart. Vi behöver explicit konfigurera Kestrel via `UseUrls()` för att garantera att `0.0.0.0` används.

### Q: Påverkar detta säkerheten?
A: Tjänsterna är endast tillgängliga från lokala nätverket. För extern åtkomst rekommenderas VPN eller Nginx med HTTPS och stark autentisering.

### Q: Fungerar detta med SSL/HTTPS?
A: Ja, Nginx hanterar SSL/HTTPS och vidarebefordrar till backend-tjänsterna via HTTP på localhost. Detta är standard practice för reverse proxies.

### Q: Vad händer om PRIVATEKONOMI_RASPBERRY_PI inte är satt?
A: Då används default-konfigurationen vilket lyssnar på localhost. Detta är säkrare för utvecklingsmiljöer.

### Q: Kan jag använda detta på andra plattformar än Raspberry Pi?
A: Ja, sätt `PRIVATEKONOMI_RASPBERRY_PI=true` miljövariabel på vilken Linux-server som helst för att aktivera nätverksåtkomst.

## Relaterade Dokument

- [Raspberry Pi Installation Guide](RASPBERRY_PI_GUIDE.md)
- [Network Troubleshooting](RASPBERRY_PI_NETWORK_TROUBLESHOOTING.md)
- [Nginx & SSL Configuration](RASPBERRY_PI_NGINX_SSL.md)
- [Debugging Guide](RASPBERRY_PI_FELSOKNING.md)

## Teknisk Referens

### Miljövariabler
- `PRIVATEKONOMI_RASPBERRY_PI=true` - Aktiverar nätverksåtkomst
- `ASPNETCORE_ENVIRONMENT=Production` - Laddar Production-konfiguration
- `DOTNET_DASHBOARD_URLS=http://0.0.0.0:17127` - Aspire Dashboard URL
- `PORT` - Används av Aspire för att sätta port (5274 för Web, 5277 för API)

### Portar
- `17127` - Aspire Dashboard
- `5274` - Web Application (Blazor)
- `5277` - API (REST endpoints)
- `80` - HTTP (Nginx)
- `443` - HTTPS (Nginx, om SSL är konfigurerat)

### Loggning
```bash
# Systemd-tjänst loggar
journalctl -u privatekonomi -f

# Nginx loggar
sudo tail -f /var/log/nginx/error.log
sudo tail -f /var/log/nginx/access.log
```

## Framtida Förbättringar

- [ ] Automatisk detektion av Raspberry Pi (utan miljövariabel)
- [ ] Grafiskt konfigurationsverktyg
- [ ] Automatisk SSL-konfiguration med Let's Encrypt
- [ ] Integrerad VPN-konfiguration
- [ ] Dashboard för nätverksövervakning

## Bidrag

Hittade du ett problem eller har förbättringsförslag?
1. Öppna ett issue på GitHub
2. Skicka en pull request
3. Diskutera i projektets forum

GitHub: https://github.com/pownas/Privatekonomi

---

**Uppdaterad:** 2025-11-11
**Version:** 1.0
**Författare:** GitHub Copilot (via pownas)
