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
    const emailField = page.locator('input[type="email"]').or(page.locator('label:has-text("E-post") + input'));
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
    const loginLink = page.locator('a[href="/Account/Login"]');
    await expect(loginLink).toBeVisible();
  });

  test('should post all registration fields and show one header login control', async ({ page }) => {
    let submittedForm: URLSearchParams | undefined;

    await page.route('**/Account/PerformRegister', async route => {
      submittedForm = new URLSearchParams(route.request().postData() ?? '');
      await route.fulfill({ status: 302, headers: { location: '/Account/Register' } });
    });

    await page.goto('/Account/Register');
    await page.locator('input[autocomplete="given-name"]').fill('Ada');
    await page.locator('input[autocomplete="family-name"]').fill('Lovelace');
    await page.locator('input[autocomplete="username"]').fill('ada@example.com');
    await page.locator('input[type="password"]').nth(0).fill('Passw0rd!');
    await page.locator('input[type="password"]').nth(1).fill('Passw0rd!');
    await page.getByRole('button', { name: 'Registrera' }).click();

    expect(submittedForm?.get('FirstName')).toBe('Ada');
    expect(submittedForm?.get('LastName')).toBe('Lovelace');
    expect(submittedForm?.get('Email')).toBe('ada@example.com');
    expect(submittedForm?.get('Password')).toBe('Passw0rd!');
    expect(submittedForm?.get('ConfirmPassword')).toBe('Passw0rd!');
    await expect(page.locator('.mud-appbar a[href="/Account/Login"]')).toHaveCount(1);
  });
});
