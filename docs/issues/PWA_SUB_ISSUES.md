# PWA Implementation Sub-Issues

> Historisk test-/arbetsplan. Routes, testmiljöer och issue-statusar behöver
> verifieras mot aktuell kod och issue tracker innan de används.

Based on `docs/PWA_NEXT_STEPS.md`, here are the recommended sub-issues to implement PWA testing and production features.

---

## Testing Sub-Issues

### Issue 1: PWA Deployment to Test Environment

**Title:** Deploy PWA to Test Environment and Verify Basic Functionality

**Labels:** `pwa`, `deployment`, `testing`, `infrastructure`

**Description:**

Deploy the PWA-enabled Privatekonomi application to a test environment and verify basic functionality.

**Tasks:**
- [ ] Set up HTTPS-configured test server
- [ ] Configure HTTP to HTTPS redirect
- [ ] Deploy application to test environment (Azure/other)
- [ ] Verify application starts without errors
- [ ] Verify HTTPS is working correctly
- [ ] Verify Service Worker loads (check in DevTools → Application → Service Workers)
- [ ] Verify manifest.json is accessible at `/manifest.json`
- [ ] Document test environment URL and access details

**Acceptance Criteria:**
- Application is deployed and accessible over HTTPS
- Service Worker is registered successfully
- Manifest.json returns 200 OK
- No console errors on page load

**Reference:** See `docs/PWA_NEXT_STEPS.md` section "1. Deploy till testmiljö"

---

### Issue 2: Run Lighthouse PWA Audit

**Title:** Perform Lighthouse PWA Audit and Achieve Score > 90

**Labels:** `pwa`, `testing`, `lighthouse`, `performance`

**Description:**

Run comprehensive Lighthouse audits on the deployed PWA and ensure PWA score > 90.

**Tasks:**
- [ ] Install Lighthouse CLI (`npm install -g lighthouse`)
- [ ] Run desktop audit: `lighthouse https://test.privatekonomi.se --preset=desktop --view`
- [ ] Run mobile audit: `lighthouse https://test.privatekonomi.se --preset=mobile --view`
- [ ] Verify PWA score > 90
- [ ] Verify all PWA requirements are met:
  - [ ] Service Worker registered
  - [ ] Offline response (200 status)
  - [ ] Valid manifest.json
  - [ ] Viewport configured
  - [ ] Icons (192x192, 512x512)
  - [ ] Theme-color meta tag
  - [ ] Apple touch icon
- [ ] Document any issues found and fixes applied
- [ ] Save final Lighthouse reports

**Acceptance Criteria:**
- PWA score > 90 on both desktop and mobile
- All critical PWA requirements are passing
- Performance score > 80
- Accessibility score > 90

**Reference:** See `docs/PWA_NEXT_STEPS.md` section "2. Lighthouse-audit"

---

### Issue 3: Test PWA Installation on Android

**Title:** Test and Verify PWA Installation on Android Devices

**Labels:** `pwa`, `testing`, `android`, `mobile`

**Description:**

Test complete PWA functionality on Android devices including installation, offline mode, background sync, and push notifications.

**Tasks:**

**Installation:**
- [ ] Test installation via Chrome on Android 10+
- [ ] Verify install banner appears
- [ ] Complete installation
- [ ] Verify app icon on home screen
- [ ] Verify standalone mode (no address bar)

**Offline Functionality:**
- [ ] Navigate app and load data while online
- [ ] Enable airplane mode
- [ ] Verify cached data is accessible
- [ ] Create new transaction offline
- [ ] Verify offline banner shows pending count
- [ ] Disable airplane mode
- [ ] Verify automatic synchronization

**Background Sync:**
- [ ] Create transaction while offline
- [ ] Close app completely
- [ ] Restore internet connection
- [ ] Wait 1-2 minutes
- [ ] Reopen app
- [ ] Verify transaction synced in background

**Push Notifications:**
- [ ] Grant notification permission
- [ ] Trigger test notification
- [ ] Verify notification displays
- [ ] Click notification
- [ ] Verify app opens to correct page

**Acceptance Criteria:**
- PWA installs successfully on Android
- Offline functionality works as expected
- Background sync works (Android-specific feature)
- Push notifications work correctly
- All test results documented

**Reference:** See `docs/PWA_NEXT_STEPS.md` section "3. Installation och Testning - Android"

---

### Issue 4: Test PWA Installation on iOS

**Title:** Test and Document PWA Installation on iOS Devices

**Labels:** `pwa`, `testing`, `ios`, `mobile`, `documentation`

**Description:**

Test PWA functionality on iOS devices and document known limitations (background sync, push notifications).

