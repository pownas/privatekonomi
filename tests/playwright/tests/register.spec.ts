import { test, expect } from '@playwright/test';

test.describe('Register Page', () => {
  test('should load register page without crashing', async ({ page }) => {
    // Navigate to the register page
    await page.goto('/Account/Register');

    // Wait for the page to load
    await page.waitForLoadState('networkidle');

    // Verify the page title is correct
    await expect(page).toHaveTitle(/Registrera/);

    // Verify the register form is visible
    const registerHeading = page.locator('text=Skapa konto').first();
    await expect(registerHeading).toBeVisible();

    // Verify form fields are present
    const firstNameField = page.locator('label:has-text("Förnamn")');
    const lastNameField = page.locator('label:has-text("Efternamn")');
    const emailField = page.locator('input[name="Email"]');
    const passwordField = page.locator('input[type="password"]').first();
    const submitButton = page.locator('button:has-text("Registrera")');

    await expect(firstNameField).toBeVisible({ timeout: 5000 });
    await expect(lastNameField).toBeVisible();
    await expect(emailField).toBeVisible();
    await expect(passwordField).toBeVisible();
    await expect(submitButton).toBeVisible();

    // Take a screenshot of the register page
    await page.screenshot({ path: 'screenshots/register-page.png', fullPage: true });
  });

  test('should display login link', async ({ page }) => {
    // Navigate to the register page
    await page.goto('/Account/Register');
    await page.waitForLoadState('networkidle');

    // Verify the login link is present
    const loginLink = page.locator('.mud-paper a[href="/Account/Login"]');
    await expect(loginLink).toBeVisible();
  });

  test('should post all registration fields and show one header login control', async ({ page }) => {
    let submittedForm: URLSearchParams | undefined;

    await page.route('**/Account/PerformRegister', async route => {
      submittedForm = new URLSearchParams(route.request().postData() ?? '');
      await route.fulfill({ status: 302, headers: { location: '/Account/Register' } });
    });

    await page.goto('/Account/Register');
    await page.waitForLoadState('networkidle');
    const firstName = page.locator('input[autocomplete="given-name"]');
    const lastName = page.locator('input[autocomplete="family-name"]');
    const email = page.locator('input[autocomplete="username"]');
    const passwords = page.locator('input[type="password"]');
    await firstName.fill('Ada');
    await expect(firstName).toHaveValue('Ada');
    await lastName.fill('Lovelace');
    await expect(lastName).toHaveValue('Lovelace');
    await email.fill('ada@example.com');
    await expect(email).toHaveValue('ada@example.com');
    await passwords.nth(0).fill('Passw0rd!123');
    await expect(passwords.nth(0)).toHaveValue('Passw0rd!123');
    await passwords.nth(1).fill('Passw0rd!123');
    await expect(passwords.nth(1)).toHaveValue('Passw0rd!123');
    const registrationResponse = page.waitForResponse(response => response.url().endsWith('/Account/PerformRegister'));
    await page.getByRole('button', { name: 'Registrera' }).click();
    await registrationResponse;

    expect(submittedForm?.get('FirstName')).toBe('Ada');
    expect(submittedForm?.get('LastName')).toBe('Lovelace');
    expect(submittedForm?.get('Email')).toBe('ada@example.com');
    expect(submittedForm?.get('Password')).toBe('Passw0rd!123');
    expect(submittedForm?.get('ConfirmPassword')).toBe('Passw0rd!123');
    expect(submittedForm?.get('__RequestVerificationToken')).toBeTruthy();
    await expect(page.locator('.mud-appbar a[href="/Account/Login"]')).toHaveCount(1);
  });
});
