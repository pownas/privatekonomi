import { test, expect } from '@playwright/test';

test.describe('Dark Mode Screenshots', () => {
  test('applies saved dark mode before interactive rendering', async ({ page }) => {
    await page.addInitScript(() => localStorage.setItem('darkMode', 'true'));
    await page.goto('/', { waitUntil: 'domcontentloaded' });

    await expect(page.locator('html')).toHaveClass(/mud-theme-dark/);
    await expect(page.locator('body')).toHaveCSS('background-color', 'rgb(17, 24, 39)');
  });

  test('capture light mode and dark mode screenshots', async ({ page }) => {
    // Navigate to the application
    await page.goto('/');
    
    // Wait for the page to fully load
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);
    
    // Take screenshot in light mode
    await page.screenshot({ 
      path: 'screenshots/light-mode.png',
      fullPage: false 
    });
    console.log('✓ Light mode screenshot captured');
    
    // Find and click the dark mode toggle button
    const darkModeButton = page.locator('[aria-label*="Mörkt"]');
    await expect(darkModeButton).toBeVisible();
    await darkModeButton.click();
    
    // Wait for theme transition
    await page.waitForTimeout(1000);
    
    // Verify dark mode is active by checking for the dark theme class
    const body = page.locator('body');
    await expect(body).toHaveClass(/mud-theme-dark/);
    
    // Take screenshot in dark mode
    await page.screenshot({ 
      path: 'screenshots/dark-mode.png',
      fullPage: false 
    });
    console.log('✓ Dark mode screenshot captured');
    
    // Also capture a screenshot showing the toggle button
    await page.screenshot({ 
      path: 'screenshots/dark-mode-with-toggle.png',
      fullPage: false 
    });
    console.log('✓ Dark mode with toggle screenshot captured');
    
    // Toggle back to light mode
    const lightModeButton = page.locator('[aria-label*="Ljust"]');
    await expect(lightModeButton).toBeVisible();
    await lightModeButton.click();
    
    // Wait for theme transition
    await page.waitForTimeout(1000);
    
    // Take another light mode screenshot to verify toggle works
    await page.screenshot({ 
      path: 'screenshots/light-mode-toggled-back.png',
      fullPage: false 
    });
    console.log('✓ Light mode (toggled back) screenshot captured');
  });
  
  test('capture dashboard in both modes', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);
    
    // Light mode dashboard
    await page.screenshot({ 
      path: 'screenshots/dashboard-light.png',
      fullPage: true 
    });
    console.log('✓ Dashboard light mode screenshot captured');
    
    // Switch to dark mode
    const darkModeButton = page.locator('[aria-label*="Mörkt"]');
    await darkModeButton.click();
    await page.waitForTimeout(1000);
    
    // Dark mode dashboard
    await page.screenshot({ 
      path: 'screenshots/dashboard-dark.png',
      fullPage: true 
    });
    console.log('✓ Dashboard dark mode screenshot captured');
  });
  
  test('capture focus indicators in dark mode', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    
    // Switch to dark mode
    const darkModeButton = page.locator('[aria-label*="Mörkt"]');
    await darkModeButton.click();
    await page.waitForTimeout(1000);
    
    // Focus on the dark mode toggle button to show focus indicator
    await page.keyboard.press('Tab');
    await page.keyboard.press('Tab');
    
    await page.screenshot({ 
      path: 'screenshots/dark-mode-focus-indicator.png',
      fullPage: false 
    });
    console.log('✓ Dark mode focus indicator screenshot captured');
  });
});
