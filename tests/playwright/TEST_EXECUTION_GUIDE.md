# Test Execution Guide with Visual Documentation

## Test Screenshots - Expected Results

This document shows what the Playwright tests verify visually.

### Login Page Test Results

**Test: "should load login page without crashing"**

When the test runs, it will:

1. Navigate to `/Account/Login`
2. Verify the page loads successfully (no crash!)
3. Verify all elements are present:
   - Page title: "Logga in"
   - Email input field
   - Password input field
   - "Kom ihåg mig" checkbox
   - "Logga in" button
   - Link to register page
4. Capture screenshot: `screenshots/login-page.png`

**Expected Visual Elements:**
```
┌─────────────────────────────────────────────────┐
│                                                 │
│              Logga in                           │
│                                                 │
│  ┌───────────────────────────────────────────┐ │
│  │ E-post                                    │ │
│  └───────────────────────────────────────────┘ │
│                                                 │
│  ┌───────────────────────────────────────────┐ │
│  │ Lösenord                      [••••••••]  │ │
│  └───────────────────────────────────────────┘ │
│                                                 │
│  ☐ Kom ihåg mig                                │
│                                                 │
│  ┌───────────────────────────────────────────┐ │
│  │         Logga in                          │ │
│  └───────────────────────────────────────────┘ │
│                                                 │
│  Har du inget konto? Registrera dig            │
│                                                 │
└─────────────────────────────────────────────────┘
```

### Register Page Test Results

**Test: "should load register page without crashing"**

When the test runs, it will:

1. Navigate to `/Account/Register`
2. Verify the page loads successfully (no crash!)
3. Verify all elements are present:
   - Page title: "Registrera"
   - Heading: "Skapa konto"
   - First name input field
   - Last name input field
   - Email input field
   - Password input field
   - Confirm password input field
   - "Registrera" button
   - Link to login page
4. Capture screenshot: `screenshots/register-page.png`

**Expected Visual Elements:**
```
┌─────────────────────────────────────────────────┐
│                                                 │
│            Skapa konto                          │
│                                                 │
│  ┌───────────────────────────────────────────┐ │
│  │ Förnamn                                   │ │
│  └───────────────────────────────────────────┘ │
│                                                 │
│  ┌───────────────────────────────────────────┐ │
│  │ Efternamn                                 │ │
│  └───────────────────────────────────────────┘ │
│                                                 │
│  ┌───────────────────────────────────────────┐ │
│  │ E-post                                    │ │
│  └───────────────────────────────────────────┘ │
│                                                 │
│  ┌───────────────────────────────────────────┐ │
│  │ Lösenord                      [••••••••]  │ │
│  └───────────────────────────────────────────┘ │
│                                                 │
│  ┌───────────────────────────────────────────┐ │
│  │ Bekräfta lösenord             [••••••••]  │ │
│  └───────────────────────────────────────────┘ │
│                                                 │
│  ┌───────────────────────────────────────────┐ │
│  │         Registrera                        │ │
│  └───────────────────────────────────────────┘ │
│                                                 │
│  Har du redan ett konto? Logga in              │
│                                                 │
└─────────────────────────────────────────────────┘
```

## Running Tests to Generate Real Screenshots

To generate actual screenshots from the running application:

### Step 1: Start the Application

```bash
cd src/Privatekonomi.Web
dotnet run
```

Wait for the application to start and be available at `http://localhost:5274`

### Step 2: Run the Tests

In a new terminal:

```bash
cd tests/playwright
./run-login-tests.sh
```

Or manually:

```bash
cd tests/playwright
npm install
npx playwright install chromium
npx playwright test login.spec.ts register.spec.ts
```

### Step 3: View Screenshots

Screenshots will be saved to:
- `tests/playwright/screenshots/login-page.png`
- `tests/playwright/screenshots/register-page.png`

## Test Output Example

When the tests run successfully, you should see output like:

```
Running 4 tests using 1 worker

  ✓  [chromium] › login.spec.ts:4:3 › Login Page › should load login page without crashing (2.1s)
  ✓  [chromium] › login.spec.ts:28:3 › Login Page › should display validation error for empty email (1.5s)
  ✓  [chromium] › login.spec.ts:49:3 › Login Page › should display register link (1.2s)
  ✓  [chromium] › register.spec.ts:4:3 › Register Page › should load register page without crashing (2.0s)

  4 passed (6.8s)
```

## What Success Means

✅ **All 4 tests pass** - The login and register pages load without crashing
✅ **Screenshots generated** - Visual proof that the pages render correctly
✅ **No NullReferenceException** - The fix has resolved the Raspberry Pi crash issue

## Before vs After the Fix

### Before the Fix ❌

```
User navigates to /Account/Login on Raspberry Pi
   ↓
Application tries to render the page
   ↓
EditForm tries to bind to Input property
   ↓
Input is null (= default!)
   ↓
NullReferenceException thrown
   ↓
💥 APPLICATION CRASHES 💥
```

### After the Fix ✅

```
User navigates to /Account/Login on Raspberry Pi
   ↓
Application tries to render the page
   ↓
EditForm tries to bind to Input property
   ↓
Input exists (= new())
   ↓
Form binds successfully
   ↓
✅ PAGE LOADS CORRECTLY ✅
```

## Verification Checklist

After running the tests, verify:

- [ ] All 4 tests pass
- [ ] `screenshots/login-page.png` exists and shows the login form
- [ ] `screenshots/register-page.png` exists and shows the register form
- [ ] No errors in the test output
- [ ] Login page displays: Email field, Password field, Login button
- [ ] Register page displays: Name fields, Email field, Password fields, Register button

## Deploying to Raspberry Pi

After verifying the tests pass locally, deploy to Raspberry Pi:

```bash
# On your development machine, push changes
git push origin copilot/fix-login-crash-on-raspberry-pi

# On Raspberry Pi
cd ~/privatekonomi/scripts/raspberry-pi
bash update.sh
bash start.sh
```

Then test manually by navigating to:
- `http://[raspberry-pi-ip]:5274/Account/Login`
- `http://[raspberry-pi-ip]:5274/Account/Register`

Both pages should now load successfully! 🎉
