# Påminnelse-hantering med Snooze och Uppföljning

Detta dokument beskriver implementationen av flexibel påminnelsehantering med snooze-funktionalitet i Privatekonomi.

## Översikt

Systemet tillhandahåller nu en komplett lösning för hantering av påminnelser med följande funktioner:
- **Snooze-funktionalitet** med olika varaktigheter (1 timme, 1 dag, 1 vecka)
- **Markera som betald** direkt från notifikation
- **Automatisk uppföljning** för ohanterade påminnelser
- **Eskalering** för kritiska påminnelser
- **Detektion av återkommande snooze** för att identifiera problemfall

## Datamodell

### BillReminder

Följande fält har lagts till i `BillReminder`-modellen:

```csharp
public DateTime? SnoozeUntil { get; set; }          // Snooze till detta datum/tid
public int SnoozeCount { get; set; }                // Antal gånger påminnelsen snoozats
public bool IsCompleted { get; set; }               // Om påminnelsen markerats som slutförd
public DateTime? CompletedDate { get; set; }        // När påminnelsen slutfördes
public int EscalationLevel { get; set; }            // Eskaleringsnivå (0-3)
public DateTime? LastFollowUpDate { get; set; }     // Senaste uppföljning
```

### Notification

Följande fält har lagts till i `Notification`-modellen:

```csharp
public DateTime? SnoozeUntil { get; set; }          // Snooze till detta datum/tid
public int SnoozeCount { get; set; }                // Antal gånger notifikationen snoozats
public int? BillReminderId { get; set; }            // Koppling till BillReminder
```

### SnoozeDuration Enum

```csharp
public enum SnoozeDuration
{
    OneHour = 1,    // 1 timme
    OneDay = 2,     // 1 dag
    OneWeek = 3     // 1 vecka
}
```

## API Endpoints

### POST /api/notifications/{id}/snooze

Snooze en notifikation med vald varaktighet.

**Request Body:**
```json
{
  "duration": 2  // SnoozeDuration: 1=1h, 2=1d, 3=1v
}
```

**Responses:**
- `204 No Content` - Snoozning lyckades
- `404 Not Found` - Notifikation hittades inte
- `500 Internal Server Error` - Serverfel

**Exempel:**
```bash
curl -X POST https://localhost:5000/api/notifications/123/snooze \
  -H "Content-Type: application/json" \
  -d '{"duration": 2}'
```

### POST /api/notifications/{id}/complete

Markera en påminnelse som slutförd och räkningen som betald.

**Responses:**
- `204 No Content` - Slutförandet lyckades
- `404 Not Found` - Notifikation hittades inte
- `500 Internal Server Error` - Serverfel

**Exempel:**
```bash
curl -X POST https://localhost:5000/api/notifications/123/complete
```

### GET /api/notifications/active

Hämta aktiva notifikationer (exkluderar snoozade).

**Query Parameters:**
- `unreadOnly` (boolean, optional) - Visa endast olästa notifikationer

**Response:**
```json
[
  {
    "notificationId": 1,
    "title": "Påminnelse: Betala Elräkning",
    "message": "Räkning förfaller imorgon",
    "type": 20,
    "priority": 2,
    "isRead": false,
    "snoozeUntil": null,
    "snoozeCount": 0,
    "billReminderId": 5
  }
]
```

## Service-funktioner

### SnoozeNotificationAsync

Snooze en notifikation med vald varaktighet.

```csharp
await notificationService.SnoozeNotificationAsync(
    notificationId, 
    userId, 
    SnoozeDuration.OneDay);
```

**Funktionalitet:**
- Sätter `SnoozeUntil` baserat på vald varaktighet
- Ökar `SnoozeCount`
- Uppdaterar kopplad `BillReminder` om sådan finns
- Loggar varning vid 3+ snooze-tillfällen (återkommande snooze-mönster)

### MarkReminderAsCompletedAsync

Markerar en påminnelse som slutförd och räkningen som betald.

```csharp
await notificationService.MarkReminderAsCompletedAsync(notificationId, userId);
```

**Funktionalitet:**
- Markerar notifikation som läst
- Markerar `BillReminder` som slutförd
- Uppdaterar räkningens status till "Paid"
- Sätter betalningsdatum

### GetActiveNotificationsAsync

Hämtar aktiva notifikationer (exkluderar snoozade).

```csharp
var activeNotifications = await notificationService.GetActiveNotificationsAsync(
    userId, 
    unreadOnly: true);
```

**Funktionalitet:**
- Filtrerar bort notifikationer där `SnoozeUntil > DateTime.UtcNow`
- Inkluderar notifikationer där snooze har gått ut
- Kan filtrera på endast olästa

### ProcessReminderFollowUpsAsync

Bearbetar uppföljningar för ohanterade påminnelser (körs automatiskt).

```csharp
await notificationService.ProcessReminderFollowUpsAsync();
```

**Funktionalitet:**
- Hittar påminnelser som:
  - Är skickade men ej slutförda
  - Är äldre än deras påminnelsedatum
  - Inte är snoozade
  - Inte fått uppföljning senaste 24h
- Eskalerar baserat på tid sedan påminnelse:
  - **1 dag**: EscalationLevel 1, High priority
  - **3 dagar**: EscalationLevel 2, High priority
  - **7 dagar**: EscalationLevel 3, Critical priority
- Skickar uppföljningsnotifikation med lämplig ton
- Noterar om påminnelsen snoozats 3+ gånger

### ShouldEscalateReminderAsync

Kontrollerar om en påminnelse bör eskaleras.

```csharp
bool shouldEscalate = await notificationService.ShouldEscalateReminderAsync(notificationId);
```

