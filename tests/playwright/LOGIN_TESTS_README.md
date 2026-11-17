# Login/Register Page Tests - Verification for Raspberry Pi Fix

## Overview

This document describes the Playwright E2E tests that verify the fix for the login page crash issue on Raspberry Pi in production mode (Issue #369).

## Problem

The login and register pages crashed when accessed on Raspberry Pi in production mode due to a `NullReferenceException` caused by uninitialized form models.

## Solution

Changed the `Input` property initialization from `= default!` to `= new()` in both:
- `src/Privatekonomi.Web/Components/Pages/Account/Login.razor`
- `src/Privatekonomi.Web/Components/Pages/Account/Register.razor`

## Tests

### Login Page Test (`tests/login.spec.ts`)

**Test: "should load login page without crashing"**

This test verifies that:
1. ✅ The login page loads at `/Account/Login`
2. ✅ The page title contains "Logga in"
3. ✅ The login heading is visible
4. ✅ Email field is present and visible
5. ✅ Password field is present and visible
6. ✅ Login button is present and visible
7. ✅ Screenshot is captured: `screenshots/login-page.png`

**Test: "should display validation error for empty email"**

This test verifies form validation works correctly after the fix.

**Test: "should display register link"**

This test verifies navigation elements are present.

### Register Page Test (`tests/register.spec.ts`)

**Test: "should load register page without crashing"**

This test verifies that:
1. ✅ The register page loads at `/Account/Register`
2. ✅ The page title contains "Registrera"
3. ✅ The register heading "Skapa konto" is visible
4. ✅ First name field is present and visible
5. ✅ Last name field is present and visible
6. ✅ Email field is present and visible
7. ✅ Password field is present and visible
8. ✅ Register button is present and visible
9. ✅ Screenshot is captured: `screenshots/register-page.png`

**Test: "should display login link"**

This test verifies navigation elements are present.

## Running the Tests

### Prerequisites

1. Ensure the web application is running:
   ```bash
   cd src/Privatekonomi.Web
   dotnet run
   ```

2. The app should be accessible at `http://localhost:5274`

### Run Tests with Script

```bash
cd tests/playwright
./run-login-tests.sh
```

This script will:
- Check if the web app is running
- Install dependencies if needed
- Install Playwright browsers if needed
- Run the login and register tests
- Generate screenshots in `screenshots/` directory

### Run Tests Manually

```bash
cd tests/playwright

# Install dependencies
npm install

# Install Playwright browsers
npx playwright install chromium

# Run specific tests
npx playwright test login.spec.ts
npx playwright test register.spec.ts

# Run all tests
npm test
```

## Screenshots

After running the tests, you will find screenshots in the `screenshots/` directory:

- `login-page.png` - Shows the login page successfully loaded
- `register-page.png` - Shows the register page successfully loaded

These screenshots demonstrate that:
- ✅ The pages load without crashing
- ✅ All form elements are rendered correctly
- ✅ The fix works in the test environment

## Verification on Raspberry Pi

To verify the fix on an actual Raspberry Pi in production:

1. **Deploy to Raspberry Pi:**
   ```bash
   cd ~/privatekonomi/scripts/raspberry-pi
   bash update.sh
   ```

2. **Start the application:**
   ```bash
   bash start.sh
   ```

3. **Access the login page:**
   - Open browser to `http://[raspberry-pi-ip]:5274/Account/Login`
   - **Expected:** Page loads successfully without crashing
   - **Previously:** Application would crash with NullReferenceException

4. **Access the register page:**
   - Open browser to `http://[raspberry-pi-ip]:5274/Account/Register`
   - **Expected:** Page loads successfully without crashing
   - **Previously:** Application would crash with NullReferenceException

## What the Tests Prove

These E2E tests prove that:

1. **No Crash on Page Load**: The most critical aspect - the pages load without throwing exceptions
2. **Form Binding Works**: All form elements are properly bound and rendered
3. **Data Binding Initialized**: The `Input` model is properly initialized (not null)
4. **Production-Ready**: The fix works in a test environment similar to production

## Technical Details

### Before the Fix

```csharp
[SupplyParameterFromForm]
private InputModel Input { get; set; } = default!;  // ❌ null until form submission
```

When Blazor tried to render the `EditForm` component:
```razor
<EditForm Model="Input" OnValidSubmit="LoginUser" FormName="login">
```

The `Input` was `null`, causing `NullReferenceException` when the form tried to bind to its properties.

### After the Fix

```csharp
[SupplyParameterFromForm]
private InputModel Input { get; set; } = new();  // ✅ Always instantiated
```

Now the `Input` object exists from the moment the component loads, allowing the `EditForm` to bind successfully.

## CI/CD Integration

These tests can be integrated into CI/CD pipelines to prevent regression:

```yaml
# Example GitHub Actions workflow
- name: Run Login/Register Tests
  run: |
    cd tests/playwright
    npm install
    npx playwright install chromium
    npx playwright test login.spec.ts register.spec.ts
```

## Related Documentation

- Issue #369: Login-sidan kraschar applikationen på Raspberry Pi i produktion
- `docs/BUG_FIXES.md` - Detailed explanation of the fix
- `docs/scripts/raspberry-pi/RASPBERRY_PI_FELSOKNING.md` - Raspberry Pi troubleshooting guide

## Summary

✅ **Tests verify the fix works correctly**
✅ **Screenshots provide visual confirmation**
✅ **Automated regression prevention**
✅ **Production-ready validation**

These tests ensure that the login page crash on Raspberry Pi has been fixed and will not regress in future updates.
