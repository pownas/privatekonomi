# Privatekonomi.AppHost

Detta är Aspire AppHost-projektet som orchestrerar alla tjänster i Privatekonomi-applikationen.

## Vad gör detta projekt?

AppHost är startpunkten för hela applikationen när du använder .NET Aspire. Det:

- **Startar alla tjänster** (Web och API) automatiskt
- **Konfigurerar service discovery** så tjänster kan hitta varandra
- **Tillhandahåller Aspire Dashboard** för övervakning och felsökning
- **Hanterar miljövariabler** och konfiguration för alla tjänster

## Hur kör jag det?

```bash
cd src/Privatekonomi.AppHost
dotnet run
```

Aspire Dashboard öppnas automatiskt i webbläsaren där du kan:
- Se alla tjänster och deras status
- Läsa logs från alla tjänster i realtid
- Spåra HTTP-requests mellan tjänster
- Övervaka metrics och prestanda

## Konfiguration

### Program.cs

Detta är huvudfilen som definierar vilka tjänster som ska köras:

```csharp
var api = builder.AddProject<Projects.Privatekonomi_Api>("api");
var web = builder.AddProject<Projects.Privatekonomi_Web>("web")
    .WithReference(api);
```

### Lägga till fler tjänster

För att lägga till en ny tjänst:

1. Lägg till projektreferens i `Privatekonomi.AppHost.csproj`
2. Registrera tjänsten i `Program.cs`
3. Konfigurera eventuella beroenden med `.WithReference()`

## Krav

- Docker Desktop måste köra (krävs för Aspire DCP)
- .NET 11 SDK
- Aspire AppHost SDK återställs automatiskt från NuGet

## Mer information

Se [ASPIRE_GUIDE.md](../../docs/ASPIRE_GUIDE.md) i projektets rotkatalog för detaljerad dokumentation.