**Tasks:**

**Installation:**
- [ ] Test installation via Safari on iOS 15+
- [ ] Use "Add to Home Screen" feature
- [ ] Verify app icon on home screen
- [ ] Verify standalone mode

**Offline Functionality:**
- [ ] Navigate app while online
- [ ] Enable airplane mode
- [ ] Verify cached data accessible
- [ ] Create transaction offline
- [ ] Verify offline banner displays
- [ ] Disable airplane mode with app open
- [ ] Verify synchronization occurs

**Background Sync (Expected to NOT work):**
- [ ] Create transaction offline
- [ ] Close app
- [ ] Restore internet
- [ ] Wait 5 minutes
- [ ] Reopen app
- [ ] Document that background sync does NOT work
- [ ] Verify transaction syncs when app is opened

**Push Notifications (Limited support):**
- [ ] Test on iOS 16.4+ if available
- [ ] Attempt to grant notification permission
- [ ] Document whether push works or not
- [ ] Note iOS version tested

**Acceptance Criteria:**
- PWA installs on iOS successfully
- Offline functionality works
- Background sync limitation documented
- Push notification limitation documented
- Manual sync works when app is open

**Reference:** See `docs/PWA_NEXT_STEPS.md` section "4. Installation och Testning - iOS"

---

### Issue 5: Test PWA Installation on Desktop

**Title:** Test PWA Installation on Desktop Platforms (Windows/Mac/Linux)

**Labels:** `pwa`, `testing`, `desktop`

**Description:**

Test PWA installation and functionality on desktop platforms.

**Tasks:**

**Windows:**
- [ ] Install via Chrome/Edge on Windows 10/11
- [ ] Verify installation process
- [ ] Test offline functionality
- [ ] Test background sync
- [ ] Verify app appears in Start Menu

**macOS:**
- [ ] Install via Chrome/Edge on macOS
- [ ] Verify installation process
- [ ] Test offline functionality
- [ ] Test background sync (may be limited in Safari)
- [ ] Verify app appears in Launchpad

**Linux:**
- [ ] Install via Chrome/Firefox on Linux
- [ ] Verify installation process
- [ ] Test offline functionality
- [ ] Test background sync

**Acceptance Criteria:**
- PWA installs on all major desktop platforms
- Offline functionality works on all platforms
- Background sync works on Windows and Linux
- All test results documented

**Reference:** See `docs/PWA_NEXT_STEPS.md` section "5. Installation och Testning - Desktop"

---

### Issue 6: Verify PWA Cache Strategy

**Title:** Verify and Test PWA Cache Strategy Implementation

**Labels:** `pwa`, `testing`, `cache`, `service-worker`

**Description:**

Verify that the cache strategy is working correctly for both static and dynamic resources.

**Tasks:**
- [ ] Open Chrome DevTools → Network
- [ ] Load page first time (verify network requests)
- [ ] Reload page (verify resources from Service Worker/cache)
- [ ] Verify API calls go to network first
- [ ] Enable offline mode in DevTools
- [ ] Reload page
- [ ] Verify page loads from cache
- [ ] Check Cache Storage in DevTools → Application
- [ ] Verify `privatekonomi-static-v2` cache exists
- [ ] Verify `privatekonomi-dynamic-v2` cache exists
- [ ] Verify static assets are cached (CSS, JS, icons)
- [ ] Check IndexedDB → PrivatekonomyOfflineDB
- [ ] Verify database schema is correct

**Acceptance Criteria:**
- Static resources load from cache on repeat visits
- API calls use network-first strategy
- Offline page loads correctly when offline
- Cache storage contains expected resources
- IndexedDB is created with correct schema

**Reference:** See `docs/PWA_NEXT_STEPS.md` section "6. Verifiering av Cache-strategi"

---

## Production Implementation Sub-Issues

### Issue 7: Implement Server-Side VAPID Key Generation

**Title:** Implement VAPID Key Generation and Push Notification Service

**Labels:** `pwa`, `backend`, `push-notifications`, `feature`

**Description:**

Implement server-side VAPID key generation and push notification infrastructure for PWA.

**Tasks:**

**Setup:**
- [ ] Install WebPush NuGet package
- [ ] Create PushNotificationService class
- [ ] Create PushSubscription data model
- [ ] Add DbSet to ApplicationDbContext
- [ ] Create migration for PushSubscriptions table

**VAPID Keys:**
- [ ] Implement VAPID key generation method
- [ ] Generate production VAPID keys (run once)
- [ ] Store keys in appsettings.Production.json
- [ ] Document key management process

