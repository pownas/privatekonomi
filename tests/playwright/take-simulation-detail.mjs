import { chromium } from '@playwright/test';
import { fileURLToPath } from 'url';
import { dirname, join } from 'path';
import fs from 'fs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

async function takeSimulationDetails() {
  console.log('Taking detailed simulation screenshots...');
  
  const browser = await chromium.launch({
    executablePath: '/usr/bin/google-chrome',
    headless: true,
    args: ['--no-sandbox', '--disable-setuid-sandbox']
  });
  
  const context = await browser.newContext({
    viewport: { width: 1920, height: 1080 },
    deviceScaleFactor: 1,
  });
  
  const page = await context.newPage();
  
  const screenshotsDir = join(__dirname, 'screenshots', 'savings-goals');
  
  try {
    // Navigate and login
    await page.goto('http://localhost:5274', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    
    const emailInput = await page.locator('input[type="email"]').first();
    if (await emailInput.count() > 0) {
      await emailInput.fill('test@example.com');
      await page.locator('input[type="password"]').first().fill('TestPassword123!');
      await page.locator('button[type="submit"]').first().click();
      await page.waitForTimeout(3000);
    }
    
    // Go to goals page
    await page.goto('http://localhost:5274/goals', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    
    // Try to find and click a simulation button
    // Look for all icon buttons in the actions column
    const tableRows = await page.locator('table tbody tr').all();
    
    for (let i = 0; i < Math.min(tableRows.length, 3); i++) {
      const row = tableRows[i];
      const buttons = await row.locator('button.mud-icon-button').all();
      
      // The simulation button should be the first button in actions (calculator icon)
      if (buttons.length >= 3) {
        const simulationButton = buttons[0]; // First button is usually the simulation
        
        try {
          await simulationButton.scrollIntoViewIfNeeded();
          await page.waitForTimeout(500);
          await simulationButton.click();
          await page.waitForTimeout(2000);
          
          // Check if dialog opened
          const dialog = await page.locator('.mud-dialog').first();
          if (await dialog.isVisible()) {
            console.log('Simulation dialog opened!');
            
            // Take initial screenshot
            console.log('Taking screenshot: 07-simulation-dialog-initial.png');
            await page.screenshot({
              path: join(screenshotsDir, '07-simulation-dialog-initial.png'),
              fullPage: false
            });
            
            // Find the input field for monthly savings
            const inputs = await page.locator('.mud-dialog input[type="number"], .mud-dialog input.mud-input-root').all();
            
            for (const input of inputs) {
              try {
                await input.scrollIntoViewIfNeeded();
                await input.click({ timeout: 1000 });
                await input.fill('2700');
                await page.waitForTimeout(2000);
                
                console.log('Taking screenshot: 08-simulation-with-increased-savings.png');
                await page.screenshot({
                  path: join(screenshotsDir, '08-simulation-with-increased-savings.png'),
                  fullPage: false
                });
                
                // Try a lower amount too
                await input.click();
                await input.fill('800');
                await page.waitForTimeout(2000);
                
                console.log('Taking screenshot: 09-simulation-with-decreased-savings.png');
                await page.screenshot({
                  path: join(screenshotsDir, '09-simulation-with-decreased-savings.png'),
                  fullPage: false
                });
                
                break;
              } catch (e) {
                continue;
              }
            }
            
            // Close dialog
            const closeButton = await page.getByText('Stäng').first();
            if (await closeButton.count() > 0) {
              await closeButton.click();
              await page.waitForTimeout(1000);
            }
            
            break;
          }
        } catch (e) {
          console.log(`Attempt ${i+1} failed, trying next row...`);
          continue;
        }
      }
    }
    
    console.log('Detailed simulation screenshots complete!');
    
  } catch (error) {
    console.error('Error:', error);
  } finally {
    await browser.close();
  }
}

takeSimulationDetails().catch(console.error);
