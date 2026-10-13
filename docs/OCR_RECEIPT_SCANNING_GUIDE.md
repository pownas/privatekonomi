# OCR Kvittoskanning - Användarguide

## Översikt

OCR (Optical Character Recognition) kvittoskanning gör det möjligt att automatiskt extrahera information från papperskvitton och foton. Funktionen använder Tesseract OCR-motor med stöd för både svenska och engelska kvitton.

## Funktioner

- **Automatisk textextraktion**: Skanna kvitton och extrahera text automatiskt
- **Intelligent parsing**: Identifierar automatiskt:
  - Butik/återförsäljare
  - Totalt belopp
  - Datum
  - Kvittonummer
  - Betalningsmetod
  - Radposter (produkter med pris)
- **Bildhantering**: Bilden avkodas direkt av Tesseract vid OCR-bearbetning
- **Svensk språkstöd**: Optimerad för svenska kvittoformat
- **Redigerbar data**: Granska och redigera extraherad data innan sparning
- **Säkerhetskontroller**: Filvalidering och storleksbegränsningar

## Användning

### Steg 1: Öppna OCR-skanningsdialog

1. Gå till **Kvitton** i menyn
2. Klicka på knappen **"Skanna kvitto (OCR)"**
3. En dialog öppnas för uppladdning av kvittobild

### Steg 2: Ladda upp kvittobild

1. Klicka på uppladdningsområdet eller dra och släpp en bild
2. Tillåtna filformat: JPEG, PNG, GIF, WebP
3. Maximal filstorlek: 10 MB
4. Klicka på **"Skanna med OCR"** för att starta bearbetningen

### Steg 3: Granska OCR-resultat

När skanningen är klar visas:
- Extraherad data i redigerbar form
- Säkerhetsnivå (hur säker OCR:en är på resultatet)
- Lista över identifierade radposter
- Råtext från OCR (expanderbar sektion)

### Steg 4: Redigera och spara

1. Granska all extraherad data
2. Redigera eventuella felaktigheter
3. Klicka på **"Använd data"** för att överföra till kvittoformuläret
4. Spara kvittot som vanligt

## Tips för bästa resultat

### Fotografering

- **Använd god belysning**: Fotografera i dagsljus eller under starkt ljus
- **Fotografera rakt uppifrån**: Undvik sneda vinklar
- **Hela kvittot synligt**: Se till att hela kvittot finns med i bilden
- **Undvik skuggor**: Se till att belysningen är jämn
- **Undvik reflektioner**: Glansiga kvitton kan ge reflektioner från blixt
- **Plant kvitto**: Kvittot ska vara utbrett och inte vikt eller skrynkligt

### Bildkvalitet

- **Hög upplösning**: Använd minst 1000x1000 pixlar
- **Tydlig text**: Texten måste vara läsbar för ögat
- **Ingen oskärpa**: Fokusera kameran ordentligt
- **Kontrast**: Bra kontrast mellan text och bakgrund

### Kvittotyper som fungerar bäst

- ✅ Tydliga termalkvitton från butiker
- ✅ E-kvitton (PDF konverterade till bild)
- ✅ Kvitton med tydlig struktur
- ⚠️ Gamla, blekta kvitton kan ge sämre resultat
- ⚠️ Skadade eller fläckiga kvitton kan vara svåra att läsa

## Säkerhet och integritet

- **Lokal bearbetning**: All OCR-bearbetning sker på servern, ingen data skickas till tredje part
- **Filvalidering**: Endast bildfiler accepteras
- **Storleksbegränsning**: Max 10 MB per fil
- **Användarisolation**: Kvitton är kopplade till din användare

## Tekniska detaljer

### OCR-motor

- **Motor**: Tesseract OCR 5.x
- **Språk**: Svenska + Engelska
- **Datamodeller**: tessdata_fast (snabbare men något lägre noggrannhet)

### Bildhantering

Den uppladdade bilden skickas direkt till Tesseract, som avkodar bilden före textigenkänningen. Ingen separat bildoptimering tillämpas.

### Parsing av kvittodata

OCR-tjänsten använder regex-mönster för att identifiera:

**Belopp**:
- `Totalt: 123,45 kr`
- `Total: 123.45 SEK`
- `Summa att betala: 123,45`

**Datum**:
- `2024-01-15`
- `15/01/2024`
- `15.01.2024`
- `Datum: 2024-01-15`

**Butik**:
- Längsta raden i de första 5 raderna
- Exkluderar rader med endast siffror eller vanliga header-ord

**Radposter**:
- Format: `Produkt ... 29,50`
- Format: `2x Produkt ... 59,00`
- Extraherar beskrivning, antal, och pris

## Felsökning

### "OCR-tjänsten är inte tillgänglig"

**Problem**: Tesseract-data är inte installerad

**Lösning**:
```bash
# Kör installationsskriptet
./setup-tesseract.sh

# Eller manuellt:
mkdir -p src/Privatekonomi.Web/tessdata
cd src/Privatekonomi.Web/tessdata
curl -L -O https://github.com/tesseract-ocr/tessdata_fast/raw/main/eng.traineddata
curl -L -O https://github.com/tesseract-ocr/tessdata_fast/raw/main/swe.traineddata
```

### "Ingen text kunde läsas från bilden"

**Möjliga orsaker**:
- Bilden är för suddig eller oskarp
- För dålig belysning
- Texten är för liten
- Kvittot är för gammalt och blekt

**Lösning**:
- Ta en ny bild med bättre kvalitet
- Försök med bättre belysning
- Zooma in på kvittot

### Felaktig eller ofullständig data

**Problem**: OCR läser fel eller missar information

**Lösning**:
- Detta är normalt för OCR-teknik
- Granska alltid extraherad data
- Redigera manuellt innan du sparar
- Försök med en ny bild om kvaliteten är dålig

### Låg säkerhetsnivå

**Problem**: OCR visar låg konfidensgrad (<50%)

**Lösning**:
- Granska data extra noga
- Överväg att ta en ny bild
- Kontrollera att bildkvaliteten är god

## Kända begränsningar

- **Noggrannhet**: 70-90% för tydliga kvitton, lägre för gamla/dåliga kvitton
- **Språkstöd**: Optimerad för svenska och engelska
- **Kvittoformat**: Fungerar bäst med vanliga butikskvitton
- **Handskriven text**: Kan inte läsa handskrivna kvitton
- **Komplexa layouter**: Kvitton med komplexa layouter kan ge sämre resultat

## Framtida förbättringar

Planerade förbättringar för OCR-funktionen:
- [ ] Kamera-integration direkt i webbläsaren
- [ ] Automatisk rotation och perspektivkorrigering
- [ ] ML-baserad förbättring av parsing
- [ ] Stöd för fler språk
- [ ] Batch-scanning av flera kvitton
- [ ] Automatisk kategorisering baserat på butik

## Support och feedback

Har du problem med OCR-funktionen eller förslag på förbättringar?
- Skapa ett issue på GitHub
- Kontakta support via applikationen
- Bidra med förbättringar via pull requests

## Relaterad dokumentation

- [Kvittohantering](RECEIPT_TRANSACTION_LINKING.md)
- [Transaktionshantering](TRANSACTION_EDIT_SPEC.md)
- [Tesseract OCR Dokumentation](https://tesseract-ocr.github.io/)
