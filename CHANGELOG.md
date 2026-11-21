# Ändringslogg

## 0.1.0-alfa

Första versionsmärkningen av den befintliga applikationen. Detta är en
alfa-version, inte en beskrivning av en färdig backendmigrering.

### Pågående backendmigrering

- `/api-info` visar version och tillgänglig bygginformation samt värdmiljö och konfigurerad lagringsprovider.
- API:ets demodata kräver ett uttryckligt val och ett nytt InMemory-lager. Okända lagringsproviders avvisas.
- Äldre API-endpoints är spärrade tills de har säker användarisolering. Det påverkar bland annat bankkopplingar och notifieringsåtgärder; se [säkerhetsinformationen](docs/LEGACY_API_SECURITY.md).
- Pi-installationen avvisar delad JsonFile-lagring för Web och API för att undvika att processerna skriver över varandras data.
- Webbappen använder fortfarande databasen direkt; migreringen är inte färdig.

### Befintliga funktioner

- Dashboard, transaktionshantering och budgetuppföljning. Se [README](README.md).
- Sparmål, investeringar och pensioner. Se [investerings- och pensionsguiden](docs/INVESTMENT_PENSION_GUIDE.md).
- Hushåll och delad ekonomi. Se [guiden för delad ekonomi](docs/DELAD_EKONOMI_GUIDE.md).
- CSV-import och datahantering. Se [CSV-importguiden](docs/CSV_IMPORT_GUIDE.md) och [lagringsguiden](docs/STORAGE_GUIDE.md).
- Installerbar PWA med offlinefunktioner och plattformsberoende begränsningar. Se [PWA-guiden](docs/PWA_GUIDE.md).

### Versionsinformation

- Appversionen anges centralt och visas tillsammans med byggnummer och källkodens SHA i navigationsmenyn.
- GitHub Actions byggnummer används vid CI-byggen. Om byggnummer eller SHA saknas visas «okänd».
- Byggnumret kan också anges vid bygge med `-p:BuildNumber=123`. SHA hämtas från
  byggd assembly eller, om den saknas där, från `GITHUB_SHA` vid byggtillfället.
- Uppdateringsskripten för Raspberry Pi och macOS skickar med källkodens SHA
  även när publiceringen byggs från ett Git-arkiv utan `.git`.
