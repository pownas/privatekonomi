# Utgifts-heatmap och Mönsteranalys

## Översikt

Utgifts-heatmap är ett analysverktyg som visualiserar dina utgiftsmönster över tid genom att visa när på dygnet och vilka veckodagar du spenderar mest pengar. Verktyget hjälper dig att identifiera mönster, upptäcka impulsköp och få insikter om dina konsumtionsvanor.

## Funktioner

### 🔥 Heatmap-visualisering

Heatmapen visar dina utgifter i ett 7×6 rutnät:
- **Kolumner**: Veckodagar (Måndag - Söndag)
- **Rader**: Tidsperioder (00-04, 04-08, 08-12, 12-16, 16-20, 20-24)
- **Färgkodning**: 
  - 🟦 **Blå (Låg)**: 0-25% av maximal utgift
  - 🟨 **Gul (Medel)**: 25-50% av maximal utgift
  - 🟧 **Orange (Hög)**: 50-75% av maximal utgift
  - 🟥 **Röd (Mycket hög)**: 75-100% av maximal utgift

### 📊 Filtreringsmöjligheter

- **Datumintervall**: Välj start- och slutdatum för analysen
- **Kategorifilter**: Filtrera på en specifik kategori för mer detaljerad analys
- **Uppdatera**: Hämta ny data efter filterändringar

### 💡 Automatiska Insikter

Systemet analyserar dina utgiftsmönster och presenterar:

#### Dyraste och Billigaste Dagar
- Visar vilken veckodag du spenderar mest respektive minst
- Inkluderar totalt belopp och antal transaktioner
- Visar vanligaste kategorin för dyraste dagen

#### Utgiftstoppar
- Identifierar de tre största utgiftsperioderna
- Visar dag, tid och totalt belopp
- Inkluderar vanligaste kategorin för varje topp

#### Impulsköp-detektion ⚠️
Systemet detekterar automatiskt potentiella impulsköp genom att analysera:
- **Tid**: Transaktioner mellan 20:00-00:00 (kvällstid)
- **Tröskelvärde**: Aktiveras om impulsköp utgör >5% av totala utgifter
- **Statistik**: Visar totalt belopp, procent av totala utgifter, vanligaste dag och kategori

#### Vanliga Mönster
Systemet identifierar automatiskt olika utgiftsmönster:
- **"Lunch på vardagar"**: >20% av utgifter är lunch (11:00-14:00) på vardagar
- **"Shoppingmönster på helger"**: >30% av utgifter sker på helger
- **"Kvällsutgifter dominerar"**: >40% av utgifter sker på kvällen (18:00-22:00)
- **"Blandade utgiftsmönster"**: Ingen tydlig trend

## Användning

### Navigering
1. Öppna menyn **Ekonomi**
2. Klicka på **Utgifts-heatmap**

### Grundläggande Analys
1. Sidan laddas med aktuell månad som standard
2. Heatmapen visar dina utgifter grupperade per veckodag och tidperiod
3. Scrolla ner för att se automatiskt genererade insikter

### Detaljerad Analys
1. **Ändra period**: Välj startdatum och slutdatum för önskad period
2. **Filtrera på kategori**: Välj en kategori i rullgardinsmenyn för att fokusera analysen
3. **Uppdatera**: Klicka på "Uppdatera"-knappen för att hämta ny data
4. **Tolka färger**: 
   - Mörkare färger = högre utgifter
   - Ljusare färger = lägre utgifter
   - Grå/vit = inga utgifter

### Hovra på Celler
- Håll muspekaren över en cell för att se exakt belopp och antal transaktioner
- Exempel: "437,59 kr (1 transaktioner)"

## Praktiska Användningsfall

### 1. Identifiera Dyra Dagar
**Problem**: Vill veta vilken dag i veckan du spenderar mest.

**Lösning**: 
- Kolla "Insikter"-sektionen för "Dyraste dagen"
- Se heatmapen för att identifiera vilka tider på dagen som driver upp kostnaderna

**Exempel**: Om fredag är dyrast och utgifterna är koncentrerade till 16-20, kan det bero på after work-utgifter.

### 2. Upptäck Impulsköp
**Problem**: Misstänker att du gör impulsköp sent på kvällen.

