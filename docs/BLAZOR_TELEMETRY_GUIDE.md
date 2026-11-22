# Blazor Interaktionsspårning - Guide

## Översikt

Privatekonomi har nu inbyggd telemetri-spårning för Blazor-komponenter som hjälper dig att:
- **Spåra användarklick** och se exakt vad som händer i applikationen
- **Diagnostisera blockeringar** - förstå varför vissa knappar inte går att klicka på
- **Mäta prestanda** - se hur lång tid olika operationer tar
- **Fånga fel** - automatisk logging av exceptions med full stack trace

All telemetri visas i **Aspire Dashboard** tillsammans med övrig observability-data.

## Hur det fungerar

### Statiska filer och traces

GET- och HEAD-anrop för vanliga statiska filtyper filtreras bort från serverns traces. Webbläsarens hämtningar av CSS, JavaScript, bilder, fonter och liknande är separata HTTP-anrop; de blir inte barn till sidans trace automatiskt. För att koppla dem till sidvisningen krävs även klienttelemetri som skickar trace-kontext med varje resursanrop.

### 1. OpenTelemetry Integration

Projektet använder redan .NET Aspire med OpenTelemetry. Vi har lagt till:
- Custom `ActivitySource` för Blazor-events: `Privatekonomi.Web.Blazor`
- Automatisk registrering i `ServiceDefaults/Extensions.cs`
- Detaljerad logging för Blazor Server Circuit i development mode

### 2. Tracy Activities för UI-events

Varje användarinteraktion kan spåras som en "Activity" (trace span) med:
- **Name**: T.ex. "User.Click", "Form.Submit", "Validation.Block"
- **Tags**: Metadata som komponentnamn, element-ID, åtgärd, felmeddelanden
- **Duration**: Automatisk mätning av hur lång tid operationen tar
- **Status**: Success/Error med detaljer

## Användning

### Metod 1: Ärv från TrackedComponentBase (Rekommenderat)

```razor
@page "/my-page"
@using Privatekonomi.Web.Components.Base
@inherits TrackedComponentBase
@rendermode InteractiveServer

<MudButton OnClick="HandleClick">Klicka här</MudButton>

@code {
    private async Task HandleClick()
    {
        // Starta spårning av klicket
        using var activity = TrackClick("my-button", "save-data");
        
        try
        {
            // Din logik här
            await SaveDataAsync();
            
            // Logga framgång
            Logger.LogInformation("Data saved successfully");
        }
        catch (Exception ex)
        {
            // Fel loggas automatiskt i activity
            activity?.RecordException(ex);
            Logger.LogError(ex, "Failed to save data");
        }
    }
}
```

### Metod 2: Använd BlazorActivitySource direkt

Om du inte kan ärva från `TrackedComponentBase`:

```csharp
@using Privatekonomi.Web.Telemetry

@code {
    private async Task HandleClick()
    {
        using var activity = BlazorActivitySource.StartUserClick("MyComponent", "save-button", "save");
        
        try
        {
            await SaveDataAsync();
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

### Spåra blockerade klick

När en knapp är disabled eller användaren saknar behörighet:

```razor
<MudButton Disabled="@(!_canSave)" OnClick="HandleSave">
    Spara
</MudButton>

@code {
    private bool _canSave = false;
    
    private void AttemptSave()
    {
        if (!_canSave)
        {
            // Logga varför klicket blockerades
            TrackBlockedClick("save-button", "User lacks permission to save");
            return;
        }
        
        // Normal save logic...
    }
}
```

### Spåra valideringsfel

```csharp
private async Task HandleFormSubmit()
{
    using var activity = TrackFormSubmit("transaction-form");
    
    if (string.IsNullOrEmpty(_amount))
    {
        TrackValidationBlock("amount", "Amount is required");
        return;
    }
    
    if (_amount <= 0)
    {
        TrackValidationBlock("amount", "Amount must be positive");
        return;
    }
    
    // Submit form...
}
```

## Visa Traces i Aspire Dashboard

### 1. Starta applikationen med Aspire

```bash
cd src/Privatekonomi.AppHost
dotnet run
```

### 2. Öppna Aspire Dashboard

Vanligtvis på: `https://localhost:17033`

### 3. Navigera till Traces

1. Klicka på "Traces" i vänstermenyn
2. Filtrera på resource: `privatekonomi-web`
3. Sök efter traces:
   - `User.Click` - Användarklick
   - `User.ClickBlocked` - Blockerade klick
   - `Form.Submit` - Formulärinlämningar
   - `Validation.Block` - Valideringsfel
   - `Component.Render` - Komponentrendering (om aktiverad)

