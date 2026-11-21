# Designriktlinjer för Grafer i Privatekonomi

## Syfte
Detta dokument definierar designprinciper, riktlinjer och tekniska specifikationer för grafer i Privatekonomi-systemet. Målet är att skapa moderna, tillgängliga och responsiva visualiseringar som fungerar väl på alla enheter.

## Designprinciper

### 1. Klarhet och Läsbarhet
- **Enkelhet först**: Grafer ska vara enkla att förstå vid första anblicken
- **Tydliga etiketter**: Alla axlar, dataserier och datapunkter ska vara tydligt märkta
- **Lämplig datadensitet**: Undvik överbelastad information; gruppera data vid behov
- **Hierarki**: Viktigaste informationen ska synas först

### 2. Tillgänglighet (WCAG 2.1 AA)
- **Färgkontrast**: Minst 4.5:1 för normal text, 3:1 för stor text och grafiska element
- **Färgoberoende information**: Använd inte enbart färg för att förmedla information
- **Tangentbordsnavigering**: Alla interaktiva element ska vara tillgängliga via tangentbord
- **Skärmläsarstöd**: Semantisk HTML och ARIA-attribut där det behövs
- **Textalternativ**: Tillhandahåll alt-text och dataTabeller som komplement

### 3. Responsivitet
- **Mobile-first**: Design för mobil först, skala sedan upp
- **Flexibla storlekar**: Grafer ska anpassa sig automatiskt till behållarens bredd
- **Touch-vänlig**: Tillräckligt stora klickområden (minimum 44x44px)
- **Adaptiv komplexitet**: Visa mindre detaljer på små skärmar

### 4. Konsistens
- **Enhetlig stil**: Samma färgpalett och typografi genom hela systemet
- **Standardiserade komponenter**: Återanvänd samma grafkomponenter
- **Förutsägbart beteende**: Interaktioner ska fungera likadant överallt

## Färgpalett

### Primära Färger (WCAG AA-godkända)
Följande färgpaletter är optimerade för både ljust och mörkt läge med minst 4.5:1 kontrast mot bakgrund:

#### Ljust Läge (på vit bakgrund #FFFFFF)
- **Primär Blå**: `#1976D2` (Kontrast: 4.54:1)
- **Sekundär Lila**: `#7B1FA2` (Kontrast: 7.29:1)
- **Framgång Grön**: `#388E3C` (Kontrast: 5.03:1)
- **Varning Orange**: `#F57C00` (Kontrast: 4.52:1)
- **Fel Röd**: `#D32F2F` (Kontrast: 6.25:1)
- **Info Cyan**: `#0097A7` (Kontrast: 4.51:1)

#### Mörkt Läge (på mörk bakgrund #1E1E1E)
- **Primär Blå**: `#64B5F6` (Kontrast: 7.56:1)
- **Sekundär Lila**: `#CE93D8` (Kontrast: 7.95:1)
- **Framgång Grön**: `#81C784` (Kontrast: 7.79:1)
- **Varning Orange**: `#FFB74D` (Kontrast: 8.91:1)
- **Fel Röd**: `#E57373` (Kontrast: 6.57:1)
- **Info Cyan**: `#4DD0E1` (Kontrast: 9.67:1)

### Dataserier Färgpalett
För att representera olika dataserier i samma graf:

#### Serie 1-6 (Ljust läge)
1. `#1976D2` - Primär Blå
2. `#388E3C` - Grön
3. `#F57C00` - Orange
4. `#7B1FA2` - Lila
5. `#0097A7` - Cyan
6. `#C2185B` - Rosa (Kontrast: 6.39:1)

#### Serie 1-6 (Mörkt läge)
1. `#64B5F6` - Ljus Blå
2. `#81C784` - Ljus Grön
3. `#FFB74D` - Ljus Orange
4. `#CE93D8` - Ljus Lila
5. `#4DD0E1` - Ljus Cyan
6. `#F48FB1` - Ljus Rosa (Kontrast: 7.12:1)

### Neutrala Färger
- **Text (Ljust läge)**: `#212121` (Primär), `#757575` (Sekundär)
- **Text (Mörkt läge)**: `#FFFFFF` (Primär), `#B0B0B0` (Sekundär)
- **Rutnät**: `rgba(0, 0, 0, 0.12)` (Ljust), `rgba(255, 255, 255, 0.12)` (Mörkt)
- **Hover-bakgrund**: `rgba(0, 0, 0, 0.04)` (Ljust), `rgba(255, 255, 255, 0.08)` (Mörkt)

