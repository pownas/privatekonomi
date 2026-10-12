import { chromium } from '@playwright/test';
import { fileURLToPath } from 'url';
import { dirname, join } from 'path';
import fs from 'fs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

async function takeGoalsScreenshots() {
  console.log('Starting screenshot capture for Goals and Savings Simulation features...');
  
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
  
  // Create screenshots directory
  const screenshotsDir = join(__dirname, 'screenshots', 'savings-goals');
  if (!fs.existsSync(screenshotsDir)) {
    fs.mkdirSync(screenshotsDir, { recursive: true });
  }
  
  try {
    console.log('Navigating to application...');
    await page.goto('http://localhost:5274', { waitUntil: 'networkidle', timeout: 30000 });
    await page.waitForTimeout(2000);
    
    // Check if we need to login
    const emailInput = await page.locator('input[type="email"]').first();
    const emailInputAlt = await page.locator('input[name="Input.Email"]').first();
    
    if (await emailInput.count() > 0 || await emailInputAlt.count() > 0) {
      console.log('Logging in...');
      
      // Try different selectors for email
      if (await emailInput.count() > 0) {
        await emailInput.fill('test@example.com');
      } else {
        await emailInputAlt.fill('test@example.com');
      }
      
      // Try different selectors for password
      const passwordInput = await page.locator('input[type="password"]').first();
      const passwordInputAlt = await page.locator('input[name="Input.Password"]').first();
      
      if (await passwordInput.count() > 0) {
        await passwordInput.fill('TestPassword123!');
      } else {
        await passwordInputAlt.fill('TestPassword123!');
      }
      
      // Click login button
      const loginButton = await page.locator('button[type="submit"]').first();
      await loginButton.click();
      
      await page.waitForTimeout(3000);
    }
    
    console.log('Navigating to Goals page...');
    await page.goto('http://localhost:5274/goals', { waitUntil: 'networkidle', timeout: 30000 });
    await page.waitForTimeout(2000);
    
    // 1. Goals list overview
    console.log('Taking screenshot: 01-goals-list-overview.png');
    await page.screenshot({
      path: join(screenshotsDir, '01-goals-list-overview.png'),
      fullPage: true
    });
    
    // 2. Create new goal form - click "Nytt Sparmål" button
    console.log('Opening create goal form...');
    const newGoalButton = await page.getByText('Nytt Sparmål').first();
    if (await newGoalButton.count() > 0) {
      await newGoalButton.click();
      await page.waitForTimeout(1000);
      
      console.log('Taking screenshot: 02-create-goal-form.png');
      await page.screenshot({
        path: join(screenshotsDir, '02-create-goal-form.png'),
        fullPage: true
      });
      
      // Scroll to auto-savings section
      const autoSavingsSection = await page.locator('text=Automatiskt sparande').first();
      if (await autoSavingsSection.count() > 0) {
        await autoSavingsSection.scrollIntoViewIfNeeded();
        await page.waitForTimeout(500);
        
        console.log('Taking screenshot: 03-auto-savings-section.png');
        await page.screenshot({
          path: join(screenshotsDir, '03-auto-savings-section.png'),
          fullPage: true
        });
        
        // Select "Fast belopp per månad" option
        const autoSavingsSelect = await page.locator('select, .mud-select').filter({ hasText: /Typ av automatiskt sparande|Ingen automatisk/ }).first();
        if (await autoSavingsSelect.count() > 0) {
          await autoSavingsSelect.click();
          await page.waitForTimeout(500);
          
          // Try to select fixed amount option
          const fixedAmountOption = await page.getByText('Fast belopp per månad', { exact: true }).first();
          if (await fixedAmountOption.count() > 0) {
            await fixedAmountOption.click();
            await page.waitForTimeout(1000);
            
            console.log('Taking screenshot: 04-auto-savings-fixed-amount.png');
            await page.screenshot({
              path: join(screenshotsDir, '04-auto-savings-fixed-amount.png'),
              fullPage: true
            });
          }
        }
      }
      
      // Close the form
      const cancelButton = await page.getByText('Avbryt').first();
      if (await cancelButton.count() > 0) {
        await cancelButton.click();
        await page.waitForTimeout(1000);
      }
    }
    
    // 3. Priority controls - find a goal in the table
    console.log('Looking for priority controls...');
    const priorityArrow = await page.locator('button[title*="prioritet"], .mud-icon-button').filter({ has: page.locator('[class*="arrow"]') }).first();
    if (await priorityArrow.count() > 0) {
      await priorityArrow.scrollIntoViewIfNeeded();
      await page.waitForTimeout(500);
      
      console.log('Taking screenshot: 05-priority-controls.png');
      await page.screenshot({
        path: join(screenshotsDir, '05-priority-controls.png'),
        fullPage: false
      });
    }
    
    // 4. Simulation dialog - click calculator icon
    console.log('Looking for simulation button...');
    const calculatorButton = await page.locator('button[title*="Simulera"], button[aria-label*="Simulera"]').first();
    const calculatorIcon = await page.locator('.mud-icon-button').filter({ has: page.locator('svg.mud-icon-root') }).first();
    
    let simulationOpened = false;
    
    if (await calculatorButton.count() > 0) {
      await calculatorButton.scrollIntoViewIfNeeded();
      await page.waitForTimeout(500);
      await calculatorButton.click();
      await page.waitForTimeout(2000);
      simulationOpened = true;
    } else if (await calculatorIcon.count() > 0) {
      // Try to find calculator icon by looking for the calculate icon
      const allIconButtons = await page.locator('.mud-icon-button').all();
      for (const button of allIconButtons) {
        const svg = await button.locator('svg').first();
        if (await svg.count() > 0) {
          const pathD = await svg.locator('path').first().getAttribute('d');
          // Calculator icon path typically contains specific patterns
          if (pathD && pathD.includes('M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2z')) {
            await button.scrollIntoViewIfNeeded();
            await page.waitForTimeout(500);
            await button.click();
            await page.waitForTimeout(2000);
            simulationOpened = true;
            break;
          }
        }
      }
    }
    
    if (simulationOpened) {
      console.log('Taking screenshot: 06-simulation-dialog-opened.png');
      await page.screenshot({
        path: join(screenshotsDir, '06-simulation-dialog-opened.png'),
        fullPage: false
      });
      
      // Find the new monthly savings input and change it
      const monthlyInput = await page.locator('input[label*="Nytt månadsbelopp"], input').filter({ hasText: /månad/ }).first();
      const allInputs = await page.locator('input[type="text"], input[type="number"]').all();
      
      for (const input of allInputs) {
        const label = await input.getAttribute('aria-label') || await input.getAttribute('placeholder') || '';
        if (label.includes('månad') || label.includes('Nytt')) {
          await input.scrollIntoViewIfNeeded();
          await input.click();
          await input.fill('2700');
          await page.waitForTimeout(2000);
          
          console.log('Taking screenshot: 07-simulation-with-change.png');
          await page.screenshot({
            path: join(screenshotsDir, '07-simulation-with-change.png'),
            fullPage: false
          });
          break;
        }
      }
      
      // Close dialog
      const closeButton = await page.getByText('Stäng').first();
      if (await closeButton.count() > 0) {
        await closeButton.click();
        await page.waitForTimeout(1000);
      }
    }
    
    // 5. Full goals page with data
    console.log('Taking screenshot: 08-goals-page-full.png');
    await page.screenshot({
      path: join(screenshotsDir, '08-goals-page-full.png'),
      fullPage: true
    });
    
    console.log('Screenshots saved successfully!');
    console.log(`Location: ${screenshotsDir}`);
    
  } catch (error) {
    console.error('Error taking screenshots:', error);
  } finally {
    await browser.close();
  }
}

takeGoalsScreenshots().catch(console.error);
