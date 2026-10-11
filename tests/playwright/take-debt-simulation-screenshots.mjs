import { chromium } from '@playwright/test';
import { fileURLToPath } from 'url';
import { dirname, join } from 'path';
import fs from 'fs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

async function takeDebtSimulationScreenshots() {
  console.log('Starting screenshot capture for Debt Simulation features...');
  
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
  const screenshotsDir = join(__dirname, 'screenshots', 'debt-simulation');
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
      if (await passwordInput.count() > 0) {
        await passwordInput.fill('TestPassword123!');
      }
      
      // Click login button
      const loginButton = await page.locator('button[type="submit"]').first();
      if (await loginButton.count() > 0) {
        await loginButton.click();
        await page.waitForTimeout(3000);
      }
    }
    
    // Navigate to Loans page
    console.log('Navigating to Loans page...');
    await page.goto('http://localhost:5274/loans', { waitUntil: 'networkidle', timeout: 30000 });
    await page.waitForTimeout(2000);
    
    // Screenshot 1: Overview tab with summary cards
    console.log('Taking screenshot 1: Overview tab with summary cards...');
    await page.screenshot({ 
      path: join(screenshotsDir, '01-loans-overview.png'),
      fullPage: true 
    });
    
    // Screenshot 2: Amorteringsplan tab with export button
    console.log('Taking screenshot 2: Amortization schedule tab...');
    await page.click('text=Amorteringsplan');
    await page.waitForTimeout(1500);
    await page.screenshot({ 
      path: join(screenshotsDir, '02-amortization-schedule-with-export.png'),
      fullPage: true 
    });
    
    // Screenshot 3: Avbetalningsstrategier tab with simulation tool
    console.log('Taking screenshot 3: Repayment strategies tab...');
    await page.click('text=Avbetalningsstrategier');
    await page.waitForTimeout(1500);
    await page.screenshot({ 
      path: join(screenshotsDir, '03-strategies-with-simulation-tool.png'),
      fullPage: true 
    });
    
    // Screenshot 4: Fill simulation input and run
    console.log('Taking screenshot 4: After running simulation...');
    const simulationInput = await page.locator('input[label="Tillgänglig månadsbetalning (kr)"]').first();
    if (await simulationInput.count() > 0) {
      await simulationInput.fill('5000');
      await page.waitForTimeout(500);
    }
    
    const runButton = await page.locator('text=Kör simulering').first();
    if (await runButton.count() > 0) {
      await runButton.click();
      await page.waitForTimeout(2000);
    }
    
    await page.screenshot({ 
      path: join(screenshotsDir, '04-simulation-results.png'),
      fullPage: true 
    });
    
    // Screenshot 5: Zoom in on simulation panel
    console.log('Taking screenshot 5: Simulation panel closeup...');
    const simulationPanel = await page.locator('text=Simuleringsverktyg').locator('..').locator('..');
    if (await simulationPanel.count() > 0) {
      await simulationPanel.screenshot({ 
        path: join(screenshotsDir, '05-simulation-panel-closeup.png')
      });
    }
    
    // Screenshot 6: Strategy cards with export buttons
    console.log('Taking screenshot 6: Strategy cards...');
    const snowballCard = await page.locator('.mud-card').filter({ hasText: 'Snöboll-metoden' }).first();
    if (await snowballCard.count() > 0) {
      await snowballCard.screenshot({ 
        path: join(screenshotsDir, '06-snowball-strategy-card.png')
      });
    }
    
    const avalancheCard = await page.locator('.mud-card').filter({ hasText: 'Lavin-metoden' }).first();
    if (await avalancheCard.count() > 0) {
      await avalancheCard.screenshot({ 
        path: join(screenshotsDir, '07-avalanche-strategy-card.png')
      });
    }
    
    // Screenshot 7: Detaljerad Simulering tab
    console.log('Taking screenshot 7: Detailed simulation tab...');
    await page.click('text=Detaljerad Simulering');
    await page.waitForTimeout(1500);
    await page.screenshot({ 
      path: join(screenshotsDir, '08-detailed-simulation-overview.png'),
      fullPage: true 
    });
    
    // Screenshot 8: Summary cards in detailed view
    console.log('Taking screenshot 8: Summary cards...');
    const summarySection = await page.locator('.mud-grid').first();
    if (await summarySection.count() > 0) {
      await summarySection.screenshot({ 
        path: join(screenshotsDir, '09-summary-cards.png')
      });
    }
    
    // Screenshot 9: Payoff order table
    console.log('Taking screenshot 9: Payoff order table...');
    const payoffTable = await page.locator('text=Avbetalningsordning').locator('..').locator('..');
    if (await payoffTable.count() > 0) {
      await payoffTable.screenshot({ 
        path: join(screenshotsDir, '10-payoff-order-table.png')
      });
    }
    
    // Screenshot 10: Monthly payment schedule
    console.log('Taking screenshot 10: Monthly payment schedule...');
    const monthlySchedule = await page.locator('text=Månatlig betalningsplan').locator('..').locator('..');
    if (await monthlySchedule.count() > 0) {
      await monthlySchedule.screenshot({ 
        path: join(screenshotsDir, '11-monthly-payment-schedule.png')
      });
    }
    
    console.log('All screenshots taken successfully!');
    console.log(`Screenshots saved to: ${screenshotsDir}`);
    
  } catch (error) {
    console.error('Error taking screenshots:', error);
    throw error;
  } finally {
    await browser.close();
  }
}

takeDebtSimulationScreenshots().catch(console.error);
