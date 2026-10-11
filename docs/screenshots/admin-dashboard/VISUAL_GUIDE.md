# Admin Dashboard - Screenshots och Visuell Dokumentation

## Översikt
Denna dokumentation visar den nya admin-dashboarden som implementerades för att övervaka plattformens mätetal och success metrics.

## Navigering till Admin Dashboard

### 1. Admin-länk i navigationsmenyn
När en systemadministratör är inloggad visas en röd "Admin Dashboard"-länk i den vänstra navigationsmenyn:

```
Navigeringsmeny:
├── Dashboard
├── Anpassad Dashboard
├── 🔴 Admin Dashboard  ← Ny länk (endast för systemadmins)
├── Ekonomi
├── Sparande
└── ...
```

**Utseende:**
- Färg: #ff6b6b (röd för att indikera administrativ funktionalitet)
- Ikon: AdminPanelSettings
- Text: "Admin Dashboard"
- Synlighet: Endast för användare med `IsSystemAdmin = true`

### 2. Åtkomstkontroll
- URL: `/admin/metrics`
- Kräver: `[Authorize]` attribut + `IsSystemAdmin` check
- Om ej behörig: Röd varningsruta med meddelandet "Åtkomst nekad - Du måste vara systemadministratör för att se denna sida"

## Huvudvy - Admin Dashboard

### Sidhuvud
```
┌─────────────────────────────────────────────────────────────┐
│ 📊 Admin Dashboard - Mätetal & Success Metrics              │
└─────────────────────────────────────────────────────────────┘
```

### Tidsperiod-filter (Överst på sidan)
```
┌──────────────────────────────────────────────────────────────┐
│  Tidsperiod: [Månadsvis ▼]  Antal perioder: [12 ▼]  [Uppdatera] │
│                                                                  │
│  Alternativ för Tidsperiod:                                     │
│  • Daglig                                                       │
│  • Veckovis                                                     │
│  • Månadsvis (standard)                                         │
│  • Kvartalsvis                                                  │
│  • Årlig                                                        │
│                                                                  │
│  Alternativ för Antal perioder: 6, 12, 24                      │
└──────────────────────────────────────────────────────────────┘
```

## Metrics-sektioner

### Användarstatistik (User Metrics)
```
┌─────────────────────────────────────────────────────────────┐
│                    ANVÄNDARSTATISTIK                          │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐          │
│  │ MAU         │  │ DAU         │  │ DAU/MAU     │          │
│  │ 1           │  │ 0           │  │ 0,00%       │          │
│  │             │  │             │  │             │          │
│  │ 🔴 < mål    │  │ 🔴 < mål    │  │ 🔴 < 40%    │          │
│  │ Mål: +20%   │  │             │  │             │          │
│  └─────────────┘  └─────────────┘  └─────────────┘          │
│                                                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐          │
│  │ Retention   │  │ Churn Rate  │  │ Nya         │          │
│  │ 0,00%       │  │ 0,00%       │  │ användare   │          │
│  │             │  │             │  │ 0           │          │
│  │ 🔴 < 60%    │  │ 🟢 OK       │  │             │          │
│  │             │  │             │  │             │          │
│  └─────────────┘  └─────────────┘  └─────────────┘          │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

**Färgkodning:**
- 🟢 Grön: Mål uppnått (>= 100% av mål)
- 🟡 Gul: Närmar sig mål (80-99% av mål)
- 🔴 Röd: Under mål (< 80% av mål)

### Engagement-statistik
```
┌─────────────────────────────────────────────────────────────┐
│                  ENGAGEMENT-STATISTIK                         │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐          │
│  │ Trans/      │  │ Session     │  │ Feature     │          │
│  │ användare   │  │ Duration    │  │ Adoption    │          │
│  │ 0,00        │  │ 00:00       │  │ 0,00%       │          │
│  │             │  │             │  │             │          │
│  │ 🔴 < 30     │  │ 🔴 < 5min   │  │ 🔴 < 50%    │          │
│  └─────────────┘  └─────────────┘  └─────────────┘          │
│                                                               │
│  ┌─────────────┐                                             │
│  │ NPS Score   │                                             │
│  │ 0,00        │                                             │
│  │             │                                             │
│  │ 🔴 < 50     │                                             │
│  └─────────────┘                                             │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

