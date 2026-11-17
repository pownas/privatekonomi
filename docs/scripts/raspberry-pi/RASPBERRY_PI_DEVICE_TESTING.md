# Testguide: Åtkomst från Olika Enheter

Denna guide beskriver hur du testar och verifierar att Privatekonomi på din Raspberry Pi är tillgänglig från olika enheter på ditt lokala nätverk.

## Förberedelser

### 1. Hitta din Raspberry Pi IP-adress

På Raspberry Pi:
```bash
hostname -I
```

Exempel: `192.168.1.100`

Spara denna IP-adress, du kommer att använda den i alla tester nedan.

### 2. Verifiera att tjänsterna körs

```bash
cd ~/Privatekonomi
./raspberry-pi-debug.sh
```

Kontrollera att alla kontroller är gröna (✓).

## Test 1: Webbläsare på Desktop (Windows/Mac/Linux)

### Windows

1. **Anslut till samma WiFi** som Raspberry Pi
   - Öppna Inställningar → Nätverk & Internet
   - Kontrollera att du är ansluten till rätt WiFi

2. **Öppna webbläsare** (Chrome, Edge, Firefox)

3. **Testa direktåtkomst:**
   ```
   http://192.168.1.100:5274
   ```
   - Ska visa Privatekonomi login-sida
   - Om det fungerar: ✅ Grundläggande nätverksåtkomst OK

4. **Testa via Nginx (om konfigurerat):**
   ```
   http://192.168.1.100
   ```
   - Ska också visa Privatekonomi
   - Om det fungerar: ✅ Nginx proxy OK

5. **Testa HTTPS (om konfigurerat):**
   ```
   https://192.168.1.100
   ```
   - Kan visa säkerhetsvarning (för self-signed certifikat)
   - Klicka "Advanced" → "Proceed to site"
   - Om det fungerar: ✅ HTTPS OK

### Mac

1. **Anslut till samma WiFi**
   - Klicka WiFi-ikonen i menyraden
   - Kontrollera att du är ansluten till rätt nätverk

2. **Öppna Safari eller Chrome**

3. **Testa åtkomst:**
   ```
   http://192.168.1.100:5274
   ```

4. **Alternativt, använd mDNS:**
   ```
   http://raspberrypi.local:5274
   ```
   - mDNS fungerar automatiskt på Mac
   - Ändra hostname om du ändrat Raspberry Pi hostname

### Linux

1. **Anslut till samma nätverk**

2. **Testa med curl:**
   ```bash
   curl -I http://192.168.1.100:5274
   ```
   
   Förväntat svar:
   ```
   HTTP/1.1 200 OK
   Date: ...
   Server: Kestrel
   ```

3. **Testa i webbläsare:**
   ```
   http://192.168.1.100:5274
   ```

## Test 2: Smartphone (iOS/Android)

### iPhone/iPad (iOS)

1. **Anslut till samma WiFi**
   - Öppna Inställningar → WiFi
   - Kontrollera att du är ansluten till samma nätverk som Raspberry Pi
   - Tryck på (i) bredvid nätverksnamnet
   - Kontrollera IP-adress: ska vara samma subnät (t.ex. 192.168.1.x)

2. **Öppna Safari**

3. **Skriv in adressen:**
   ```
   http://192.168.1.100:5274
   ```

4. **Om det fungerar:**
   - Du ska se Privatekonomi login-sida
   - Sidan ska vara responsiv och anpassad för mobil
   - ✅ Mobil åtkomst fungerar

5. **Installera som PWA (Progressive Web App):**
   - Tryck på dela-ikonen (↑)
   - Välj "Lägg till på hemskärmen"
   - Ge den ett namn (t.ex. "Privatekonomi")
   - Tryck "Lägg till"
   - ✅ Nu har du en app-ikon på hemskärmen

6. **Testa PWA:**
   - Öppna appen från hemskärmen
   - Ska öppnas i fullskärm utan Safari-gränssnitt
   - ✅ PWA fungerar

### Android

1. **Anslut till samma WiFi**
   - Öppna Inställningar → Anslutningar → WiFi
   - Kontrollera att du är ansluten till rätt nätverk
   - Tryck och håll in på nätverket → Hantera nätverk
   - Kontrollera IP-adress under "IP-adress"

2. **Öppna Chrome**

3. **Skriv in adressen:**
   ```
   http://192.168.1.100:5274
   ```

4. **Om det fungerar:**
   - Login-sida ska visas
   - Sidan ska vara responsiv
   - ✅ Mobil åtkomst fungerar

5. **Installera som PWA:**
   - Tryck på meny (⋮)
   - Välj "Lägg till på startskärmen" eller "Installera app"
   - Bekräfta
   - ✅ PWA installerad

6. **Testa PWA:**
   - Öppna appen från startskärmen
   - Ska fungera som en native app
   - ✅ PWA fungerar

### Felsökning Mobil