## Typografi

### Graftext
- **Rubrik (H6)**: Roboto Medium, 20px (1.25rem), Line-height: 1.6
- **Axeletiketter**: Roboto Regular, 14px (0.875rem), Line-height: 1.43
- **Datalabel**: Roboto Medium, 12px (0.75rem), Line-height: 1.66
- **Verktygstips**: Roboto Regular, 14px (0.875rem), Line-height: 1.43
- **Legend**: Roboto Regular, 14px (0.875rem), Line-height: 1.43

Verktygstips för linje- och stapeldiagram ska visa dataseriens namn, värdet och den fullständiga period- eller kategorietiketten. Använd MudBlazors platshållare `{{SERIES_NAME}}`, `{{Y_VALUE}}` och `{{X_VALUE}}`; .NET-platshållare som `{0}` ersätts inte. Glesa ut axelns etiketter med `XAxisLabelInterval` i stället för att lämna datapunkternas etiketter tomma.

För cirkeldiagram med en serie per kategori används `GroupByDataSet`. I MudBlazors cirkeldiagram är `{{Y_VALUE}}` kategorietiketten och `{{X_VALUE}}` segmentets värde; `ShowAsPercentage` måste vara avstängt när beloppet ska visas.

### Siffror och Valutor
- **Format**: Svenska lokalen (sv-SE)
- **Valuta**: `currency: 'SEK', minimumFractionDigits: 0` för stora belopp
- **Procenttal**: En decimal: `F1` (ex: 12.5%)
- **Tusentalsavskiljare**: Mellanslag (ex: 1 234 567 kr)

## Ikonografi

### Standardikoner (Material Icons)
- **Trend Uppåt**: `Icons.Material.Filled.TrendingUp`
- **Trend Nedåt**: `Icons.Material.Filled.TrendingDown`
- **Export CSV**: `Icons.Material.Filled.FileDownload`
- **Export PNG**: `Icons.Material.Filled.Image`
- **Hjälp/Info**: `Icons.Material.Filled.Info`
- **Fullskärm**: `Icons.Material.Filled.Fullscreen`
- **Filter**: `Icons.Material.Filled.FilterList`
- **Zoom In**: `Icons.Material.Filled.ZoomIn`
- **Zoom Out**: `Icons.Material.Filled.ZoomOut`

### Ikonstorlekar
- **Standard**: 24px
- **Små knappar**: 20px
- **Stora aktioner**: 32px
- **Touch-mål**: Minst 44x44px för touch-enheter

## Responsiv Design

### Brytpunkter
Enligt MudBlazor och Material Design:

| Enhet | Brytpunkt | Storlek |
|-------|-----------|---------|
| **Mobile (xs)** | 0-599px | 100% bredd |
| **Tablet (sm)** | 600-959px | 100% bredd, mindre höjd |
| **Tablet Landskap (md)** | 960-1279px | 50% bredd (side-by-side) |
| **Desktop (lg)** | 1280-1919px | 33-50% bredd |
| **Desktop Large (xl)** | 1920px+ | 33% bredd |

### Grafanpassningar per Enhet

#### Mobile (xs: <600px)
- **Höjd**: 280px-320px
- **Legend**: Position Bottom, förenkla vid behov
- **Axlar**: Rotera etiketter om nödvändigt, reducera antal ticks
- **Touch**: Aktivera touch-gester, större klickområden
- **Verktygstips**: Touch-aktiverade, stor text
- **Datapunkter**: Färre synliga punkter, gruppera data

#### Tablet (sm: 600-959px)
- **Höjd**: 320px-400px
- **Legend**: Position Right eller Bottom
- **Axlar**: Standard etikettering
- **Interaktion**: Både touch och mus

#### Desktop (md-xl: >960px)
- **Höjd**: 350px-450px för primära grafer, 300px för sekundära
- **Legend**: Position Right eller Top beroende på layouten
- **Axlar**: Full etikettering med alla detaljer
- **Interaktion**: Mus med hover-effekter, zoom-funktionalitet
- **Datapunkter**: Alla punkter synliga

### MudBlazor Grid Layout
```razor
<MudGrid>
    <!-- Fullbredd graf på mobil, halv bredd på desktop -->
    <MudItem xs="12" md="6">
        <MudPaper Class="pa-4">
            <MudChart ... Height="300px" />
        </MudPaper>
    </MudItem>
    
    <!-- Fullbredd graf på alla enheter -->
    <MudItem xs="12">
        <MudPaper Class="pa-4">
            <MudChart ... Height="350px" />
        </MudPaper>
    </MudItem>
</MudGrid>
```

