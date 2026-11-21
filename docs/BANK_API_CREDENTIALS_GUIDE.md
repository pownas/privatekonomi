# Hur man erhåller API-nycklar för bankintegrationer

> **Historisk/experimentell information.** Bankkopplingsendpoints och
> OAuth-callbacken är spärrade i den aktuella API-konfigurationen. Callback-
> adresser och tokenlivslängder i detta dokument är inte verifierade
> driftinstruktioner. Se [säkerhetsguiden](LEGACY_API_SECURITY.md) innan någon
> bankintegration aktiveras.

Denna guide beskriver hur du erhåller nödvändiga API-nycklar och credentials för att integrera med Swedbank, Avanza Bank och ICA Banken.

## Innehåll

- [Swedbank (PSD2)](#swedbank-psd2)
- [ICA Banken (PSD2)](#ica-banken-psd2)
- [Avanza Bank](#avanza-bank)
- [Säkerhet och best practices](#säkerhet-och-best-practices)

---

## Swedbank (PSD2)

Swedbank använder PSD2-standarden för Open Banking. För att integrera med Swedbanks API behöver du registrera din applikation som en Third Party Provider (TPP).

### Steg för att erhålla API-nycklar

#### 1. Förberedelser

**Krav:**
- Företagsregistrering eller privatperson med F-skattsedel
- eIDAS-certifikat (för produktion)
- Teknisk kunskap om OAuth2 och PSD2

#### 2. Registrera hos Swedbank

1. **Besök Swedbanks developer portal:**
   - Sandbox: [https://developer.swedbank.com](https://developer.swedbank.com)
   - Produktionsmiljö kräver fullständig registrering

2. **Skapa ett utvecklarkonto:**
   - Registrera dig på developer portalen
   - Verifiera din e-postadress

3. **Registrera din applikation:**
   - Logga in på developer portalen
   - Navigera till "My Applications" eller "Mina Applikationer"
   - Klicka på "Create New Application"

4. **Fyll i applikationsdetaljer:**
   - **Application Name**: Namnet på din app (t.ex. "Privatekonomi")
   - **Description**: Kort beskrivning av din applikation
   - **Redirect URIs**: Lägg till:
     - För lokal utveckling: `http://localhost:5001/api/bankconnections/callback`
     - För produktion: `https://din-domän.se/api/bankconnections/callback`
   - **API Access**: Välj "Account Information Service (AIS)"

5. **Erhåll credentials:**
   - Efter registrering får du:
     - **Client ID**: En unik identifierare för din app
     - **Client Secret**: En hemlig nyckel (spara denna säkert!)

#### 3. Sandbox vs Produktion

**Sandbox (Testmiljö):**
- Använd för utveckling och testning
- Kräver inget eIDAS-certifikat
- Inga riktiga bankuppgifter används
- Begränsad API-funktionalitet

**Produktion:**
- Kräver eIDAS-certifikat (Qualified Website Authentication Certificate)
- Fullständig registrering som TPP
- Kontakta Swedbank för produktionsåtkomst
- Kostnad kan tillkomma

#### 4. eIDAS-certifikat (för produktion)

För produktionsmiljö behöver du ett eIDAS-certifikat:

1. **Välj en certifikatutfärdare:**
   - Exempel: DigiCert, GlobalSign, Telia
   
2. **Ansök om QWAC (Qualified Website Authentication Certificate)**

3. **Registrera certifikatet hos Swedbank:**
   - Skicka certifikatinformation till Swedbank
   - Vänta på godkännande

### Konfiguration i Privatekonomi

Lägg till följande i `appsettings.json` eller använd User Secrets för utveckling:

```json
{
  "Swedbank": {
    "ClientId": "din-client-id-från-swedbank",
    "ClientSecret": "din-client-secret-från-swedbank",
    "Environment": "sandbox"  // eller "production"
  }
}
```

**För produktion:**
```bash
# Använd miljövariabler
export Swedbank__ClientId="din-client-id"
export Swedbank__ClientSecret="din-client-secret"
export Swedbank__Environment="production"
```

---

## ICA Banken (PSD2)

ICA Banken använder Nordic API Gateway (NAG) för sin PSD2-implementation.

### Steg för att erhålla API-nycklar

#### 1. Kontakta ICA Banken

**OBS:** ICA Bankens PSD2-API är inte öppet tillgängligt för alla utvecklare.

1. **Kontakta ICA Bankens API-support:**
   - E-post: [Kontrollera ICA Bankens hemsida för aktuell kontaktinformation]
   - Beskriv ditt användningsfall och behov av API-åtkomst

2. **Förklara din integration:**
   - Beskriv applikationen och dess syfte
   - Antal förväntade användare
   - Teknisk arkitektur

#### 2. Registrering

Om ICA Banken godkänner din ansökan:

1. **Fyll i registreringsformulär:**
   - Företagsinformation eller privatperson
   - Teknisk kontaktperson
   - Applikationsdetaljer

2. **Tekniska krav:**
   - Redirect URIs för OAuth2-callback
   - Webhook-URLs (om tillämpligt)

3. **Erhåll credentials:**
   - **Client ID**: Från ICA Banken
   - **Client Secret**: Från ICA Banken
   - **API Documentation**: Tillgång till teknisk dokumentation

#### 3. Sandbox-miljö

- ICA Banken kan erbjuda en sandbox-miljö
- Kontakta ICA Banken för åtkomst
- Testdata och mock-svar kan vara tillgängliga

### Konfiguration i Privatekonomi

```json
{
  "IcaBanken": {
    "ClientId": "din-client-id-från-ica",
    "ClientSecret": "din-client-secret-från-ica",
    "Environment": "sandbox"  // eller "production"
  }
}
```

**För produktion:**
```bash
# Använd miljövariabler
export IcaBanken__ClientId="din-client-id"
export IcaBanken__ClientSecret="din-client-secret"
export IcaBanken__Environment="production"
```

---

## Avanza Bank

**VIKTIGT:** Avanza Bank använder **INTE** PSD2-standarden. Istället använder de ett proprietary API som inte kräver Client ID eller Client Secret på samma sätt.

### Autentiseringsmetod

Avanza använder användarnamn och lösenord med sessionsbaserad autentisering:

1. **Användaren loggar in** via Privatekonomi-gränssnittet
2. **Autentisering sker** med Avanza-användarnamn och lösenord
3. **Session skapas** och lagras temporärt
4. **TOTP (2FA)** kan krävas om aktiverat på kontot

### Ingen förregistrering krävs

**Du behöver INTE:**
- Registrera en applikation hos Avanza
- Erhålla Client ID eller Client Secret
- Ansöka om API-åtkomst

### Användarens perspektiv

Användaren behöver:

1. **Ett aktivt Avanza-konto**
   - Skapa konto på [www.avanza.se](https://www.avanza.se)

2. **Inloggningsuppgifter:**
   - Användarnamn (personnummer eller användarnamn)
   - Lösenord

3. **TOTP-app (om 2FA är aktiverat):**
   - Google Authenticator
   - Microsoft Authenticator
   - Eller annan TOTP-kompatibel app

### Användning i Privatekonomi

Ingen konfiguration i `appsettings.json` krävs för Avanza. Användaren anger sina uppgifter direkt i UI:t när de ansluter sitt Avanza-konto.

**I appsettings.json kan du lägga till en notis:**
```json
{
  "Avanza": {
    "Note": "Avanza använder inte PSD2. Användare loggar in direkt med sina Avanza-uppgifter via UI."
  }
}
```

### Säkerhetsöverväganden för Avanza

- **Lagra ALDRIG** användarens Avanza-lösenord i klartext
- **Sessioner** har begränsad livslängd och måste förnyas
- **2FA** rekommenderas starkt för användare
- **Transaktionsdata** lagras lokalt efter import

### API-begränsningar

Avanza's API är **inofficiellt** och kan ändras utan förvarning:
- Ingen officiell dokumentation från Avanza
- Community-driven API-specifikation
- Risk för breaking changes
- Använd på egen risk

**Rekommendation:** Informera användare om att detta är en inofficiell integration.

---

## Säkerhet och Best Practices

### Lagring av Credentials

#### ❌ Gör ALDRIG detta:

```json
// appsettings.json (i version control)
{
  "Swedbank": {
    "ClientId": "abc123real",
    "ClientSecret": "secret123real"
  }
}
```

#### ✅ Gör istället:

**1. Använd User Secrets (Utveckling):**

```bash
cd Privatekonomi.Api
dotnet user-secrets set "Swedbank:ClientId" "din-client-id"
dotnet user-secrets set "Swedbank:ClientSecret" "din-client-secret"
```

**2. Använd Environment Variables (Produktion):**

```bash
# Linux/Mac
export Swedbank__ClientId="din-client-id"
export Swedbank__ClientSecret="din-client-secret"

# Windows PowerShell
$env:Swedbank__ClientId="din-client-id"
$env:Swedbank__ClientSecret="din-client-secret"
```

**3. Använd Azure Key Vault (Produktion):**

```csharp
// Program.cs
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

### Redirect URIs

Konfigurera korrekt redirect URIs i respektive banks developer portal:

**Lokal utveckling:**
```
http://localhost:5001/api/bankconnections/callback
http://localhost:7023/api/bankconnections/callback
```

**Produktion:**
```
https://din-domän.se/api/bankconnections/callback
```

### HTTPS i Produktion

- Använd **ALLTID** HTTPS i produktion
- Installera giltiga SSL-certifikat (Let's Encrypt är gratis)
- Aktivera HSTS (HTTP Strict Transport Security)

### Token Management

- Tokens krypteras automatiskt av Privatekonomi
- Tokens förvaras säkert i databasen
- Refresh tokens används för att förnya åtkomst
- Tokens har begränsad livslängd (90-180 dagar)

### GDPR

- Informera användare om datahantering
- Användare kan när som helst ta bort sina bankkopplingar
- Transaktionsdata lagras lokalt enligt användarens val
- Se [PSD2_SECURITY_GDPR.md](PSD2_SECURITY_GDPR.md) för mer information

## Felsökning

### Problem: Kan inte erhålla Client ID från Swedbank

**Lösning:**
- Kontrollera att du har ett verifierat konto på developer portalen
- Se till att din applikation är korrekt registrerad
- Verifiera att du har valt rätt API-typ (AIS)

### Problem: OAuth callback-fel

**Lösning:**
- Kontrollera att redirect URI matchar exakt mellan:
  - Din appsettings.json
  - Bankens developer portal
  - Callback-implementationen i koden
- Glöm inte protokoll (http:// eller https://)
- Kontrollera port-nummer

### Problem: "Invalid client" fel

**Lösning:**
- Verifiera Client ID och Client Secret
- Kontrollera att du använder rätt miljö (sandbox vs production)
- Se till att credentials är korrekt konfigurerade

### Problem: Token expired

**Lösning:**
- Privatekonomi försöker automatiskt förnya tokens
- Om det misslyckas, be användaren autentisera på nytt
- Kontrollera refresh token-logik

## Support och Kontakt

### Bankernas Support

**Swedbank:**
- Developer Portal: [https://developer.swedbank.com](https://developer.swedbank.com)
- Support: Via developer portalen

**ICA Banken:**
- Kontakta ICA Bankens kundsupport eller företagsavdelning
- Begär kontakt med API-team

**Avanza:**
- Ingen officiell API-support
- Community: GitHub och forum

### Privatekonomi Support

För frågor om Privatekonomi-implementationen:
- Öppna ett issue på GitHub
- Se befintlig dokumentation i `/wiki`
- Kontrollera [PSD2_API_GUIDE.md](PSD2_API_GUIDE.md)

## Relaterad Dokumentation

- [PSD2_API_GUIDE.md](PSD2_API_GUIDE.md) - Användningsguide för PSD2-API
- [PSD2_SECURITY_GDPR.md](PSD2_SECURITY_GDPR.md) - Säkerhet och GDPR
- [AVANZA_IMPORT_GUIDE.md](AVANZA_IMPORT_GUIDE.md) - Avanza-specifik guide

## Changelog

### Version 1.0 (2025-10-26)
- Initial version med instruktioner för Swedbank, ICA Banken och Avanza
- Säkerhetsriktlinjer och best practices
- Felsökningssektion
