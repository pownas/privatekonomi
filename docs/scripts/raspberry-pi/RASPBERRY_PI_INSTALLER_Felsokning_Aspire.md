# Felsökningsdokument om man får fel vid dotnet workload restore för Aspire

Om din Raspberry pi skulle få för lite ström, så kommer den bara starta om också mitt under installationen av `dotnet workload restore`. Se därför till att ha en tillräckligt kraftfull USB-C laddare. 

## Fråga om felmeddelandet
`dotnet workload restore` ger felmeddelandet på en raspberry pi likt: 
```shell
Unhandeled exception: The imported project file "/home/username/.nuget/packages/aspire.apphost.sdk/9.5.2/Sdk/Sdk.props" could not be loaded. Root element is missing.  /home/username/Privatekonomi/src/Privatekonomi.AppHost/Privatekonomi.AppHost.csproj
```

## Svar av GitHub Copilot
Det felet betyder nästan alltid att NuGet-paketet för Aspire AppHost har blivit korrupt (filen Sdk.props är tom eller avklippt), ofta p.g.a. avbruten nedladdning, full disk, certifikat-/TLS-problem eller trasig cache.

Gör så här steg för steg:

1) Verifiera att filen verkligen är trasig
- Kolla storlek och de första raderna:
```bash
ls -l ~/.nuget/packages/aspire.apphost.sdk/9.5.2/Sdk/Sdk.props
wc -c ~/.nuget/packages/aspire.apphost.sdk/9.5.2/Sdk/Sdk.props
sed -n '1,40p' ~/.nuget/packages/aspire.apphost.sdk/9.5.2/Sdk/Sdk.props
```
Om den är 0 byte eller inte börjar som giltig XML är den korrupt.

2) Rensa NuGet-cachar och ta bort det specifika paketet
```bash
dotnet nuget locals all --list
dotnet nuget locals all --clear
rm -rf ~/.nuget/packages/aspire.apphost.sdk/9.5.2
# (alternativt hela paketmappen)
# rm -rf ~/.nuget/packages/aspire.apphost.sdk
```

3) Temporärt flytta din NuGet.Config (om den varit korrupt tidigare)
```bash
mkdir -p ~/.nuget/NuGet
mv ~/.nuget/NuGet/NuGet.Config ~/.nuget/NuGet/NuGet.Config.bak 2>/dev/null || true
```
Testa först utan egen Config (dotnet använder standardkällor). Om du vill ha en minimal giltig fil, skapa den igen:
```bash
cat <<EOF > ~/.nuget/NuGet/NuGet.Config
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
EOF
```

4) Säkerställ omgivningen (vanliga orsaker på Pi)
- Kontrollera att disken inte är full:
```bash
df -h ~
```
- Se till att certifikat finns och är aktuella:
```bash
sudo apt-get update && sudo apt-get install -y ca-certificates
```
- Kolla tid/synk (fel systemtid kan ställa till TLS):
```bash
timedatectl status
```

5) Försök återställa med ren MSBuild-process och enkeltråd
```bash
export DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1
dotnet restore -m:1 -v n
# eller om du behöver just workloaden:
dotnet workload restore -m:1 -v n
```

6) Om det fortfarande felar
- Ta bort hela globala package-cachen och försök igen:
```bash
rm -rf ~/.nuget/packages
export DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1
dotnet restore -m:1 -v diag
```
- Kontrollera att din .NET-arkitektur matchar Pi: kör
```bash
dotnet --info
uname -m
```
Dela gärna utdata här. Om du kör 32-bitars OS på en äldre Pi kan vissa SDK/komponenter sakna stöd; då kan .NET 8 vara ett säkrare val.

7) Extra tips
- Prova även en manuell paketåterställning i projektroten:
```bash
dotnet restore src/Privatekonomi.AppHost/Privatekonomi.AppHost.csproj -m:1 -v n
```
- Se till att projektet refererar till en rimlig version av Aspire (t.ex. 9.x). Uppdatera till en nyare patch om sådan finns.

Om felet kvarstår: klistra in utdata från sed/ls/wc på Sdk.props, resultatet från dotnet --info, samt de sista ~50 raderna från en körning med -v diag, så pinpointar vi nästa steg.