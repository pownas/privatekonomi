# Kom igång och dela Privatekonomi hemma

Den här guiden hjälper dig att starta Privatekonomi på en Mac eller Raspberry Pi, skapa familjens konton och dela appen på ett privat nätverk.

## 1. Installera och starta appen

Följ installationsguiden för din plattform:

- [macOS-installation](scripts/macos/installation.md)
- [Raspberry Pi-installation](scripts/raspberry-pi/RASPBERRY_PI_första_installationen.md)

I macOS- och Raspberry Pi-installationerna i den här guiden väljer du SQLite om du vill följa stegen för systemadministratör längre ner. JSON-lagring fungerar för grundläggande användning, men SQLite-instruktionen för `IsSystemAdmin` gäller inte JSON.

När installationen är klar startar du tjänsterna enligt plattformens guide. Kontrollera sedan på själva värddatorn att webbappen öppnas:

```text
http://localhost:5274
```

API:t använder port `5277`; för appens vanliga användning är det webbplatsen på port `5274` som ska delas.

## 2. Skapa ägarens konto först

Öppna webbappen på värddatorn och skapa ditt konto via **Registrera**. Slutför inloggningen och onboarding innan du bjuder in familjen. Ge inte ut ägarens inloggningsuppgifter; varje person bör använda sitt eget konto.

## 3. Anslut från familjens enheter

Hitta värddatorns lokala IP-adress:

```bash
# macOS: visa nätverksgränssnitt och adresser
ipconfig getifaddr en0

# Raspberry Pi
hostname -I
```

Om Macens aktiva nätverk inte är `en0`, kontrollera nätverksinställningarna i macOS. Öppna sedan `http://<IP-adress>:5274` i en annan enhets webbläsare. Exempel: `http://192.168.1.25:5274`.

Enheterna måste vara anslutna till samma privata hemnätverk. Om anslutningen inte fungerar, kontrollera att värddatorn är på, att appen är igång och att routern inte isolerar Wi-Fi-enheter från varandra. macOS kan också fråga om inkommande nätverksanslutningar för .NET ska tillåtas. Använd en DHCP-reservation i routern om du vill att adressen ska vara stabil.

På macOS körs tjänsterna som LaunchAgents och startar när den installerade användaren loggar in. Datorn måste därför vara på och användaren inloggad för att familjen ska kunna ansluta. På Raspberry Pi behöver tjänsterna vara konfigurerade för automatisk start.

Anslutningen använder HTTP på det lokala nätverket och är inte krypterad. Dela inte adressen på internet och vidarebefordra inte port `5274` eller `5277` i routern. För åtkomst utanför hemmet ska du använda en betrodd VPN-lösning till hemnätverket.

## 4. Skapa familjekonton och hushåll

1. Låt varje familjemedlem registrera ett eget konto från webbappen med sin egen e-postadress.
2. Logga in och öppna **Familj & Hushåll → Hushåll**.
3. Skapa ett hushåll. Öppna det och lägg till varje person som medlem med samma e-postadress som personens appkonto.
4. Be sedan varje person logga in på sitt eget konto, öppna hushållet och välja **Koppla mitt konto** på sin medlemsrad. E-postadressen för kontot måste matcha den okopplade medlemmen.
5. Varje kopplad medlem väljer själv om hen samtycker till delning av balansräkningen. Först när alla aktiva medlemmar med kopplade appkonton har samtyckt kan delningen aktiveras.

Balansräkningsdelning är frivillig och omfattar de kopplade användarnas konton, investeringar, tillgångar, lån och transaktioner. Var och en kan återkalla sitt samtycke; då stängs hushållets delning av. Skapa bara ett hushåll och aktivera delning när alla förstår vad som delas.

## 5. Säkerhetskopiera och kontrollera återställning

Kontrollera efter installationen att automatisk backup är konfigurerad och att en ny backupfil faktiskt skapas. På macOS ligger backuper normalt i `~/Library/Application Support/Privatekonomi-Backups/`; på Raspberry Pi i `~/privatekonomi-backups/`.

Gör en återställningsövning innan du börjar använda appen med viktig data: kopiera en backup till en separat testplats och kontrollera att den går att öppna. Återställ aldrig över den aktiva databasen utan att först stoppa Web och API och spara en extra kopia. En backup på samma dator skyddar inte mot förlust eller haveri av datorn; kopiera backupfiler till en annan enhet eller en säker lagring.

