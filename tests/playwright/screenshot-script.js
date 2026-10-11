const { chromium } = require('playwright');

(async () => {
    const browser = await chromium.launch({ headless: true });
    const context = await browser.newContext({ viewport: { width: 1400, height: 900 } });
    const page = await context.newPage();

    // Navigate to login page
    await page.goto('http://localhost:5275/Account/Login');
    await page.waitForLoadState('networkidle');
    
    // Fill in login credentials
    await page.fill('input[name="Input.Email"]', 'test@example.com');
    await page.fill('input[name="Input.Password"]', 'TestPassword123!');
    
    // Click login button
    await page.click('button[type="submit"]');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);
    
    // Navigate to Transactions page
    await page.goto('http://localhost:5275/transaktioner');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000);
    
    // Screenshot 1: Transaction list page
    await page.screenshot({ 
        path: '/home/runner/work/Privatekonomi/Privatekonomi/docs/screenshots/transactions-list.png',
        fullPage: false 
    });
    console.log('Screenshot 1: Transaction list saved');
    
    // Click on filter panel to expand if collapsed
    const filterExpandButton = await page.$('button:has-text("Filter")');
    if (filterExpandButton) {
        await filterExpandButton.click();
        await page.waitForTimeout(1000);
    }
    
    // Screenshot 2: Filter panel expanded 
    await page.screenshot({ 
        path: '/home/runner/work/Privatekonomi/Privatekonomi/docs/screenshots/transactions-filters.png',
        fullPage: false 
    });
    console.log('Screenshot 2: Filter panel saved');
    
    // Look for quick categorize button on a row
    const quickCategorizeBtn = await page.$('button:has(.mud-icon-default)');
    if (quickCategorizeBtn) {
        await quickCategorizeBtn.click();
        await page.waitForTimeout(2000);
        
        // Screenshot 3: Quick categorize dialog
        await page.screenshot({ 
            path: '/home/runner/work/Privatekonomi/Privatekonomi/docs/screenshots/quick-categorize-dialog.png',
            fullPage: false 
        });
        console.log('Screenshot 3: Quick categorize dialog saved');
    }
    
    await browser.close();
    console.log('All screenshots captured successfully!');
})().catch(err => {
    console.error('Error:', err);
    process.exit(1);
});
