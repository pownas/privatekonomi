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