**Eskalering sker om:**
- Påminnelsen snoozats 3+ gånger
- Eskaleringsnivå är redan 2 eller högre

## Användargränssnitt

### Notifikationslista

Varje påminnelse-notifikation visar:

```
🔔 Påminnelse: Betala Elräkning

Belopp: 1,500 kr
Förfallodatum: Imorgon

[Markera som betald]  [Snooze ▼]  [Skapa transaktion]
                       • 1 timme
                       • 1 dag
                       • 1 vecka
```

**Visuella indikatorer:**
- Snoozade notifikationer visas med halvtransparent stil (opacity: 0.7)
- Snooze-status visas: "💤 Snoozad till 2025-11-05 14:30"
- Antal snooze-tillfällen: "(3 snooz)" om > 0

### Quick Actions

**Markera som betald:**
- Markerar påminnelsen som slutförd
- Sätter räkningen till "Paid"
- Döljer notifikationen från olästa

**Snooze-meny:**
- **1 timme**: För kortsiktiga uppskjutningar
- **1 dag**: Standardval för nästa dag
- **1 vecka**: För långsiktig uppskjutning

**Skapa transaktion:**
- Navigerar till transaktionssidan med förfylld data från räkningen
- Underlättar snabb betalningsregistrering

## Affärslogik

### Återkommande Snooze-detektion

När en påminnelse snoozas 3 eller fler gånger:
1. System loggar en varning
2. Informationen visas i UI: "(3 snooz)"
3. Påminnelsen flaggas för eskalering vid nästa uppföljning

### Eskalering

Eskalering sker automatiskt baserat på:

| Tid sedan påminnelse | EscalationLevel | Priority | Titel-prefix |
|---------------------|----------------|----------|--------------|
| < 1 dag             | 0              | Normal   | "Påminnelse:" |
| 1-3 dagar           | 1              | High     | "Påminnelse:" |
| 3-7 dagar           | 2              | High     | "⚠️ BRÅDSKANDE:" |
| 7+ dagar            | 3              | Critical | "⚠️ BRÅDSKANDE:" |

**Eskalerade meddelanden:**
- Nivå 1-2: "Påminnelse om räkning på X kr som förföll YYYY-MM-DD"
- Nivå 3: "Räkningen på X kr förföll för N dagar sedan. Åtgärd krävs omedelbart!"

### Uppföljning

Automatisk uppföljning sker varje 24:e timme för:
- Skickade men ej slutförda påminnelser
- Påminnelser vars datum passerats
- Inte snoozade påminnelser
- Påminnelser utan uppföljning senaste 24h

## Testning

Implementationen har 22 unit tests som täcker:

### Snooze-funktionalitet
- ✅ Snooze med 1 timme sätter korrekt tidpunkt
- ✅ Snooze med 1 dag sätter korrekt tidpunkt
- ✅ Snooze med 1 vecka sätter korrekt tidpunkt
- ✅ Flera snooze-tillfällen ökar räknaren korrekt
- ✅ Ogiltigt notifikations-ID kastar exception

### Completion-funktionalitet
- ✅ Markera som slutförd uppdaterar notifikation
- ✅ Markera som slutförd uppdaterar BillReminder
- ✅ Markera som slutförd markerar räkning som betald

### Filtering-funktionalitet
- ✅ GetActiveNotifications exkluderar snoozade
- ✅ GetActiveNotifications inkluderar utgångna snooze

### Eskalering
- ✅ ShouldEscalate returnerar true vid högt snooze-antal
- ✅ ShouldEscalate returnerar false vid lågt snooze-antal

## Exempel på Användning

### Backend (C#)

```csharp
// Snooze en påminnelse
await notificationService.SnoozeNotificationAsync(
    notificationId: 123,
    userId: "user-abc",
    duration: SnoozeDuration.OneDay
);

// Markera som slutförd
await notificationService.MarkReminderAsCompletedAsync(
    notificationId: 123,
    userId: "user-abc"
);

// Hämta aktiva påminnelser
var active = await notificationService.GetActiveNotificationsAsync(
    userId: "user-abc",
    unreadOnly: true
);
```

### Frontend (JavaScript/Blazor)

```javascript
// Snooze notifikation
async function snoozeNotification(notificationId, duration) {
    const response = await fetch(`/api/notifications/${notificationId}/snooze`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ duration })
    });
    return response.ok;
}

// Markera som betald
async function completeReminder(notificationId) {
    const response = await fetch(`/api/notifications/${notificationId}/complete`, {
        method: 'POST'
    });
    return response.ok;
}

// Hämta aktiva notifikationer
async function getActiveNotifications() {
    const response = await fetch('/api/notifications/active?unreadOnly=true');
    return await response.json();
}
```

## Framtida Förbättringar

Potentiella förbättringar för framtida versioner:

1. **Anpassningsbara snooze-intervall**: Låt användare själva välja varaktighet
2. **Smart snooze-förslag**: AI-baserade förslag baserat på användarens beteende
3. **Gruppsnooze**: Snooze flera påminnelser samtidigt
4. **Snooze-historik**: Visa historik över snooze-tillfällen
5. **Push-notifikationer**: Integration med mobila enheter för påminnelser
6. **Webhook-integration**: Notifiera externa system vid eskalering

## Relaterade Dokument

- [Notification System Documentation](NOTIFICATION_SYSTEM.md)
- [API Documentation](API_GUIDE.md)
- [Notification UI Guide](NOTIFICATION_UI_GUIDE.md)

## Support och Frågor

För frågor eller problem, öppna en issue på GitHub eller kontakta utvecklingsteamet.
