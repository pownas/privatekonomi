import { test, expect } from '@playwright/test';

const semanticColors = ['primary', 'secondary', 'tertiary', 'success', 'info', 'warning', 'error'];

async function getContrastRatios(page: import('@playwright/test').Page) {
  return page.evaluate((colors) => {
    const style = getComputedStyle(document.documentElement);
    const parseRgb = (value: string) => {
      const channels = value.match(/[\d.]+/g)?.slice(0, 3).map(Number);
      if (!channels || channels.length !== 3) {
        throw new Error(`Unexpected theme color value: ${value}`);
      }
      return channels;
    };
    const luminance = (value: string) => {
      const [red, green, blue] = parseRgb(value).map(channel => {
        const normalized = channel / 255;
        return normalized <= 0.04045
          ? normalized / 12.92
          : ((normalized + 0.055) / 1.055) ** 2.4;
      });
      return 0.2126 * red + 0.7152 * green + 0.0722 * blue;
    };

    return Object.fromEntries(colors.map(color => {
      const background = luminance(style.getPropertyValue(`--mud-palette-${color}`));
      const foreground = luminance(style.getPropertyValue(`--mud-palette-${color}-text`));
      const lighter = Math.max(background, foreground);
      const darker = Math.min(background, foreground);
      return [color, (lighter + 0.05) / (darker + 0.05)];
    }));
  }, semanticColors);
}

test('semantic color foregrounds meet WCAG AA in light and dark themes', async ({ page }) => {
  await page.addInitScript(() => localStorage.setItem('darkMode', 'false'));
  await page.goto('/', { waitUntil: 'domcontentloaded' });
  await page.waitForLoadState('networkidle');

  const lightRatios = await getContrastRatios(page);
  for (const [color, ratio] of Object.entries(lightRatios)) {
    expect(ratio, `${color} light-theme contrast`).toBeGreaterThanOrEqual(4.5);
  }

  await page.getByRole('button', { name: 'Mörkt läge' }).click();
  await expect(page.locator('html')).toHaveClass(/mud-theme-dark/);

  const darkRatios = await getContrastRatios(page);
  for (const [color, ratio] of Object.entries(darkRatios)) {
    expect(ratio, `${color} dark-theme contrast`).toBeGreaterThanOrEqual(4.5);
  }
});

test('dark account-row hover is distinct and green text meets WCAG AA', async ({ page }) => {
  await page.addInitScript(() => localStorage.setItem('darkMode', 'true'));
  await page.goto('/accounts');
  await page.waitForLoadState('networkidle');

  const amount = page.locator('.mud-table-body span[style*="--mud-palette-success"]').first();
  await expect(amount).toBeVisible();
  const row = amount.locator('xpath=ancestor::tr');
  const contrastRatio = async () => amount.evaluate(element => {
    const parseColor = (value: string) => {
      const channels = value.match(/[\d.]+/g)?.map(Number);
      if (!channels || channels.length < 3) {
        throw new Error(`Unexpected color value: ${value}`);
      }
      return [channels[0], channels[1], channels[2], channels[3] ?? 1];
    };
    const surface = parseColor(getComputedStyle(document.documentElement).getPropertyValue('--mud-palette-surface'));
    const rowColor = parseColor(getComputedStyle(element.closest('tr')!).backgroundColor);
    const foreground = parseColor(getComputedStyle(element).color);
    const alpha = rowColor[3] ?? 1;
    const background = rowColor.slice(0, 3).map((channel, index) => channel * alpha + surface[index] * (1 - alpha));
    const luminance = (channels: number[]) => channels.slice(0, 3).map(channel => {
      const normalized = channel / 255;
      return normalized <= 0.04045 ? normalized / 12.92 : ((normalized + 0.055) / 1.055) ** 2.4;
    }).reduce((sum, channel, index) => sum + channel * [0.2126, 0.7152, 0.0722][index], 0);
    const lighter = Math.max(luminance(foreground), luminance(background));
    const darker = Math.min(luminance(foreground), luminance(background));
    return (lighter + 0.05) / (darker + 0.05);
  });

  const normalContrast = await contrastRatio();
  const backgroundBeforeHover = await row.evaluate(element => getComputedStyle(element).backgroundColor);
  await row.hover();
  await expect(row).toHaveCSS('background-color', 'rgb(51, 65, 85)');
  const backgroundOnHover = await row.evaluate(element => getComputedStyle(element).backgroundColor);
  const hoverContrast = await contrastRatio();

  expect(backgroundOnHover).toBe('rgb(51, 65, 85)');
  expect(backgroundOnHover).not.toBe(backgroundBeforeHover);
  expect(normalContrast, 'green amount contrast on normal row').toBeGreaterThanOrEqual(4.5);
  expect(hoverContrast, 'green amount contrast on hovered row').toBeGreaterThanOrEqual(4.5);
});
