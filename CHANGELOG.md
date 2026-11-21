# Ändringshistorik (CHANGELOG / Release notes)

Alla betydande förändringar i detta projekt dokumenteras i den här filen.
Formatet följer [Keep a Changelog 1.1.0](https://keepachangelog.com/sv/1.1.0/)
och versionsnumren följer i huvudsak [Semantisk versionshantering](https://semver.org/lang/sv/).

## Ändringstyper

- `Added` – nya funktioner.
- `Changed` – ändringar i existerande funktionalitet.
- `Deprecated` – funktioner som snart tas bort.
- `Removed` – borttagna funktioner.
- `Fixed` – felrättningar.
- `Security` – åtgärdade eller dokumenterade säkerhetsproblem.

## [Unreleased]

### Changed

- Rättade Aspires konfigurationsprioritet så explicita AppHost-parametrar kan
  styra effektiva lagringsinställningar efter lokala filer och User Secrets.
- Lade till API-data profiler för Mock/InMemory, SQLite och SQL Server samt
  en explicit `web-api-base-url`-parameter.
- Bytte dataprofil och provider endast vid API-processens uppstart.
- `/api-info` visar nu den aktiva API-data profilen.
- Flyttade och dokumenterade backend-/frontend-lagringsgränsen; Web använder
  fortfarande eget datalager tills dataflödena är fullt migrerade till API:t.

### Security

- Mock-profil och testseeding nekas i Production.
- SQL Server-profilen kräver en separat anslutningssträng.
- Äldre API-controllers förblir spärrade tills deras användarisolering är klar.


## [v0.0.4](https://github.com/pownas/privatekonomi/releases/tag/v0.0.4) – November 2026

[Commit `ec3a23a`](https://github.com/pownas/privatekonomi/commit/ec3a23a3f716f6e962c0b0bd9f9d3274f444dc0c)
Koden i: [Diff mellan v0.0.3 och v0.0.4](https://github.com/pownas/privatekonomi/compare/v0.0.3...v0.0.4)
Koden i beta diff: [Diff mellan v0.0.4-beta och v0.0.4](https://github.com/pownas/privatekonomi/compare/d8bdfc8685e9a624fdc67fcebda289cc2d0fc328...ec3a23a3f716f6e962c0b0bd9f9d3274f444dc0c)

### Added

- Utökad hantering av investeringskonton, banker och kontotyper.
- SBOM-skanner med kontroll mot NuGets sårbarhetsdatabas och offlinekörning.
- REST API-avstämning mot DIGG:s profil och sammanfattande bedömning.
- Filloggning med Serilog och stöd för att läsa loggar vid felsökning.
- Utbyggd installation och uppdatering för macOS, Windows och Raspberry Pi,
  inklusive tjänster, säkerhetskopiering och tydligare byggstatus.

### Changed

- Uppgraderat till .NET 11 och Aspire 17 samt uppdaterade beroenden och konfiguration.
- Flyttat installationsskript till `scripts`.
- Förbättrad parsning av Nordnet- och Swedbank-innehav, bankval och filuppladdning.
- Förbättrad schemainitialisering och uppgradering av äldre databaskolumner.
- Förbättrad onboarding och administration samt Swagger öppnas vid API-start.

### Fixed

- Rättade PWA-cacheproblem efter uppgraderingar.

### Security

- Förbättrade skyddet för hushållens balansräkningar, delade konton, samtycke
  och äldre data.
- Skyddade Identity-konton och begränsade konfigurerbar demoinitialisering
  till utvecklingsmiljön.

## [v0.0.4-beta](https://github.com/pownas/privatekonomi/commit/d8bdfc8685e9a624fdc67fcebda289cc2d0fc328) – September–oktober 2026

[Commit `d8bdfc8`](https://github.com/pownas/privatekonomi/commit/d8bdfc8685e9a624fdc67fcebda289cc2d0fc328)
Koden i: [Diff mellan v0.0.4-alfa.3 och v0.0.4-beta](https://github.com/pownas/privatekonomi/compare/c0f076bd7801b856358ba6407b40be79799b7524...d8bdfc8685e9a624fdc67fcebda289cc2d0fc328)

### Added

- Frivillig delning av balansräkningar inom hushåll och redigerbara BAS-kopplingar.
- Administrativ vy för senaste ändringar och driftvy för uppdateringar och omstarter.
- Webbinitierad uppdaterare för Raspberry Pi med behörighetskontroller,
  hälsokontroller och återställning.

### Changed

- Förbättrad kontosäkerhet och inloggning med passkeys samt ägarskapskontroller.
- Transaktionsfiltrering och sidindelning flyttades till databasfrågor.
- Förbättrad bankimport, registrering, responsiv layout och diagram.
- Förbättrad hantering av HTTPS-certifikat, Cloudflare DNS-01 och branchval.
- Tog bort ImageSharp-beroendet från OCR-bearbetningen.

### Fixed

- Hantering av saknade användaridentiteter och bankkrediter.

## [v0.0.4-alfa.3](https://github.com/pownas/privatekonomi/commit/c0f076bd7801b856358ba6407b40be79799b7524) – Juli–augusti 2026

[Commit `c0f076b`](https://github.com/pownas/privatekonomi/commit/c0f076bd7801b856358ba6407b40be79799b7524)
Koden i: [Diff mellan v0.0.4-alfa.2 och v0.0.4-alfa.3](https://github.com/pownas/privatekonomi/compare/ba87dadfb050dfd65f484273874592b700e19fff...c0f076bd7801b856358ba6407b40be79799b7524)

### Added

- Central pakethantering och separata systemd-tjänster för Raspberry Pi.

### Changed

- Uppdaterade NuGet-beroenden och förbättrade byggkonfiguration,
  exempelkonfigurationer och loggning.
- Förbättrad CSV-import med bankidentifiering, användaravgränsad dubblettkontroll,
  kodningshantering, radvisa varningar och normaliserade transaktionsdatum.
- Förbättrad Raspberry Pi-installation och uppdatering.

### Fixed

- Rättade svenska tecken i äldre Swedbank-filer och problem i mobilens sidhuvud.

## [v0.0.4-alfa.2](https://github.com/pownas/privatekonomi/commit/ba87dadfb050dfd65f484273874592b700e19fff) – Februari–mars 2026

[Commit `ba87dad`](https://github.com/pownas/privatekonomi/commit/ba87dadfb050dfd65f484273874592b700e19fff)
Koden i: [Diff mellan v0.0.4-alfa.1 och v0.0.4-alfa.2](https://github.com/pownas/privatekonomi/compare/49facc81f4c6e68738fd2a84a1280d1cb29ec971...ba87dadfb050dfd65f484273874592b700e19fff)

### Added

- Fakturascheman och bakgrundstjänst för påminnelser.
- Månadsvis partitionering av transaktioner vid JSON-lagring.

### Changed

- Förbättrad CSV-import för ICA Banken, Swedbank och Avanza.
- Förbättrad transaktionslista, prestanda, tomma lägen, inloggning,
  animationer och mobil/PWA-upplevelse.
- Uppdaterade Aspire och MudBlazor samt förbättrade tester och databasåtkomst.

### Fixed

- Rättade flera problem med byggning, analys och service workers.

## [v0.0.4-alfa.1](https://github.com/pownas/privatekonomi/commit/49facc81f4c6e68738fd2a84a1280d1cb29ec971) – December 2025

[Commit `49facc8`](https://github.com/pownas/privatekonomi/commit/49facc81f4c6e68738fd2a84a1280d1cb29ec971)
Koden i: [Diff mellan v0.0.3 och v0.0.4-alfa.1](https://github.com/pownas/privatekonomi/compare/187a10ab5a2e68a609908b462fc1dca319a04ee0...49facc81f4c6e68738fd2a84a1280d1cb29ec971)

### Changed

- Uppdaterade kontoplanen till BAS 2026.
- Moderniserade lösningsformatet till `.slnx`.
- Flyttade tester från xUnit till MSTest.
- Förbättrade dashboardens design, testdata, navigationsdiagnostik, Swagger
  och Swedbanks CSV-parser.

## [v0.0.3](https://github.com/pownas/privatekonomi/releases/tag/v0.0.3) – 12 November 2025, Fungerande raspberry pi installer + .NET10

[Commit `187a10a`](https://github.com/pownas/privatekonomi/commit/187a10ab5a2e68a609908b462fc1dca319a04ee0)
Koden i: [Diff mellan v0.0.2 och v0.0.3](https://github.com/pownas/privatekonomi/compare/v0.0.2...v0.0.3)

### Added

- Månadsrapport, budgetbyggare, OFX-parser och statusändpunkt för importjobb.
- Bilder för aktiviteter, återkommande uppgifter och Kanban-vy för hushåll.

### Changed

- Förbättrade ekonomisk översikt, snabbkategorisering, filter, automatisk
  kategorisering, inloggning och hantering av gemensamma budgetar.
- Budgetförslag i onboarding började sparas.

### Fixed

- Rättade inloggning, CSV-import, delade sparmål, Safari/iOS-tema
  och bankval i kontodialoger.

## [v0.0.2](https://github.com/pownas/privatekonomi/releases/tag/v0.0.2) – 9 November 2025, andra release (med MySQL-stöd)

[Commit `1a41fa6`](https://github.com/pownas/privatekonomi/commit/1a41fa670d7fa8c1545839c121ff3cf65811178f)
Koden i: [Diff mellan v0.0.1 och v0.0.2](https://github.com/pownas/privatekonomi/compare/v0.0.1...v0.0.2)

### Added

- Stöd för MySQL/MariaDB.
- Bolåneanalys, sexstegs-onboarding och administrativ dashboard.

### Changed

- Uppgraderade till .NET 10 LTS och Aspire 13.
- Utökade releaseflödet och driftsättningsdokumentationen för webbhotell.
- Förbättrade autentisering, gemensamma sparmål och Raspberry Pi-installationen.
- Webben och API:et kunde köras direkt utan Aspire på Raspberry Pi.

## [v0.0.1](https://github.com/pownas/privatekonomi/releases/tag/v0.0.1) – 9 November 2025, första releasen

[Commit `79fa60a`](https://github.com/pownas/privatekonomi/commit/79fa60a45d4e3cfacfbf60530e30af49ee802eb6)
Koden i: [Tag v0.0.1](https://github.com/pownas/privatekonomi/tree/v0.0.1)


### Added

- Rollbaserad åtkomstkontroll, kontohantering och hushållens tidslinje och uppgifter.
- Delade budgetar, skuldbalansering och kassaflödesscenarier.
- OCR-skanning av kvitton med Tesseract och en landningssida.
- Utbyggt installations-, nätverks- och uppdateringsstöd för Raspberry Pi.
- Releasepipeline.

### Changed

- Förbättrade skuldstrategier.

## [v0.0.1-beta] – Början av november 2025

[Commit `90477a1`](https://github.com/pownas/privatekonomi/commit/90477a102c77de57c182cc7f7d5b23789e780981)

### Added

- Svenska hushållsbudgetmallar, budgetvarningar, målstolpar och avrundningssparande.
- Påminnelser med snooze, utgiftsanalys, värmekarta och massåtgärder.
- Koppling mellan kvitton och transaktioner.

### Changed

- Samlade importfunktionerna och förbättrade PWA-stöd, offlinehantering,
  navigering, mobilanpassning och testdata.

## [v0.0.1-alpha.6] – Slutet av oktober 2025

[Commit `06b9785`](https://github.com/pownas/privatekonomi/commit/06b97854aee048f51caac965bd44b8507a773fcb)

### Added

- Kvitton, fakturor, abonnemang och pensioner.
- Sparutmaningar, ML-baserad kategorisering, notifieringar och livsplanering.
- Funktioner för försäkring och arv samt ekonomiskt hälsopoäng och KALP.
- Jämförelse med Konsumentverkets riktlinjer och kostnadsperiodisering.
- Periodjämförelser, nettoförmögenhet och anpassningsbara dashboardwidgetar.

### Changed

- Utökade investeringsöversikten med fler tillgångs- och kontotyper.

## [v0.0.1-alpha.5] – Slutet av oktober 2025

[Commit `a614008`](https://github.com/pownas/privatekonomi/commit/a6140089b31027faf5b892e82de50150cec8159a)

### Added

- Import och export av data, säkerhetskopiering och export per år.
- Historik för data, anpassningsbar dashboard, transaktionskalender,
  Sankey-diagram och etiketter med filtrering.
- BAS-inspirerade kontonummer i kontoplanen.

### Changed

- Förbättrade navigering, tillgänglighet, mobillayout, tangentbordsgenvägar
  och grundläggande PWA-stöd.

## [v0.0.1-alpha.4] – 22–25 oktober 2025

[Commit `7c52b8c`](https://github.com/pownas/privatekonomi/commit/7c52b8c617141b788e3761e1c7339f086bd973d6)

### Added

- Sparfickor, kategoriöversikt och visualisering av nettoförmögenhet.
- Hantering av utländsk valuta och API-fel enligt ProblemDetails.
- Lagring med InMemory, SQLite, SQL Server och JSON-fil.

### Changed

- Förbättrade transaktionsredigering, uppdelning på flera kategorier,
  hushållskoppling och budgetredigering.

### Security

- Förbättrade bankkopplingar med tokenkryptering och OAuth-state-validering.

## [v0.0.1-alpha.3] – 21 oktober 2025

[Commit `91f4486`](https://github.com/pownas/privatekonomi/commit/91f448643d481f41d54571940e30a7658051a94a)

### Added

- Användarautentisering med ASP.NET Core Identity.
- Regelbaserad automatisk kategorisering, budgetmallar och kassaflödesrapport.
- Lönehistorik, gemensamma sparmål och familjefunktioner.
- ROT/RUT-, K4- och SIE-export.

### Changed

- Utökad export, förbättrad skuldhantering, mörkt läge och tillgänglighet.

### Security

- Begränsade ekonomiska data till inloggad användare.

## [v0.0.1-alpha.2] – 17–20 oktober 2025

[Commit `5d5bfdc`](https://github.com/pownas/privatekonomi/commit/5d5bfdc4b91376ea4f8fc781ccc1f5b9fc84273d)

### Added

- Avanza-import och export för investeringar samt uppdatering av aktiekurser.
- CSN-format i Swedbank-parsern.
- Hushållshantering, sparmål, fysiska tillgångar och balansräkning.
- Grund för bankkopplingar via PSD2.

### Changed

- Utökade API:et och datamodellerna.

## [v0.0.1-alpha.1] – 16 oktober 2025

[Commit `896d690`](https://github.com/pownas/privatekonomi/commit/896d69076b2f00d65ea01e03f89fda26f4bcd2f4)

### Added

- Grundapplikation med Blazor, API, .NET och Aspire.
- Transaktioner, kategorier, budgetar, lån och investeringar.
- Dashboard med kategoriöversikt och diagram.
- CSV-import från ICA Banken och Swedbank.
- Testdata, Playwright-tester och grundläggande dokumentation.

---

## Mall för en ny version

Använd mallens blocker. Ta med användarpåverkande funktioner, ändringar i befintligt beteende,
åtgärdade fel, säkerhetsförändringar, borttagningar och föråldringar. Dölj inte en
brytande eller säkerhetskritisk ändring i en generell punkt; dokumentera den i rätt typ.

```markdown

## [X.Y.Z] – Månad ÅÅÅÅ

### Added

- Ny användbar funktion.

### Changed

- Förändring i befintlig funktionalitet.

### Deprecated

- Funktion som snart tas bort, med rekommenderad ersättning.

### Removed

- Funktion eller stödd konfiguration som har tagits bort.

### Fixed

- Fel som har rättats.

### Security

- Säkerhetsproblem som har åtgärdats.

[Unreleased]: https://github.com/pownas/privatekonomi/compare/vX.Y.Z...HEAD
[X.Y.Z]: https://github.com/pownas/privatekonomi/compare/vföregående...vX.Y.Z

```
