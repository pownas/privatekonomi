# Dokumentationsöversikt

## Aktuell arkitektur

Web och API körs som separata processer med separata Identity- och lagringskonfigurationer. **Web använder fortfarande Core-tjänster och sitt eget datalager direkt**; den fullständiga migreringen till API:t är inte klar. API:t äger därför ännu inte all applikationsdata. Ta inte bort Web-appens `Storage`-inställningar eller databasregistrering förrän Web-flödena har migrerats, identiteter och datalager har samordnats och åtkomstkontrollerna har verifierats.

Aspire exponerar inte längre separata `web-storage-provider`,
`web-seed-test-data`, `api-storage-provider` eller `api-seed-test-data`-
parametrar. API-datakällan styrs genom `api-data-profile` och profilens
anslutningssträngar. Separata Web-lagringsparametrar är inte ett API-kontrakt
och tas inte med i Aspire-dashboarden. Den fullständiga Web→API-migreringen
återstår ändå; detta tar inte bort Web-appens nuvarande interna lagring.

Konfigurationsordningen i Aspire är nu: projektets basinställningar,
miljöspecifika inställningar, Development/Local `appsettings.local.json` och
User Secrets, miljövariabler och till sist explicita AppHost-överstyrningar.
I Web/API-processerna ligger `PRIVATEKONOMI_ASPIRE_`-miljövariabler sist och
har högst prioritet. En lokal fil ska därför inte tyst åsidosätta ett värde som
driftsmiljön eller Aspire uttryckligen skickat in.

API:ets begränsade, aktiva personliga yta är inte en ersättning för hela webbappen. Se [aktuell API-guide](API_GUIDE.md), [säkerhetsgränsen för äldre API:er](LEGACY_API_SECURITY.md) och [lagringsguiden](STORAGE_GUIDE.md). `/api-info` visar version och konfigurerad provider, inte om data är test- eller produktionsdata.

## Aktuella guider

- [Utvecklarens snabbstart](DEVELOPER_QUICKSTART.md) – starta Web, API och Aspire.
- [Lagringsguide](STORAGE_GUIDE.md) och [snabbguide för lagring](STORAGE_QUICKSTART.md) – konfiguration per process, isolering, backup och begränsningar.
- [API-guide](API_GUIDE.md) och [säkerhetsguide för äldre API:er](LEGACY_API_SECURITY.md) – aktiva API-anrop och avstängda routes.
- [Aspire-guide](ASPIRE_GUIDE.md) – lokal orkestrering och service discovery.
- [Release- och deploymentsguide](DEPLOYMENT_GUIDE.md) – kontrollera också workflowfilen innan drift.
- Plattformsspecifika installationsguider finns under [`scripts/`](scripts/).

Konfiguration och implementation ändras över tid. Innan en driftåtgärd följs ska instruktionen stämmas av mot aktuell kod, exempelkonfiguration och installationsskript. För lagring, identitet och åtkomstkontroll är koden och testerna normerande.

## Historik, specifikationer och mockups

Dokument med namn som `*_IMPLEMENTATION_SUMMARY.md`, `*_2025.md`, `*_NEXT_STEPS.md`, `*_SPECIFICATION.md` eller `*_MOCKUP*.md` är historiska rapporter, förslag eller visuella referenser om inget annat uttryckligen anges. De beskriver inte automatiskt funktioner som finns i aktuell version. Kontrollera alltid status mot källkod och tester. En datumstämpel anger dokumentets historiska sammanhang, inte att innehållet fortfarande gäller.

Behåll värdefull beslutshistorik och kravdokument, men märk verifierat inaktuella drift- eller API-instruktioner som föråldrade och länka till en aktuell guide i stället för att lämna dem som körbara exempel.
