# Raspberry Pi felsokning for Privatekonomi

Detta dokument beskriver hur du felsoker en Raspberry Pi-installation av Privatekonomi nar Aspire-dashboarden, webbappen eller API:t inte svarar som vantat. Guiden kompletterar `doc/RASPBERRY_PI_forsta_installationen.md` och fokuserar pa port-explicita problem dar tjansterna inte lyssnar pa ratt adresser.

## Snabb checklista

1. **Uppdatera installationen**
   ```bash
   cd ~/Privatekonomi
   ./raspberry-pi-update.sh
   ```
2. **Starta om tjansten**
   ```bash
   sudo systemctl restart privatekonomi
   ```
3. **Kontrollera portar**
   ```bash
   sudo ss -lntp | grep -E "17127|5274|5277"
   ```
4. **Validera miljo**
   ```bash
   sudo systemctl show privatekonomi --property=Environment
   ```
5. **Testa lokalt**
   ```bash
   curl -v http://localhost:5274
   ```

## Vanliga symtom och losningar

### Portarna 17127, 5274, 5277 lyssnar inte

* Atgard: Se till att inga alternativa runtimeprocesser kopierade fran aldre installationer fortfarande exporterar `ASPNETCORE_URLS`. Det globalt satta variabeln tvingar alla dotnet-processer att lyssna pa samma port.
* Kommandon:
  ```bash
  unset ASPNETCORE_URLS
  unset DOTNET_URLS
  ./raspberry-pi-start.sh
  ```
* Om systemd anvands, kontrollera att miljovariabeln inte finns kvar:
  ```bash
  sudo systemctl show privatekonomi --property=Environment
  ```
  Raden `Environment=...ASPNETCORE_URLS=...` far inte finnas kvar. Om den gor det, redigera `/etc/systemd/system/privatekonomi.service`, ta bort raden och kora `sudo systemctl daemon-reload` samt `sudo systemctl restart privatekonomi`.

### Portarna lyssnar pa 127.0.0.1

* Sannolik orsak: Tjansten startades utan att appsettings.Production.json anger `http://0.0.0.0:PORT` eller miljo saknas.
* Kontrollera `~/Privatekonomi/src/Privatekonomi.Web/appsettings.Production.json` och `~/Privatekonomi/src/Privatekonomi.Api/appsettings.Production.json`.
* Exempel (Web):
  ```json
  "Urls": "http://0.0.0.0:5274"
  ```
* Atgard: Kora `./raspberry-pi-install.sh` igen eller lagg till varden manuellt.

### Systemd-tjansten startar men inga portar ar oppna

1. Kolla loggar:
   ```bash
   journalctl -u privatekonomi -n 200 --no-pager
   ```
2. Leta efter fel kring portar, databas eller ASP.NET Core.
3. Uppdatera publicerade binarer vid behov:
   ```bash
   ./raspberry-pi-update.sh --force
   ```

### Hitta applikationsloggar och exceptions

Webbappen och API:t skriver roterande loggfiler till `~/privatekonomi/logs/`, utanfor publiceringskatalogen sa att de finns kvar efter uppdateringar. Filer behalls i upp till 14 rullningar och ar begransade till 5 MB per fil. Pa Linux skyddas katalogen sa att bara tjansteanvandaren kan lasa den.

```bash
ls -lh ~/privatekonomi/logs
tail -f ~/privatekonomi/logs/privatekonomi-web-*.log
tail -f ~/privatekonomi/logs/privatekonomi-api-*.log
sudo journalctl -u privatekonomi-web -n 200 --no-pager
sudo journalctl -u privatekonomi-api -n 200 --no-pager
```

Sok efter `ERR`, `FTL` eller `Exception`. Vid ett API-fel kan `traceId` i svaret anvandas for att hitta motsvarande post i loggen. Om loggarna ska ligga pa en annan skrivbar plats kan `PRIVATEKONOMI_LOG_DIRECTORY` anges for Web- och API-tjansterna.

En inloggad systemadministrator kan aven lasa loggarna pa sidan **Applikationsloggar** (`/admin/logs`). Sidan visar de senaste 2 000 raderna och tillater radering av aldre loggfiler efter en bekraftelse. Aktiva loggfiler kan inte raderas. Loggsidan kraver bade inloggning och systemadministratorbehorighet.

