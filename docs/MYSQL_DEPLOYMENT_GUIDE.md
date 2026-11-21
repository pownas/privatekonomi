# MySQL/MariaDB Deployment Guide

Detta dokument beskriver hur du konfigurerar och driftsätter Privatekonomi med MySQL eller MariaDB på ett webbhotell.

## Innehållsförteckning

1. [Översikt](#översikt)
2. [Förutsättningar](#förutsättningar)
3. [Databas-setup](#databas-setup)
4. [GitHub Secrets Konfiguration](#github-secrets-konfiguration)
5. [Lokal utveckling med MySQL](#lokal-utveckling-med-mysql)
6. [Deployment till Webbhotell](#deployment-till-webbhotell)
7. [Aspire Dashboard](#aspire-dashboard)
8. [Felsökning](#felsökning)

## Översikt

Privatekonomi stödjer nu MySQL och MariaDB för datalagring, vilket gör det möjligt att driftsätta applikationen på de flesta webbhotell. Projektet använder Pomelo.EntityFrameworkCore.MySql för optimal MySQL-prestanda och kompatibilitet.

### Stödda databaser

- **MySQL** 5.7 eller senare
- **MariaDB** 10.2 eller senare

### Komponenter som deployeras

Releaseflödet deployas två separata applikationer:

1. **Privatekonomi.Web** - Blazor Server webbapplikation (huvudgränssnittet)
2. **Privatekonomi.Api** - ASP.NET Core Web API (REST endpoints)

## Förutsättningar

### Webbhotell-krav

1. **MySQL/MariaDB databas**
   - MySQL 5.7+ eller MariaDB 10.2+
   - Administratörsåtkomst för att skapa databas och användare
   - Minst 100 MB databasutrymme (rekommenderat 500 MB)

2. **SFTP/FTPS-åtkomst**
   - Hostname och port
   - Användarnamn och lösenord
   - Två separata kataloger (för Web och API)

3. **Server-miljö**
   - .NET 9.0 Runtime installerad
   - Linux-baserad server (rekommenderat)
   - Minst 512 MB RAM per applikation
   - Port 80/443 för Web, separat port för API

### GitHub-åtkomst

- Admin-behörighet till repositoryt
- Möjlighet att skapa och hantera GitHub Secrets
- Möjlighet att skapa Git tags

## Databas-setup

### Steg 1: Skapa MySQL-databas

Logga in på din MySQL-server och skapa en databas:

```sql
-- Skapa databas för Privatekonomi
CREATE DATABASE privatekonomi 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci;

-- Skapa dedikerad användare
CREATE USER 'privatekonomi_user'@'localhost' 
IDENTIFIED BY 'DITT_SÄKRA_LÖSENORD';

-- Ge användaren fullständiga rättigheter till databasen
GRANT ALL PRIVILEGES ON privatekonomi.* 
TO 'privatekonomi_user'@'localhost';

-- Uppdatera behörigheter
FLUSH PRIVILEGES;
```

**Säkerhetsnoteringar:**
- Använd ett starkt lösenord (minst 16 tecken med blandad case, siffror och specialtecken)
- Om möjligt, använd `'privatekonomi_user'@'%'` endast om du behöver fjärråtkomst
- Begränsa behörigheter till endast vad som behövs

### Steg 2: Verifiera anslutning

Testa att du kan ansluta till databasen:

```bash
mysql -u privatekonomi_user -p -h localhost privatekonomi
```

Om anslutningen fungerar, fortsätt till nästa steg.

### Steg 3: Skapa Connection String

Din MySQL connection string ska följa detta format:

```
Server=HOSTNAME;Port=3306;Database=privatekonomi;User=privatekonomi_user;Password=DITT_LÖSENORD;
```

**Exempel:**

```
Server=localhost;Port=3306;Database=privatekonomi;User=privatekonomi_user;Password=MyS3cur3P@ssw0rd!;
```

För webbhotell kan hostname vara något som `mysql.example.com` eller en IP-adress.

## GitHub Secrets Konfiguration

För att deployas applikationen automatiskt måste du konfigurera följande secrets i ditt GitHub-repository.

### Navigera till Secrets

1. Gå till ditt repository på GitHub
2. Klicka på **Settings** (Inställningar)
3. I vänstermenyn, välj **Secrets and variables** → **Actions**
4. Klicka på **New repository secret**

### Obligatoriska Secrets

#### 1. MYSQL_CONNECTION_STRING

- **Namn:** `MYSQL_CONNECTION_STRING`
- **Värde:** Din MySQL connection string (från steg 3 ovan)
- **Exempel:** 
  ```
  Server=mysql.myhost.com;Port=3306;Database=privatekonomi;User=privatekonomi_user;Password=MyS3cur3P@ssw0rd!;
  ```

#### 2. SFTP_HOST

- **Namn:** `SFTP_HOST`
- **Värde:** Din SFTP-server hostname
- **Exempel:** `ftp.example.com`

#### 3. SFTP_USERNAME

- **Namn:** `SFTP_USERNAME`
- **Värde:** Ditt SFTP-användarnamn
- **Exempel:** `privatekonomi_ftp`

#### 4. SFTP_PASSWORD

- **Namn:** `SFTP_PASSWORD`
- **Värde:** Ditt SFTP-lösenord
- **Säkerhet:** Använd ett unikt, starkt lösenord

#### 5. SFTP_PORT

- **Namn:** `SFTP_PORT`
- **Värde:** SFTP-portnummer
- **Standard:** `21` (FTPS) eller `22` (SFTP)

#### 6. SFTP_WEB_DIR

- **Namn:** `SFTP_WEB_DIR`
- **Värde:** Målkatalog för webbapplikationen
- **Exempel:** `/var/www/privatekonomi-web/` eller `/home/user/public_html/`
- **OBS:** Måste sluta med `/`

#### 7. SFTP_API_DIR

- **Namn:** `SFTP_API_DIR`
- **Värde:** Målkatalog för API
- **Exempel:** `/var/www/privatekonomi-api/`
- **OBS:** Måste sluta med `/`

#### 8. PRODUCTION_URL (Valfritt)

- **Namn:** `PRODUCTION_URL`
- **Värde:** URL till din webbapplikation
- **Exempel:** `https://privatekonomi.example.com`

### Verifiera Secrets

Efter att ha skapat alla secrets, dubbelkolla:

- ✅ Inga typos i secret-namnen
- ✅ Connection string är korrekt formaterad
- ✅ SFTP-directories slutar med `/`
- ✅ Port-nummer är korrekt

## Lokal utveckling med MySQL

### Konfigurera lokalt

1. **Skapa lokal databas** (se [Databas-setup](#databas-setup))

2. **Skapa appsettings.local.json** för Web-projektet:

```bash
cd src/Privatekonomi.Web
```

Skapa filen `appsettings.local.json`:

```json
{
  "Storage": {
    "Provider": "MySQL",
    "ConnectionString": "Server=localhost;Port=3306;Database=privatekonomi;User=privatekonomi_user;Password=DITT_LÖSENORD;",
    "SeedTestData": false
  }
}
```

3. **Skapa appsettings.local.json** för API-projektet:

```bash
cd ../Privatekonomi.Api
```

Skapa filen `appsettings.local.json`:

```json
{
  "Storage": {
    "Provider": "MySQL",
    "ConnectionString": "Server=localhost;Port=3306;Database=privatekonomi;User=privatekonomi_user;Password=DITT_LÖSENORD;",
    "SeedTestData": false
  }
}
```

**OBS:** Filerna `appsettings.local.json` är i `.gitignore` och kommer inte committas till Git.

### Kör applikationen lokalt

```bash
# Från projektets rot
cd /home/runner/work/Privatekonomi/Privatekonomi

# Kör med Aspire (rekommenderat)
cd src/Privatekonomi.AppHost
dotnet run

# Eller kör individuella tjänster
cd ../Privatekonomi.Web
dotnet run

# I annan terminal för API
cd ../Privatekonomi.Api
dotnet run
```

Vid första körningen kommer Entity Framework att skapa alla tabeller automatiskt.

### Skapa migration (om datamodellen ändras)

```bash
cd src/Privatekonomi.Core
dotnet ef migrations add InitialMySqlMigration --context PrivatekonomyContext
```

## Deployment till Webbhotell

### Automatisk deployment via GitHub Actions

#### Steg 1: Skapa en release

Deploya automatiskt genom att skapa en version tag:

```bash
# Se till att alla ändringar är committade
git status

# Skapa och pusha tag
git tag -a v1.0.0 -m "Initial MySQL deployment"
git push origin v1.0.0
```

#### Steg 2: Övervaka deployment

1. Gå till **Actions** i GitHub
2. Hitta workflow-körningen "Release and Deploy to Web Hosting"
3. Följ med i följande steg:
   - ✅ **Build** - Bygger både Web och API
   - ✅ **Deploy Web** - Deployas webbapplikation via SFTP
   - ✅ **Deploy API** - Deployas API via SFTP
   - ✅ **Create Release** - Skapar GitHub Release med distributionsfiler

#### Steg 3: Verifiera deployment

**På servern (via SSH):**

```bash
# Kontrollera Web-filerna
ssh user@your-server
cd /var/www/privatekonomi-web
ls -la
cat DEPLOYMENT_INFO.txt

# Kontrollera API-filerna
cd /var/www/privatekonomi-api
ls -la
cat DEPLOYMENT_INFO.txt
```

**Testa applikationen:**

1. Öppna webbläsaren och gå till din produktion-URL
2. Logga in (testanvändare om SeedTestData=true)
3. Verifiera att data sparas korrekt i MySQL

### Manuell deployment

Om automatisk deployment inte fungerar kan du deployas manuellt:

#### Steg 1: Ladda ner release

1. Gå till **Releases** på GitHub
2. Ladda ner:
   - `privatekonomi-web-v1.0.0-linux-x64.tar.gz`
   - `privatekonomi-api-v1.0.0-linux-x64.tar.gz`

#### Steg 2: Extrahera och konfigurera

```bash
# Extrahera Web
tar -xzf privatekonomi-web-v1.0.0-linux-x64.tar.gz -C /tmp/web-deploy

# Skapa production config för Web
cat > /tmp/web-deploy/appsettings.Production.json << EOF
{
  "Storage": {
    "Provider": "MySQL",
    "ConnectionString": "Server=mysql.myhost.com;Port=3306;Database=privatekonomi;User=privatekonomi_user;Password=DITT_LÖSENORD;",
    "SeedTestData": false
  }
}
EOF

# Extrahera API
tar -xzf privatekonomi-api-v1.0.0-linux-x64.tar.gz -C /tmp/api-deploy

# Skapa production config för API
cat > /tmp/api-deploy/appsettings.Production.json << EOF
{
  "Storage": {
    "Provider": "MySQL",
    "ConnectionString": "Server=mysql.myhost.com;Port=3306;Database=privatekonomi;User=privatekonomi_user;Password=DITT_LÖSENORD;",
    "SeedTestData": false
  }
}
EOF
```

#### Steg 3: Uploada till server

```bash
# Web via SFTP
sftp user@your-server
put -r /tmp/web-deploy/* /var/www/privatekonomi-web/
exit

# API via SFTP
sftp user@your-server
put -r /tmp/api-deploy/* /var/www/privatekonomi-api/
exit
```

#### Steg 4: Starta tjänsterna

Se [SYSTEMD_SERVICE_GUIDE.md](./SYSTEMD_SERVICE_GUIDE.md) för att konfigurera systemd-tjänster för automatisk start.

## Aspire Dashboard

### Status

.NET Aspire Dashboard är primärt designad för lokal utveckling och kan inte enkelt deployeras till traditionella webbhotell som endast stöder SFTP.

### Alternativ

1. **Lokal utveckling:** Använd Aspire Dashboard lokalt för utveckling och debugging
   ```bash
   cd src/Privatekonomi.AppHost
   dotnet run
   ```

2. **Container deployment:** Om webbhotellet stöder Docker containers:
   - Skapa Docker image för Aspire Dashboard
   - Deployas som container tillsammans med Web och API
   - Containerstöd och orkestrering beror på aktuell hostingmiljö; se [Aspire-guiden](ASPIRE_GUIDE.md) för lokal utveckling.

3. **Separat monitoring:** Använd webbhotellets inbyggda monitoring eller:
   - Application Insights (Azure)
   - Elastic APM
   - Datadog
   - New Relic

### Rekommendation

För produktion på webbhotell rekommenderar vi att:
- Använd Aspire Dashboard endast lokalt
- Implementera strukturerad logging (Serilog till fil eller extern tjänst)
- Konfigurera health check endpoints
- Använd webbhotellets monitoring-verktyg

## Felsökning

### Problem: Connection String fel

**Symptom:** `MySqlException: Unable to connect to any of the specified MySQL hosts`

**Lösningar:**

1. Verifiera hostname och port:
   ```bash
   telnet mysql.example.com 3306
   # eller
   nc -zv mysql.example.com 3306
   ```

2. Kontrollera användarnamn och lösenord
3. Se till att användarens behörigheter är korrekta
4. Kontrollera firewall-regler

### Problem: Entity Framework skapar inte tabeller

**Symptom:** `MySqlException: Table 'privatekonomi.Transactions' doesn't exist`

**Lösning:**

Entity Framework skapar tabeller automatiskt vid första körningen. Om detta inte händer:

```bash
# Försäkra dig om att EnsureCreated körs
# Kontrollera Program.cs i både Web och API
```

Alternativt, skapa tabellerna manuellt via migrations:

```bash
cd src/Privatekonomi.Core
dotnet ef database update --context PrivatekonomyContext
```

### Problem: Performance issues

**Symptom:** Långsam respons från applikationen

**Lösningar:**

1. **Aktivera connection pooling** (aktiverad som standard):
   ```
   Server=mysql.example.com;Port=3306;Database=privatekonomi;User=user;Password=pass;Pooling=true;MinimumPoolSize=5;MaximumPoolSize=100;
   ```

2. **Optimera index:** Kontrollera att alla index är skapade
   ```sql
   SHOW INDEX FROM Transactions;
   ```

3. **Öka server resources:** Kontrollera RAM och CPU-användning

### Problem: Character encoding issues

**Symptom:** Svenska tecken (å, ä, ö) visas felaktigt

**Lösning:**

Säkerställ att databasen använder UTF-8:

```sql
-- Kontrollera encoding
SHOW VARIABLES LIKE 'character_set%';

-- Ändra om nödvändigt
ALTER DATABASE privatekonomi CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

### Problem: Timeout under deployment

**Symptom:** SFTP timeout i GitHub Actions

**Lösningar:**

1. Kontrollera att SFTP_PORT är korrekt
2. Verifiera att servern accepterar anslutningar från GitHub Actions IP-adresser
3. Öka timeout i workflow (om möjligt)
4. Kontakta webbhotelleverantör för firewall-regler

## Säkerhetsrekommendationer

### Databas-säkerhet

- ✅ Använd starka lösenord (minst 16 tecken)
- ✅ Begränsa fjärråtkomst (endast från applikationsserver)
- ✅ Regelbundna backups (dagligen)
- ✅ Kryptera backups
- ✅ Håll MySQL/MariaDB uppdaterad

### Connection String-säkerhet

- ✅ Lagra aldrig connection strings i källkod
- ✅ Använd GitHub Secrets för CI/CD
- ✅ Använd environment variables på servern
- ✅ Rotera lösenord regelbundet

### Applikations-säkerhet

- ✅ Använd HTTPS (SSL-certifikat)
- ✅ Sätt `ASPNETCORE_ENVIRONMENT=Production`
- ✅ Aktivera rate limiting
- ✅ Implementera säkerhetsloggning

## Resurser

### Dokumentation

- [README](../README.md)
- [Deployment Guide](./DEPLOYMENT_GUIDE.md)
- [Storage Guide](./STORAGE_GUIDE.md)
- [Systemd Service Guide](./SYSTEMD_SERVICE_GUIDE.md)

### Externa länkar

- [Pomelo.EntityFrameworkCore.MySql](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql)
- [MySQL Documentation](https://dev.mysql.com/doc/)
- [MariaDB Documentation](https://mariadb.com/kb/en/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)

## Support

Om du stöter på problem:

1. Kontrollera [Felsökning](#felsökning) ovan
2. Sök i [GitHub Issues](https://github.com/pownas/Privatekonomi/issues)
3. Skapa en ny issue med:
   - Detaljerad beskrivning av problemet
   - Felmeddelanden (loggar)
   - Steg för att återskapa problemet
   - Information om miljön (MySQL version, .NET version, OS)

---

**Senast uppdaterad:** 2025-11-09  
**Version:** 1.0.0  
**Dokumenterad av:** GitHub Copilot Coding Agent
