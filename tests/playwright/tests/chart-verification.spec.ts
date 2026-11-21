import { test, expect } from '@playwright/test';

test('dashboard charts render and expose data tooltips', async ({ page }) => {
  await page.goto('http://localhost:5275/Account/Login');
  await page.waitForLoadState('networkidle');

  await page.locator('input[name="Email"]').fill('test@example.com');
  await page.locator('input[type="password"]').first().fill('TestPassword123!');
  await page.locator('form[action="/Account/PerformLogin"] button[type="submit"]').click();

  await page.waitForURL(/\/(|dashboard)$/);
  await page.waitForLoadState('networkidle');

  const charts = page.locator('.mud-chart-line, .mud-chart-bar, .mud-chart-pie');
  await expect(charts.first()).toBeVisible({ timeout: 15000 });

  await page.screenshot({ path: 'screenshots/dashboard-charts.png', fullPage: true });

  const chartPaths = page.locator('.mud-chart-line .mud-chart-serie, .mud-chart-bar .mud-chart-serie');
  if (await chartPaths.count()) {
    await chartPaths.first().hover();
    await expect(page.locator('.mud-chart-tooltip')).toBeVisible({ timeout: 5000 });
  }
});