**Problem: Får "Site can't be reached"**
- Kontrollera att telefonen använder WiFi (inte mobildata)
- Kontrollera att telefonen är på samma nätverk
- Prova med Raspberry Pi IP-adress direkt
- Kontrollera brandväggen på Raspberry Pi

**Problem: Sidan laddar men är väldigt långsam**
- Kontrollera WiFi-signalstyrka
- Prova närmare WiFi-router
- Kolla Raspberry Pi-prestanda: `top` (på Pi)

**Problem: "Connection timeout"**
- Kontrollera att brandväggen tillåter anslutningar:
  ```bash
  sudo ufw status
  sudo ufw allow 5274/tcp
  ```

## Test 3: Surfplatta (iPad/Android Tablet)

Följ samma steg som för smartphone ovan.

**Fördelar med surfplatta:**
- Större skärm = bättre användarupplevelse
- Lättare att hantera transaktioner och budget
- Perfekt för hemmabruk

**Tips:**
- Använd landscape-läge för budget och diagram
- PWA-installation rekommenderas för bästa upplevelse

## Test 4: Smart TV

Om din Smart TV har webbläsare:

1. **Anslut TV till samma nätverk**
   - Kontrollera WiFi eller Ethernet-anslutning

2. **Öppna TV:ns webbläsare**

3. **Navigera till:**
   ```
   http://192.168.1.100:5274
   ```

4. **Använd fjärrkontroll** för att navigera

**Observera:** 
- Användarupplevelsen kan vara begränsad
- TV-webbläsare är ofta långsammare
- Rekommenderas främst för dashboard-visning

## Test 5: Bokmärken och Genvägar

### Desktop

**Chrome/Edge:**
1. Besök `http://192.168.1.100:5274`
2. Klicka på stjärnan i adressfältet
3. Spara bokmärke

**Firefox:**
1. Besök webbplatsen
2. Ctrl+D (Windows/Linux) eller Cmd+D (Mac)
3. Spara bokmärke

### Mobil

**iOS:**
1. Besök sidan i Safari
2. Tryck dela → Lägg till bokmärke

**Android:**
1. Besök sidan i Chrome
2. Tryck stjärnan eller meny → Lägg till bokmärke

## Test 6: Olika Nätverkssituationer

### Samma WiFi-nätverk ✅
**Setup:** Både Pi och enhet på samma WiFi
**Förväntat:** Fungerar perfekt

### Ethernet + WiFi ✅
**Setup:** Pi på Ethernet, enhet på WiFi (samma router)
**Förväntat:** Fungerar perfekt, ofta snabbare

### Gäst-nätverk ❌
**Setup:** Pi på huvudnätverk, enhet på gäst-nätverk
**Förväntat:** Fungerar INTE (gäst-nätverk är isolerade)
**Lösning:** Använd samma nätverk

### Mobilt hotspot ❌
**Setup:** Pi hemma, försöker nå via mobil hotspot
**Förväntat:** Fungerar INTE (olika nätverk)
**Lösning:** Använd VPN eller extern access

## Test 7: Prestanda från Olika Enheter

Testa laddningstider:

### På Desktop
```bash
# Mät laddningstid
time curl -o /dev/null -s http://192.168.1.100:5274
```

Förväntat: < 1 sekund

### På Mobil
- Använd webbläsarens utvecklarverktyg
- Chrome: Inställningar → Mer verktyg → Utvecklarverktyg → Network
- Förväntat: 1-3 sekunder första gången, sedan snabbare (caching)

### Optimeringstips för Bästa Prestanda

**Raspberry Pi:**
- Använd Ethernet istället för WiFi
- Raspberry Pi 4 (4GB+) eller Pi 5 rekommenderas
- SSD istället för SD-kort för databas

**Nätverk:**
- WiFi 5 (802.11ac) eller bättre
- Router nära både Pi och enheter
- Minimal nätverksbelastning

**Nginx:**
- Aktivera gzip-komprimering
- Aktivera HTTP/2
- Cache statiska resurser

## Test 8: Samtidiga Användare

**Raspberry Pi 3:**
- 1-2 samtidiga användare: Bra
- 3+ användare: Kan bli långsamt

**Raspberry Pi 4 (4GB+):**
- 3-5 samtidiga användare: Bra
- 5+ användare: Bör fungera men kan vara långsammare

**Test:**
1. Öppna Privatekonomi på flera enheter samtidigt
2. Utför olika åtgärder (visa dashboard, lägg till transaktion, etc.)
3. Kontrollera prestanda på alla enheter

## Checklista för Lyckad Nätverksåtkomst

### Grundkrav
- [ ] Raspberry Pi har fast IP-adress (DHCP-reservation eller statisk)
- [ ] Tjänsterna lyssnar på `0.0.0.0` (inte `127.0.0.1`)
- [ ] Brandväggen tillåter portar 5274, 5277, 17127 (eller 80/443 för Nginx)
- [ ] Alla enheter är på samma WiFi-nätverk
- [ ] WiFi-isolering är inaktiverat på router

