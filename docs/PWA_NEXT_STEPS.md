# PWA - Nästa Steg för Testning och Produktion

> **Historisk testplan, ej driftsinstruktion.** Endpoints och testmiljöer som
> nämns här kan saknas eller vara inaktuella. Verifiera varje route mot aktuell
> API-kod innan den används; se [API-guiden](API_GUIDE.md).

Detta dokument beskriver de steg som behöver göras för att testa och förbereda PWA-implementationen för produktion.

## 📋 Översikt

PWA-implementationen är tekniskt komplett, men behöver genomgå omfattande testning och produktionsförberedelser innan lansering. Detta dokument innehåller checklists för:

1. **Testmiljö-deployment och validering**
2. **Produktionsförberedelser**
3. **Monitering och felhantering**
4. **iOS-specifika överväganden**

## 🧪 Testmiljö - Deployment och Validering

### 1. Deploy till testmiljö

**Förutsättningar:**
- [ ] HTTPS-konfigurerad testserver
- [ ] HTTP till HTTPS-omdirigering aktiverad
- [ ] Samma konfiguration som produktion

**Deployment-steg:**
```bash
# Bygg applikationen
dotnet publish src/Privatekonomi.Web -c Release -o ./publish

# Deploy till testserver (exempel med Azure)
az webapp deployment source config-zip \
  --resource-group privatekonomi-test \
  --name privatekonomi-test-app \
  --src ./publish.zip
```

**Verifiering:**
- [ ] Applikationen startar utan fel
- [ ] HTTPS fungerar korrekt
- [ ] Service Worker laddas (kontrollera i DevTools → Application → Service Workers)
- [ ] Manifest.json är tillgänglig på `/manifest.json`

### 2. Lighthouse-audit

**Kör Lighthouse-audit:**
```bash
# Installera Lighthouse CLI (om ej installerat)
npm install -g lighthouse

# Kör audit på desktop
lighthouse https://test.privatekonomi.se --preset=desktop --view

# Kör audit på mobile
lighthouse https://test.privatekonomi.se --preset=mobile --view
```

**Förväntade resultat:**

| Kategori | Förväntat Score | Kritiska Krav |
|----------|----------------|---------------|
| **PWA** | **> 90** | ✅ Service Worker registrerad |
|  |  | ✅ Offline-svar (200 status) |
|  |  | ✅ Valid manifest.json |
|  |  | ✅ Viewport konfigurerad |
|  |  | ✅ Ikoner (192x192, 512x512) |
|  |  | ✅ Theme-color meta tag |
|  |  | ✅ Apple touch icon |
| Performance | > 80 | ⚠️ First Contentful Paint < 2s |
| Accessibility | > 90 | ⚠️ WCAG 2.1 Level AA |
| Best Practices | > 90 | ⚠️ HTTPS, inga konsol-fel |
| SEO | > 80 | ⚠️ Meta-taggar, robots.txt |

**Felsökning om PWA-score < 90:**
1. Kontrollera att service worker är registrerad: `navigator.serviceWorker.getRegistration()`
2. Verifiera att offline-sidan finns: Navigera till `/offline.html`
3. Kontrollera manifest: Öppna DevTools → Application → Manifest
4. Validera ikoner: Kontrollera att alla ikoner returnerar 200 OK
5. Se Lighthouse-rapporten för specifika fel

### 3. Installation och Testning - Android

**Testenheter:**
- Android 10+ rekommenderat
- Chrome eller Edge webbläsare

**Installationstest:**

**3.1 Installation via Chrome:**
- [ ] Öppna `https://test.privatekonomi.se` i Chrome
- [ ] Vänta på install-banner (eller tryck meny → "Installera app")
- [ ] Tryck "Installera"
- [ ] Verifiera att appen läggs till på hemskärmen
- [ ] Öppna appen från hemskärmen
- [ ] Kontrollera att appen öppnas i standalone-läge (ingen adressfält)

**3.2 Offline-funktionalitet:**
- [ ] Öppna appen och navigera runt (ladda data)
- [ ] Aktivera flygplansläge på enheten
- [ ] Navigera i appen (kontrollera att cachad data visas)
- [ ] Försök skapa en ny transaktion
- [ ] Verifiera att offline-banner visas med "X transaktioner väntar"
- [ ] Inaktivera flygplansläge
- [ ] Kontrollera att transaktionen synkas automatiskt
- [ ] Verifiera att offline-banner försvinner

