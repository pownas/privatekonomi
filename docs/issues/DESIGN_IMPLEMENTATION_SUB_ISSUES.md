# Design Implementation Sub-Issues

> Historical plan. The manual issue-creation checklist and its statuses have
> not been verified against the current issue tracker; do not treat them as
> active implementation instructions.

Baserat på `docs/DESIGN_ANALYSIS_2025.md`, här är de rekommenderade sub-issues för att implementera designförbättringarna.

**Huvudissue:** pownas/Privatekonomi#426  
**Referensdokument:** [DESIGN_ANALYSIS_2025.md](../DESIGN_ANALYSIS_2025.md), [VISUAL_UX_IMPROVEMENTS.md](../VISUAL_UX_IMPROVEMENTS.md)

> **Obs:** Denna dokumentation innehåller specifikationer för 7 sub-issues som ska skapas manuellt i GitHub kopplat till huvudissuen #426.
>
> **De 7 sub-issues som ska skapas:**
> 1. Dashboard-omdesign — Fas 1, ⭐⭐⭐, 3-4 dagar
> 2. Förbättrad Sidnavigation — Fas 1, ⭐⭐⭐, 1-2 dagar
> 3. Moderniserade Datakort — Fas 1, ⭐⭐⭐, 2-3 dagar
> 4. Diagramförbättringar — Fas 2, ⭐⭐, 2-3 dagar
> 5. Förbättrad Inloggningssida — Fas 2, ⭐⭐, 2-3 dagar
> 6. Mikrointeraktioner — Fas 3, ⭐⭐, 2-3 dagar
> 7. Empty States & Feedback — Fas 3, ⭐, 2-3 dagar

---

## Översikt

### Prioriteringsmatris

| Förslag | Påverkan | Komplexitet | Prioritet | Fas |
|---------|----------|-------------|-----------|-----|
| Dashboard-omdesign | Hög | Medel | ⭐⭐⭐ | Fas 1 |
| Förbättrad Sidnavigation | Medel | Låg | ⭐⭐⭐ | Fas 1 |
| Moderniserade Datakort | Hög | Medel | ⭐⭐⭐ | Fas 1 |
| Diagramförbättringar | Medel | Låg | ⭐⭐ | Fas 2 |
| Förbättrad Inloggningssida | Medel | Medel | ⭐⭐ | Fas 2 |
| Mikrointeraktioner | Medel | Låg | ⭐⭐ | Fas 3 |
| Empty States & Feedback | Låg | Låg | ⭐ | Fas 3 |

### Fasindelning

- **Fas 1: Snabba Vinster (1-2 veckor)** - Issues 1-3
- **Fas 2: Visuella Förbättringar (2-4 veckor)** - Issues 4-5
- **Fas 3: Polish (1-2 veckor)** - Issues 6-7

---

## Fas 1: Snabba Vinster

### Issue 1: Dashboard-omdesign

**Titel:** Implementera Dashboard-omdesign med Trender, Hierarki och Förbättrade Färger

**Labels:** `design`, `dashboard`, `ux`, `high-priority`, `fas-1`

**Prioritet:** ⭐⭐⭐ Hög

**Estimat:** 3-4 dagar

**Beskrivning:**

Omdesigna Dashboard för att ge användare snabbare insikt om ekonomisk hälsa genom förbättrade trendindikatorer, tydligare visuell hierarki och bättre färgkodning.

**Bakgrund:**
- Dashboard visar mycket information men kan kännas överväldigande
- Viktiga KPI:er (inkomst, utgift, netto) har liknande visuell vikt
- Saknar trendindikatorer för att visa förändring över tid

**Åtgärder:**

#### Fas 1a: Trendindikatorer
- [ ] Lägg till trend-indikatorer (pilar och procentuell förändring) på sammanfattningskort
- [ ] Implementera beräkning av förändring mot föregående period
- [ ] Färgkoda bakgrund baserat på positiv/negativ trend
- [ ] Visa jämförelsetext (t.ex. "vs förra månaden")

#### Fas 1b: Visuell Hierarki
- [ ] Använd större typografi (Typo.h4 → Typo.h3) för huvudsiffror
- [ ] Lägg till fontvikt 600 för belopp
- [ ] Öka gap mellan kort (24px → 32px)
- [ ] Implementera section-avskiljare för tydligare gruppering

