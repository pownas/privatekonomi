# Navigation Performance Tracking - Demonstration

Detta dokument visar hur navigation performance tracking fungerar i praktiken.

## Flödesdiagram

```
┌──────────────────────────────────────────────────────────────────────┐
│                    Navigation Performance Flow                        │
└──────────────────────────────────────────────────────────────────────┘

  Användare                NavMenu              NavigationPerformance         Page
     │                        │                         Service                │
     │                        │                            │                   │
     │   Klickar på           │                            │                   │
     │   "Transaktioner"      │                            │                   │
     ├────────────────────────>                            │                   │
     │                        │                            │                   │
     │                        │   StartNavigation()        │                   │
     │                        ├───────────────────────────>│                   │
     │                        │   (URL, "NavMenu:Trans")   │                   │
     │                        │                            │                   │
     │                        │                   Creates Activity             │
     │                        │                   with timestamp               │
     │                        │                   and tags                     │
     │                        │                            │                   │
     │                                                      │                   │
     │        Blazor navigerar till /economy/transactions  │                   │
     │<────────────────────────────────────────────────────┼──────────────────>│
     │                                                      │                   │
     │                                                      │                   │
     │                                                      │   OnAfterRender   │
     │                                                      │   (firstRender)   │
     │                                                      │<──────────────────┤
     │                                                      │                   │
     │                                                      │   CompleteNav()   │
     │                                                      │<──────────────────┤
     │                                                      │   (URL, "Trans")  │
     │                                                      │                   │
     │                                       Stoppar Activity                   │
     │                                       Beräknar duration                  │
     │                                       Loggar till Aspire                 │
     │                                                      │                   │
     │                        Sidan visas för användaren                        │
     │<─────────────────────────────────────────────────────────────────────────┤
     │                                                                           │
```

## Kodexempel

### 1. NavMenu.razor - Starta spårning

```razor
<MudNavLink Href="/economy/transactions" 
            Icon="@Icons.Material.Filled.List" 
            OnClick="@(() => TrackNavigation("/economy/transactions", "Transaktioner"))">
    Transaktioner
</MudNavLink>

@code {
    [Inject]
    protected INavigationPerformanceService NavigationPerformanceService { get; set; }

    private void TrackNavigation(string targetUrl, string linkName)
    {
        // Startar en Activity med timestamp
        NavigationPerformanceService.StartNavigation(targetUrl, $"NavMenu:{linkName}");
    }
}
```

### 2. Transactions.razor - Slutföra spårning

```razor
@page "/economy/transactions"
@inherits PerformanceTrackedPageBase

<PageTitle>Transaktioner</PageTitle>

<MudDataGrid T="Transaction" Items="@transactions">
    <!-- Grid markup -->
</MudDataGrid>

@code {
    // Denna property är required från PerformanceTrackedPageBase
    protected override string PageName => "Transaktioner";
    
    // OnAfterRender i PerformanceTrackedPageBase anropar automatiskt:
    // NavigationPerformanceService.CompleteNavigation("/economy/transactions", "Transaktioner")
}
```

### 3. NavigationPerformanceService.cs - Spåra metriker

```csharp
public void StartNavigation(string targetUrl, string sourceName)
{
    var metric = new NavigationMetric
    {
        NavigationId = Guid.NewGuid().ToString(),
        TargetUrl = targetUrl,
        SourceName = sourceName,
        StartTime = DateTimeOffset.UtcNow,
        Activity = ActivitySource.StartActivity("Navigation", ActivityKind.Internal)
    };

    metric.Activity?.SetTag("navigation.target_url", targetUrl);
    metric.Activity?.SetTag("navigation.source", sourceName);
    
    _activeNavigations[targetUrl] = metric;
}

public void CompleteNavigation(string targetUrl, string pageName)
{
    if (_activeNavigations.TryGetValue(targetUrl, out var metric))
    {
        metric.EndTime = DateTimeOffset.UtcNow;
        metric.DurationMs = (metric.EndTime.Value - metric.StartTime).TotalMilliseconds;

        metric.Activity?.SetTag("navigation.duration_ms", metric.DurationMs);
        metric.Activity?.Stop();
        
        _activeNavigations.Remove(targetUrl);
    }
}
```

## Aspire Dashboard - Trace View

När du öppnar Aspire Dashboard och tittar på Traces ser du något liknande:

