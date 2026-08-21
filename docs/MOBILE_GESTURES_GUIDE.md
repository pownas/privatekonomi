# Mobil-optimerad UI med Gester - Guide

## Översikt

Privatekonomi har implementerat touch-optimerad UI för mobil användning med intuitiva gester och förbättrad tillgänglighet enligt WCAG 2.1 Level AA.

## Funktioner

### 1. Swipe-gester på transaktioner

#### Swipe vänster (←) - Ta bort transaktion
- **Beskrivning**: Svep åt vänster på en transaktion för att ta bort den
- **Visuell feedback**: Röd bakgrund under sveprörelser
- **Bekräftelse**: Du får en bekräftelsedialog innan borttagning

#### Swipe höger (→) - Redigera transaktion
- **Beskrivning**: Svep åt höger på en transaktion för att redigera den
- **Visuell feedback**: Grön bakgrund under sveprörelser
- **Resultat**: Öppnar redigeringsdialogen automatiskt

### 2. Pull-to-refresh (↓)

- **Beskrivning**: Dra ner från toppen av sidan för att uppdatera transaktionslistan
- **Visuell feedback**: En uppdateringsindikator visas med animerad ikon
- **Användning**: 
  1. Se till att du är längst upp på sidan (scroll position 0)
  2. Dra ner med fingret
  3. Släpp när indikatorn visar "Släpp för att uppdatera!"

### 3. Större touch targets

Alla interaktiva element följer WCAG-riktlinjer med minst 44×44px touch targets:

- **Knappar**: Minst 44×44px på mobila enheter
- **Icon buttons**: Minst 48×48px för bättre precision
- **Input-fält**: Minst 48px höjd
- **Chips**: Minst 32px höjd med tillräcklig padding

### 4. Thumbzone-optimerad layout

- **Bottom action bar**: Primära åtgärder placeras längst ner där tummen når lätt
- **Responsiv design**: Layout anpassas automatiskt för olika skärmstorlekar
- **Stackad layout**: På extra små skärmar (<480px) staplas knappar vertikalt

## Teknisk implementering

### JavaScript API

Mobile gesture handler exponeras globalt via `window.mobileGestureHandler`:

```javascript
// Initiera med .NET interop
window.mobileGestureHandler.init(dotNetHelper);

// Fäst swipe-lyssnare på hela griden (event delegation – EN lyssnare för alla rader)
window.mobileGestureHandler.attachGesturesToGrid();

// Kontrollera om enheten är mobil
const isMobile = window.mobileGestureHandler.isMobileDevice();

// Uppdatera gesttillstånd baserat på viewport
window.mobileGestureHandler.updateGestureState();
```

> **Observera:** Använd alltid `attachGesturesToGrid()` istället för att anropa
> `attachSwipeListeners(element, itemId)` per rad. Per-rad-registrering genererar ett
> SignalR-anrop per rad och kan orsaka tusentals bakgrundsanrop vid stora datamängder.

### CSS-klasser

- `.mobile-gestures-enabled`: Läggs till på `<body>` när gestures är aktiverade
- `.transaction-row-swipeable`: Markerar rader som stöder swipe
- `.mobile-only`: Döljs på desktop (min-width: 769px)
- `.desktop-only`: Döljs på mobil (max-width: 768px)
- `.mobile-action-bar`: Container för mobila åtgärdsknappar
- `.bottom-sheet`: Modal som öppnas från botten (för framtida menyer)

### Blazor Integration

Exempel på hur gestures implementeras i en Blazor-komponent:

```razor
@page "/transactions"
@inject IJSRuntime JSRuntime
@implements IDisposable

@code {
    private DotNetObjectReference<Transactions>? _dotNetHelper;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetHelper = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("mobileGestureHandler.init", _dotNetHelper);
            // Fäst EN lyssnare på hela griden via event delegation
            await JSRuntime.InvokeVoidAsync("mobileGestureHandler.attachGesturesToGrid");
        }
    }

    [JSInvokable]
    public async Task HandleSwipeGesture(string direction, int itemId)
    {
        if (direction == "left")
        {
            // Ta bort
        }
        else if (direction == "right")
        {
            // Redigera
        }
    }

    [JSInvokable]
    public async Task HandlePullToRefresh()
    {
        // Ladda om data
    }

    public void Dispose()
    {
        _dotNetHelper?.Dispose();
    }
}
```

