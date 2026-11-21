# Ändringshistorik

En sammanfattning av större förändringar från projektstarten den 16 oktober 2025
till och med commit `ec3a23a` den 21 november 2026. Versionsnumren nedan är
**påhittade historiska milstolpar**, inte faktiska releaser eller Git-taggar.
Datumen anger ungefär när arbetet utfördes, inte utgivningsdatum. Funktioner
kan ha ändrats eller tagits bort senare.

De faktiska publicerade releaserna är [v0.0.1](https://github.com/pownas/privatekonomi/releases/tag/v0.0.1)
och [v0.0.2](https://github.com/pownas/privatekonomi/releases/tag/v0.0.2)
från den 9 november 2025 samt [v0.0.3](https://github.com/pownas/privatekonomi/releases/tag/v0.0.3)
från den 12 november 2025. De motsvarar inte milstolparna med samma nummer nedan.

## v0.0.15 – November 2026

- Uppgraderade till .NET 11 och Aspire 17 samt uppdaterade beroenden och
  konfiguration.
- Utökade hanteringen av investeringskonton, banker och kontotyper, inklusive
  parsning av innehav från Nordnet och Swedbank samt förbättrat bankval och
  filuppladdning.
- Lade till en SBOM-skanner med kontroll mot NuGets sårbarhetsdatabas och
  möjlighet till offlinekörning. Lade även till en REST API-avstämning mot
  DIGG:s profil och en sammanfattande bedömning.
- Förbättrade säkerheten kring hushållens balansräkningar och delade konton,
  samtycke, medlemskap och äldre data. Skyddade Identity-konton och begränsade
  konfigurerbar demoinitialisering till utvecklingsmiljön.
- Lade till filloggning med Serilog och stöd för att läsa loggar vid felsökning.
  Förbättrade databasens schemainitialisering och uppgradering av äldre kolumner.
- Flyttade installationsskript till `scripts` och byggde ut installation och
  uppdatering för macOS, Windows och Raspberry Pi, med bland annat tjänster,
  säkerhetskopiering och tydligare byggstatus.
- Rättade PWA-cacheproblem efter uppgraderingar, förbättrade onboarding och
  administration samt öppnade Swagger vid API-start.

## v0.0.14 – September–oktober 2026

- Förbättrade kontosäkerhet och inloggning med passkeys, kontroller av ägarskap
  för budgetar och konton samt hantering av saknade användaridentiteter.
- Lade till frivillig delning av balansräkningar inom hushåll, redigerbara
  BAS-kopplingar och tydligare hantering av bankkrediter.
- Lade till en administrativ vy för senaste ändringar och en driftvy för
  uppdateringar och omstarter. Transaktionsfiltrering och sidindelning flyttades
  till databasfrågor.
- Införde en valbar webbinitierad uppdaterare för Raspberry Pi med
  behörighetskontroller, hälsokontroller och återställning. Förbättrade även
  HTTPS-certifikat, Cloudflare DNS-01 och val av branch vid driftsättning.
- Förbättrade bankimport, registrering, responsiv layout och diagram samt tog
  bort ImageSharp-beroendet från OCR-bearbetningen.

## v0.0.13 – Juli–augusti 2026

- Införde central pakethantering, uppdaterade NuGet-beroenden och förbättrade
  byggkonfiguration, exempelkonfigurationer och loggning.
- Förbättrade CSV-importen med bankidentifiering, användaravgränsad
  dubblettkontroll, kodningshantering och detaljerade radvisa varningar.
  Normaliserade transaktionsdatum och rättade svenska tecken i äldre
  Swedbank-filer.
- Förbättrade Raspberry Pi-installation och uppdatering, inklusive separata
  systemd-tjänster, samt rättade problem i mobilens sidhuvud.

## v0.0.12 – Februari–mars 2026

- Förbättrade CSV-importen för ICA Banken, Swedbank och Avanza, inklusive
  Avanza-transaktioner som investeringar, kontonummer och hantering av
  filströmmar.
- Lade till fakturascheman och en bakgrundstjänst för påminnelser samt
  månadsvis partitionering av transaktioner vid JSON-lagring.
- Förbättrade transaktionslistans sidindelning och prestanda, tomma lägen,
  inloggningssidan, animationer och mobil/PWA-upplevelsen.
- Uppdaterade Aspire och MudBlazor, förbättrade tester och databasåtkomst samt
  åtgärdade flera bygg-, analys- och service worker-problem.

## v0.0.11 – December 2025

- Uppdaterade kontoplanen till BAS 2026, moderniserade lösningsformatet till
  `.slnx` och flyttade tester från xUnit till MSTest.
- Förbättrade dashboardens design, testdata, navigationsdiagnostik, Swagger
  och Swedbanks CSV-parser, bland annat för metadata och citerade fält.

## v0.0.10 – November 2025, efter v0.0.3-releasen

- Förbättrade den ekonomiska översikten med månadsrapport, budgetbyggare,
  snabbkategorisering och filter. Budgetförslag i onboarding började sparas.
- Lade till OFX-parser, spårning av importjobb och statusändpunkt samt
  förbättrade automatisk kategorisering och inloggning.
- Utökade hushållsfunktionerna med bilder för aktiviteter, återkommande
  uppgifter och Kanban-vy. Förbättrade även hantering av gemensamma budgetar.
- Rättade bland annat inloggning, CSV-import, delade sparmål, Safari/iOS-tema
  och bankval i kontodialoger.

## v0.0.9 – November 2025, v0.0.2–v0.0.3-releaserna

- Lade till MySQL/MariaDB och utökade releaseflödet och
  driftsättningsdokumentationen för webbhotell.
- Uppgraderade till .NET 10 LTS och Aspire 13.
- Lade till bolåneanalys med svenska amorteringskrav och ränterisk, simulering
  och prioritering av sparmål samt ett onboardingflöde i sex steg.
- Lade till en administrativ dashboard och förbättrade autentisering och
  gränssnitt för gemensamma sparmål.
- Förbättrade Raspberry Pi-installationen: .NET- och EF-verktyg, nätverksåtkomst,
  systemd och Nginx. Webben och API:et kördes direkt utan Aspire på Pi.

## v0.0.8 – November 2025, första releasen

- Lade till rollbaserad åtkomstkontroll, kontohantering och hushållens
  tidslinje och uppgifter.
- Införde delade budgetar och skuldbalansering samt analys av
  kassaflödesscenarier och förbättrade skuldstrategier.
- Lade till OCR-skanning av kvitton med Tesseract och en landningssida.
- Byggde ut installations-, nätverks- och uppdateringsstöd för Raspberry Pi
  samt lade till en releasepipeline.

## v0.0.7 – Början av november 2025

- Införde svenska hushållsbudgetmallar, budgetvarningar, målstolpar,
  avrundningssparande och påminnelser med snooze.
- Lade till utgiftsanalys och värmekarta, koppling mellan kvitton och
  transaktioner samt massåtgärder för transaktioner.
- Samlade importfunktionerna och förbättrade PWA-stöd, offlinehantering,
  navigering, mobilanpassning och testdata.

## v0.0.6 – Slutet av oktober 2025

- Lade till kvitton, fakturor, abonnemang och pensioner samt utökade
  investeringsöversikten med fler tillgångs- och kontotyper.
- Införde sparutmaningar, ML-baserad kategorisering, notifieringar,
  livsplanering och funktioner för försäkring och arv.
- Lade till ekonomiskt hälsopoäng, KALP, jämförelse med Konsumentverkets
  riktlinjer och kostnadsperiodisering.
- Utökade analys och visualisering med periodjämförelser, nettoförmögenhet
  och anpassningsbara dashboardwidgetar.

## v0.0.5 – Slutet av oktober 2025

- Lade till import och export av data, säkerhetskopiering och export per år.
  Kontoplanen utökades med BAS-inspirerade kontonummer.
- Införde historik för data, anpassningsbar dashboard, transaktionskalender,
  Sankey-diagram och etiketter med filtrering.
- Förbättrade navigering, tillgänglighet, mobillayout, tangentbordsgenvägar
  och grundläggande PWA-stöd.

## v0.0.4 – 22–25 oktober 2025

- Lade till sparfickor, kategoriöversikt och visualisering av nettoförmögenhet.
- Förbättrade transaktionsredigering, uppdelning på flera kategorier,
  hushållskoppling och budgetredigering.
- Införde hantering av utländsk valuta, API-fel enligt ProblemDetails samt
  lagring med InMemory, SQLite, SQL Server och JSON-fil.
- Förbättrade säkerheten för bankkopplingar med tokenkryptering,
  OAuth-state-validering och automatisk synkronisering.

## v0.0.3 – 21 oktober 2025

- Lade till användarautentisering med ASP.NET Core Identity och
  användaravgränsning av ekonomiska data.
- Införde regelbaserad automatisk kategorisering, budgetmallar, kassaflödesrapport
  och utökad export.
- Lade till lönehistorik, gemensamma sparmål, familjefunktioner och förbättrad
  skuldhantering.
- Utökade svenska integrationsfunktioner med bland annat ROT/RUT, K4 och
  SIE-export samt förbättrade mörkt läge och tillgänglighet.

## v0.0.2 – 17–20 oktober 2025

- Lade till Avanza-import och export för investeringar, uppdatering av
  aktiekurser via Yahoo Finance samt stöd för CSN-format i Swedbank-parsern.
- Införde hushållshantering, sparmål, fysiska tillgångar och balansräkning.
- Utökade API:et och datamodellerna samt lade grunden för bankkopplingar
  via PSD2.

## v0.0.1 – 16 oktober 2025

- Startade projektet och byggde grundapplikationen med Blazor, API, .NET och
  Aspire.
- Lade till transaktioner, kategorier, budgetar, lån och investeringar samt
  en dashboard med kategoriöversikt och diagram.
- Införde CSV-import från ICA Banken och Swedbank, testdata, Playwright-tester
  och grundläggande dokumentation.
