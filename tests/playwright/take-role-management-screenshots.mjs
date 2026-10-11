import { chromium } from '@playwright/test';
import { fileURLToPath } from 'url';
import { dirname, join } from 'path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

async function takeRoleManagementScreenshots() {
  console.log('Starting screenshot capture for RoleManagement page...');
  
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
  const screenshotsDir = join(__dirname, 'screenshots', 'role-management');
  
  try {
    console.log('Navigating to application...');
    await page.goto('http://localhost:5274', { waitUntil: 'networkidle' });
    
    // Login first
    console.log('Logging in...');
    await page.fill('input[name="Input.Email"]', 'test@example.com');
    await page.fill('input[name="Input.Password"]', 'TestPassword123!');
    await page.click('button[type="submit"]');
    await page.waitForTimeout(2000);
    
    // Navigate to RoleManagement page - assuming household ID 1
    console.log('Navigating to Role Management page...');
    await page.goto('http://localhost:5274/households/1/roles', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    
    // Screenshot 1: Full page overview
    console.log('Taking screenshot 1: Full page overview...');
    await page.screenshot({ 
      path: join(screenshotsDir, '01-role-management-overview.png'),
      fullPage: true 
    });
    
    // Screenshot 2: Members table
    console.log('Taking screenshot 2: Members and roles table...');
    const membersTable = await page.locator('text=Medlemmar och roller').locator('..').locator('..');
    if (await membersTable.count() > 0) {
      await membersTable.screenshot({ 
        path: join(screenshotsDir, '02-members-roles-table.png')
      });
    }
    
    // Screenshot 3: Delegations section
    console.log('Taking screenshot 3: Active delegations...');
    const delegationsSection = await page.locator('text=Aktiva delegeringar').locator('..').locator('..');
    if (await delegationsSection.count() > 0) {
      await delegationsSection.screenshot({ 
        path: join(screenshotsDir, '03-active-delegations.png')
      });
    }
    
    // Screenshot 4: Role descriptions
    console.log('Taking screenshot 4: Role descriptions...');
    const roleDescriptions = await page.locator('text=Rollbeskrivningar').locator('..').locator('..');
    if (await roleDescriptions.count() > 0) {
      await roleDescriptions.screenshot({ 
        path: join(screenshotsDir, '04-role-descriptions.png')
      });
    }
    
    // Screenshot 5: Non-admin view (if we can test it)
    console.log('Screenshots taken successfully!');
    
  } catch (error) {
    console.error('Error taking screenshots:', error);
    throw error;
  } finally {
    await browser.close();
  }
}

takeRoleManagementScreenshots().catch(console.error);