**Lösning**:
- Kolla om varningen "⚠️ Impulsköp upptäckta" visas
- Se vilken dag och kategori som dominerar impulsköpen
- Fokusera på att undvika shopping under dessa tider

**Exempel**: "Impulsköp upptäckta: 500 kr (15% av totala utgifter), vanligaste dagen: Lördag, kategori: Shopping"

### 3. Analysera Kategori-Specifika Mönster
**Problem**: Vill förstå när du spenderar mest på mat.

**Lösning**:
- Filtrera på kategorin "Mat & Dryck"
- Analysera heatmapen för att se lunch- och middagsmönster
- Identifiera om du äter ute mest på vardagar eller helger

### 4. Jämföra Perioder
**Problem**: Vill se om utgiftsmönstret har förändrats över tid.

**Lösning**:
- Analysera en månad i taget
- Ta skärmdump eller anteckna insikterna
- Jämför olika månader för att se trender

## Tips för Bättre Ekonomi

Baserat på heatmap-insikterna kan du:

1. **Planera handlingstider**: Om du tenderar att handla dyrt på helger, försök att handla på vardagar istället
2. **Identifiera onödiga utgifter**: Impulsköp sent på kvällen är ofta onödiga
3. **Budgetera per dag**: Om fredagar är dyra, sätt en tydlig budget för fredagar
4. **Optimera lunch**: Om lunch-utgifter är höga, överväg att ta med hemifrån vissa dagar
5. **Kvällsrutiner**: Om kvällsutgifter dominerar, skapa nya rutiner som inte involverar shopping

## Teknisk Information

### Databeräkning
- Transaktioner grupperas per veckodag (0 = Måndag, 6 = Söndag)
- Tidsperioder är 4-timmars intervall (00-04, 04-08, osv.)
- Intensitetsnivåer beräknas som procent av maximal cellutgift i perioden

### Impulsköp-algoritm
```
Impulsköp = Transaktioner mellan 20:00-23:59
Detekteras om: (Impulsköp-summa / Total utgift) > 5%
```

### Mönsterigenkänning
Systemet analyserar:
- Tidsdistribution över dygnet
- Veckodagsdistribution
- Kategori-tidkorrelation
- Statistiska avvikelser

## Begränsningar

- **Datakrav**: Kräver minst några transaktioner för meningsfull analys
- **Tidsperioder**: Grupperar i 4-timmars block (ej timme-för-timme)
- **Kategorifilter**: Kan endast filtrera på en kategori åt gången
- **Historik**: Analyserar endast vald period, inte historiska trender över månader

## Vanliga Frågor

**Q: Varför är alla celler blå/låga?**
A: Om du har få transaktioner eller liknande belopp, kommer variationen att vara låg. Prova en längre period eller filtrera på specifika kategorier.

**Q: Vad betyder "Shoppingmönster på helger"?**
A: Det betyder att över 30% av dina utgifter sker på lördagar och söndagar.

**Q: Hur kan jag exportera heatmap-data?**
A: För närvarande finns ingen exportfunktion. Detta kan komma i framtida versioner.

**Q: Påverkar inkomster heatmapen?**
A: Nej, endast utgifter (IsIncome = false) analyseras i heatmapen.

## Relaterade Funktioner

- **[Transaktionskalender](../src/Privatekonomi.Web/Components/Pages/TransactionCalendar.razor)**: Visa transaktioner per dag
- **[Kategoriöversikt](../src/Privatekonomi.Web/Components/Pages/CategoryOverview.razor)**: Analysera utgifter per kategori
- **[Budget](../src/Privatekonomi.Web/Components/Pages/Budgets.razor)**: Skapa budgetar baserat på mönster
- **[Ekonomisk Hälsa](../src/Privatekonomi.Web/Components/Pages/HealthScore.razor)**: Övergripande ekonomisk hälsobedömning

## Framtida Förbättringar

Möjliga förbättringar för framtiden:
- [ ] Jämföra flera månader sida vid sida
- [ ] Export till CSV/PDF
- [ ] Maskininlärning för prediktiva varningar
- [ ] Timme-för-timme visualisering (inte bara 4-timmars block)
- [ ] Anpassningsbara tröskelvärden för impulsköp
- [ ] Notiser när utgiftsmönster avviker från normalt