### Prestandastatistik (Performance Metrics)
```
┌─────────────────────────────────────────────────────────────┐
│                  PRESTANDASTATISTIK                           │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐          │
│  │ Uptime      │  │ Page Load   │  │ Lighthouse  │          │
│  │ 99,95%      │  │ 1,20s       │  │ 94          │          │
│  │             │  │             │  │             │          │
│  │ 🟢 > 99.9%  │  │ 🟢 < 2s     │  │ 🟢 > 90     │          │
│  └─────────────┘  └─────────────┘  └─────────────┘          │
│                                                               │
│  ┌─────────────┐                                             │
│  │ Crash Rate  │                                             │
│  │ 0,01%       │                                             │
│  │             │                                             │
│  │ 🟢 < 0.1%   │                                             │
│  └─────────────┘                                             │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

### Säkerhetsstatistik (Security Metrics)
```
┌─────────────────────────────────────────────────────────────┐
│                   SÄKERHETSSTATISTIK                          │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐          │
│  │ 2FA         │  │ Failed      │  │ Security    │          │
│  │ Adoption    │  │ Logins      │  │ Incidents   │          │
│  │ 0,00%       │  │ 0,00%       │  │ 0           │          │
│  │             │  │             │  │             │          │
│  │ 🔴 < 70%    │  │ 🟢 < 1%     │  │ 🟢 = 0      │          │
│  └─────────────┘  └─────────────┘  └─────────────┘          │
│                                                               │
│  ┌─────────────┐                                             │
│  │ GDPR        │                                             │
│  │ Compliance  │                                             │
│  │ 100,00%     │                                             │
│  │             │                                             │
│  │ 🟢 = 100%   │                                             │
│  └─────────────┘                                             │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

## Historiska Diagram

### MAU Trend (Monthly Active Users över tid)
```
┌─────────────────────────────────────────────────────────────┐
│              MAU TREND - Månadsaktiva Användare               │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│    5 │                                                        │
│    4 │                                                        │
│    3 │                                                        │
│    2 │     ●───●                                              │
│    1 │  ●──●   ●───●───●───●───●───●───●───●───●───●        │
│    0 ├──┼───┼───┼───┼───┼───┼───┼───┼───┼───┼───┼───┼──    │
│      Dec Jan Feb Mar Apr Maj Jun Jul Aug Sep Okt Nov Dec     │
│      2024                                           2025      │
│                                                               │
│  Linjediagram visar utveckling över 12 månader               │
└─────────────────────────────────────────────────────────────┘
```

### Transaktioner per Användare över tid
```
┌─────────────────────────────────────────────────────────────┐
│         TRANSAKTIONER PER ANVÄNDARE - Utveckling              │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│   40 │                                                        │
│   30 │                                    Mål: 30            │
│   20 │                               ─────────────────       │
│   10 │  ●───●───●───●───●───●───●───●───●───●───●───●      │
│    0 ├──┼───┼───┼───┼───┼───┼───┼───┼───┼───┼───┼───┼──    │
│      Dec Jan Feb Mar Apr Maj Jun Jul Aug Sep Okt Nov Dec     │
│      2024                                           2025      │
│                                                               │
│  Linjediagram med måltröskellinje                            │
└─────────────────────────────────────────────────────────────┘
```

## Funktionalitet

### 1. Real-time Beräkningar
- Alla metrics beräknas i realtid från databasen
- Använder `MetricsService` för att hämta data
- Cachelagrade historiska snapshots för snabb åtkomst

### 2. Tidsperiod-filtrering
**Daglig:**
- Visar metrics för varje dag
- Användbart för kortsi ktig övervakning

**Veckovis:**
- Aggregerad veckodata
- Bra för att se veckomönster

**Månadsvis (Standard):**
- Mest använda vyn
- Visar månatlig utveckling

**Kvartalsvis:**
- För långsiktig analys
- Jämför kvartal mot kvartal

**Årlig:**
- Översikt på årsnivå
- För strategisk planering

### 3. Antal Perioder
- Välj mellan 6, 12 eller 24 perioder att visa
- Påverkar både metrics och diagram

### 4. Responsiv Design
- Fungerar på desktop, tablet och mobil
- Använder MudBlazor's responsiva grid (xs="12" sm="6" md="3")
- Kort placeras i kolumner beroende på skärmstorlek

## Tekniska Detaljer

