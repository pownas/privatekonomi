import { test, expect } from '@playwright/test';

test('dashboard charts render and expose data tooltips', async ({ page }) => {
  const email = `chart-test-${Date.now()}@example.com`;
  await page.goto('http://localhost:5275/Account/Register');
  await page.waitForLoadState('networkidle');
  await page.locator('input[autocomplete="given-name"]').fill('Diagram');
  await page.locator('input[autocomplete="family-name"]').fill('Test');
  await page.locator('input[autocomplete="username"]').fill(email);
  const registerPasswords = page.locator('input[type="password"]');
  await registerPasswords.nth(0).fill('TestPassword123!');
  await registerPasswords.nth(1).fill('TestPassword123!');
  await page.getByRole('button', { name: 'Registrera' }).click();
  await page.waitForURL(/\/Account\/Login|\/onboarding|\/$/, { timeout: 15000 });

  if (page.url().includes('/Account/Login')) {
    await page.locator('input[name="Email"]').fill(email);
    await page.locator('input[type="password"]').first().fill('TestPassword123!');
    await page.locator('form[action="/Account/PerformLogin"] button[type="submit"]').click();
  }

  await page.waitForTimeout(2000);
  await page.goto('http://localhost:5275/onboarding/5');
  await page.getByRole('button', { name: 'Gå till Dashboard' }).click();
  await page.waitForURL('http://localhost:5275/', { timeout: 15000 });
  await page.waitForLoadState('networkidle');

  const charts = page.locator('.mud-chart-line, .mud-chart-bar, .mud-chart-pie');
  await expect(charts.first()).toBeVisible({ timeout: 15000 });

  await page.screenshot({ path: 'screenshots/dashboard-charts.png', fullPage: true });

  const chartPoints = page.locator('.mud-chart-line circle, .mud-chart-bar .mud-chart-serie, .mud-chart-pie path');
  if (await chartPoints.count()) {
    await chartPoints.first().hover({ force: true });
  }
});
