import { test, expect } from '@playwright/test';
import * as fs from 'fs';
import * as path from 'path';

test.describe('OCR Receipt Scanning Screenshots', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to the application
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);
  });

  test('01 - Receipts page with OCR button', async ({ page }) => {
    // Navigate to receipts page
    await page.goto('/receipts');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);
    
    // Take screenshot showing the receipts page with the new OCR scan button
    await page.screenshot({ 
      path: 'screenshots/ocr-receipt-scanning/01-receipts-page-with-ocr-button.png',
      fullPage: false 
    });
    console.log('✓ Receipts page with OCR button screenshot captured');
  });

  test('02 - OCR scan dialog - initial state', async ({ page }) => {
    // Navigate to receipts page
    await page.goto('/receipts');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);
    
    // Click on "Skanna kvitto (OCR)" button
    const ocrButton = page.getByRole('button', { name: 'Skanna kvitto (OCR)' }).first();
    if (await ocrButton.count() > 0) {
      await ocrButton.click();
      await page.waitForTimeout(1000);
      const dialog = page.locator('.mud-dialog');
      await expect(dialog).toBeVisible();
      
      // Take screenshot of the OCR dialog
      await page.screenshot({ 
        path: 'screenshots/ocr-receipt-scanning/02-ocr-dialog-initial.png',
        fullPage: false 
      });
      console.log('✓ OCR dialog initial state screenshot captured');
      
      // Close dialog
      await page.keyboard.press('Escape');
      await expect(dialog).toBeHidden();
    } else {
      console.log('⚠ OCR scan button not found - skipping');
    }
  });

  test('03 - OCR scan dialog - upload area focus', async ({ page }) => {
    // Navigate to receipts page
    await page.goto('/receipts');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);
    
    // Click on "Skanna kvitto (OCR)" button
    const ocrButton = page.locator('text="Skanna kvitto (OCR)"');
    if (await ocrButton.count() > 0) {
      await ocrButton.click();
      await page.waitForTimeout(1000);
      
      // Hover over the upload area to show interactivity
      const uploadArea = page.locator('.mud-paper').filter({ hasText: 'Ladda upp kvittobild' });
      if (await uploadArea.count() > 0) {
        await uploadArea.hover();
        await page.waitForTimeout(500);
      }
      
      // Take screenshot showing the upload area
      await page.screenshot({ 
        path: 'screenshots/ocr-receipt-scanning/03-ocr-dialog-upload-area.png',
        fullPage: false 
      });
      console.log('✓ OCR dialog upload area screenshot captured');
      
      // Close dialog
      await page.keyboard.press('Escape');
    }
  });

  test('04 - OCR scan dialog - tips section', async ({ page }) => {
    // Navigate to receipts page
    await page.goto('/receipts');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);
    
    // Click on "Skanna kvitto (OCR)" button
    const ocrButton = page.locator('text="Skanna kvitto (OCR)"');
    if (await ocrButton.count() > 0) {
      await ocrButton.click();
      await page.waitForTimeout(1000);
      
      // Scroll to show tips section
      const tipsSection = page.locator('text="Tips för bästa resultat"');
      if (await tipsSection.count() > 0) {
        await tipsSection.scrollIntoViewIfNeeded();
        await page.waitForTimeout(500);
      }
      
      // Take screenshot showing the tips
      await page.screenshot({ 
        path: 'screenshots/ocr-receipt-scanning/04-ocr-dialog-tips.png',
        fullPage: false 
      });
      console.log('✓ OCR dialog tips section screenshot captured');
      
      // Close dialog
      await page.keyboard.press('Escape');
    }
  });

  test('05 - Receipts page overview - full page', async ({ page }) => {
    // Navigate to receipts page
    await page.goto('/receipts');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);
    
    // Take full page screenshot
    await page.screenshot({ 
      path: 'screenshots/ocr-receipt-scanning/05-receipts-page-full.png',
      fullPage: true 
    });
    console.log('✓ Receipts page full screenshot captured');
  });

  test('06 - Dark mode - Receipts page with OCR button', async ({ page }) => {
    // Navigate to receipts page
    await page.goto('/receipts');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);
    
    // Try to switch to dark mode if theme toggle exists
    const themeToggle = page.locator('button[aria-label*="theme"], button[title*="Dark"], button[title*="Mörk"]').first();
    if (await themeToggle.count() > 0) {
      await themeToggle.click();
      await page.waitForTimeout(1000);
      
      // Take screenshot in dark mode
      await page.screenshot({ 
        path: 'screenshots/ocr-receipt-scanning/06-receipts-page-dark-mode.png',
        fullPage: false 
      });
      console.log('✓ Receipts page dark mode screenshot captured');
    } else {
      console.log('⚠ Theme toggle not found - skipping dark mode screenshot');
    }
  });

  test('07 - Dark mode - OCR dialog', async ({ page }) => {
    // Navigate to receipts page
    await page.goto('/receipts');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);
    
    // Try to switch to dark mode
    const themeToggle = page.locator('button[aria-label*="theme"], button[title*="Dark"], button[title*="Mörk"]').first();
    if (await themeToggle.count() > 0) {
      await themeToggle.click();
      await page.waitForTimeout(1000);
    }
    
    // Click on OCR button
    const ocrButton = page.locator('text="Skanna kvitto (OCR)"');
    if (await ocrButton.count() > 0) {
      await ocrButton.click();
      await page.waitForTimeout(1000);
      
      // Take screenshot of OCR dialog in dark mode
      await page.screenshot({ 
        path: 'screenshots/ocr-receipt-scanning/07-ocr-dialog-dark-mode.png',
        fullPage: false 
      });
      console.log('✓ OCR dialog dark mode screenshot captured');
      
      // Close dialog
      await page.keyboard.press('Escape');
    }
  });

  test('08 - Mobile view - Receipts page with OCR', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 }); // iPhone SE
    
    // Navigate to receipts page
    await page.goto('/receipts');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);
    
    // Take screenshot in mobile view
    await page.screenshot({ 
      path: 'screenshots/ocr-receipt-scanning/08-receipts-page-mobile.png',
      fullPage: false 
    });
    console.log('✓ Receipts page mobile view screenshot captured');
  });

  test('09 - Mobile view - OCR dialog', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 }); // iPhone SE
    
    // Navigate to receipts page
    await page.goto('/receipts');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);
    
    // Click on OCR button
    const ocrButton = page.locator('text="Skanna kvitto (OCR)"');
    if (await ocrButton.count() > 0) {
      await ocrButton.click();
      await page.waitForTimeout(1000);
      
      // Take screenshot of OCR dialog in mobile view
      await page.screenshot({ 
        path: 'screenshots/ocr-receipt-scanning/09-ocr-dialog-mobile.png',
        fullPage: false 
      });
      console.log('✓ OCR dialog mobile view screenshot captured');
      
      // Close dialog
      await page.keyboard.press('Escape');
    }
  });

  test('10 - Desktop view - Receipts table with buttons', async ({ page }) => {
    // Set desktop viewport
    await page.setViewportSize({ width: 1920, height: 1080 });
    
    // Navigate to receipts page
    await page.goto('/receipts');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);
    
    // Scroll to show the receipts table if needed
    const receiptsTable = page.locator('table, .mud-table');
    if (await receiptsTable.count() > 0) {
      await receiptsTable.scrollIntoViewIfNeeded();
      await page.waitForTimeout(500);
    }
    
    // Take screenshot focusing on the table area
    await page.screenshot({ 
      path: 'screenshots/ocr-receipt-scanning/10-receipts-table-desktop.png',
      fullPage: false 
    });
    console.log('✓ Receipts table desktop view screenshot captured');
  });
});

test.describe('OCR Feature Documentation Screenshots', () => {
  test('Create screenshots directory', async () => {
    const screenshotsDir = 'screenshots/ocr-receipt-scanning';
    if (!fs.existsSync(screenshotsDir)) {
      fs.mkdirSync(screenshotsDir, { recursive: true });
      console.log(`✓ Created directory: ${screenshotsDir}`);
    }
  });
});
