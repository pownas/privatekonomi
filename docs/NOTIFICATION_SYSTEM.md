# Smart Notifikationssystem

> Historisk funktionsbeskrivning. API-notifieringsroutes är spärrade som
> standard; in-app-funktioner i Web kan använda interna tjänster. Se
> [säkerhetsguiden för äldre API:er](LEGACY_API_SECURITY.md) innan API-exemplen
> nedan används.

Detta dokument beskriver det smarta notifikationssystemet med flera kanaler som implementerats i Privatekonomi.

## Översikt

Notifikationssystemet erbjuder ett flexibelt och konfigurerbart sätt att meddela användare om viktiga händelser i deras privatekonomi. Systemet stöder flera notifikationskanaler och ger användaren full kontroll över hur och när de vill bli notifierade.

## Funktioner

### Notifikationskanaler

Systemet stöder följande kanaler:

- **In-App** - Realtidsnotifikationer i webbapplikationen
- **Email** - E-postnotifikationer
- **SMS** - SMS via Twilio (kräver konfiguration)
- **Push** - Push-notifikationer för PWA
- **Slack** - Integration med Slack via webhooks
- **Teams** - Integration med Microsoft Teams via webhooks

### Notifikationstyper

Följande typer av notifikationer stöds:

#### Budget & Ekonomi
- 📊 **Budgetöverdrag** - När en budgetkategori överskrids
- ⚠️ **Budgetvarning** - När en kategori närmar sig budgetgränsen
- 💰 **Låg balans** - När kontosaldo är lågt

#### Räkningar
- 📅 **Kommande räkning** - Påminnelse om kommande räkning
- 🔔 **Räkning förfaller** - Räkning förfaller snart
- ⏰ **Försenad räkning** - Räkning har förfallit

#### Sparmål
- 🎯 **Sparmål uppnått** - Ett sparmål har nåtts
- 📈 **Sparmål milstolpe** - Ett delmål har uppnåtts

#### Investeringar
- 💹 **Investeringsförändring** - Betydande förändring i portföljen
- ⬆️ **Stor vinst** - Investeringen har ökat mer än 5% (konfigurerbart framöver)
- ⬇️ **Stor förlust** - Investeringen har minskat mer än 5% (konfigurerbart framöver)

#### Transaktioner
- ⚠️ **Ovanlig transaktion** - Transaktion som avviker från normalt mönster
- 💸 **Stor transaktion** - Mycket större transaktion än vanligt

#### Banksync
- ❌ **Banksynk misslyckades** - Problem med automatisk synkronisering
- ✅ **Banksynk lyckades** - Synkronisering slutförd

#### Hushåll
- 👥 **Hushållsaktivitet** - Annan medlem gjorde en transaktion
- 📧 **Hushållsinbjudan** - Inbjudan till att gå med i hushåll

#### Abonnemang
- 📈 **Prenumerationspris ökat** - Ett abonnemang har ökat i pris
- 🔄 **Prenumerationsförnyelse** - Ett abonnemang förnyas snart

### Prioritetsnivåer

Varje notifikation har en prioritetsnivå:

- **Låg** - Informativa notifikationer
- **Normal** - Standardnotifikationer
- **Hög** - Viktiga notifikationer som kräver uppmärksamhet
- **Kritisk** - Brådskande notifikationer (skickas alltid, även under DND)

### Do Not Disturb (DND)

Användare kan konfigurera DND-scheman för att undvika notifikationer under vissa tider:

- **Tidsintervall** - Ange starttid och sluttid (ex: 22:00 - 08:00)
- **Veckodagar** - Välj specifika dagar eller alla dagar
- **Tillåt kritiska** - Kritiska notifikationer kan skickas trots DND
- **Flera scheman** - Skapa olika scheman för olika veckodagar

### Digest-läge

Istället för att få notifikationer direkt kan användare välja att få dem grupperade:

- **Aktivera digest** - Gruppera notifikationer per typ
- **Intervall** - Välj hur ofta digest ska skickas (ex: var 24:e timme)
- **Sammanfattning** - Få en sammanfattning av alla notifikationer

### Konfiguration per Notifikationstyp

Varje notifikationstyp kan konfigureras individuellt:

- **Aktivera/Inaktivera** - Slå på eller av notifikationen
- **Kanaler** - Välj vilka kanaler som ska användas
- **Minimiprioritet** - Endast notifikationer över denna prioritet skickas
- **Digest-läge** - Aktivera digest för denna typ

## Användarinterface

### Notifikationsklocka

En notifikationsklocka-ikon visas i appens huvudmeny med:
- Badge som visar antal olästa notifikationer
- Klickbar för att navigera till notifikationssidan
- Uppdateras automatiskt var 30:e sekund

### Notifikationssida