## 6. Ge ägarkontot systemadministratörsbehörighet (endast SQLite)

`IsSystemAdmin` ger åtkomst till systemadministrativa funktioner, exempelvis sidan **Drift**. Utför ändringen bara för ett konto du själv kontrollerar. Skapa först kontot i appen och kontrollera att SQLite är vald lagring.

Installera SQLite-kommandoradsverktyget om `sqlite3` saknas:

```bash
# macOS med Homebrew
brew install sqlite
export PATH="$(brew --prefix sqlite)/bin:$PATH"

# Raspberry Pi OS / Debian
sudo apt update
sudo apt install -y sqlite3
```

Kontrollera installationen:

```bash
sqlite3 --version
```

Stoppa båda apptjänsterna innan databasen ändras. På macOS öppnar du terminalen i repositoryts rot:

```bash
launchctl bootout "gui/$(id -u)/com.privatekonomi.web" 2>/dev/null || true
launchctl bootout "gui/$(id -u)/com.privatekonomi.api" 2>/dev/null || true
```

På Raspberry Pi där systemd-tjänster installerats:

```bash
sudo systemctl stop privatekonomi-web.service privatekonomi-api.service
```

Välj sökväg för din plattform och kopiera databasen till backupmappen innan du fortsätter:

```bash
# macOS
DB="$HOME/Library/Application Support/Privatekonomi/privatekonomi.db"
BACKUP_DIR="$HOME/Library/Application Support/Privatekonomi-Backups"

# Raspberry Pi: använd i stället dessa två rader
# DB="$HOME/privatekonomi-data/privatekonomi.db"
# BACKUP_DIR="$HOME/privatekonomi-backups"

umask 077
if [[ ! -s "$DB" ]]; then
    echo "Databasen saknas eller är tom: $DB" >&2
    exit 1
fi
mkdir -p "$BACKUP_DIR"
BACKUP_FILE="$BACKUP_DIR/privatekonomi-before-admin-$(date +%Y%m%d-%H%M%S).db"
cp "$DB" "$BACKUP_FILE"
chmod 600 "$BACKUP_FILE"
if [[ ! -s "$BACKUP_FILE" ]]; then
    echo "Backupen kunde inte verifieras: $BACKUP_FILE" >&2
    exit 1
fi
echo "Backup skapad: $BACKUP_FILE"
```

Om databasen inte finns på den valda sökvägen, kontrollera lagringsprovider och anslutningssträng i `appsettings.Production.json`. Fortsätt inte med någon annan databas än den som appen faktiskt använder.

Öppna sedan databasen:

```bash
sqlite3 "$DB"
```

Kontrollera e-postadress och ID innan du ändrar något:

```sql
.headers on
.mode column
SELECT Id, Email, IsSystemAdmin
FROM AspNetUsers
ORDER BY Email;
```

Kopiera `Id` för ägarkontot. Kör uppdateringen med just det ID:t:

```sql
BEGIN IMMEDIATE;
UPDATE AspNetUsers
SET IsSystemAdmin = 1
WHERE Id = 'KLISTRA_IN_ÄGARKONTOTS_EXAKTA_ID';
SELECT changes();
SELECT Id, Email, IsSystemAdmin
FROM AspNetUsers
WHERE Id = 'KLISTRA_IN_ÄGARKONTOTS_EXAKTA_ID';
```

Innan du kör `COMMIT`, kontrollera att `changes()` visar `1` och att rätt e-postadress visas med `IsSystemAdmin = 1`. Om resultatet är oväntat, kör `ROLLBACK;` i stället. När resultatet stämmer:

```sql
COMMIT;
.quit
```

Starta tjänsterna igen:

```bash
# macOS, från repositoryts rot
bash scripts/macos/start.sh

# Raspberry Pi med systemd
sudo systemctl start privatekonomi-api.service privatekonomi-web.service
```

Logga ut och in igen och kontrollera att **Drift** är tillgängligt för ägarkontot. Ändra inte andra användares behörigheter utan deras uttryckliga godkännande.

## Relaterade guider

- [Appens onboarding-flöde](ONBOARDING_GUIDE.md)
- [macOS: installation, drift och backup](scripts/macos/installation.md)
- [Raspberry Pi: snabbstart](scripts/raspberry-pi/RASPBERRY_PI_QUICKSTART.md)
- [Hushållens samtycke till delning](USER_AUTHENTICATION.md)
