# SBOM och NuGet-beroenden

## Kör skannern

Kör från lösningens rot med .NET 11 SDK:

```bash
dotnet restore Privatekonomi.slnx
dotnet run --project tests/SbomScanner/SbomScanner.csproj
```

Skannern läser de återställda `artifacts/obj/<projektnamn>/project.assets.json` för lösningens app- och testprojekt och skriver `docs/sbom-report.md`. Återställ lösningen före skanningen så att rapporten använder aktuella beroenden; återställningen kan kräva nätverk. Skannern ingår inte själv som projekt i rapporten; paket som andra projekt använder via den kan däremot ingå. Rapporten omfattar NuGet-paket, inte npm-paket eller installerade ramverk. En föreslagen paketuppdatering kräver separat kompatibilitetsbedömning. Olika versioner i olika projekt innebär inte nödvändigtvis en konflikt.

## Lokal sårbarhetsdatabas

Med nätverksåtkomst hämtar skannern alla NuGets sårbarhetssidor och uppdaterar `tests/SbomScanner/vulnerabilities-cache.json`. Vid nätverksfel eller timeout används den senaste kompletta lokala kopian, och rapporten samt konsolen visar källa, hämtningstid i UTC och en varning om att nya sårbarheter kan saknas. Senaste paketversioner visas som `Okänd` när de inte kan hämtas. Saknas en giltig lokal kopia misslyckas skanningen tydligt. Välj offline-läge med `dotnet run --project tests/SbomScanner/SbomScanner.csproj --no-restore -- --offline`, eller kräv aktuella onlineuppgifter med `--require-online`.

## Schemalagd körning

GitHub Actions kör skannerns separata enhetstester och uppdaterar rapporten och den lokala sårbarhetsdatabasen varje måndag kl. 05:17 UTC. Arbetsflödet **Update SBOM report** kan också startas manuellt och skriver endast dessa två filer till standardgrenen. Den automatiska körningen kräver nätverksåtkomst så att gamla uppgifter inte publiceras som nya. Den kräver också att GitHub Actions får skriva till grenen; grenskydd kan annars stoppa uppdateringen. Testerna själva uppdaterar inte rapporten eller kontaktar nuget.org, men paketåterställning kan kräva nätverk.