**API Implementation:**
- [ ] Create PushController with endpoints:
  - [ ] GET `/api/push/vapid-public-key`
  - [ ] POST `/api/push/subscribe`
  - [ ] POST `/api/push/unsubscribe`
- [ ] Implement SendNotificationAsync method
- [ ] Handle invalid subscriptions (410 Gone)

**Client-Side:**
- [ ] Update pwaManager with subscribeToPush function
- [ ] Implement urlBase64ToUint8Array helper
- [ ] Test subscription flow

**Testing:**
- [ ] Test VAPID key generation
- [ ] Test push subscription on Android/Windows
- [ ] Test notification sending
- [ ] Test unsubscribe flow

**Acceptance Criteria:**
- VAPID keys generated and stored securely
- PushNotificationService implemented with full functionality
- API endpoints working correctly
- Push notifications work on Android/Windows
- Code follows existing patterns in Privatekonomi.Core and Privatekonomi.Api

**Reference:** See `docs/PWA_NEXT_STEPS.md` section "1. Server-side VAPID-nyckelgenerering"

**Code Example:** See documentation for complete C# implementation

---

### Issue 8: Implement Service Worker Registration Monitoring

**Title:** Implement Monitoring for Service Worker Registration Rate

**Labels:** `pwa`, `monitoring`, `telemetry`, `feature`

**Description:**

Implement telemetry to track Service Worker registration success/failure rates.

**Tasks:**

**Backend:**
- [ ] Create TelemetryController in Privatekonomi.Api
- [ ] Implement POST `/api/telemetry/pwa/sw-registered` endpoint
- [ ] Implement POST `/api/telemetry/pwa/sw-failed` endpoint
- [ ] Implement POST `/api/telemetry/pwa/installed` endpoint
- [ ] Integrate with Application Insights

**Client-Side:**
- [ ] Add trackServiceWorkerRegistration to pwaManager
- [ ] Add trackInstallation to pwaManager
- [ ] Implement getPlatform helper
- [ ] Implement getBrowser helper
- [ ] Call tracking on SW registration success/failure

**Dashboard:**
- [ ] Create Application Insights queries (Kusto)
- [ ] Set up dashboard for PWA metrics
- [ ] Configure alert for low registration rate (<80%)

**Acceptance Criteria:**
- TelemetryController implemented
- Service Worker registration tracked
- Installation events tracked
- Dashboard queries working
- Alert configured

**Reference:** See `docs/PWA_NEXT_STEPS.md` section "2. Monitering av Service Worker-registreringsgrad"

---

### Issue 9: Implement Offline Transaction Queue Tracking

**Title:** Implement Tracking for Offline Transaction Queue

**Labels:** `pwa`, `monitoring`, `offline`, `feature`

**Description:**

Implement telemetry to track offline transaction queue size and sync success rate.

**Tasks:**

**Backend:**
- [ ] Add POST `/api/telemetry/pwa/queue-status` endpoint
- [ ] Add POST `/api/telemetry/pwa/sync-completed` endpoint
- [ ] Create QueueTelemetry and SyncTelemetry models
- [ ] Track metrics in Application Insights

**Service Worker:**
- [ ] Add reportSyncCompleted function
- [ ] Track sync duration and count
- [ ] Report success/failure status
- [ ] Handle errors gracefully

**Client-Side:**
- [ ] Add reportQueueSize to pwaManager
- [ ] Call periodically (e.g., on online event)
- [ ] Track queue changes

**Dashboard:**
- [ ] Create queries for average queue size
- [ ] Create queries for sync success rate
- [ ] Set up alert for large queues (>10 transactions)
- [ ] Set up alert for low sync rate (<95%)

**Acceptance Criteria:**
- Queue size tracking implemented
- Sync completion tracking implemented
- Dashboard queries working
- Alerts configured

**Reference:** See `docs/PWA_NEXT_STEPS.md` section "3. Spårning av köade offline-transaktioner"

---

### Issue 10: Implement Error Logging for Service Worker

**Title:** Set Up Error Logging and Tracking for Service Worker

**Labels:** `pwa`, `monitoring`, `error-handling`, `feature`

**Description:**

Implement centralized error logging for Service Worker to track and diagnose issues.

**Tasks:**

**Choose Solution:**
- [ ] Decide: Sentry or Custom Backend Logging
- [ ] Document decision and reasoning

**Option A: Sentry Integration**
- [ ] Sign up for Sentry account
- [ ] Install Sentry in Service Worker
- [ ] Configure DSN and environment
- [ ] Add global error handlers
- [ ] Test error reporting

