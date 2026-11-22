# Kontohantering - Användarguide

## Översikt

Kontohanteringsfunktionen i Privatekonomi gör det möjligt att tydligt organisera och hantera alla dina olika kontotyper på ett och samma ställe. Systemet stöder:

- **Lönekonton (checking)** - Ditt huvudsakliga bankkonto för löner och vardagliga transaktioner
- **Sparkonton (savings)** - Konton för sparande med högre ränta
- **Kreditkort (credit_card)** - Kreditkortskonton som visar negativa saldon som skulder
- **Investeringskonton (investment)** - Investeringskonton som ISK, AF, kapitalförsäkring
- **Lån (loan)** - Lånekonton som bolån, billån, studielån
- **Pensionskonton (pension)** - Pensionskonton som tjänstepension och privat pensionssparande
- **Kontanter (cash)** - Kontanta medel i hushållet

## Hitta kontohanteringen

Navigera till **Inställningar → Konton** i huvudmenyn.

## Funktioner

### Översiktssida

Översiktssidan visar alla dina konton grupperade efter kontotyp. För varje konto ser du:

- **Namn** - Det namn du gett kontot
- **Kontonummer** - Clearingnummer och kontonummer (t.ex. 8327-123456789)
- **Institution** - Banken eller institutet som hanterar kontot
- **Valuta** - Kontots valuta (standard är SEK)
- **Saldo** - Aktuellt saldo beräknat från ingående saldo + transaktioner
- **Kontoplan** - BAS-kontoplan kod för redovisning (t.ex. 1930)
- **Status** - Om kontot är aktivt eller stängt
- **Åtgärder** - Knappar för att redigera eller ta bort kontot

### Lägga till ett nytt konto