### 4. Granska en trace

Klicka på en trace för att se:
- **Duration**: Hur lång tid operationen tog
- **Tags**: 
  - `component.name`: Vilken komponent
  - `element.id`: Vilket element som klickades
  - `action`: Vilken åtgärd som utfördes
  - `blocked.reason`: Varför klicket blockerades
  - `validation.errors`: Valideringsfel
  - `error.*`: Feldetaljer om något gick fel
- **Events**: Logg-meddelanden kopplade till tracen
- **Parent/Child spans**: Relaterade operationer

## Detaljerad Blazor Server Logging

I `appsettings.Development.json` har vi aktiverat:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.Components.Server": "Debug",
      "Microsoft.AspNetCore.SignalR": "Debug",
      "Microsoft.AspNetCore.Components": "Debug"
    }
  }
}
```

Detta ger dig:
- **SignalR reconnections** - Se när klienter tappar anslutning
- **Component lifecycle** - OnInitialized, OnParametersSet, etc.
- **Render batches** - När UI uppdateras
- **JavaScript interop** - JSInterop-anrop

## Felsökning av "Kan inte klicka"

### Scenario 1: Knappen är disabled

```razor
<MudButton Disabled="_isProcessing" OnClick="HandleClick">
    @(_isProcessing ? "Bearbetar..." : "Spara")
</MudButton>

@code {
    private bool _isProcessing = false;
    
    // I Aspire Dashboard kommer du se:
    // User.ClickBlocked med tag "blocked.reason": "Processing in progress"
}
```

### Scenario 2: Validering misslyckas

Traces visar:
- `Validation.Block` med alla valideringsfel
- `Form.Submit` med status Error
- Tags visar exakt vilket fält som failade

### Scenario 3: SignalR-anslutning bruten

I loggen (Aspire Dashboard → Logs → privatekonomi-web):
```
[Debug] Circuit disconnected
[Debug] Attempting reconnection...
```

### Scenario 4: JavaScript-fel

Om `@onclick` inte fungerar pga JS-fel, använd browser console:
```javascript
// I appsettings.Development.json är Blazor.start konfigurerad med:
Blazor.start({
  configureSignalR: function (builder) {
    builder.withUrl("/_blazor")
      .configureLogging(LogLevel.Debug);
  }
});
```

## Best Practices

### ✅ Gör detta:

1. **Ärv från TrackedComponentBase** för komplexa komponenter med användarinteraktion
2. **Använd using-statements** för activities så de avslutas korrekt
3. **Logga blockeringsorsaker** när knappar är disabled
4. **Fånga exceptions** och använd `activity.RecordException(ex)`
5. **Lägg till beskrivande element IDs** för bättre spårbarhet

### ❌ Undvik detta:

1. Spåra varje liten komponent (overhead)
2. Glömma dispose activities (memory leak)
3. Logga känslig data i tags (PII-risk)
4. Spåra rendering i produktion (prestanda)

## Exempel: Konvertera befintlig komponent

**Före:**
```razor
@page "/transactions"
@inherits ComponentBase

<MudButton OnClick="SaveTransaction">Spara</MudButton>

@code {
    private async Task SaveTransaction()
    {
        await _service.SaveAsync(_transaction);
    }
}
```

**Efter:**
```razor
@page "/transactions"
@using Privatekonomi.Web.Components.Base
@inherits TrackedComponentBase

<MudButton OnClick="SaveTransaction">Spara</MudButton>

@code {
    private async Task SaveTransaction()
    {
        using var activity = TrackClick("save-transaction-btn", "save");
        
        try
        {
            await _service.SaveAsync(_transaction);
            Logger.LogInformation("Transaction saved: {Id}", _transaction.Id);
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            Logger.LogError(ex, "Failed to save transaction");
            // Visa felmeddelande till användaren
        }
    }
}
```

## Performance Impact

- **Development**: Minimal påverkan (~1-2ms per trace)
- **Production**: Ej aktiverat som standard (konfigureras via environment)
- **Sampling**: Kan konfigureras i OpenTelemetry för att bara spåra % av requests

## Se också

- [ASPIRE_GUIDE.md](ASPIRE_GUIDE.md) - Allmän guide för .NET Aspire
- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/languages/net/)
- [Blazor Server Troubleshooting](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/logging)

## Test-sida

Besök `/telemetry-example` för att se ett fungerande exempel med:
- Normal klick-spårning
- Blockerade klick
- Formulärvalidering
- Långvariga operationer

Öppna Aspire Dashboard samtidigt för att se traces i realtid!