### Brandvagg/UFW blockerar trafik

* Kontrollera status:
  ```bash
  sudo ufw status numbered
  ```
* Agg till regler om de saknas:
  ```bash
  sudo ufw allow 17127/tcp comment "Privatekonomi Aspire"
  sudo ufw allow 5274/tcp comment "Privatekonomi Web"
  sudo ufw allow 5277/tcp comment "Privatekonomi API"
  sudo ufw reload
  ```

### Lokalt fungerar men klienter pa natverket far ingen respons

* Kontrollera Pi:s IP-adress:
  ```bash
  hostname -I
  ```
* Testa fran Pi med IP-adressen:
  ```bash
  curl http://<PI-IP>:5274
  ```
* Om lokalt OK men andra enheter misslyckas: kontrollera router-brandvagg eller VLAN. Se "Firewall and network" i Microsoft IoT-guiden nedan.

### dotnet-ef installationsfel: "Settings file 'DotnetToolSettings.xml' was not found"

Om du far felmeddelandet:
```
Tool 'dotnet-ef' failed to update due to the following: The settings file in the tool's NuGet package is invalid: Settings file 'DotnetToolSettings.xml' was not found in the package.
```

Detta beror vanligtvis pa korrupt NuGet-cache eller saknade paketkallor. `raspberry-pi-install.sh` hanterar nu detta automatiskt, men om problemet kvarhaller:

**Losning 1: Skapa NuGet.Config manuellt**
```bash
mkdir -p ~/.nuget/NuGet
cat > ~/.nuget/NuGet/NuGet.Config << 'EOF'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
EOF
```

**Losning 2: Lagg till och aktivera kallor manuellt**
```bash
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
dotnet nuget enable source nuget.org
```

**Losning 3: Installera med --ignore-failed-sources**
```bash
dotnet tool install --global dotnet-ef --ignore-failed-sources
```

**Losning 4: Rensa cache och forsok igen**
```bash
dotnet nuget locals all --clear
dotnet tool uninstall --global dotnet-ef
dotnet tool install --global dotnet-ef
```

**Verifiera installation:**
```bash
dotnet-ef --version
```

## Djupdiagnostik

### Kontrollera applikationsprocesser

```bash
ps -ef | grep Privatekonomi.AppHost
ps -ef | grep dotnet | grep -E "5274|5277"
```

### Kontrollera Aspire-dashboardens miljo

```bash
sudo systemctl cat privatekonomi
```
* Se att endast `DOTNET_DASHBOARD_URLS=http://0.0.0.0:17127` exporteras.

### Manuell start for felsokning

```bash
cd ~/Privatekonomi/src/Privatekonomi.AppHost
unset ASPNETCORE_URLS DOTNET_URLS
PRIVATEKONOMI_RASPBERRY_PI=true DOTNET_DASHBOARD_URLS=http://0.0.0.0:17127 dotnet run --configuration Release
```

Se till att loggar visar att Web/API binders till 0.0.0.0:5274/5277.

### Kontroll av `raspberry-pi-debug.sh`

* Kora `./raspberry-pi-debug.sh` efter atgarder.
* Avsnitt 3, 7 och 8 ska markeras som OK.

## Relaterade resurser

* [Microsoft: Deploy .NET IoT apps to Linux-devices](https://learn.microsoft.com/en-us/dotnet/iot/deployment)
* [Microsoft: Debugging .NET IoT apps](https://learn.microsoft.com/en-us/dotnet/iot/debugging?tabs=self-contained&pivots=vscode)
* `docs/RASPBERRY_PI_forsta_installationen.md` - ursprunglig installationsguide.
* `docs/ASPIRE_GUIDE.md` - detaljer om Aspire-orchestrering.

## Tipssammanfattning

* Satt aldrig `ASPNETCORE_URLS` globalt nar flera hostar ska lyssna pa olika portar.
* Verifiera alltid systemd-miljo och testkora med manuell `dotnet run` om nagot ser konstigt ut.
* Anvand `journalctl` nar `raspberry-pi-debug.sh` rapporterar rott pa steg 3 eller 7.
* Dokumentera resultat efter varje felsokningskorsning for snabbare support i framtiden.