## WCAG 2.1 AA Krav

### 1.1 Textalternativ (A)
- **1.1.1 Icke-textbaserat innehåll**: Alla grafer ska ha:
  - Beskrivande rubriker (`<MudText Typo="Typo.h6">`)
  - Tillhörande datatabeller som alternativ (`<MudTable>`)
  - ARIA-labels där lämpligt

**Implementation**:
```razor
<div role="img" aria-label="Nettoförmögenhetsutveckling över tid">
    <MudChart ... />
</div>
<MudTable Items="@data" aria-label="Detaljerad data för grafvisning">
    <!-- Tabelldata -->
</MudTable>
```

### 1.3 Anpassningsbar (A)
- **1.3.1 Information och relationer**: Semantisk HTML-struktur
- **1.3.2 Meningsfull sekvens**: Logisk läsordning (rubrik → filter → graf → tabell)

### 1.4 Urskiljbar (A/AA)
- **1.4.1 Användning av färg (A)**: Använd inte enbart färg
  - Positiva värden: Grön färg + uppåt-ikon
  - Negativa värden: Röd färg + nedåt-ikon
  
- **1.4.3 Kontrast (minimum) (AA)**: Minst 4.5:1 för normal text, 3:1 för grafiska element
  - Alla fördefinierade färger uppfyller detta
  
- **1.4.11 Icke-textkontrast (AA)**: Minst 3:1 för UI-komponenter
  - Graflinjer: Minst 2px bredd
  - Datapunkter: Tydliga markörer med kontrast

### 2.1 Tangentbordstillgänglig (A)
- **2.1.1 Tangentbord (A)**: Alla funktioner tillgängliga via tangentbord
  - Filterknappar: Tab-navigering
  - Export-knappar: Tab + Enter/Space
  - Interaktiva grafelement: Focus-visible

**Implementation**:
```razor
<MudButton OnClick="..." 
           aria-label="Exportera data som CSV"
           tabindex="0">
    Exportera CSV
</MudButton>
```

### 2.4 Navigerbar (A/AA)
- **2.4.3 Fokusordning (A)**: Logisk tab-ordning
- **2.4.7 Synlig fokus (AA)**: Tydlig fokusindikator

### 2.5 Inmatningsmodaliteter (A)
- **2.5.5 Målets storlek (AAA men rekommenderat)**: Touch-mål minst 44x44px

### 3.1 Läsbar (A)
- **3.1.1 Språk på sidan (A)**: `<html lang="sv">`

### 3.2 Förutsägbar (A)
- **3.2.1 Vid fokus**: Inga automatiska förändringar vid fokus
- **3.2.2 Vid inmatning**: Inga överraskande händelser

### 4.1 Kompatibel (A)
- **4.1.2 Namn, roll, värde**: Korrekta ARIA-attribut

## Interaktivitet

### Hover-effekter (Desktop)
- **Graflinjer**: Öka linjens tjocklek från 2px till 3px
- **Datapunkter**: Visa större markör (6px → 8px radius)
- **Staplar**: Lätt highlight med opacity 0.8 → 1.0
- **Legend**: Highlight motsvarande dataserie i grafen
- **Verktygstips**: Visa vid hover efter 200ms fördröjning

### Verktygstips (Tooltips)
- **Innehåll**: 
  - Period/Datum
  - Serienamn
  - Värde formaterat (valuta/procent)
  - Förändring sedan föregående period (om relevant)
- **Stil**:
  - Vit bakgrund med lätt skugga (Ljust läge)
  - Mörk bakgrund med skugga (Mörkt läge)
  - 14px text, medium font-weight för värde
  - Padding: 8px 12px
  - Max-width: 300px
- **Positionering**: Ovan datapunkten om möjligt, annars under

### Touch-gester (Mobile/Tablet)
- **Tap**: Visa verktygstips för datapunkt
- **Pan**: Scrolla graf horisontellt (för stora dataset)
- **Pinch**: Zoom in/ut på graf (om aktiverat)
- **Long press**: Visa kontextuell meny (export, detaljer)

### Zoom-funktionalitet
- **Desktop**: 
  - Zoom-knappar i hörnet
  - Musskroll för zoom (med Ctrl)
  - Dra för att zooma område
- **Mobile**:
  - Pinch-to-zoom
  - Dubbel-tap för zoom in