**3.3 Background Sync:**
- [ ] Skapa transaktion offline (se 3.2)
- [ ] Stäng appen helt (swipe away från recent apps)
- [ ] Återställ internetanslutning
- [ ] Vänta 1-2 minuter
- [ ] Öppna appen igen
- [ ] Verifiera att transaktionen har synkats

**3.4 Push-notifikationer:**
- [ ] Ge tillstånd för notifikationer när appen frågar
- [ ] Trigga en notifikation (t.ex. budgetvarning)
- [ ] Verifiera att push-notis visas
- [ ] Tryck på notisen
- [ ] Verifiera att appen öppnas på rätt sida

### 4. Installation och Testning - iOS

**Testenheter:**
- iOS 15+ (iOS 16.4+ för begränsat push-stöd)
- Safari webbläsare

**Viktigt att förstå:**
⚠️ **iOS har betydande begränsningar** - läs [PWA_IOS_LIMITATIONS.md](PWA_IOS_LIMITATIONS.md) för detaljer.

**4.1 Installation via Safari:**
- [ ] Öppna `https://test.privatekonomi.se` i Safari
- [ ] Tryck på dela-knappen (□ med pil uppåt)
- [ ] Scrolla ner och välj "Lägg till på hemskärmen"
- [ ] Ge appen ett namn (eller behåll "Privatekonomi")
- [ ] Tryck "Lägg till"
- [ ] Verifiera att appen finns på hemskärmen
- [ ] Öppna appen från hemskärmen
- [ ] Kontrollera att appen öppnas i standalone-läge

**4.2 Offline-funktionalitet:**
- [ ] Öppna appen och navigera runt (ladda data)
- [ ] Aktivera flygplansläge
- [ ] Navigera i appen (kontrollera att cachad data visas)
- [ ] Försök skapa en ny transaktion
- [ ] Verifiera att offline-banner visas
- [ ] Inaktivera flygplansläge
- [ ] **VIKTIGT:** Ha appen öppen och vänta
- [ ] Verifiera att transaktionen synkas (kan ta längre tid än Android)

**4.3 Background Sync:**
⚠️ **Fungerar INTE på iOS** - detta är en Apple-begränsning.

**Test för att verifiera begränsningen:**
- [ ] Skapa transaktion offline
- [ ] Stäng appen helt
- [ ] Återställ internetanslutning
- [ ] Vänta 5 minuter (inget händer - förväntat)
- [ ] Öppna appen igen
- [ ] Verifiera att transaktionen synkas NU när appen är öppen

**Dokumentera resultatet:**
- Transaktioner synkas INTE i bakgrunden på iOS
- Användaren måste ha appen öppen för synkronisering
- Detta är dokumenterat och förväntat beteende

**4.4 Push-notifikationer:**
⚠️ **Mycket begränsat stöd på iOS** - fungerar bara på iOS 16.4+ och endast om appen är installerad.

**Test (endast iOS 16.4+):**
- [ ] Installera appen (steg 4.1)
- [ ] Ge tillstånd för notifikationer
- [ ] Trigga notifikation
- [ ] Verifiera om notis visas (kan misslyckas - det är OK)
- [ ] Dokumentera vilket iOS-version som testades
- [ ] Dokumentera om push fungerade eller inte

**Förväntat resultat:** Push fungerar troligen INTE ens på iOS 16.4+. Detta är dokumenterat.

### 5. Installation och Testning - Desktop

**Testplattformar:**
- Windows 10/11 (Chrome, Edge)
- macOS (Chrome, Edge, Safari)
- Linux (Chrome, Firefox)

**5.1 Installation:**
- [ ] Öppna `https://test.privatekonomi.se` i Chrome/Edge
- [ ] Klicka på installations-ikonen i adressfältet (eller vänta på banner)
- [ ] Tryck "Installera"
- [ ] Verifiera att appen öppnas i eget fönster
- [ ] Kontrollera att appen finns i startmenyn/Launchpad
- [ ] Stäng appen och öppna från startmenyn

**5.2 Offline och Background Sync:**
- [ ] Samma test som Android (3.2 och 3.3)
- [ ] Background sync förväntas fungera på Windows och Linux
- [ ] Background sync kan vara begränsat på macOS Safari

### 6. Verifiering av Cache-strategi

