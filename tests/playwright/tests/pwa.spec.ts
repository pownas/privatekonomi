import { test, expect } from '@playwright/test';

test.describe('PWA Functionality', () => {
  test('should have a valid manifest.json', async ({ page, baseURL }) => {
    // Navigate to the manifest
    const response = await page.goto(`${baseURL}/manifest.json`);
    
    // Check response is successful
    expect(response?.status()).toBe(200);
    
    // Get the manifest content
    const manifest = await response?.json();
    
    // Verify required manifest fields
    expect(manifest).toHaveProperty('name');
    expect(manifest).toHaveProperty('short_name');
    expect(manifest).toHaveProperty('start_url');
    expect(manifest).toHaveProperty('display');
    expect(manifest).toHaveProperty('icons');
    
    // Verify manifest values
    expect(manifest.name).toContain('Privatekonomi');
    expect(manifest.display).toBe('standalone');
    expect(manifest.icons.length).toBeGreaterThan(0);
    
    // Verify icons
    const icons = manifest.icons;
    const icon192 = icons.find((icon: any) => icon.sizes === '192x192');
    const icon512 = icons.find((icon: any) => icon.sizes === '512x512');
    
    expect(icon192).toBeDefined();
    expect(icon512).toBeDefined();
  });

  test('should have accessible service worker file', async ({ page, baseURL }) => {
    // Navigate to the service worker
    const response = await page.goto(`${baseURL}/service-worker.js`);
    
    // Check response is successful
    expect(response?.status()).toBe(200);
    
    // Verify content type
    const contentType = response?.headers()['content-type'];
    expect(contentType).toContain('javascript');
    
    // Verify service worker content
    const content = await response?.text();
    expect(content).toContain('Service Worker');
    expect(content).toContain('install');
    expect(content).toContain('fetch');
    expect(content).toContain('activate');
  });

  test('should have accessible PWA icons', async ({ page, baseURL }) => {
    // Test 192x192 icon
    const icon192Response = await page.goto(`${baseURL}/icon-192x192.png`);
    expect(icon192Response?.status()).toBe(200);
    
    // Test 512x512 icon
    const icon512Response = await page.goto(`${baseURL}/icon-512x512.png`);
    expect(icon512Response?.status()).toBe(200);
    
    // Test apple touch icon
    const appleTouchResponse = await page.goto(`${baseURL}/apple-touch-icon.png`);
    expect(appleTouchResponse?.status()).toBe(200);
  });

  test('should have offline.html page', async ({ page, baseURL }) => {
    const response = await page.goto(`${baseURL}/offline.html`);
    
    expect(response?.status()).toBe(200);
    
    const content = await response?.text();
    expect(content).toContain('offline');
    expect(content).toContain('Privatekonomi');
  });

  test('should register service worker on page load', async ({ page }) => {
    await page.goto('/');
    
    // Wait for the page to load
    await page.waitForLoadState('networkidle');
    
    // Check if service worker is registered
    const swRegistered = await page.evaluate(async () => {
      if ('serviceWorker' in navigator) {
        const registration = await navigator.serviceWorker.getRegistration();
        return registration !== undefined;
      }
      return false;
    });
    
    expect(swRegistered).toBe(true);
  });

  test('should have pwaManager available', async ({ page }) => {
    await page.goto('/');
    
    // Wait for the page to load
    await page.waitForLoadState('networkidle');
    
    // Check if pwaManager is available
    const hasPwaManager = await page.evaluate(() => {
      return typeof window.pwaManager !== 'undefined';
    });
    
    expect(hasPwaManager).toBe(true);
    
    // Check pwaManager methods
    const hasMethods = await page.evaluate(() => {
      return (
        typeof window.pwaManager.init === 'function' &&
        typeof window.pwaManager.register === 'function' &&
        typeof window.pwaManager.canInstall === 'function' &&
        typeof window.pwaManager.isRunningAsPWA === 'function'
      );
    });
    
    expect(hasMethods).toBe(true);
  });

  test('should not show the install prompt when installation becomes available', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    await page.evaluate(() => {
      window.dispatchEvent(new Event('beforeinstallprompt', { cancelable: true }));
    });

    await page.waitForTimeout(3500);
    await expect(page.locator('.install-prompt')).toHaveCount(0);
  });

  test('should have manifest link in HTML', async ({ page }) => {
    await page.goto('/');
    
    // Check for manifest link
    const manifestLink = await page.locator('link[rel="manifest"]').getAttribute('href');
    expect(manifestLink).toBe('manifest.json');
  });

  test('should have apple-touch-icon link in HTML', async ({ page }) => {
    await page.goto('/');
    
    // Check for apple touch icon link
    const appleTouchIconLink = await page.locator('link[rel="apple-touch-icon"]').getAttribute('href');
    expect(appleTouchIconLink).toBe('apple-touch-icon.png');
  });

  test('should have theme-color meta tag', async ({ page }) => {
    await page.goto('/');
    
    // Check for theme color meta tag
    const themeColor = await page.locator('meta[name="theme-color"]').getAttribute('content');
    expect(themeColor).toBeDefined();
    expect(themeColor).toMatch(/^#[0-9A-Fa-f]{6}$/); // Valid hex color
  });

  test('should have viewport meta tag', async ({ page }) => {
    await page.goto('/');
    
    // Check for viewport meta tag
    const viewport = await page.locator('meta[name="viewport"]').getAttribute('content');
    expect(viewport).toBeDefined();
    expect(viewport).toContain('width=device-width');
  });
});