#### Fas 1c: Färgschema
- [ ] Uppdatera sammanfattningskort med subtil gradient eller skuggning
- [ ] Harmonisera kategori-chips med samma mättnadsnivå
- [ ] Förbättra sidebar-kontrast (vit bakgrund → lätt grå #F8FAFC)

**Teknisk Implementation:**

```razor
<!-- Föreslaget kort-design med trend -->
<MudPaper Class="summary-card" Elevation="2">
    <MudText Typo="Typo.caption" Class="text-muted">Inkomster</MudText>
    <MudText Typo="Typo.h3" Class="font-weight-bold">821 692 kr</MudText>
    <div class="trend-indicator">
        <MudChip Size="Size.Small" 
                 Color="Color.Success" 
                 Icon="@Icons.Material.Filled.TrendingUp">
            +12% vs förra månaden
        </MudChip>
    </div>
</MudPaper>
```

```css
/* Förbättrad spacing och gradient */
.summary-card {
    padding: var(--spacing-xl);
    border-radius: var(--radius-lg);
    background: linear-gradient(135deg, var(--mud-palette-surface) 0%, rgba(99, 102, 241, 0.05) 100%);
}

.dashboard-grid {
    gap: 32px; /* Ökat från 24px */
}
```

**Berörd Kod:**
- `src/Privatekonomi.Web/Components/Pages/Home.razor`
- `src/Privatekonomi.Web/wwwroot/app.css`
- `src/Privatekonomi.Core/Services/DashboardService.cs` (lägg till trendberäkningar)

**Acceptanskriterier:**
- [ ] Sammanfattningskort visar trend-indikatorer med procentuell förändring
- [ ] Huvudsiffror är mer framträdande med större typografi
- [ ] Korten har subtila gradienter och förbättrad spacing
- [ ] Dashboard känns mindre överväldigande med bättre visuell gruppering

**Referens:** Se `docs/DESIGN_ANALYSIS_2025.md` sektion "Förslag 1: Dashboard-omdesign"

---

### Issue 2: Förbättrad Sidnavigation

**Titel:** Förbättra Sidnavigation med Tydligare Aktiv Markering och Gruppering

**Labels:** `design`, `navigation`, `ux`, `high-priority`, `fas-1`

**Prioritet:** ⭐⭐⭐ Hög

**Estimat:** 1-2 dagar

**Beskrivning:**

Förbättra sidnavigeringen med tydligare visuell markering av aktiv sida, bättre gruppering av menypunkter och förbättrad typografisk hierarki.

**Bakgrund:**
- Sidmenyn har många element som kan vara svåra att överblicka
- Aktiv sida markeras subtilt
- Alla menyobjekt har samma visuella vikt

**Åtgärder:**

#### Fas 2a: Aktiv Markering
- [ ] Implementera tydligare färgad highlight med vänsterkant (3px solid #6366F1)
- [ ] Lägg till gradient-bakgrund för aktiv länk
- [ ] Öka fontvikt till 600 för aktiv länk

#### Fas 2b: Gruppering och Hierarki
- [ ] Lägg till subtila section-headers för grupperade funktioner
- [ ] Implementera uppercase text med letter-spacing för sektionsrubriker
- [ ] Öka spacing mellan ikoner (24px → 32px)

#### Fas 2c: Visuell Feedback
- [ ] Förbättra hover-effekter på menylänkar
- [ ] Lägg till slide-right animation vid hover
- [ ] Konsekvent 32px spacing för lättare klickning/tapping

**Teknisk Implementation:**

```css
/* Ny aktiv-markering */
.nav-item-active {
    background: linear-gradient(90deg, 
        rgba(99, 102, 241, 0.15) 0%, 
        transparent 100%);
    border-left: 3px solid #6366F1;
    font-weight: 600;
}

/* Grupperade sektioner med subtila headers */
.nav-section-header {
    font-size: 0.75rem;
    text-transform: uppercase;
    letter-spacing: 0.1em;
    color: #9CA3AF;
    padding: 16px 16px 8px;
    margin-top: 16px;
}

/* Förbättrad hover */
.mud-nav-link:hover {
    transform: translateX(4px);
    transition: transform var(--transition-fast);
}
```

```razor
<!-- Exempel på grupperad navigation -->
<MudNavMenu>
    <div class="nav-section-header">Översikt</div>
    <MudNavLink Href="/" Icon="@Icons.Material.Filled.Dashboard">Dashboard</MudNavLink>
    
    <div class="nav-section-header">Ekonomi</div>
    <MudNavLink Href="/transaktioner" Icon="@Icons.Material.Filled.Receipt">Transaktioner</MudNavLink>
    <MudNavLink Href="/budgetar" Icon="@Icons.Material.Filled.AccountBalance">Budgetar</MudNavLink>
    <!-- ... -->
</MudNavMenu>
```

**Berörd Kod:**
- `src/Privatekonomi.Web/Components/Layout/NavMenu.razor`
- `src/Privatekonomi.Web/Components/Layout/MainLayout.razor`
- `src/Privatekonomi.Web/wwwroot/app.css`

**Acceptanskriterier:**
- [ ] Aktiv sida har tydlig visuell markering med färgad vänsterkant
- [ ] Menyn har logiska grupperingar med tydliga sektionsrubriker
- [ ] Huvudkategorier har större/tydligare text än undermenyer
- [ ] Hover-effekter ger tydlig visuell feedback

**Referens:** Se `docs/DESIGN_ANALYSIS_2025.md` sektion "Förslag 2: Förbättrad Sidnavigation"

---

### Issue 3: Moderniserade Datakort

**Titel:** Modernisera Datakort med Gradient, Typografi och Ikoner

**Labels:** `design`, `components`, `ux`, `high-priority`, `fas-1`

**Prioritet:** ⭐⭐⭐ Hög

**Estimat:** 2-3 dagar

**Beskrivning:**

Omdesigna sammanfattnings- och datakort med moderna gradienter, förbättrad typografi och bakgrundsikoner för ett mer visuellt tilltalande utseende.

**Bakgrund:**
- Sammanfattningskort har enkel bakgrund
- Alla kort ser likadana ut
- Saknar visuell differentiering mellan olika datatyper

**Åtgärder:**

#### Fas 3a: Gradient Bakgrunder
- [ ] Implementera subtila gradienter för sammanfattningskort
- [ ] Skapa olika gradient-varianter för olika korttyper (inkomst, utgift, netto)
- [ ] Säkerställ WCAG-kontrast för text på gradient

#### Fas 3b: Bakgrundsikoner
- [ ] Lägg till stora semi-transparenta bakgrundsikoner (opacity 0.15)
- [ ] Positionera ikoner i nedre högra hörnet
- [ ] Använd relevanta Material Icons för varje korttyp

#### Fas 3c: Typografisk Förbättring
- [ ] Öka fontvikt för belopp till 600-700
- [ ] Lägg till trend-chips med färgkodning
- [ ] Förbättra hierarki mellan etikett och värde

**Teknisk Implementation:**

```razor
<!-- Föreslaget kort-design -->
<MudPaper Class="summary-card gradient-primary" Elevation="0">
    <div class="card-icon-bg">
        <MudIcon Icon="@Icons.Material.Filled.TrendingUp" />
    </div>
    <div class="card-content">
        <MudText Typo="Typo.caption" Class="text-muted">Nettoresultat</MudText>
        <MudText Typo="Typo.h4" Class="font-weight-bold">306 209 kr</MudText>
        <MudChip Size="Size.Small" Color="Color.Success" Icon="@Icons.Material.Filled.ArrowUpward">
            +8,3%
        </MudChip>
    </div>
</MudPaper>
```

```css
/* Gradient kort-stilar */
.summary-card {
    position: relative;
    overflow: hidden;
    padding: 24px;
    border-radius: 16px;
}

.gradient-income {
    background: linear-gradient(135deg, #10B981 0%, #059669 100%);
    color: white;
}

.gradient-expense {
    background: linear-gradient(135deg, #EF4444 0%, #DC2626 100%);
    color: white;
}

.gradient-primary {
    background: linear-gradient(135deg, #6366F1 0%, #8B5CF6 100%);
    color: white;
}

.card-icon-bg {
    position: absolute;
    right: -20px;
    bottom: -20px;
    opacity: 0.15;
    font-size: 100px;
}

.card-icon-bg .mud-icon-root {
    font-size: 120px;
}
```

**Berörd Kod:**
- `src/Privatekonomi.Web/Components/Pages/Home.razor`
- `src/Privatekonomi.Web/Components/Shared/SummaryCard.razor` (skapa om den inte finns)
- `src/Privatekonomi.Web/wwwroot/app.css`

**Acceptanskriterier:**
- [ ] Sammanfattningskort har färgade gradienter baserat på typ
- [ ] Varje kort har en relevant bakgrundsikon
- [ ] Typografi är tydlig och läsbar mot gradientbakgrund
- [ ] Dark mode fungerar korrekt med anpassade färger

**Referens:** Se `docs/DESIGN_ANALYSIS_2025.md` sektion "Förslag 3: Moderniserade Datakort"

---

## Fas 2: Visuella Förbättringar

### Issue 4: Diagramförbättringar

**Titel:** Förbättra Diagram med Enhetlig Färgpalett och Interaktivitet

**Labels:** `design`, `charts`, `ux`, `medium-priority`, `fas-2`

**Prioritet:** ⭐⭐ Medel

**Estimat:** 2-3 dagar

**Beskrivning:**

Förbättra diagram med en modern, harmonisk färgpalett, bättre kortstruktur och ökad interaktivitet för en mer engagerande användarupplevelse.

**Bakgrund:**
- Diagram använder grundläggande MudBlazor-styling
- Färgpaletten kan förbättras för bättre harmoni
- Saknar interaktiva element och navigeringslänkar

**Åtgärder:**

#### Fas 4a: Enhetlig Färgpalett
- [ ] Implementera modern färgpalett för diagram
- [ ] Säkerställ tillräcklig kontrast mellan angränsande färger
- [ ] Skapa återanvändbara färgkonstanter

#### Fas 4b: Diagramkort
- [ ] Lägg till header med titel och filterknappar
- [ ] Implementera footer med navigeringslänk
- [ ] Förbättra padding och spacing

#### Fas 4c: Interaktivitet
- [ ] Lägg till entrance-animationer för diagram
- [ ] Implementera filter-knappar (Månadsvis/Kvartalsvis)
- [ ] Förbättra tooltips med mer information

**Teknisk Implementation:**

```csharp
// ChartColors.cs - Återanvändbar färgpalett
public static class ChartColors
{
    public static readonly string[] ModernPalette = new[]
    {
        "#6366F1",  // Indigo (primär)
        "#EC4899",  // Rosa
        "#8B5CF6",  // Lila
        "#10B981",  // Grön
        "#F59E0B",  // Orange
        "#3B82F6",  // Blå
        "#EF4444",  // Röd
        "#06B6D4",  // Cyan
    };
}
```

```razor
<MudPaper Class="chart-container" Elevation="2">
    <div class="chart-header">
        <MudText Typo="Typo.h6">Utgiftsfördelning per Kategori</MudText>
        <MudButtonGroup Size="Size.Small" Variant="Variant.Outlined">
            <MudButton OnClick="@(() => SetPeriod("month"))">Månadsvis</MudButton>
            <MudButton OnClick="@(() => SetPeriod("quarter"))">Kvartalsvis</MudButton>
        </MudButtonGroup>
    </div>
    <MudChart ChartType="ChartType.Pie" 
              InputData="@data" 
              InputLabels="@labels"
              ChartOptions="@chartOptions" />
    <div class="chart-footer">
        <MudLink Href="/utgifter">Se alla utgifter →</MudLink>
    </div>
</MudPaper>
```

```css
.chart-container {
    padding: var(--spacing-lg);
    border-radius: var(--radius-lg);
}

.chart-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: var(--spacing-md);
}

.chart-footer {
    margin-top: var(--spacing-md);
    text-align: right;
}

/* Entrance animation */
.chart-enter {
    animation: fadeInUp 0.5s ease-out;
}

@keyframes fadeInUp {
    from { opacity: 0; transform: translateY(20px); }
    to { opacity: 1; transform: translateY(0); }
}
```

**Berörd Kod:**
- `src/Privatekonomi.Web/Components/Pages/Home.razor`
- `src/Privatekonomi.Web/Components/Shared/ChartCard.razor` (skapa)
- `src/Privatekonomi.Core/Constants/ChartColors.cs` (skapa)
- `src/Privatekonomi.Web/wwwroot/app.css`

**Acceptanskriterier:**
- [ ] Alla diagram använder den nya harmoniska färgpaletten
- [ ] Diagramkort har tydlig header med titel
- [ ] Filter-knappar fungerar för att byta tidsperiod
- [ ] Navigeringslänkar leder till detaljerade vyer

**Referens:** Se `docs/DESIGN_ANALYSIS_2025.md` sektion "Förslag 4: Diagramförbättringar"

---

### Issue 5: Förbättrad Inloggningssida

**Titel:** Förbättra Inloggningssida med Illustration och Moderniserat Formulär

**Labels:** `design`, `authentication`, `ux`, `medium-priority`, `fas-2`

**Prioritet:** ⭐⭐ Medel

**Estimat:** 2-3 dagar

**Beskrivning:**

Förbättra inloggningssidan med en välkomnande illustration/grafik, förbättrad formulärdesign och bättre användning av skärmutrymme för ett professionellare första intryck.

**Bakgrund:**
- Inloggningssidan är ren och funktionell men kan vara mer inbjudande
- Saknar visuellt intresse
- Använder inte skärmutrymmet effektivt

**Åtgärder:**

#### Fas 5a: Illustration och Layout
- [ ] Skapa/välj illustration som representerar "ekonomisk frihet" eller "ta kontroll"
- [ ] Implementera split-screen layout (illustration + formulär) på desktop
- [ ] Dölj illustration på mobil (responsivt)
- [ ] Lägg till välkomnande text och tagline

#### Fas 5b: Formulärförbättringar
- [ ] Centrera och begränsa formulärbredd (max-width: 400px)
- [ ] Öka padding och spacing mellan fält
- [ ] Förstora inloggningsknappen (height: 48px, font-weight: 600)
- [ ] Lägg till fokusanimationer på formulärfält

#### Fas 5c: Varumärkesidentitet
- [ ] Lägg till logotyp/appnamn prominent
- [ ] Använd primärfärg för accenter
- [ ] Säkerställ konsekvent typografi

**Teknisk Implementation:**

```razor
@page "/Account/Login"

<MudGrid Class="login-page">
    <MudItem xs="12" md="6" Class="login-illustration d-none d-md-flex">
        <div class="illustration-content">
            <img src="/images/login-illustration.svg" alt="Ta kontroll över din ekonomi" />
            <MudText Typo="Typo.h4" Class="mt-6">Ta kontroll över din ekonomi</MudText>
            <MudText Typo="Typo.body1" Class="mt-2 text-secondary">
                Spara tid, pengar och få bättre koll med Privatekonomi.
            </MudText>
        </div>
    </MudItem>
    <MudItem xs="12" md="6" Class="login-form-container">
        <div class="login-form">
            <MudImage Src="/images/logo.svg" Alt="Privatekonomi" Width="60" Class="mb-4" />
            <MudText Typo="Typo.h5" Class="mb-6">Logga in</MudText>
            
            <MudTextField Label="E-post" 
                          @bind-Value="email" 
                          Variant="Variant.Outlined"
                          Class="mb-4"
                          Adornment="Adornment.Start"
                          AdornmentIcon="@Icons.Material.Filled.Email" />
            
            <MudTextField Label="Lösenord" 
                          @bind-Value="password" 
                          Variant="Variant.Outlined"
                          InputType="InputType.Password"
                          Class="mb-4"
                          Adornment="Adornment.Start"
                          AdornmentIcon="@Icons.Material.Filled.Lock" />
            
            <MudCheckBox @bind-Value="rememberMe" Label="Kom ihåg mig" Class="mb-4" />
            
            <MudButton Variant="Variant.Filled" 
                       Color="Color.Primary" 
                       FullWidth="true"
                       Size="Size.Large"
                       Class="login-button">
                Logga in
            </MudButton>
            
            <MudText Typo="Typo.body2" Align="Align.Center" Class="mt-4">
                Har du inget konto? <MudLink Href="/Account/Register">Registrera dig</MudLink>
            </MudText>
        </div>
    </MudItem>
</MudGrid>
```

```css
.login-page {
    min-height: 100vh;
}

.login-illustration {
    background: linear-gradient(135deg, #6366F1 0%, #8B5CF6 100%);
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 48px;
}

.illustration-content {
    text-align: center;
    color: white;
    max-width: 400px;
}

.illustration-content img {
    max-width: 300px;
    margin-bottom: 24px;
}

.login-form-container {
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 48px;
}

.login-form {
    max-width: 400px;
    width: 100%;
}

.login-button {
    height: 48px;
    font-size: 1rem;
    font-weight: 600;
}

/* Focus animation */
.login-form .mud-input-root {
    transition: box-shadow var(--transition-base);
}

.login-form .mud-input-root:focus-within {
    box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.2);
}
```

**Berörd Kod:**
- `src/Privatekonomi.Web/Components/Pages/Account/Login.razor`
- `src/Privatekonomi.Web/wwwroot/app.css`
- `src/Privatekonomi.Web/wwwroot/images/` (lägg till illustration)

**Acceptanskriterier:**
- [ ] Inloggningssidan har split-screen layout på desktop
- [ ] Illustration och välkomnande text visas på vänster sida
- [ ] Formuläret är centrerat med förbättrad spacing
- [ ] Responsiv design fungerar på mobil (utan illustration)
- [ ] Inloggningsknappen är mer framträdande

**Referens:** Se `docs/DESIGN_ANALYSIS_2025.md` sektion "Förslag 5: Förbättrad Inloggningssida"

---

## Fas 3: Polish

### Issue 6: Mikrointeraktioner

**Titel:** Implementera Mikrointeraktioner och Animationer för Förbättrad Feedback

**Labels:** `design`, `animations`, `ux`, `medium-priority`, `fas-3`

**Prioritet:** ⭐⭐ Medel

**Estimat:** 2-3 dagar

**Beskrivning:**

Lägg till subtila animationer och mikrointeraktioner för att ge applikationen en mer polerad och professionell känsla med bättre användarfeedback.

**Bakgrund:**
- Begränsade animationer och övergångar idag
- Statisk känsla på många element
- Saknar tydlig feedback vid interaktion

**Åtgärder:**

#### Fas 6a: Kort och Komponenter
- [ ] Implementera smooth hover-transition för kort (lift-effekt)
- [ ] Lägg till subtil skugga vid hover
- [ ] Implementera press-down feedback på knappar

#### Fas 6b: Siffror och Data
- [ ] Skapa count-up animation för belopp vid sidladdning
- [ ] Implementera fade-in-up för nya element
- [ ] Lägg till subtle pulse för viktiga värden

#### Fas 6c: Diagram och Visualiseringar
- [ ] Implementera entrance-animationer för diagram
- [ ] Lägg till staggered animation för tabellrader
- [ ] Förbättra tooltip-animationer

#### Fas 6d: Respektera Tillgänglighet
- [ ] Implementera `prefers-reduced-motion` media query
- [ ] Säkerställ att alla animationer kan inaktiveras
- [ ] Testa med reducerade rörelser aktiverat

**Teknisk Implementation:**

```css
/* Kort-hover med smooth transition */
.mud-card {
    transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.mud-card:hover {
    transform: translateY(-4px);
    box-shadow: 0 12px 40px rgba(0, 0, 0, 0.12);
}

/* Knapp-tryck feedback */
.mud-button:active {
    transform: scale(0.98);
}

/* Siffra-räknare animation */
@keyframes countUp {
    from { opacity: 0; transform: translateY(10px); }
    to { opacity: 1; transform: translateY(0); }
}

.amount-animated {
    animation: countUp 0.4s ease-out;
}

/* Diagram entrance */
@keyframes fadeInUp {
    from { opacity: 0; transform: translateY(20px); }
    to { opacity: 1; transform: translateY(0); }
}

.chart-enter {
    animation: fadeInUp 0.5s ease-out;
}

/* Staggered table rows */
.table-row-enter {
    animation: fadeIn 0.3s ease-out;
    animation-fill-mode: both;
}

.table-row-enter:nth-child(1) { animation-delay: 0.05s; }
.table-row-enter:nth-child(2) { animation-delay: 0.1s; }
.table-row-enter:nth-child(3) { animation-delay: 0.15s; }
/* ... */

/* Respektera prefers-reduced-motion */
@media (prefers-reduced-motion: reduce) {
    *,
    *::before,
    *::after {
        animation-duration: 0.01ms !important;
        animation-iteration-count: 1 !important;
        transition-duration: 0.01ms !important;
    }
}
```

```razor
<!-- Exempel på animated amount -->
<MudText Typo="Typo.h4" Class="amount-animated">
    @income.ToString("C0", new CultureInfo("sv-SE"))
</MudText>
```

**Berörd Kod:**
- `src/Privatekonomi.Web/wwwroot/app.css`
- `src/Privatekonomi.Web/Components/Pages/Home.razor`
- `src/Privatekonomi.Web/Components/Shared/` (diverse komponenter)

**Acceptanskriterier:**
- [ ] Kort lyfts subtilt vid hover
- [ ] Knappar har tydlig feedback vid klick
- [ ] Belopp animeras in vid sidladdning
- [ ] Diagram har smooth entrance-animation
- [ ] Animationer respekterar `prefers-reduced-motion`

**Referens:** Se `docs/DESIGN_ANALYSIS_2025.md` sektion "Förslag 6: Mikrointeraktioner"

---

### Issue 7: Empty States och Feedback

**Titel:** Implementera Visuella Empty States med Illustrationer och Vägledning

**Labels:** `design`, `empty-states`, `ux`, `low-priority`, `fas-3`

**Prioritet:** ⭐ Låg

**Estimat:** 2-3 dagar

**Beskrivning:**

Lägg till visuella tomma tillstånd med illustrationer och tydlig vägledning för att förbättra användarupplevelsen när data saknas.

**Bakgrund:**
- Tomma tillstånd kan sakna visuell vägledning
- Saknas illustrationer för att guida användare
- Nya användare kan bli förvirrade utan tydlig nästa-steg-vägledning

**Åtgärder:**

#### Fas 7a: Illustrationer
- [ ] Skapa/välj illustrationer för olika tomma tillstånd:
  - Inga transaktioner
  - Inga budgetar
  - Inga sparmål
  - Inga investeringar
  - Inga lån
- [ ] Säkerställ konsekvent stil mellan illustrationer
- [ ] Optimera för både light och dark mode

#### Fas 7b: Vägledande Text
- [ ] Skriv vänliga, informativa rubriker
- [ ] Lägg till förklarande text om vad användaren kan göra
- [ ] Översätt all text till svenska

#### Fas 7c: Call-to-Action
- [ ] Lägg till primär CTA-knapp för att påbörja
- [ ] Implementera sekundär länk för hjälp/guide
- [ ] Konsekvent knappstil över alla empty states

#### Fas 7d: Återanvändbar Komponent
- [ ] Skapa `EmptyState.razor` komponent
- [ ] Parametrisera för olika scenarion
- [ ] Implementera i alla relevanta vyer

**Teknisk Implementation:**

```razor
<!-- EmptyState.razor -->
@code {
    [Parameter] public string ImagePath { get; set; } = "/images/empty-default.svg";
    [Parameter] public string Title { get; set; } = "Ingen data";
    [Parameter] public string Description { get; set; } = "";
    [Parameter] public string ActionText { get; set; } = "";
    [Parameter] public string ActionHref { get; set; } = "";
    [Parameter] public EventCallback OnActionClick { get; set; }
    [Parameter] public string HelpText { get; set; } = "";
    [Parameter] public string HelpHref { get; set; } = "";
}

<MudPaper Class="empty-state text-center pa-8" Elevation="0">
    <img src="@ImagePath" 
         alt="@Title" 
         style="max-width: 200px; opacity: 0.8;" />
    
    <MudText Typo="Typo.h6" Class="mt-4">
        @Title
    </MudText>
    
    @if (!string.IsNullOrEmpty(Description))
    {
        <MudText Typo="Typo.body2" Class="text-muted mb-4" Style="max-width: 400px; margin: 0 auto;">
            @Description
        </MudText>
    }
    
    @if (!string.IsNullOrEmpty(ActionText))
    {
        @if (OnActionClick.HasDelegate)
        {
            <MudButton Variant="Variant.Filled" 
                       Color="Color.Primary" 
                       StartIcon="@Icons.Material.Filled.Add"
                       OnClick="OnActionClick">
                @ActionText
            </MudButton>
        }
        else if (!string.IsNullOrEmpty(ActionHref))
        {
            <MudButton Variant="Variant.Filled" 
                       Color="Color.Primary" 
                       StartIcon="@Icons.Material.Filled.Add"
                       Href="@ActionHref">
                @ActionText
            </MudButton>
        }
    }
    
    @if (!string.IsNullOrEmpty(HelpText))
    {
        <MudText Typo="Typo.caption" Class="mt-4">
            <MudLink Href="@HelpHref">@HelpText</MudLink>
        </MudText>
    }
</MudPaper>
```

```razor
<!-- Användningsexempel i Transactions.razor -->
@if (!transactions.Any())
{
    <EmptyState 
        ImagePath="/images/empty-transactions.svg"
        Title="Inga transaktioner än"
        Description="Börja med att importera transaktioner från din bank, eller lägg till din första transaktion manuellt."
        ActionText="Lägg till transaktion"
        OnActionClick="OpenAddTransactionDialog"
        HelpText="Lär dig mer om att importera transaktioner"
        HelpHref="/docs/csv-import" />
}
```

```css
.empty-state {
    background: var(--mud-palette-background-gray);
    border-radius: var(--radius-lg);
    padding: var(--spacing-2xl);
}

.empty-state img {
    filter: opacity(0.8);
}

/* Dark mode adjustment */
.mud-theme-dark .empty-state img {
    filter: opacity(0.7) brightness(0.9);
}
```

**Berörd Kod:**
- `src/Privatekonomi.Web/Components/Shared/EmptyState.razor` (skapa)
- `src/Privatekonomi.Web/Components/Pages/Transactions.razor`
- `src/Privatekonomi.Web/Components/Pages/Budgets.razor`
- `src/Privatekonomi.Web/Components/Pages/Goals.razor`
- `src/Privatekonomi.Web/Components/Pages/Investments.razor`
- `src/Privatekonomi.Web/Components/Pages/Loans.razor`
- `src/Privatekonomi.Web/wwwroot/images/` (lägg till illustrationer)

**Acceptanskriterier:**
- [ ] Alla vyer med möjliga tomma tillstånd har visuella illustrationer
- [ ] Vägledande text är tydlig och på svenska
- [ ] CTA-knappar leder till rätt åtgärder
- [ ] EmptyState-komponenten är återanvändbar
- [ ] Fungerar i både light och dark mode

**Referens:** Se `docs/DESIGN_ANALYSIS_2025.md` sektion "Förslag 7: Empty States och Feedback"

---

## Sammanfattning

### Fas 1: Snabba Vinster (1-2 veckor):
- **Issue 1:** Dashboard-omdesign (⭐⭐⭐, 3-4 dagar)
- **Issue 2:** Förbättrad Sidnavigation (⭐⭐⭐, 1-2 dagar)
- **Issue 3:** Moderniserade Datakort (⭐⭐⭐, 2-3 dagar)

### Fas 2: Visuella Förbättringar (2-4 veckor):
- **Issue 4:** Diagramförbättringar (⭐⭐, 2-3 dagar)
- **Issue 5:** Förbättrad Inloggningssida (⭐⭐, 2-3 dagar)

### Fas 3: Polish (1-2 veckor):
- **Issue 6:** Mikrointeraktioner (⭐⭐, 2-3 dagar)
- **Issue 7:** Empty States & Feedback (⭐, 2-3 dagar)

**Totalt: 7 sub-issues**

**Rekommendation:** Börja med Fas 1 issues (1-3) för snabba vinster och hög påverkan. Issue 4-5 kan implementeras parallellt. Issue 6-7 bör göras sist som polish.

---

## Hur man skapar Sub-Issues

För att skapa dessa 7 sub-issues i GitHub:

1. Gå till huvudissuen #426
2. Skapa varje issue med:
   - Titel enligt "Titel" i respektive sektion ovan
   - Beskrivning inkluderande bakgrund, åtgärder och teknisk implementation
   - Labels: `design`, relevant label (t.ex. `dashboard`, `navigation`), samt rätt faslabel (`fas-1`, `fas-2` eller `fas-3`)
   - Lägg till som sub-issue till #426
   - Tilldela till rätt person

### Exempellabels per issue:

| Issue | Labels |
|-------|--------|
| Dashboard-omdesign | `design`, `dashboard`, `ux`, `high-priority`, `fas-1` |
| Förbättrad Sidnavigation | `design`, `navigation`, `ux`, `high-priority`, `fas-1` |
| Moderniserade Datakort | `design`, `components`, `ux`, `high-priority`, `fas-1` |
| Diagramförbättringar | `design`, `charts`, `ux`, `medium-priority`, `fas-2` |
| Förbättrad Inloggningssida | `design`, `authentication`, `ux`, `medium-priority`, `fas-2` |
| Mikrointeraktioner | `design`, `animations`, `ux`, `medium-priority`, `fas-3` |
| Empty States & Feedback | `design`, `empty-states`, `ux`, `low-priority`, `fas-3` |

---

## Relaterade Dokument

- **[DESIGN_ANALYSIS_2025.md](../DESIGN_ANALYSIS_2025.md)** - Detaljerad visuell analys och designförslag
- **[VISUAL_UX_IMPROVEMENTS.md](../VISUAL_UX_IMPROVEMENTS.md)** - Redan implementerade UX-förbättringar
- **[CHART_DESIGN_GUIDELINES.md](../CHART_DESIGN_GUIDELINES.md)** - Riktlinjer för diagramdesign
- **[DARK_MODE_IMPLEMENTATION.md](../DARK_MODE_IMPLEMENTATION.md)** - Dark mode implementation

---

**Senast uppdaterad:** 2025-12-04  
**Version:** 1.0  
**Antal sub-issues:** 7
