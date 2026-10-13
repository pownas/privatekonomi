# Nginx Reverse Proxy och SSL för Privatekonomi på Raspberry Pi

## Översikt

Detta dokument beskriver hur Nginx konfigureras som reverse proxy för Privatekonomi och hur SSL/HTTPS aktiveras med antingen Let's Encrypt eller self-signed certifikat.

## Varför använda Nginx?

### Fördelar

1. **SSL/HTTPS-terminering**
   - Kryptering av all trafik
   - Gratis certifikat med Let's Encrypt
   - Automatisk certifikatförnyelse

2. **Enkel åtkomst**
   - Alla tjänster via en port (80/443)
   - Ingen portspecifikation i URL behövs
   - Domänbaserad åtkomst

3. **Säkerhet**
   - Säkerhetsheaders (X-Frame-Options, CSP, HSTS)
   - Skydd mot vanliga attacker
   - Centraliserad åtkomstkontroll

4. **Prestanda**
   - HTTP/2-stöd
   - Caching och buffering
   - Komprimering av statiska resurser

5. **Övervakning**
   - Centraliserad loggning
   - Access logs och error logs
   - Enklare felsökning

## Automatisk installation

Det enklaste sättet är att låta installationsskriptet hantera allt:

```bash
# Full installation med Nginx och SSL
./raspberry-pi-install.sh

# Hoppa över Nginx
./raspberry-pi-install.sh --no-nginx

# Hoppa över SSL (men behåll Nginx)
./raspberry-pi-install.sh --no-ssl

# Konfigurera endast SSL (för befintlig installation)
./raspberry-pi-install.sh --configure-ssl
```

## Manuell Nginx-installation

### 1. Installera Nginx

```bash
sudo apt update
sudo apt install -y nginx
```

### 2. Skapa Privatekonomi-konfiguration

Skapa `/etc/nginx/sites-available/privatekonomi`:

```nginx
# Privatekonomi Nginx Reverse Proxy Configuration

# Main server block (HTTP)
server {
    listen 80;
    listen [::]:80;
    server_name privatekonomi.example.com;  # Ändra till din domän eller IP
    
    # Security headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    
    # Increase body size for file uploads
    client_max_body_size 20M;
    
    # Web Application (Main Site)
    location / {
        proxy_pass http://localhost:5274;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Real-IP $remote_addr;
        
        # Blazor SignalR specific settings
        proxy_buffering off;
        proxy_read_timeout 100s;
    }
    
    # API Endpoints
    location /api/ {
        proxy_pass http://localhost:5277/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Real-IP $remote_addr;
    }
    
    # Aspire Dashboard (optional - comment out in production)
    location /aspire/ {
        proxy_pass http://localhost:17127/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
    
    # Health check endpoint
    location /health {
        access_log off;
        return 200 "healthy\n";
        add_header Content-Type text/plain;
    }
}
```

### 3. Aktivera konfigurationen

```bash
# Skapa symbolisk länk
sudo ln -s /etc/nginx/sites-available/privatekonomi /etc/nginx/sites-enabled/

# Testa konfigurationen
sudo nginx -t

# Starta om Nginx
sudo systemctl restart nginx
sudo systemctl enable nginx
```

### 4. Öppna brandväggsportar

```bash
sudo ufw allow 80/tcp comment "HTTP"
sudo ufw allow 443/tcp comment "HTTPS"
sudo ufw reload
```

## SSL/HTTPS-konfiguration

### Alternativ 1: Let's Encrypt (Rekommenderat för produktion)

#### Förutsättningar
- Registrerat domännamn
- **HTTP-01:** Domänen pekar på din publika IP-adress och routern vidarebefordrar port 80 till Raspberry Pi:n. Port 443 behövs för klienttrafiken.
- **DNS-01 (Cloudflare):** DNS-zonen hanteras av Cloudflare och ett API-token med `Zone:DNS:Edit` för den aktuella zonen. Ingen inkommande port behöver exponeras och A-recorden behöver inte peka på Pi:n. Klienterna måste ändå kunna slå upp subdomänen till Pi:ns lokala IP, exempelvis via lokal DNS.