```
┌─────────────────────────────────────────────────────────────────────────┐
│ Aspire Dashboard - Traces                                               │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  Resource: privatekonomi-web                                            │
│  Trace Name: Navigation                                                 │
│                                                                          │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │ Navigation: /economy/transactions                                │  │
│  │ Duration: 127 ms                                                 │  │
│  │ Status: OK                                                       │  │
│  │                                                                  │  │
│  │ Tags:                                                            │  │
│  │   navigation.target_url: economy/transactions                   │  │
│  │   navigation.source: NavMenu:Transaktioner                      │  │
│  │   navigation.page_name: Transaktioner                           │  │
│  │   navigation.duration_ms: 127.43                                │  │
│  │   navigation.id: 3e4f5a6b-7c8d-9e0f-1a2b-3c4d5e6f7a8b          │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                                                          │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │ Navigation: /economy/budgets                                     │  │
│  │ Duration: 89 ms                                                  │  │
│  │ Status: OK                                                       │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

## /navigation-performance Dashboard

Realtids-dashboard i applikationen:

```
┌─────────────────────────────────────────────────────────────────────────┐
│ 🚀 Navigation Performance Tracking                                      │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ℹ️ Denna sida visar realtidsspårning av användarens navigationsklick   │
│     från menyn till att sidan renderas.                                 │
│                                                                          │
├─────────────────────────┬───────────────────────────────────────────────┤
│ ⏳ Aktiva Navigeringar  │ ✅ Senaste Slutförda Navigeringar            │
├─────────────────────────┼───────────────────────────────────────────────┤
│                         │                                               │
│ Mål: /economy/budgets   │  ⦿ 14:35:42                                   │
│ Källa: NavMenu:Budget   │    Transaktioner                              │
│ Starttid: 14:35:42.123  │    /economy/transactions                      │
│ [=========>        ]    │    127 ms 🟢                                   │
│                         │                                               │
│                         │  ⦿ 14:35:38                                   │
│                         │    Dashboard                                  │
│                         │    /                                          │
│                         │    84 ms 🟢                                    │
│                         │                                               │
│                         │  ⦿ 14:35:30                                   │
│                         │    Sparmål                                    │
│                         │    /savings/goals                             │
│                         │    234 ms 🟡                                   │
│                         │                                               │
└─────────────────────────┴───────────────────────────────────────────────┘

📊 Information

Navigationsmetriker spåras automatiskt och syns i Aspire Dashboard under 
"Traces". Aktiva navigeringar uppdateras var 500:e millisekund.

💻 Implementationsguide

För att spåra navigation performance på dina sidor, gör så här:

1. Ärv från PerformanceTrackedPageBase

   @page "/min-sida"
   @inherits Privatekonomi.Web.Components.Shared.PerformanceTrackedPageBase

   @code {
       protected override string PageName => "Min Sida";
   }

2. Alternativt: Manuell spårning

   @inject INavigationPerformanceService NavigationPerformanceService

   @code {
       protected override void OnAfterRender(bool firstRender)
       {
           if (firstRender)
           {
               var currentUrl = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
               NavigationPerformanceService.CompleteNavigation(currentUrl, "Sidnamn");
           }
       }
   }

✅ Spårningen sker automatiskt när användaren klickar i menyn och 
   rapporteras när sidan är färdigrenderad. All data syns i Aspire 
   Dashboard under Traces.
```

## Prestandamål

| Duration | Färg | Status | Beskrivning |
|----------|------|--------|-------------|
| < 100 ms | 🟢 Grön | Utmärkt | Mycket snabb respons |
| 100-300 ms | 🔵 Blå | Bra | Acceptabel prestanda |
| 300-500 ms | 🟡 Gul | OK | Lite långsamt |
| > 500 ms | 🔴 Röd | Långsam | Behöver optimering |

## Integration med befintlig telemetri

Navigation Performance Tracking bygger på samma `ActivitySource`-infrastruktur som redan används i:

- `TrackedComponentBase` - För komponent-lifecycle tracking
- `BlazorActivitySource` - För användarinteraktioner (klick, form submit, etc.)
- Aspire - För distributed tracing och observability

Detta ger ett enhetligt sätt att spåra hela användarflödet från klick till render.

## Nästa steg

1. **Implementera på alla sidor**: Låt alla sidor ärva från `PerformanceTrackedPageBase`
2. **Analysera bottlenecks**: Använd Aspire Dashboard för att hitta långsamma sidor
3. **Optimera**: Fokusera på sidor med duration > 500 ms
4. **Mät resultat**: Jämför före/efter-metriker

---

**Tips**: Använd Aspire Dashboard's filter-funktioner för att analysera specifika URL-mönster eller tidsperioder!