**Option B: Custom Backend Logging**
- [ ] Create POST `/api/telemetry/pwa/error` endpoint
- [ ] Implement logError function in Service Worker
- [ ] Add error handlers for fetch events
- [ ] Log errors with context

**Both Options:**
- [ ] Implement error tracking for:
  - [ ] Service Worker installation errors
  - [ ] Fetch errors
  - [ ] Cache errors
  - [ ] Sync errors
- [ ] Configure alerts for critical errors
- [ ] Test error logging

**Acceptance Criteria:**
- Error logging solution implemented
- All error types tracked
- Alerts configured for critical errors
- Error logs accessible and searchable

**Reference:** See `docs/PWA_NEXT_STEPS.md` section "4. Fel-loggning och Error Tracking"

---

### Issue 11: Implement Email Notifications as iOS Fallback

**Title:** Implement Email Notification Fallback for iOS Users

**Labels:** `pwa`, `notifications`, `ios`, `email`, `feature`

**Description:**

Implement email notification system as fallback for iOS users who cannot receive push notifications.

**Tasks:**

**NotificationService:**
- [ ] Create unified NotificationService class
- [ ] Implement SendNotificationAsync with push/email fallback
- [ ] Check for active push subscription
- [ ] Fall back to email if push fails/unavailable
- [ ] Implement SendEmailNotificationAsync

**Email Templates:**
- [ ] Design HTML email template
- [ ] Include title, body, and action button
- [ ] Match Privatekonomi branding
- [ ] Test in multiple email clients

**User Preferences:**
- [ ] Create UserNotificationPreferences model
- [ ] Add properties for:
  - [ ] EnablePush
  - [ ] EnableEmail
  - [ ] EnableSms
  - [ ] Per-notification-type preferences
  - [ ] EmailDigestMode
  - [ ] QuietHoursStart/End
- [ ] Create migration for preferences table

**UI:**
- [ ] Create NotificationSettings.razor component
- [ ] Add toggle switches for each channel
- [ ] Show iOS warning about push limitations
- [ ] Add digest mode option for email

**Backend:**
- [ ] Configure SMTP/SendGrid settings
- [ ] Test email sending
- [ ] Handle email failures gracefully

**Testing:**
- [ ] Test notification fallback on iOS
- [ ] Test email template rendering
- [ ] Test quiet hours functionality
- [ ] Test digest mode

**Acceptance Criteria:**
- NotificationService with fallback implemented
- Email templates created and tested
- User preferences model created
- Settings UI implemented
- iOS users receive email notifications
- All notification types supported

**Reference:** See `docs/PWA_NEXT_STEPS.md` section "5. iOS-specifika överväganden - Email-notifikationer"

---

## Meta Issue (Optional)

### Issue 12: PWA Monitoring Dashboard

**Title:** Create Production PWA Monitoring Dashboard

**Labels:** `pwa`, `monitoring`, `dashboard`, `operations`

**Description:**

Create comprehensive monitoring dashboard for PWA metrics in production.

**Tasks:**
- [ ] Set up Application Insights workspace
- [ ] Create dashboard with all PWA metrics:
  - [ ] Service Worker registration rate (target >95%)
  - [ ] PWA installation rate (target >30%)
  - [ ] Offline queue size (target <3 avg)
  - [ ] Sync success rate (target >98%)
  - [ ] Cache hit rate (target >80%)
  - [ ] Service Worker errors (target <10/day)
  - [ ] Push subscription rate (target >60%)
  - [ ] Email fallback rate (target <40%)
- [ ] Configure alerts for all thresholds
- [ ] Document dashboard access and usage
- [ ] Train team on monitoring

**Acceptance Criteria:**
- Dashboard created and accessible
- All metrics visible and updating
- Alerts configured and tested
- Team trained on dashboard usage

**Reference:** See `docs/PWA_NEXT_STEPS.md` section "Monitoring Dashboard"

---

## Summary

**Testing Issues (1-6):**
- Issue 1: Deploy to Test Environment
- Issue 2: Lighthouse Audit
- Issue 3: Android Testing
- Issue 4: iOS Testing  
- Issue 5: Desktop Testing
- Issue 6: Cache Strategy Verification

**Production Issues (7-11):**
- Issue 7: VAPID Keys & Push Notifications
- Issue 8: Service Worker Monitoring
- Issue 9: Offline Queue Tracking
- Issue 10: Error Logging
- Issue 11: Email Notifications (iOS)

**Optional Issue (12):**
- Issue 12: Monitoring Dashboard

**Total: 12 sub-issues**

**Recommendation:** Create issues 1-6 for immediate testing, then 7-11 for production features. Issue 12 can be created after issues 8-10 are complete.