### Testat och Fungerar
- [ ] Desktop webbläsare (Chrome/Firefox/Edge/Safari)
- [ ] iPhone/iPad Safari
- [ ] Android Chrome
- [ ] PWA installation på mobil
- [ ] Direktåtkomst (`:5274`)
- [ ] Via Nginx proxy (port 80)
- [ ] HTTPS (om konfigurerat)

### Optimering (Valfritt)
- [ ] mDNS konfigurerat (`raspberrypi.local`)
- [ ] Statiskt DNS-namn i router
- [ ] Gzip-komprimering aktiverad i Nginx
- [ ] HTTP/2 aktiverat
- [ ] Bokmärken skapade på alla enheter
- [ ] PWA installerad på huvudenheter

## Felsökning per Enhetstyp

### Windows Desktop - Fungerar inte

**Kontrollera:**
```powershell
# Testa ping
ping 192.168.1.100

# Testa port (PowerShell)
Test-NetConnection -ComputerName 192.168.1.100 -Port 5274

# Öppna i webbläsare
start http://192.168.1.100:5274
```

**Vanliga problem:**
- Windows Defender brandvägg blockerar
- Proxy-inställningar i webbläsare
- VPN aktiverad som blockerar lokalt nätverk

### Mac - Fungerar inte

**Kontrollera:**
```bash
# Testa ping
ping 192.168.1.100

# Testa anslutning
nc -zv 192.168.1.100 5274

# Prova mDNS
ping raspberrypi.local
```

**Vanliga problem:**
- macOS brandvägg blockerar
- Fel WiFi-nätverk
- Företags-VPN aktiverad

### iPhone/iPad - Fungerar inte

**Kontrollera:**
1. Inställningar → WiFi → (i) bredvid nätverket
2. Kontrollera IP-adress: Ska börja med samma siffror som Pi (t.ex. 192.168.1.x)
3. Prova stänga av och slå på WiFi
4. Starta om Safari
5. Prova i Chrome-appen istället

**Vanliga problem:**
- Mobil data är på
- Ansluten till fel WiFi
- Router har AP-isolering

### Android - Fungerar inte

**Kontrollera:**
1. Inställningar → Anslutningar → WiFi → Avancerat
2. Kontrollera IP-adress
3. Stäng av mobil data temporärt
4. Rensa Chrome cache
5. Prova i Firefox-appen

**Vanliga problem:**
- Data Saver aktiverad
- Privat DNS konfigurerad
- Work Profile blockerar

## Exempel på Fungerande Setup

### Hemmanätverk - Optimal Setup

**Utrustning:**
- Raspberry Pi 4 (4GB) med Ethernet
- Router: TP-Link Archer C7 (eller liknande)
- Enheter: 2x iPhone, 1x iPad, 2x Windows-laptop

**Konfiguration:**
- Pi IP: 192.168.1.100 (statisk DHCP)
- Router DNS: raspberrypi → 192.168.1.100
- Nginx med self-signed SSL
- Brandvägg: Aktiverad med öppna portar 80, 443

**Åtkomst:**
- Desktop: `http://privatekonomi` (via router DNS)
- Mobil: PWA installerad på hemskärmen
- Alla enheter: < 2 sekunder laddningstid

**Resultat:** ✅ Perfekt!

### Lägenhets-setup - Enkel

**Utrustning:**
- Raspberry Pi 3B+ med WiFi
- ISP router (standard)
- Enheter: iPhone, Windows-laptop

**Konfiguration:**
- Pi IP: 192.168.0.55 (DHCP, ändras ibland)
- Ingen Nginx (direktåtkomst)
- Ingen brandvägg

**Åtkomst:**
- Desktop: Bokmärke med IP `http://192.168.0.55:5274`
- Mobil: Bokmärke med IP
- Uppdatera bokmärke om IP ändras

**Resultat:** ✅ Fungerar, men kan behöva uppdatera IP ibland

## Slutsats

**Grundinställning som fungerar för de flesta:**
1. Fast IP-adress på Raspberry Pi (via router DHCP-reservation)
2. Nginx med HTTP (port 80)
3. Brandväggen öppen för port 80
4. PWA installerad på mobila enheter
5. Bokmärke på desktop

**För bästa upplevelse:**
- Nginx med SSL (self-signed för lokal användning)
- DNS-namn via router
- HTTP/2 och komprimering aktiverat
- Raspberry Pi 4 eller 5 med Ethernet

**Support:**
Om något inte fungerar, kör:
```bash
./raspberry-pi-debug.sh > ~/test-resultat.txt
```

Och skapa en GitHub Issue med resultatfilen.

## Ytterligare Resurser

- [Network Access Guide](RASPBERRY_PI_NETWORK_ACCESS.md)
- [Network Troubleshooting](RASPBERRY_PI_NETWORK_TROUBLESHOOTING.md)
- [Nginx & SSL Guide](RASPBERRY_PI_NGINX_SSL.md)
- [PWA Guide](../../PWA_GUIDE.md)
