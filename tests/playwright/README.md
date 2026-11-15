# Playwright Tests för Privatekonomi

Detta projekt innehåller end-to-end tester för Privatekonomi-applikationen med hjälp av Playwright.

## Översikt

Testerna verifierar att testdata visas korrekt i användargränssnittet, särskilt på transaktionssidan där ca 50 transaktioner med olika kategorier, datum och belopp presenteras.

## Förutsättningar

- [Node.js](https://nodejs.org/) (v16 eller senare)
- [npm](https://www.npmjs.com/)
- .NET 11 SDK (för att köra webbapplikationen)

## Installation

1. Installera npm-beroenden:
```bash
cd tests/playwright
npm install
```

2. Installera Playwright-browsers:
```bash
npx playwright install chromium
```

Om du stöter på problem med browserinstallationen, installera systemdependencies först:
```bash
npx playwright install-deps chromium
```

## Köra tester

### Köra alla tester
```bash
npm test
```

### Köra tester i headed mode (synlig browser)
```bash
npm run test:headed
```

### Köra tester i debug-läge
```bash
npm run test:debug
```

### Köra tester med UI-läge
```bash
npm run test:ui
```

### Visa testrapport
```bash
npm run test:report
```

## Testdata

Applikationen använder en in-memory databas som seedas med testdata vid start. Testdata består av:

- **50 transaktioner** med olika:
  - Datum (spridda över de senaste 3 månaderna)
  - Belopp (varierar beroende på kategori)
  - Beskrivningar (realistiska svenska exempel)
  - Kategorier (9 fördefinierade kategorier)

### Kategorier som används

1. **Mat & Dryck** - ICA Maxi, Coop, Restauranger, etc.
2. **Transport** - SL-kort, Bensin, Parkering, etc.
3. **Boende** - Hyra, El, Bredband, Försäkring
4. **Nöje** - Bio, Spotify, Netflix, Gym
5. **Shopping** - H&M, IKEA, Elgiganten, etc.
6. **Hälsa** - Tandvård, Apotek, Naprapat, etc.
7. **Lön** - Lön, Bonus, Semesterersättning
8. **Sparande** - Sparkonto, Aktier, ISK
9. **Övrigt** - Swish, Gåvor, Diverse

## Testfall

### transactions.spec.ts

Denna testsvit verifierar transaktionssidan:

1. **should display all 50 seeded transactions** - Verifierar att alla 50 transaktioner visas
2. **should display transaction details correctly** - Kontrollerar att alla kolumner (Datum, Beskrivning, Kategori, Belopp, Åtgärder) finns
3. **should display transaction with date, description, category and amount** - Verifierar att en transaktion innehåller korrekt formaterad data
4. **should be able to search/filter transactions** - Testar sökfunktionalitet
5. **should display different categories with colored chips** - Verifierar att kategorier visas med färgkodade chips
6. **should display both income and expense transactions** - Kontrollerar att både inkomster och utgifter visas
7. **should have delete button for each transaction** - Verifierar att varje transaktion har en raderingsknapp

## Konfiguration

Playwright-konfigurationen finns i `playwright.config.ts`. Den inkluderar:

- Automatisk start av webbservern (dotnet run) vid testkörning
- Chromium som testbrowser
- Skärmdumpar vid fel
- HTML-rapporter
- Retry-logik för flakiga tester

## Struktur

```
tests/playwright/
├── README.md                     # Denna fil
├── package.json                  # npm-konfiguration och scripts
├── playwright.config.ts          # Playwright-konfiguration
└── tests/
    └── transactions.spec.ts      # Transaktionssidans tester
```

## Felsökning

### Problem med browserinstallation

Om `npx playwright install` misslyckas, prova:

1. Installera systemdependencies:
```bash
npx playwright install-deps chromium
```

2. Manuell nedladdning (endast vid behov):
```bash
# Ladda ner manuellt från Playwright CDN
wget https://playwright.azureedge.net/builds/chromium/1194/chromium-headless-shell-linux.zip
# Extrahera till ~/.cache/ms-playwright/chromium_headless_shell-1194/
```

### Applikationen körs inte

Om testerna misslyckas med "connection refused", säkerställ att:
1. .NET 11 SDK är installerat
2. Applikationen kan starta med `dotnet run` från `src/Privatekonomi.Web`
3. Port 5274 är tillgänglig

### Tester timeout

Om tester tar för lång tid eller timeout, öka timeout-värdet i `playwright.config.ts` eller i enskilda tester.

## CI/CD

För att köra tester i CI/CD-miljö, sätt miljövariabeln `CI=true`:

```bash
CI=true npm test
```

Detta aktiverar:
- Automatiska retries (2 gånger)
- Parallellitet begränsad till 1 worker
- Ingen återanvändning av befintlig server

## Bidra

När du lägger till nya tester:
1. Följ befintlig teststruktur
2. Använd beskrivande testnamn
3. Lägg till kommentarer för komplexa assertions
4. Verifiera att testerna är stabila genom att köra dem flera gånger

## Ytterligare resurser

- [Playwright-dokumentation](https://playwright.dev/)
- [Playwright Test](https://playwright.dev/docs/intro)
- [Playwright Best Practices](https://playwright.dev/docs/best-practices)