### Komponentstruktur
```
Admin.razor (Blazor Component)
├── PageTitle
├── MudContainer (MaxWidth.ExtraExtraLarge)
│   ├── Loading State (MudProgressLinear)
│   ├── Authorization Check
│   │   └── Error Alert (om ej behörig)
│   └── Authorized Content
│       ├── Header (H3)
│       ├── Time Period Filter (MudPaper)
│       │   ├── Period Type Select
│       │   ├── Period Count Select
│       │   └── Update Button
│       ├── User Metrics Grid (4 cards)
│       ├── Engagement Metrics Grid (4 cards)
│       ├── Performance Metrics Grid (4 cards)
│       ├── Security Metrics Grid (4 cards)
│       └── Historical Charts
│           ├── MAU Trend Chart
│           └── Transactions per User Chart
```

### Data Flow
```
1. User → /admin/metrics
2. CheckIsAuthorized()
   ├── Get authenticated user
   ├── Check IsSystemAdmin
   └── Set _isAuthorized flag
3. LoadMetricsAsync()
   ├── Call MetricsService.GetCurrentMetricsAsync()
   ├── Call MetricsService.GetHistoricalMetricsAsync()
   └── Update UI
4. Render Metrics
   ├── Calculate colors based on targets
   ├── Format values
   └── Display charts
```

### Färglogik
```csharp
private Color GetMetricColor(decimal actual, decimal target)
{
    if (actual >= target) return Color.Success;  // Grön
    if (actual >= target * 0.8m) return Color.Warning;  // Gul
    return Color.Error;  // Röd
}
```

## Exempel på Användning

### Scenario 1: Daglig Övervakning
1. Logga in som systemadmin (test@example.com)
2. Klicka på "Admin Dashboard" i menyn
3. Kontrollera DAU och aktuella värden
4. Kontrollera säkerhetsincidenter (ska vara 0)

### Scenario 2: Månatlig Rapportering
1. Välj "Månadsvis" och "12 perioder"
2. Granska MAU-trend över året
3. Kontrollera om transaktioner/användare når målet
4. Notera avvikelser från mål (röda kort)

### Scenario 3: Kvartalsuppföljning
1. Välj "Kvartalsvis" och "4 perioder"
2. Jämför senaste kvartalet mot föregående
3. Kontrollera MAU-tillväxt (mål: +20% per kvartal)
4. Utvärdera churn rate-trend

## Framtida Förbättringar (Dokumenterade i ADMIN_DASHBOARD.md)

1. **Export-funktionalitet**: Excel/PDF-export av metrics
2. **Email-rapporter**: Automatiska vecko/månadsrapporter
3. **Custom Alerts**: Konfiguerbara larm vid tröskelvärden
4. **Jämförelse-vy**: Jämför perioder mot varandra
5. **Drill-down**: Detaljerad data vid klick på metrics
6. **Real-time Updates**: Live-uppdatering med SignalR
7. **Segmentering**: Filtrera per användargrupp

## Säkerhetsaspekter

- Endast användare med `IsSystemAdmin = true` har åtkomst
- Authorization sker både på page-nivå och i kod
- Ingen känslig data exponeras i API:et utan auth
- Audit log för alla systemadmin-åtgärder (framtida feature)

## Test-användare

För att testa admin-dashboarden:
- Email: test@example.com
- Password: TestPassword123!
- Flaggad som SystemAdmin: Ja

## Filer i Implementationen

### Modeller
- `AdminMetrics.cs` - Huvudmodell för metrics
- `UserMetrics.cs` - Användarstatistik
- `EngagementMetrics.cs` - Engagement-data
- `PerformanceMetrics.cs` - Prestandadata
- `SecurityMetrics.cs` - Säkerhetsdata
- `MetricsSnapshot.cs` - Historisk data

### Services
- `IMetricsService.cs` - Service interface
- `MetricsService.cs` - Metrics-beräkningar

### UI
- `Admin.razor` - Admin dashboard-sida
- `NavMenu.razor` - Uppdaterad med admin-länk

### Dokumentation
- `docs/ADMIN_DASHBOARD.md` - Användardokumentation
- `docs/screenshots/admin-dashboard/VISUAL_GUIDE.md` - Denna fil

## Kontakt och Support

Vid frågor om admin-dashboarden, kontakta utvecklingsteamet eller skapa en issue i GitHub-repositoryt.