#### Installation

1. **Installera Certbot**

```bash
sudo apt update
sudo apt install -y certbot python3-certbot-nginx
```

2. **Hämta certifikat**

```bash
# Ersätt privatekonomi.example.com med din domän
sudo certbot --nginx -d privatekonomi.example.com

# Följ instruktionerna:
# - Ange e-postadress för förnyelse-notifikationer
# - Acceptera användarvillkor
# - Välj att omdirigera HTTP till HTTPS (rekommenderat)
```

Vid körning av `raspberry-pi-install.sh` kan du välja HTTP-01 eller DNS-01 med Cloudflare. DNS-01-alternativet installerar `python3-certbot-dns-cloudflare`, frågar efter token utan att visa det och sparar det i `/etc/letsencrypt/cloudflare.ini` med rättigheter `600`. Certbot sparar förnyelseinställningarna, och `certbot.timer` förnyar certifikatet automatiskt. Skapa ett begränsat API-token i Cloudflare och använd det bara på Pi:n; lägg aldrig token i kommandoraden, dokumentation eller versionshantering.

En subdomän, till exempel `ekonomi.example.com`, är lämplig för installationen. Med DNS-01 kan samma namn ha en lokal DNS-post som pekar på Pi:ns privata IP; publika resolvrar behöver inte kunna nå applikationen. Klienter måste dock ansluta med just subdomänen som certifikatet utfärdats för. Om zonen inte hanteras av Cloudflare stöder installationsflödet inte DNS-01 för den leverantören; använd HTTP-01 eller konfigurera leverantörens Certbot-plugin separat.

3. **Verifiera automatisk förnyelse**

```bash
# Aktivera timer för automatisk förnyelse
sudo systemctl enable certbot.timer
sudo systemctl start certbot.timer

# Kontrollera status
sudo systemctl status certbot.timer

# Testa förnyelse (dry-run)
sudo certbot renew --dry-run
```

#### Certbot förnyar automatiskt
- Certifikat gäller i 90 dagar
- Automatisk förnyelse körs två gånger per dag
- Förnyas när det är 30 dagar kvar

### Alternativ 2: Self-Signed Certificate (För lokal användning)

#### När använda self-signed?
- ✅ Lokal utveckling och testning
- ✅ Privat hemmanätverk utan internet-exponering
- ✅ Inget domännamn tillgängligt
- ❌ Inte för offentlig produktion

#### Installation

1. **Skapa certifikatmapp**

```bash
sudo mkdir -p /etc/ssl/privatekonomi
cd /etc/ssl/privatekonomi
```

2. **Generera self-signed certifikat**

```bash
# Ersätt 192.168.1.100 med din Raspberry Pi IP
sudo openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout privatekonomi.key \
  -out privatekonomi.crt \
  -subj "/C=SE/O=Privatekonomi/CN=192.168.1.100" \
  -addext "subjectAltName=IP:192.168.1.100,DNS:privatekonomi.local,DNS:localhost,IP:127.0.0.1"
```

3. **Uppdatera Nginx-konfiguration**

Redigera `/etc/nginx/sites-available/privatekonomi`:

```nginx
# Redirect HTTP to HTTPS
server {
    listen 80;
    listen [::]:80;
    server_name 192.168.1.100;
    return 301 https://$host$request_uri;
}

# HTTPS server block
server {
    listen 443 ssl http2;
    listen [::]:443 ssl http2;
    server_name 192.168.1.100;
    
    # SSL Configuration
    ssl_certificate /etc/ssl/privatekonomi/privatekonomi.crt;
    ssl_certificate_key /etc/ssl/privatekonomi/privatekonomi.key;
    
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;
    
    # Security headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    
    # ... (resten av konfigurationen samma som ovan)
}
```

4. **Testa och ladda om**