1. Klicka på knappen **"Lägg till konto"**
2. Fyll i följande information:
   - **Kontonamn*** (obligatorisk) - T.ex. "Mitt lönekonto", "Nordea sparkonto"
   - **Kontotyp*** (obligatorisk) - Välj från listan av kontotyper
   - **Bank/Institution** - Namnet på banken (t.ex. Swedbank, Nordea, SEB)
   - **Clearingnummer** - För svenska bankkonton (t.ex. 8327)
   - **Kontonummer** - Kontonumret
   - **Valuta*** (obligatorisk) - Standard är SEK
   - **Kontoplan (BAS)** - BAS-kontoplan kod för integration med redovisning
   - **Ingående saldo** - Saldo när kontot öppnades
   - **Öppningsdatum** - När kontot öppnades (valfritt)
   - **Färg (hex)** - Färgkod för att identifiera kontot i grafer (t.ex. #1976D2)
3. Klicka på **"Lägg till"**

### Redigera ett konto

1. Klicka på redigeringsikonen (penna) för kontot du vill ändra
2. Uppdatera informationen du vill ändra
3. Välj vid behov ett **Hushåll** för att koppla kontot till ett hushåll där du är aktiv medlem
4. När du sparar kopplas både befintliga och kommande transaktioner på kontot till hushållet. Välj **Inget hushåll** för att ta bort kopplingen och göra kontots transaktioner personliga
5. För att stänga ett konto, ange ett **Stängningsdatum**
6. Klicka på **"Spara"**

### Ta bort ett konto

1. Klicka på raderingsikonen (papperskorg) för kontot du vill ta bort
2. Bekräfta borttagningen
3. **OBS:** Transaktioner kopplade till kontot behålls, men kopplingen till kontot tas bort

## Kontoplan-integration (BAS)

Fältet "Kontoplan (BAS)" gör det möjligt att koppla dina konton till den svenska BAS-kontoplanen. Detta underlättar:

- Export till bokföringsprogram
- Automatisk kategorisering i resultat- och balansräkning
- Professionell redovisning

### Vanliga BAS-koder för bankkonton:

- **1910** - Kassa
- **1920** - Plusgiro
- **1930** - Företagskonto/checkkonto
- **1940** - Övriga bankkonton
- **1950** - Sparbankkonto
- **2010** - Checkräkningskredit (kreditkort)
- **2350** - Företagslån (lån)

## Visa konton i balansräkningen

När du har registrerat konton med kontonummer visas dessa automatiskt i balansräkningen (**Ekonomi → Balansräkning**) med:

- Kontonamn
- Kontonummer (om angivet)
- Institution (om angiven)
- Aktuellt saldo

Kreditkort och konton av typen lån visas som skulder. Ett positivt kreditkortssaldo
(till exempel efter en överbetalning eller återbetalning) minskar skuldsumman.
För lånekonton används däremot saldots absoluta belopp.

### Dela balansräkning inom hushållet

- Den som skapar ett hushåll kopplas automatiskt som medlem. Delning är avstängd från början.
- Endast aktiva medlemmar med kopplat appkonto får lägga till, ändra eller ta bort medlemmar.
- Lägg till den andra personens e-postadress. Personen väljer **Koppla mitt konto** på sin medlemspost; appkontots e-postadress måste vara verifierad och matcha.
- Varje aktiv, kopplad medlem måste själv samtycka innan hushållets delning kan aktiveras. Samtycke kan återkallas.
- I äldre hushåll behöver en befintlig medlem först koppla sitt konto via sin registrerade e-postadress. Ett tomt hushåll kan inte tas över genom att lägga till sig själv; skapa i så fall ett nytt hushåll.

Vid start uppgraderar Web och API automatiskt de balansräkningskolumner som saknas
i befintliga relationsdatabaser, även om databasen skapats utan migrationshistorik.
När individuellt samtycke införs stängs tidigare delning av tills medlemmarna har samtyckt.

## Tips och rekommendationer

1. **Ge tydliga namn** - Använd beskrivande namn som "Swedbank Lönekonto" eller "Nordea Bolånekonto"
2. **Lägg till kontonummer** - Detta gör det lättare att identifiera rätt konto vid import av transaktioner
3. **Använd kontoplan** - Koppla konton till BAS-kontoplanen om du behöver redovisning
4. **Välj rätt kontotyp** - Rätt kontotyp hjälper systemet att visa konton korrekt i rapporter
5. **Stäng gamla konton** - Markera konton som stängda istället för att ta bort dem för att behålla historik

## Exempel

### Exempel 1: Lönekonto
- **Namn:** Swedbank Lönekonto
- **Kontotyp:** Lönekonto (checking)
- **Institution:** Swedbank
- **Clearingnummer:** 8327
- **Kontonummer:** 123456789
- **Valuta:** SEK
- **Kontoplan:** 1930
- **Ingående saldo:** 15000 kr

### Exempel 2: Kreditkort
- **Namn:** Nordea MasterCard
- **Kontotyp:** Kreditkort (credit_card)
- **Institution:** Nordea
- **Kontonummer:** **** **** **** 1234
- **Valuta:** SEK
- **Kontoplan:** 2010

### Exempel 3: Sparkonto
- **Namn:** SEB Sparkonto Plus
- **Kontotyp:** Sparkonto (savings)
- **Institution:** SEB
- **Clearingnummer:** 5432
- **Kontonummer:** 987654321
- **Valuta:** SEK
- **Kontoplan:** 1950
- **Ingående saldo:** 50000 kr

## Frågor och svar

**F: Vad händer med mina transaktioner om jag tar bort ett konto?**
S: Transaktionerna behålls i systemet men kopplingen till kontot tas bort.

**F: Kan jag ha flera konton med samma namn?**
S: Ja, men det rekommenderas att använda unika namn för att undvika förvirring.

**F: Måste jag ange kontonummer?**
S: Nej, kontonummer är valfritt, men rekommenderas för bättre spårbarhet.

**F: Vad är skillnaden mellan Lönekonto och Sparkonto?**
S: Det är främst en organisatorisk skillnad. Lönekonton (checking) används vanligtvis för dagliga transaktioner, medan sparkonton (savings) används för sparande med högre ränta.

**F: Hur kopplar jag ett konto till BAS-kontoplanen?**
S: Fyll i BAS-koden i fältet "Kontoplan (BAS)" när du skapar eller redigerar ett konto.

**F: Kan jag ändra kontotyp efter att kontot skapats?**
S: Ja, du kan ändra kontotyp genom att redigera kontot.

## Teknisk information

För utvecklare och tekniskt intresserade:

- Konton lagras i `BankSource`-tabellen i databasen
- Kontotyper är strängvärden: "checking", "savings", "credit_card", "investment", "loan", "pension", "cash"
- Kontonummer och clearingnummer lagras separat för flexibilitet
- Ett valfritt `HouseholdId` på kontot kopplar dess transaktioner till hushållet
- BAS-kontoplan-kod lagras i fältet `ChartOfAccountsCode`
- Aktuellt saldo beräknas dynamiskt: `InitialBalance + Σ(Transaktioner)`