- **Reset**: Tydlig "Återställ zoom" knapp

### Focus-hantering
- **Tangentbord**: 
  - Tab: Navigera mellan interaktiva element
  - Enter/Space: Aktivera knappar
  - Arrow keys: Navigera mellan datapunkter (om implementerat)
- **Fokusindikator**:
  - 2px solid outline i primärfärg
  - Offset: 2px från element
  - Tydligt synlig i både ljust och mörkt läge

## ChartOptions för MudChart

### Standardkonfiguration
```csharp
private ChartOptions _chartOptions = new ChartOptions
{
    // Responsiv höjd
    DisableLegend = false,
    
    // Y-axel
    YAxisTicks = 100000, // Anpassa baserat på dataskala
    YAxisFormat = "C0", // Valutaformat utan decimaler
    MaxNumYAxisTicks = 8, // Max 8 ticks för läsbarhet
    
    // Linjer
    InterpolationOption = InterpolationOption.NaturalSpline, // Mjuka kurvor
    LineStrokeWidth = 2, // Standard 2px, öka till 3px vid hover
    
    // Färger
    ChartPalette = GetColorPalette(), // Anpassad palett
};

private string[] GetColorPalette()
{
    // Returnera rätt palett baserat på tema (ljust/mörkt)
    var isDarkMode = /* check theme */;
    return isDarkMode ? DarkModePalette : LightModePalette;
}

private readonly string[] LightModePalette = new[]
{
    "#1976D2", "#388E3C", "#F57C00", "#7B1FA2", "#0097A7", "#C2185B"
};

private readonly string[] DarkModePalette = new[]
{
    "#64B5F6", "#81C784", "#FFB74D", "#CE93D8", "#4DD0E1", "#F48FB1"
};
```

## Layout-exempel

### 1. Enkelt Linechart med Filter
```razor
<MudText Typo="Typo.h4" Class="mb-4">Nettoförmögenhetskurva</MudText>

<!-- Filter Section -->
<MudPaper Class="pa-4 mb-4" Elevation="2">
    <MudGrid Spacing="2">
        <MudItem xs="12" sm="6" md="4">
            <MudText Typo="Typo.subtitle1" Class="mb-2">Tidsperiod</MudText>
            <MudButtonGroup Color="Color.Primary" Variant="Variant.Outlined" Size="Size.Small">
                <MudButton OnClick="..." Variant="@(selected ? Variant.Filled : Variant.Outlined)">
                    1 år
                </MudButton>
                <!-- Fler knappar -->
            </MudButtonGroup>
        </MudItem>
    </MudGrid>
</MudPaper>

<!-- Chart -->
<MudPaper Class="pa-4 mb-4" Elevation="2">
    <MudText Typo="Typo.h6" Class="mb-4">Utveckling över tid</MudText>
    <div role="img" aria-label="Nettoförmögenhetsutveckling">
        <MudChart ChartType="ChartType.Line" 
                  ChartSeries="@series" 
                  XAxisLabels="@labels"
                  Width="100%" 
                  Height="350px"
                  ChartOptions="@chartOptions" />
    </div>
</MudPaper>

<!-- Data Table (WCAG alternative) -->
<MudPaper Class="pa-4" Elevation="2">
    <MudText Typo="Typo.h6" Class="mb-4">Detaljerad Data</MudText>
    <MudTable Items="@data" Dense="true" Hover="true" aria-label="Detaljerad historik">
        <!-- Table content -->
    </MudTable>
</MudPaper>
```

### 2. Responsiv Grid med Flera Grafer
```razor
<MudGrid>
    <!-- Summary Cards -->
    <MudItem xs="12" sm="6" md="3">
        <MudCard Elevation="2">
            <MudCardContent>
                <MudText Typo="Typo.h6">Nuvarande Värde</MudText>
                <MudText Typo="Typo.h4" Color="Color.Success">1 234 567 kr</MudText>
            </MudCardContent>
        </MudCard>
    </MudItem>
    <!-- Fler cards -->
    
    <!-- Main Chart -->
    <MudItem xs="12">
        <MudPaper Class="pa-4" Elevation="2">
            <MudChart ChartType="ChartType.Line" Height="400px" ... />
        </MudPaper>
    </MudItem>
    
    <!-- Side by Side Charts on Desktop -->
    <MudItem xs="12" md="6">
        <MudPaper Class="pa-4" Elevation="2">
            <MudText Typo="Typo.h6" Class="mb-4">Tillgångar</MudText>
            <MudChart ChartType="ChartType.Pie" Height="300px" ... />
        </MudPaper>
    </MudItem>
    
    <MudItem xs="12" md="6">
        <MudPaper Class="pa-4" Elevation="2">
            <MudText Typo="Typo.h6" Class="mb-4">Skulder</MudText>
            <MudChart ChartType="ChartType.Pie" Height="300px" ... />
        </MudPaper>
    </MudItem>
</MudGrid>
```