```bash
sudo nginx -t
sudo systemctl reload nginx
```

#### Lita på certifikatet på klientenheter

Installationsskriptet skapar ett certifikat med IP-adressen som rapporteras av Pi:n samt `privatekonomi.local` som Subject Alternative Names. För lokal DNS-åtkomst kan du konfigurera routern eller klienternas hosts-fil att slå upp `privatekonomi.local` till Pi:ns IP-adress. Använd sedan `https://privatekonomi.local` eller `https://<Pi-IP>`.

Self-signed certifikat krypterar anslutningen men garanterar inte serverns identitet förrän certifikatet har betrotts på enheten. Kopiera certifikatet säkert till varje klient, jämför dess fingeravtryck mot Pi:n och importera det som betrott certifikat:

```bash
openssl x509 -in /etc/ssl/privatekonomi/privatekonomi.crt -noout -fingerprint -sha256
```

På Windows importeras `.crt` via certifikathantering till **Trusted Root Certification Authorities**. På macOS importeras det i Keychain Access och markeras som betrott. På Android importeras det under säkerhetsinställningarnas certifikatalternativ; iOS kräver import till profilen och att full tillit aktiveras under certifikattillit. Menynamn varierar mellan versioner. Dela aldrig `.key`-filen med klienter.

Certifikatet är giltigt i 365 dagar. Installationsskriptet installerar en månatlig systemd-timer som förnyar certifikatet och laddar om Nginx. Eftersom detta är ett självsignerat servercertifikat innebär förnyelsen ett nytt fingeravtryck; klienter som installerat certifikatet behöver då lita på det nya igen. För automatisk förnyelse utan återimport, använd en privat CA (t.ex. mkcert) och installera CA-certifikatet en gång på varje klient.

Om certifikatet inte har importerats visar webbläsaren en säkerhetsvarning:

**Chrome/Edge:**
1. Klicka på "Advanced"
2. Klicka på "Proceed to [ip-address] (unsafe)"

**Firefox:**
1. Klicka på "Advanced"
2. Klicka på "Accept the Risk and Continue"

**Safari:**
1. Klicka på "Show Details"
2. Klicka på "visit this website"

**OBS:** Detta är normalt för self-signed certifikat och säkert för privat användning.

## Felsökning

### Kontrollera Nginx-status

```bash
# Status
sudo systemctl status nginx

# Felloggar
sudo tail -f /var/log/nginx/error.log

# Åtkomstloggar
sudo tail -f /var/log/nginx/access.log

# Testa konfiguration
sudo nginx -t

# Ladda om konfiguration
sudo systemctl reload nginx

# Starta om Nginx
sudo systemctl restart nginx
```

### Vanliga problem

#### Problem: "Connection refused"

**Lösning:**
```bash
# Kontrollera att Privatekonomi-tjänsterna körs
sudo systemctl status privatekonomi

# Kontrollera att portarna lyssnar
ss -lntp | grep '5274\|5277\|17127'
```

#### Problem: "502 Bad Gateway"

**Orsak:** Backend-tjänster (Web/API) körs inte

**Lösning:**
```bash
# Starta Privatekonomi
cd ~/Privatekonomi
./raspberry-pi-start.sh

# Eller med systemd
sudo systemctl start privatekonomi
```

#### Problem: Let's Encrypt misslyckas

**Möjliga orsaker:**
- Domänen pekar inte på rätt IP-adress
- Port 80 är blockerad
- DNS-propagering inte klar

**Lösning:**
```bash
# Kontrollera DNS
nslookup privatekonomi.example.com

# Kontrollera att port 80 är öppen
sudo ufw status | grep 80

# Kontrollera Nginx lyssnar på port 80
sudo netstat -tlnp | grep :80
```

#### Problem: Self-signed certifikat fungerar inte

