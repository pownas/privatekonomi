# Snabbguide: Raspberry Pi Nätverksåtkomst

Denna korta guide hjälper dig att snabbt komma igång med nätverksåtkomst till Privatekonomi på din Raspberry Pi.

## 🚀 Snabbstart (5 minuter)

### 1. Installation

```bash
cd ~/Privatekonomi
bash setup-raspberry-pi.sh install
```

Utan `install` visas skriptlistan; tryck Enter för att välja installation som standard. Du kan även välja andra åtgärder där, till exempel `start`, `update` eller `debug`.

**Välj under installationen:**
- ✅ Lagring: SQLite (rekommenderat)
- ✅ Nginx: Ja (för enklare URL)
- ✅ SSL: Self-signed (för lokal användning) eller Let's Encrypt (om du har domän)
- ✅ Systemd-tjänst: Ja (automatisk start)
- ✅ Statisk IP: Ja (rekommenderat)

### 2. Starta applikationen

```bash
# Med systemd (om konfigurerat)
sudo systemctl start privatekonomi

# Eller manuellt via root-startaren
bash setup-raspberry-pi.sh start
```

### 3. Hitta din IP-adress

```bash
hostname -I
```

Exempel: `192.168.1.100`

### 4. Testa åtkomst

**På Raspberry Pi:**
```bash
curl http://localhost:5274
```

**Från annan enhet på nätverket:**

Öppna webbläsare och gå till:
```
http://192.168.1.100:5274
```

eller om Nginx är konfigurerat:
```
http://192.168.1.100
```

### 5. Diagnostik (vid problem)

```bash
bash setup-raspberry-pi.sh debug
```

## ✅ Checklista

- [ ] Installation slutförd utan fel
- [ ] Applikationen körs (`ps aux | grep dotnet`)
- [ ] Portar lyssnar på `0.0.0.0` (inte `127.0.0.1`)
- [ ] Brandväggen tillåter portar (om UFW är aktiverad)
- [ ] Kan nå från annan enhet på nätverket

## 📱 Access från Olika Enheter

### Desktop
```
http://192.168.1.100:5274
```
Eller med Nginx:
```
http://192.168.1.100
```

### Mobil (iOS/Android)
1. Anslut till samma WiFi
2. Öppna webbläsare
3. Gå till samma URL som desktop
4. **Bonus:** Lägg till på hemskärmen som PWA

### Via mDNS (fungerar på Mac/Linux)
```
http://raspberrypi.local:5274
```

## 🔧 Vanliga Problem

### Problem: "Connection refused"

**Lösning:**
```bash
# Starta tjänsten
sudo systemctl start privatekonomi

# Eller
bash setup-raspberry-pi.sh start
```

### Problem: Portar lyssnar på 127.0.0.1

**Diagnos:**
```bash
ss -lntp | grep 5274
```

Om du ser `127.0.0.1:5274`:
```bash
# Kör om installation
bash setup-raspberry-pi.sh install
```

### Problem: Brandväggen blockerar

**Lösning:**
```bash
sudo ufw allow 5274/tcp
sudo ufw allow 5277/tcp
sudo ufw allow 17127/tcp

# Om Nginx används
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

sudo ufw reload
```

### Problem: Kan inte nå från mobil

**Kontrollera:**
1. Mobilen använder WiFi (inte mobildata)
2. Samma nätverk som Raspberry Pi
3. WiFi-isolering är avstängd på router

## 🎯 Rekommenderad Setup för Bästa Upplevelse

```bash
# 1. Fast IP-adress via router
# Logga in på router och skapa DHCP-reservation för Pi

# 2. Installera med Nginx och SSL
bash setup-raspberry-pi.sh install
# Välj Nginx: Ja
# Välj SSL: Self-signed

# 3. Konfigurera systemd för automatisk start
# Detta görs automatiskt under installation

# 4. Installera som PWA på mobila enheter
# Safari/Chrome: Dela → Lägg till på hemskärmen

# 5. Testa från alla enheter
# Följ instruktioner i RASPBERRY_PI_DEVICE_TESTING.md
```

## 📋 Nästa Steg

Efter grundinstallation:

1. **Skapa användarkonto** i Privatekonomi
2. **Importera data** (om du har befintliga data)
3. **Konfigurera backup** (automatisk daglig backup)
   ```bash
   # Backup konfigureras automatiskt under installation
   # Kontrollera att det fungerar:
   ~/scripts/backup-privatekonomi.sh
   ```
4. **Testa från alla dina enheter** (mobil, surfplatta, desktop)
5. **Sätt bokmärken** eller installera PWA

## 📚 Omfattande Guider

Om du behöver mer detaljerad information:

### Installation och Konfiguration
- [RASPBERRY_PI_GUIDE.md](RASPBERRY_PI_GUIDE.md) - Fullständig installationsguide
- [RASPBERRY_PI_NGINX_SSL.md](RASPBERRY_PI_NGINX_SSL.md) - Nginx och SSL-konfiguration

### Felsökning
- [RASPBERRY_PI_NETWORK_TROUBLESHOOTING.md](RASPBERRY_PI_NETWORK_TROUBLESHOOTING.md) - Detaljerad felsökning
- [RASPBERRY_PI_FELSOKNING.md](RASPBERRY_PI_FELSOKNING.md) - Allmän felsökning

### Testning
- [RASPBERRY_PI_DEVICE_TESTING.md](RASPBERRY_PI_DEVICE_TESTING.md) - Testa från alla enheter
- [RASPBERRY_PI_NETWORK_ACCESS.md](RASPBERRY_PI_NETWORK_ACCESS.md) - Nätverkskonfiguration

## 🆘 Support

Om något inte fungerar:

1. **Kör diagnostik:**
   ```bash
    cd ~/privatekonomi/scripts/raspberry-pi && bash debug.sh > ~/diagnostics.txt
   ```

2. **Samla loggar:**
   ```bash
   journalctl -u privatekonomi -n 100 > ~/privatekonomi.log
   ```

3. **Skapa GitHub Issue** med:
   - `diagnostics.txt`
   - `privatekonomi.log`
   - Beskrivning av problemet

GitHub: https://github.com/pownas/Privatekonomi/issues

## 💡 Tips

- **Prestanda:** Använd Ethernet istället för WiFi för bästa prestanda
- **Hårdvara:** Raspberry Pi 4 (4GB+) rekommenderas för flera användare
- **Backup:** Testa återställning från backup regelbundet
- **Säkerhet:** Exponera INTE Privatekonomi direkt på internet utan VPN
- **Uppdateringar:** Kör `cd ~/privatekonomi/scripts/raspberry-pi && bash update.sh` regelbundet

## 🎉 Klart!

Om alla checklistor är gröna är du redo att använda Privatekonomi från alla dina enheter!

```
✅ Installation klar
✅ Nätverksåtkomst fungerar
✅ Testat från alla enheter
✅ Backup konfigurerat
```

Lycka till med din ekonomi! 💰