## Best Practices

### Do's ✓
- Använd MudBlazor-komponenter för enhetlighet
- Testa färgkontraster med verktyg (ex: WebAIM Contrast Checker)
- Tillhandahåll alltid en datatabell som alternativ till grafer
- Använd semantiska rubriker (h4, h6) för grafavsnitt
- Gruppera relaterade filter och kontroller logiskt
- Testa med tangentbord och skärmläsare
- Anpassa antal datapunkter baserat på skärmstorlek
- Använd loading-indikatorer för asynkron data
- Formattera siffror enligt svenska lokalen

### Don'ts ✗
- Använd inte enbart färg för att kommunicera information
- Överbelasta inte grafer med för mycket data
- Glöm inte responsiv testning på riktiga enheter
- Hårdkoda inte höjder utan anpassa efter innehåll
- Använd inte för små fontstorlekar (<12px)
- Ignorera inte tangentbordsanvändare
- Skapa inte komplexa animationer som kan störa
- Glöm inte felhantering och tomma tillstånd

## Testning

### Manuell Testning
1. **Responsivitet**: Testa på olika skärmstorlekar (DevTools)
2. **Tangentbord**: Navigera endast med Tab, Enter, Space, Arrow keys
3. **Skärmläsare**: Testa med NVDA (Windows) eller VoiceOver (Mac)
4. **Kontrast**: Använd färgkontrast-analyzer
5. **Touch**: Testa på riktig mobil/surfplatta
6. **Zoom**: Testa browser-zoom upp till 200%

### Automatiserad Testning
- **Lighthouse**: Accessibility score >90
- **axe DevTools**: Inga violations
- **WAVE**: Web Accessibility Evaluation Tool

### Checklist för Varje Graf
- [ ] Har beskrivande rubrik (h6)
- [ ] Har alternativ datatabell
- [ ] Färgkontrast uppfyller WCAG AA (4.5:1)
- [ ] Använder inte enbart färg för information
- [ ] Fungerar med tangentbord
- [ ] Har fokusindikatorer
- [ ] Responsiv på mobil (xs), tablet (sm), desktop (md+)
- [ ] Touch-mål är minst 44x44px
- [ ] Verktygstips fungerar både på hover och touch
- [ ] Loading state är tydlig
- [ ] Hanterar tomma dataset gracefully
- [ ] Formaterar svenska valutor och datum korrekt
- [ ] Är testad med skärmläsare

## Framtida Förbättringar

### Fas 2
- **Export**: Högupplösta PNG-exporter med dynamisk rendering
- **Animationer**: Subtila intro-animationer för dataförändringar
- **Zoom**: Avancerad zoom med area-selection
- **Jämförelser**: Side-by-side jämförelser av perioder
- **Delning**: Dela grafer som länkar eller bilder

### Fas 3
- **Interaktiv Legend**: Klicka för att visa/dölja dataserier
- **Datafiltrering**: Interaktiv filtrering direkt i grafen
- **Annotationer**: Lägg till markörer för viktiga händelser
- **Real-time**: Live-uppdatering av grafer
- **Avancerade diagram**: Sankey, treemap, heatmaps

## Resurser

### Verktyg
- [WebAIM Contrast Checker](https://webaim.org/resources/contrastchecker/)
- [WAVE Browser Extension](https://wave.webaim.org/extension/)
- [axe DevTools](https://www.deque.com/axe/devtools/)
- [NVDA Screen Reader](https://www.nvaccess.org/)

### Dokumentation
- [MudBlazor Charts](https://mudblazor.com/components/chart)
- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [Material Design Charts](https://material.io/design/communication/data-visualization.html)

### Inspiration
- [Chart.js](https://www.chartjs.org/)
- [ApexCharts](https://apexcharts.com/)
- [D3.js Gallery](https://observablehq.com/@d3/gallery)

---

**Versionsinformation**:
- Version: 1.0
- Datum: 2025-10-24
- Författare: Privatekonomi Development Team
- Status: Aktiv implementation pågår