**Lösning:**
```bash
# Kontrollera att certifikatfilerna finns
ls -la /etc/ssl/privatekonomi/

# Kontrollera filrättigheter
sudo chmod 600 /etc/ssl/privatekonomi/privatekonomi.key
sudo chmod 644 /etc/ssl/privatekonomi/privatekonomi.crt

# Verifiera certifikat
openssl x509 -in /etc/ssl/privatekonomi/privatekonomi.crt -text -noout
```

## Säkerhetsbästa praxis

### 1. Håll Nginx uppdaterat

```bash
sudo apt update
sudo apt upgrade nginx
```

### 2. Konfigurera starka SSL-inställningar

Redigera `/etc/nginx/sites-available/privatekonomi`:

```nginx
# SSL Configuration
ssl_protocols TLSv1.2 TLSv1.3;
ssl_ciphers 'ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384';
ssl_prefer_server_ciphers off;
ssl_session_timeout 1d;
ssl_session_cache shared:SSL:50m;
ssl_stapling on;
ssl_stapling_verify on;

# HSTS (HTTP Strict Transport Security)
add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
```

### 3. Begränsa åtkomst till Aspire Dashboard

```nginx
# Aspire Dashboard - endast från lokalt nätverk
location /aspire/ {
    allow 192.168.1.0/24;  # Ändra till ditt nätverk
    deny all;
    
    proxy_pass http://localhost:17127/;
    # ... resten av proxy-inställningar
}
```

### 4. Aktivera rate limiting

```nginx
# I http-blocket (överst i nginx.conf eller privatekonomi-config)
limit_req_zone $binary_remote_addr zone=one:10m rate=10r/s;

# I server-blocket
location / {
    limit_req zone=one burst=20;
    # ... resten av konfigurationen
}
```

### 5. Logga endast viktiga händelser

```nginx
server {
    # ...
    
    # Minska loggning för health checks
    location /health {
        access_log off;
        return 200 "healthy\n";
        add_header Content-Type text/plain;
    }
}
```

## Prestandaoptimering

### 1. Aktivera Gzip-komprimering

Redigera `/etc/nginx/nginx.conf`:

```nginx
http {
    # ...
    
    gzip on;
    gzip_vary on;
    gzip_proxied any;
    gzip_comp_level 6;
    gzip_types text/plain text/css text/xml text/javascript application/json application/javascript application/xml+rss;
}
```

### 2. Aktivera HTTP/2

```nginx
server {
    listen 443 ssl http2;
    listen [::]:443 ssl http2;
    # ...
}
```

### 3. Caching av statiska resurser

```nginx
location ~* \.(jpg|jpeg|png|gif|ico|css|js|woff|woff2)$ {
    expires 30d;
    add_header Cache-Control "public, immutable";
}
```

## Sammanfattning

### Med Let's Encrypt
```bash
# Installation
./raspberry-pi-install.sh
# Följ instruktionerna och välj Let's Encrypt

# Åtkomst
https://privatekonomi.example.com
```

### Med Self-Signed Certificate
```bash
# Installation
./raspberry-pi-install.sh
# Följ instruktionerna och välj Self-Signed

# Åtkomst
https://192.168.1.100
# Acceptera säkerhetsvarning i webbläsare
```

### Utan SSL (endast HTTP)
```bash
# Installation
./raspberry-pi-install.sh --no-ssl

# Åtkomst
http://192.168.1.100
```

### Utan Nginx (direktåtkomst)
```bash
# Installation
./raspberry-pi-install.sh --no-nginx

# Åtkomst
http://192.168.1.100:5274  # Web App
http://192.168.1.100:5277  # API
http://192.168.1.100:17127 # Aspire Dashboard
```

## Ytterligare resurser

- [Nginx Dokumentation](https://nginx.org/en/docs/)
- [Let's Encrypt Dokumentation](https://letsencrypt.org/docs/)
- [Certbot Användningsguide](https://certbot.eff.org/)
- [SSL Labs Server Test](https://www.ssllabs.com/ssltest/) - Testa din SSL-konfiguration
- [Mozilla SSL Configuration Generator](https://ssl-config.mozilla.org/) - Generera optimala SSL-inställningar