## Detektering av mobil enhet

Systemet detekterar automatiskt mobila enheter baserat på:

1. **User Agent**: Känner igen Android, iOS, och andra mobila plattformar
2. **Viewport-storlek**: Media query för `max-width: 768px`
3. **Automatisk uppdatering**: Vid ändring av fönsterstorlek

## Responsiv design

### Breakpoints

- **Extra Small**: < 480px (staplade knappar, kompakt layout)
- **Small/Mobile**: ≤ 768px (gestures aktiverade, större touch targets)
- **Medium/Tablet**: 769px - 1024px (hybrid-läge)
- **Large/Desktop**: > 1024px (standard desktop-layout)

### Anpassningar per breakpoint

**Mobile (≤ 768px):**
- Gestures aktiverade
- Större touch targets (44×44px minimum)
- Bottom action bar synlig
- Pull-to-refresh aktiverad
- Mindre padding för mer innehåll
- Toolbar button group dold (ersätts med overflow menu)

**Desktop (> 768px):**
- Gestures inaktiverade
- Standard musstorlekar
- Bottom action bar dold
- Pull-to-refresh inaktiverad
- Normal padding
- Alla verktyg synliga

## Tillgänglighet

Implementationen följer WCAG 2.1 Level AA:

### Touch Target Sizing
- ✅ Minst 44×44px för alla interaktiva element
- ✅ Extra stor (48×48px) för viktiga åtgärder

### Visuell Feedback
- ✅ Tydlig färgkodning (grön för redigera, röd för ta bort)
- ✅ Animerade övergångar för bättre förståelse
- ✅ Textindikator för pull-to-refresh

### Fokusindikatorer
- ✅ Synliga fokusramar (2px solid)
- ✅ Fungerar i både ljust och mörkt läge
- ✅ Kontrastförhållande uppfyller WCAG AA

### Tangentbordsnavigation
- ✅ Alla gestfunktioner har tangentbordsalternativ
- ✅ Delete-knapp: Ta bort transaktion
- ✅ Edit-knapp: Redigera transaktion
- ✅ Refresh-knapp: Uppdatera lista

## Prestandaoptimering

- **Passive event listeners**: Används där möjligt för bättre scroll-prestanda
- **Debounced resize**: Resize-händelser throttlas för att undvika onödiga uppdateringar
- **Conditional attachment**: Gestures läggs bara till på mobila enheter
- **Cleanup**: Korrekt dispose av event listeners och .NET-referenser

## Framtida förbättringar

- [ ] Bottom sheet-menyer för åtgärder
- [ ] Swipe up för att visa detaljer
- [ ] Long press för fler alternativ
- [ ] Haptic feedback på iOS/Android
- [ ] Gesture hints för nya användare
- [ ] Anpassningsbara swipe-trösklar i inställningar
- [ ] Multi-select med gestures

## Felsökning

### Gestures fungerar inte

1. **Kontrollera console**: Öppna devtools och kolla efter JavaScript-fel
2. **Verifiera viewport**: Testa att `window.mobileGestureHandler.isMobileDevice()` returnerar `true`
3. **Kontrollera grid-container**: Se till att `.mud-table-container` finns i DOM innan `attachGesturesToGrid()` anropas
4. **Testa på riktig mobil**: Emulatorer kan bete sig annorlunda

### Pull-to-refresh triggas inte

1. **Scroll position**: Du måste vara längst upp (scrollY === 0)
2. **Touch events**: Se till att touch events inte blockeras av annat
3. **Threshold**: Standardgränsen är 100px - prova att dra längre

### Performance-problem

1. **Begränsa antal lyssnare**: Fäst bara lyssnare på synliga element
2. **Virtual scrolling**: Överväg för långa listor (1000+ items)
3. **Throttle**: Lägg till throttling för touchmove om det laggar

## Support

För frågor eller problem, skapa en issue på GitHub:
https://github.com/pownas/Privatekonomi/issues

## Changelog

### Version 1.0.0 (2025-01-29)
- ✅ Swipe left/right för delete/edit
- ✅ Pull-to-refresh funktionalitet
- ✅ Thumbzone-optimerad layout
- ✅ WCAG 2.1 AA touch targets
- ✅ Responsiv design med breakpoints
- ✅ Auto-detektion av mobila enheter
- ✅ Visuell feedback för gestures