Dedikerad sida för att hantera notifikationer:
- **Lista** - Visa alla eller endast olästa notifikationer
- **Filtrera** - Växla mellan alla och olästa
- **Markera som läst** - Markera enskilda eller alla notifikationer
- **Radera** - Ta bort enskilda notifikationer
- **Klickbara** - Klicka på notifikation för att navigera till relaterad sida

## API Endpoints

### Notifikationer

```http
GET /api/notifications?unreadOnly=false
GET /api/notifications/unread-count
POST /api/notifications/{id}/mark-read
POST /api/notifications/mark-all-read
DELETE /api/notifications/{id}
```

### Preferenser

```http
GET /api/notifications/preferences
PUT /api/notifications/preferences/{id}
POST /api/notifications/initialize-defaults
```

### DND-scheman

```http
GET /api/notifications/dnd-schedules
POST /api/notifications/dnd-schedules
DELETE /api/notifications/dnd-schedules/{id}
```

### Integrationer

```http
GET /api/notifications/integrations
POST /api/notifications/integrations
DELETE /api/notifications/integrations/{id}
```

## Användning i Kod

### Skicka en Notifikation

```csharp
public class BudgetService
{
    private readonly INotificationService _notificationService;
    
    public async Task CheckBudgetAsync(string userId, decimal spent, decimal budget)
    {
        if (spent > budget)
        {
            await _notificationService.SendNotificationAsync(
                userId,
                SystemNotificationType.BudgetExceeded,
                "Budget överskriden",
                $"Du har överskridit din budget med {spent - budget:C}",
                NotificationPriority.High,
                data: null,
                actionUrl: "/budgets"
            );
        }
    }
}
```

### Initiera Standardpreferenser för Ny Användare

```csharp
public async Task OnUserRegistered(string userId)
{
    await _notificationPreferenceService.InitializeDefaultPreferencesAsync(userId);
}
```

## Teknisk Implementation

### Modeller

- **Notification** - Enskild notifikation
- **NotificationPreference** - Användarinställningar per notifikationstyp
- **DoNotDisturbSchedule** - DND-schema
- **NotificationIntegration** - Externa integrationer (Slack, Teams)

### Services

- **INotificationService** - Hantera och skicka notifikationer
- **INotificationPreferenceService** - Hantera användarinställningar
- **IEmailNotificationService** - Skicka e-post (stub)
- **ISmsNotificationService** - Skicka SMS (stub)
- **IPushNotificationService** - Skicka push (stub)
- **ISlackNotificationService** - Skicka till Slack (stub)
- **ITeamsNotificationService** - Skicka till Teams (stub)

### Databas

Notifikationsdata lagras i följande tabeller:
- `Notifications` - Alla notifikationer
- `NotificationPreferences` - Användarpreferenser
- `DoNotDisturbSchedules` - DND-scheman
- `NotificationIntegrations` - Externa integrationer

## Framtida Förbättringar

Följande funktioner har stub-implementationer och behöver integreras med faktiska tjänster:

- [ ] **SignalR för realtidsnotifikationer** - In-app notifikationer uppdateras för närvarande var 30:e sekund
- [ ] **Email-service** - Faktisk SMTP-integration (för närvarande endast loggning)
- [ ] **Twilio-integration för SMS** - SMS-notifikationer (för närvarande endast loggning)
- [ ] **PWA push notifications** - Web push API-integration
- [ ] **Slack webhook-implementation** - Faktisk HTTP-integration med Slack API
- [ ] **Teams webhook-implementation** - Faktisk HTTP-integration med Teams API
- [ ] **Schemaläggning av digest-notifikationer** - Background service för periodisk digest
- [ ] **Konfigurerbara tröskelvärden** - Gör % för investeringsförändringar och andra trösklar konfigurerbara per användare
- [ ] **Notifikationshistorik och statistik** - Utökad analys av notifikationsmönster
- [ ] **Smart notifikationsgruppering** - ML-baserad gruppering baserat på användarens beteende
- [ ] **A/B-testning av notifikationstexter** - Optimera engagemang

**Nuvarande status:**
- ✅ Core-funktionalitet komplett (modeller, services, API, UI)
- ✅ 26 unit tests
- ⚠️ Channel-services har stub-implementationer som loggar istället för att faktiskt skicka
- ⚠️ Tröskelinvärden (t.ex. 5% för investeringsförändringar) är hårdkodade

## Testing

Systemet har omfattande unit tests:

```bash
# Kör alla notifikationstester
dotnet test --filter "NotificationServiceTests|NotificationPreferenceServiceTests"
```

**Teststatistik:**
- 26 unit tests
- 100% pass rate
- Täcker alla huvudfunktioner

## Support och Frågor

Vid frågor eller problem, se:
- Kodexempel i `tests/Privatekonomi.Core.Tests/NotificationServiceTests.cs`
- API-dokumentation via Swagger på `/swagger`
- Befintlig implementation i `src/Privatekonomi.Core/Services/`