**Test i Chrome DevTools:**
- [ ] Öppna DevTools → Network
- [ ] Ladda sidan första gången (alla resurser från network)
- [ ] Ladda om sidan (Cmd/Ctrl + R)
- [ ] Verifiera att statiska resurser laddas från "Service Worker" (eller disk cache)
- [ ] Kontrollera att API-anrop går till nätverket först
- [ ] Aktivera offline-läge i DevTools
- [ ] Ladda om sidan
- [ ] Verifiera att sidan laddas från cache

**Test av Cache Storage:**
- [ ] Öppna DevTools → Application → Cache Storage
- [ ] Kontrollera att `privatekonomi-static-v2` finns
- [ ] Kontrollera att `privatekonomi-dynamic-v2` finns
- [ ] Verifiera att statiska assets är cachade (CSS, JS, ikoner)
- [ ] Öppna IndexedDB → PrivatekonomyOfflineDB
- [ ] Verifiera att databasen finns och har rätt schema

## 🚀 Produktionsförberedelser

### 1. Server-side VAPID-nyckelgenerering

VAPID-nycklar behövs för Web Push-notifikationer. För närvarande är detta inte implementerat.

**Implementation (C#):**

```csharp
// Installera NuGet-paket
// dotnet add package WebPush

using WebPush;

namespace Privatekonomi.Core.Services;

public class PushNotificationService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PushNotificationService> _logger;

    public PushNotificationService(
        IConfiguration configuration,
        ApplicationDbContext context,
        ILogger<PushNotificationService> logger)
    {
        _configuration = configuration;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Generera VAPID-nycklar (kör en gång, spara i konfiguration)
    /// </summary>
    public static VapidDetails GenerateVapidKeys(string subject)
    {
        var keys = VapidHelper.GenerateVapidKeys();
        return new VapidDetails(
            subject: subject, // "mailto:admin@privatekonomi.se"
            publicKey: keys.PublicKey,
            privateKey: keys.PrivateKey
        );
    }

    /// <summary>
    /// Skicka push-notifikation till användare
    /// </summary>
    public async Task SendNotificationAsync(
        string userId, 
        string title, 
        string body,
        string? url = null)
    {
        try
        {
            // Hämta användarens push-subscription från databasen
            var subscription = await _context.PushSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);

            if (subscription == null)
            {
                _logger.LogWarning("No active push subscription found for user {UserId}", userId);
                return;
            }

            // Hämta VAPID-detaljer från konfiguration
            var vapidDetails = new VapidDetails(
                subject: _configuration["Push:Subject"] ?? "mailto:admin@privatekonomi.se",
                publicKey: _configuration["Push:PublicKey"] ?? throw new InvalidOperationException("Push:PublicKey not configured"),
                privateKey: _configuration["Push:PrivateKey"] ?? throw new InvalidOperationException("Push:PrivateKey not configured")
            );

            // Skapa push-subscription från användarens data
            var pushSubscription = new PushSubscription(
                endpoint: subscription.Endpoint,
                p256dh: subscription.P256dh,
                auth: subscription.Auth
            );

            // Skapa notifikations-payload
            var payload = new
            {
                title = title,
                body = body,
                icon = "/icon-192x192.png",
                badge = "/favicon.png",
                url = url ?? "/"
            };

            // Skicka push-notifikation
            var webPushClient = new WebPushClient();
            await webPushClient.SendNotificationAsync(
                pushSubscription,
                JsonSerializer.Serialize(payload),
                vapidDetails
            );

            _logger.LogInformation("Push notification sent to user {UserId}", userId);
        }
        catch (WebPushException ex)
        {
            _logger.LogError(ex, "Failed to send push notification to user {UserId}", userId);
            
            // Om subscription är ogiltig (410 Gone), inaktivera den
            if (ex.StatusCode == System.Net.HttpStatusCode.Gone)
            {
                var subscription = await _context.PushSubscriptions
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);
                
                if (subscription != null)
                {
                    subscription.IsActive = false;
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
```

**Datamodell för PushSubscription:**

```csharp
namespace Privatekonomi.Core.Models;

public class PushSubscription
{
    public int Id { get; set; }
    public string UserId { get; set; } = default!;
    public string Endpoint { get; set; } = default!;
    public string P256dh { get; set; } = default!;
    public string Auth { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
}
```

**Generera VAPID-nycklar (kör en gång):**

```bash
# Skapa ett script för att generera nycklar
dotnet run --project tools/GenerateVapidKeys

# Eller lägg till i Program.cs som en dev-endpoint
# GET /dev/generate-vapid-keys (endast i Development)
```

**Spara i appsettings.Production.json:**

```json
{
  "Push": {
    "Subject": "mailto:admin@privatekonomi.se",
    "PublicKey": "BEl62iUYgUivxIkv69yViEuiBIa-Ib9-SkvMeAtmSJhU...",
    "PrivateKey": "VCOSRPNw3KH88..."
  }
}
```

**API-endpoint för att registrera push-subscription:**

```csharp
// Privatekonomi.Api/Controllers/PushController.cs
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PushController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    [HttpGet("vapid-public-key")]
    public IActionResult GetVapidPublicKey()
    {
        var publicKey = _configuration["Push:PublicKey"];
        if (string.IsNullOrEmpty(publicKey))
        {
            return NotFound("VAPID public key not configured");
        }
        
        return Ok(new { publicKey });
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        // Kontrollera om subscription redan finns
        var existing = await _context.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == dto.Endpoint);
        
        if (existing != null)
        {
            existing.IsActive = true;
            existing.LastUsedAt = DateTime.UtcNow;
        }
        else
        {
            var subscription = new PushSubscription
            {
                UserId = userId!,
                Endpoint = dto.Endpoint,
                P256dh = dto.Keys.P256dh,
                Auth = dto.Keys.Auth
            };
            
            _context.PushSubscriptions.Add(subscription);
        }
        
        await _context.SaveChangesAsync();
        
        return Ok();
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] string endpoint)
    {
        var subscription = await _context.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == endpoint);
        
        if (subscription != null)
        {
            subscription.IsActive = false;
            await _context.SaveChangesAsync();
        }
        
        return Ok();
    }
}
```

**Uppdatera client-side kod (wwwroot/app.js):**

```javascript
// I pwaManager
async subscribeToPush() {
  try {
    const registration = await navigator.serviceWorker.ready;
    
    // Hämta VAPID public key från servern
    const response = await fetch('/api/push/vapid-public-key');
    const { publicKey } = await response.json();
    
    // Subscribe till push
    const subscription = await registration.pushManager.subscribe({
      userVisibleOnly: true,
      applicationServerKey: this.urlBase64ToUint8Array(publicKey)
    });
    
    // Skicka subscription till backend
    await fetch('/api/push/subscribe', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(subscription)
    });
    
    console.log('Push subscription successful');
    return subscription;
  } catch (error) {
    console.error('Push subscription failed:', error);
    throw error;
  }
},

urlBase64ToUint8Array(base64String) {
  const padding = '='.repeat((4 - base64String.length % 4) % 4);
  const base64 = (base64String + padding)
    .replace(/\-/g, '+')
    .replace(/_/g, '/');
  
  const rawData = window.atob(base64);
  const outputArray = new Uint8Array(rawData.length);
  
  for (let i = 0; i < rawData.length; ++i) {
    outputArray[i] = rawData.charCodeAt(i);
  }
  return outputArray;
}
```

**Checklista för VAPID-implementation:**
- [ ] Installera WebPush NuGet-paket
- [ ] Skapa PushNotificationService
- [ ] Skapa PushSubscription datamodell
- [ ] Lägg till DbSet i ApplicationDbContext
- [ ] Skapa migration för PushSubscriptions-tabell
- [ ] Generera VAPID-nycklar
- [ ] Spara VAPID-nycklar i appsettings.Production.json
- [ ] Skapa PushController med endpoints
- [ ] Uppdatera pwaManager med subscribe-funktionalitet
- [ ] Testa push-notifikationer på Android/Windows

### 2. Monitering av Service Worker-registreringsgrad

**Implementation med Application Insights:**

```csharp
// Privatekonomi.Api/Controllers/TelemetryController.cs
[ApiController]
[Route("api/[controller]")]
public class TelemetryController : ControllerBase
{
    private readonly TelemetryClient _telemetry;

    [HttpPost("pwa/sw-registered")]
    public IActionResult ServiceWorkerRegistered([FromBody] ServiceWorkerTelemetry data)
    {
        _telemetry.TrackEvent("ServiceWorkerRegistered", new Dictionary<string, string>
        {
            { "UserAgent", Request.Headers["User-Agent"].ToString() },
            { "Platform", data.Platform },
            { "Browser", data.Browser },
            { "Version", data.SwVersion }
        });
        
        return Ok();
    }

    [HttpPost("pwa/sw-failed")]
    public IActionResult ServiceWorkerFailed([FromBody] ServiceWorkerError error)
    {
        _telemetry.TrackException(new Exception($"Service Worker registration failed: {error.Message}"), 
            new Dictionary<string, string>
            {
                { "UserAgent", Request.Headers["User-Agent"].ToString() },
                { "Error", error.Error }
            });
        
        return Ok();
    }

    [HttpPost("pwa/installed")]
    public IActionResult PwaInstalled([FromBody] InstallTelemetry data)
    {
        _telemetry.TrackEvent("PWAInstalled", new Dictionary<string, string>
        {
            { "Platform", data.Platform },
            { "InstallPromptOutcome", data.Outcome }
        });
        
        return Ok();
    }
}
```

**Client-side tracking (wwwroot/app.js):**

```javascript
// Lägg till i pwaManager
async trackServiceWorkerRegistration(success, error = null) {
  try {
    const endpoint = success ? '/api/telemetry/pwa/sw-registered' : '/api/telemetry/pwa/sw-failed';
    const data = {
      platform: this.getPlatform(),
      browser: this.getBrowser(),
      swVersion: 'v2',
      error: error?.message,
      message: error?.toString()
    };
    
    await fetch(endpoint, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });
  } catch (e) {
    console.error('Failed to track SW registration:', e);
  }
},

async trackInstallation(outcome) {
  try {
    await fetch('/api/telemetry/pwa/installed', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        platform: this.getPlatform(),
        outcome: outcome
      })
    });
  } catch (e) {
    console.error('Failed to track installation:', e);
  }
}
```

**Dashboard-query (Application Insights):**

```kusto
// Service Worker registreringsgrad
customEvents
| where name == "ServiceWorkerRegistered"
| summarize Registered = count() by bin(timestamp, 1d)
| join kind=leftouter (
    customEvents
    | where name == "PageView"
    | summarize PageViews = count() by bin(timestamp, 1d)
) on timestamp
| extend RegistrationRate = (Registered * 100.0) / PageViews
| project timestamp, Registered, PageViews, RegistrationRate
| order by timestamp desc

// Service Worker fel
exceptions
| where outerMessage contains "Service Worker"
| summarize Count = count() by tostring(customDimensions.Error)
| order by Count desc
```

**Checklista:**
- [ ] Implementera TelemetryController
- [ ] Lägg till tracking i pwaManager
- [ ] Konfigurera Application Insights
- [ ] Skapa dashboard för PWA-metrics
- [ ] Sätt upp alert för låg registreringsgrad (<80%)

### 3. Spårning av köade offline-transaktioner

**Backend-endpoint för att rapportera kö-status:**

```csharp
// Privatekonomi.Api/Controllers/TelemetryController.cs
[HttpPost("pwa/queue-status")]
public IActionResult QueueStatus([FromBody] QueueTelemetry data)
{
    _telemetry.TrackMetric("OfflineQueueSize", data.QueueSize, new Dictionary<string, string>
    {
        { "UserId", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous" },
        { "Platform", data.Platform }
    });
    
    return Ok();
}

[HttpPost("pwa/sync-completed")]
public IActionResult SyncCompleted([FromBody] SyncTelemetry data)
{
    _telemetry.TrackEvent("OfflineSyncCompleted", new Dictionary<string, string>
    {
        { "UserId", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous" },
        { "TransactionsSynced", data.Count.ToString() },
        { "SyncDuration", data.Duration.ToString() },
        { "Success", data.Success.ToString() }
    });
    
    return Ok();
}
```

**Client-side tracking:**

```javascript
// I service-worker.js - syncPendingTransactions
async function syncPendingTransactions() {
  const startTime = Date.now();
  let syncedCount = 0;
  let success = true;
  
  try {
    const db = await openDatabase();
    const transactions = await getAllPendingTransactions(db);
    
    console.log(`[Service Worker] Syncing ${transactions.length} pending transactions`);
    
    for (const transaction of transactions) {
      try {
        const response = await fetch('/api/transactions', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(transaction.data)
        });
        
        if (response.ok) {
          await deletePendingTransaction(db, transaction.id);
          syncedCount++;
        }
      } catch (error) {
        console.error('[Service Worker] Failed to sync transaction:', error);
        success = false;
      }
    }
    
    db.close();
    
    // Rapportera sync-resultat
    await reportSyncCompleted(syncedCount, Date.now() - startTime, success);
    
  } catch (error) {
    console.error('[Service Worker] Background sync failed:', error);
    await reportSyncCompleted(syncedCount, Date.now() - startTime, false);
    throw error;
  }
}

async function reportSyncCompleted(count, duration, success) {
  try {
    await fetch('/api/telemetry/pwa/sync-completed', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        count: count,
        duration: duration,
        success: success
      })
    });
  } catch (e) {
    console.error('Failed to report sync:', e);
  }
}

// I pwaManager - rapportera kö-storlek
async reportQueueSize() {
  try {
    const db = await this.openDatabase();
    const transaction = db.transaction(['pendingTransactions'], 'readonly');
    const store = transaction.objectStore('pendingTransactions');
    const count = await new Promise((resolve, reject) => {
      const request = store.count();
      request.onsuccess = () => resolve(request.result);
      request.onerror = () => reject(request.error);
    });
    
    await fetch('/api/telemetry/pwa/queue-status', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        queueSize: count,
        platform: this.getPlatform()
      })
    });
  } catch (e) {
    console.error('Failed to report queue size:', e);
  }
}
```

**Dashboard-query:**

```kusto
// Genomsnittlig kö-storlek per dag
customMetrics
| where name == "OfflineQueueSize"
| summarize AvgQueueSize = avg(value), MaxQueueSize = max(value) by bin(timestamp, 1d)
| order by timestamp desc

// Sync success rate
customEvents
| where name == "OfflineSyncCompleted"
| extend Success = tobool(customDimensions.Success)
| summarize SuccessRate = (countif(Success) * 100.0) / count() by bin(timestamp, 1d)
| order by timestamp desc
```

**Checklista:**
- [ ] Implementera queue-status endpoints
- [ ] Lägg till tracking i service worker
- [ ] Skapa dashboard för offline-metrics
- [ ] Sätt upp alert för stora köer (>10 transaktioner)
- [ ] Sätt upp alert för låg sync success rate (<95%)

### 4. Fel-loggning och Error Tracking

**Implementera centraliserad fel-loggning för Service Worker:**

**Alternativ 1: Sentry (Rekommenderat)**

```javascript
// I service-worker.js - längst upp
importScripts('https://browser.sentry-cdn.com/7.x.x/bundle.min.js');

Sentry.init({
  dsn: 'YOUR_SENTRY_DSN',
  environment: 'production',
  release: 'privatekonomi@2.0.0'
});

// Global error handler
self.addEventListener('error', (event) => {
  Sentry.captureException(event.error);
  console.error('[Service Worker] Error:', event.error);
});

self.addEventListener('unhandledrejection', (event) => {
  Sentry.captureException(event.reason);
  console.error('[Service Worker] Unhandled rejection:', event.reason);
});
```

**Alternativ 2: Custom logging till backend**

```javascript
// I service-worker.js
async function logError(error, context) {
  try {
    await fetch('/api/telemetry/pwa/error', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        message: error.message,
        stack: error.stack,
        context: context,
        timestamp: new Date().toISOString(),
        swVersion: CACHE_VERSION
      })
    });
  } catch (e) {
    console.error('Failed to log error:', e);
  }
}

// Använd i fetch handler
self.addEventListener('fetch', (event) => {
  event.respondWith(
    networkFirst(event.request).catch((error) => {
      logError(error, {
        url: event.request.url,
        method: event.request.method
      });
      throw error;
    })
  );
});
```

**Backend endpoint:**

```csharp
[HttpPost("pwa/error")]
public IActionResult LogError([FromBody] ServiceWorkerError error)
{
    _logger.LogError("Service Worker Error: {Message}\n{Stack}\nContext: {Context}", 
        error.Message, 
        error.Stack, 
        JsonSerializer.Serialize(error.Context));
    
    _telemetry.TrackException(new Exception(error.Message), new Dictionary<string, string>
    {
        { "Source", "ServiceWorker" },
        { "Context", JsonSerializer.Serialize(error.Context) },
        { "SwVersion", error.SwVersion }
    });
    
    return Ok();
}
```

**Checklista:**
- [ ] Välj error tracking-lösning (Sentry eller custom)
- [ ] Implementera error handlers i service worker
- [ ] Skapa backend-endpoint för error logging
- [ ] Konfigurera alerts för kritiska fel
- [ ] Testa error logging

### 5. iOS-specifika överväganden - Email-notifikationer

Eftersom push-notifikationer inte fungerar på iOS, implementera email-notifikationer som fallback.

**Email Notification Service:**

```csharp
// Privatekonomi.Core/Services/NotificationService.cs
public class NotificationService
{
    private readonly IEmailSender _emailSender;
    private readonly PushNotificationService _pushService;
    private readonly IUserDeviceService _deviceService;

    public async Task SendNotificationAsync(
        string userId, 
        string title, 
        string body,
        NotificationType type,
        string? url = null)
    {
        // Försök push-notifikation först
        var hasPushSubscription = await _pushService.HasActiveSubscriptionAsync(userId);
        
        if (hasPushSubscription)
        {
            try
            {
                await _pushService.SendNotificationAsync(userId, title, body, url);
                return; // Success, skip email
            }
            catch (Exception ex)
            {
                // Log och fortsätt till email-fallback
                _logger.LogWarning(ex, "Push notification failed for user {UserId}, falling back to email", userId);
            }
        }
        
        // Fallback till email
        var user = await _userManager.FindByIdAsync(userId);
        if (user?.Email != null && user.EmailConfirmed)
        {
            await SendEmailNotificationAsync(user.Email, title, body, type, url);
        }
    }

    private async Task SendEmailNotificationAsync(
        string email, 
        string title, 
        string body,
        NotificationType type,
        string? url)
    {
        var subject = $"Privatekonomi: {title}";
        var htmlBody = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background: #594AE2; color: white; padding: 20px; text-align: center; }}
                    .content {{ padding: 20px; background: #f4f4f4; }}
                    .button {{ 
                        display: inline-block; 
                        padding: 10px 20px; 
                        background: #594AE2; 
                        color: white; 
                        text-decoration: none; 
                        border-radius: 5px; 
                        margin-top: 20px;
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Privatekonomi</h1>
                    </div>
                    <div class='content'>
                        <h2>{title}</h2>
                        <p>{body}</p>
                        {(url != null ? $"<a href='{url}' class='button'>Visa i appen</a>" : "")}
                    </div>
                </div>
            </body>
            </html>
        ";
        
        await _emailSender.SendEmailAsync(email, subject, htmlBody);
    }
}
```

**Användarinställningar för notifikationer:**

```csharp
// Privatekonomi.Core/Models/UserNotificationPreferences.cs
public class UserNotificationPreferences
{
    public int Id { get; set; }
    public string UserId { get; set; } = default!;
    
    // Kanaler
    public bool EnablePush { get; set; } = true;
    public bool EnableEmail { get; set; } = true;
    public bool EnableSms { get; set; } = false;
    
    // Per notification-typ
    public bool BudgetAlerts { get; set; } = true;
    public bool GoalAchieved { get; set; } = true;
    public bool LowBalance { get; set; } = true;
    public bool UpcomingBills { get; set; } = true;
    
    // Email-specifikt
    public bool EmailDigestMode { get; set; } = false; // Gruppera till daglig sammanfattning
    public TimeSpan? QuietHoursStart { get; set; } = new TimeSpan(22, 0, 0); // 22:00
    public TimeSpan? QuietHoursEnd { get; set; } = new TimeSpan(7, 0, 0); // 07:00
}
```

**UI för inställningar (Blazor):**

```razor
@* NotificationSettings.razor *@
<MudCard>
    <MudCardHeader>
        <CardHeaderContent>
            <MudText Typo="Typo.h6">Notifikationsinställningar</MudText>
        </CardHeaderContent>
    </MudCardHeader>
    <MudCardContent>
        <MudSwitch @bind-Checked="preferences.EnablePush" Color="Color.Primary">
            Push-notifikationer
        </MudSwitch>
        <MudText Typo="Typo.caption" Class="ml-9">
            @if (IsIOS)
            {
                <MudAlert Severity="Severity.Warning" Dense="true" Class="my-2">
                    ⚠️ Push-notifikationer har begränsat stöd på iOS. 
                    Överväg att aktivera email-notifikationer som alternativ.
                </MudAlert>
            }
        </MudText>
        
        <MudSwitch @bind-Checked="preferences.EnableEmail" Color="Color.Primary" Class="mt-4">
            Email-notifikationer
        </MudSwitch>
        
        @if (preferences.EnableEmail)
        {
            <MudSwitch @bind-Checked="preferences.EmailDigestMode" Class="ml-9">
                Daglig sammanfattning (istället för direktmeddelanden)
            </MudSwitch>
        }
    </MudCardContent>
</MudCard>
```

**Checklista för email-notifikationer:**
- [ ] Implementera NotificationService med push/email-fallback
- [ ] Skapa UserNotificationPreferences-modell
- [ ] Lägg till migration för preferences
- [ ] Skapa UI för notifikationsinställningar
- [ ] Implementera email-templates
- [ ] Konfigurera SMTP/SendGrid
- [ ] Testa email-notifikationer
- [ ] Visa iOS-specifik varning i UI

## 📊 Monitoring Dashboard

**Rekommenderade metrics att spåra:**

| Metric | Mål | Alert Threshold |
|--------|-----|-----------------|
| Service Worker registreringsgrad | >95% | <80% |
| PWA installationsgrad | >30% | <10% |
| Offline kö-storlek (genomsnitt) | <3 transaktioner | >10 |
| Sync success rate | >98% | <95% |
| Cache hit rate | >80% | <60% |
| Service Worker fel (per dag) | <10 | >50 |
| Push subscription rate (Android) | >60% | <30% |
| Email notification fallback rate | <40% | >60% |

## 🎯 Definition of Done

Innan PWA anses redo för produktion:

**Testning:**
- [ ] Lighthouse PWA score >90 på testmiljö
- [ ] Android installation testad och fungerar
- [ ] iOS installation testad (med dokumenterade begränsningar)
- [ ] Offline-funktionalitet verifierad på alla plattformar
- [ ] Background sync verifierad på Android/Windows/Linux
- [ ] iOS begränsningar testade och dokumenterade
- [ ] Desktop-installation testad på Windows/Mac/Linux
- [ ] Cache-strategi verifierad

**Produktion:**
- [ ] VAPID-nycklar genererade och konfigurerade
- [ ] Push-notifikationer fungerar på Android/Windows
- [ ] Service Worker-monitoring implementerat
- [ ] Offline kö-tracking implementerat
- [ ] Error logging konfigurerat (Sentry eller custom)
- [ ] Email-notifikationer som iOS-fallback
- [ ] Dashboard för PWA-metrics
- [ ] Alerts konfigurerade för kritiska metrics

**Dokumentation:**
- [ ] PWA_GUIDE.md uppdaterad för produktion
- [ ] PWA_TECHNICAL_IMPLEMENTATION.md kompletterad
- [ ] PWA_IOS_LIMITATIONS.md verifierad
- [ ] Denna fil (PWA_NEXT_STEPS.md) genomförd
- [ ] README.md uppdaterad med PWA-status

## 📚 Relaterad Dokumentation

- [PWA_GUIDE.md](PWA_GUIDE.md) - Användarguide för PWA-funktioner
- [PWA_TECHNICAL_IMPLEMENTATION.md](PWA_TECHNICAL_IMPLEMENTATION.md) - Teknisk implementation
- [PWA_IMPLEMENTATION_SUMMARY.md](PWA_IMPLEMENTATION_SUMMARY.md) - Sammanfattning av implementation
- [PWA_IOS_LIMITATIONS.md](PWA_IOS_LIMITATIONS.md) - iOS-begränsningar och workarounds
- [ROADMAP_2025.md](ROADMAP_2025.md) - Produktroadmap (PWA är Issue #4)

## 🚨 Viktiga Påminnelser

1. **iOS är begränsat** - Background Sync och Push fungerar inte som förväntat. Detta är dokumenterat och OK.
2. **VAPID-nycklar** - Generera EN gång och spara säkert. Byte av nycklar kräver att alla användare re-subscribe.
3. **Service Worker caching** - Vid större ändringar, öka CACHE_VERSION för att tvinga uppdatering.
4. **HTTPS är krav** - PWA fungerar BARA över HTTPS (förutom localhost).
5. **Testa på riktiga enheter** - Emulators kan bete sig annorlunda än verkliga telefoner.
6. **Monitoring är kritiskt** - Implementera metrics INNAN produktion för att upptäcka problem tidigt.

---

**Status:** Redo för implementation  
**Skapad:** 2025-11-05  
**Uppdaterad:** 2025-11-05  
**Nästa steg:** Påbörja testning enligt checklista
