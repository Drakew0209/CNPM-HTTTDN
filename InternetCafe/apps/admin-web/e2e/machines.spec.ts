import { test, expect } from '@playwright/test';

test.describe('02. Computer Fleet Management (Machines) E2E', () => {

  test.beforeEach(async ({ page }) => {
    // Navigate and login as Cashier
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.reload();
    await page.getByRole('button', { name: 'Thu ngân' }).click();
    await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
    await expect(page).toHaveURL(/.*\/management\/overview/, { timeout: 10000 });
  });

  test('TC-PC-01: Navigate to Computers page and render machine grid with status badges', async ({ page }) => {
    // Click on Phòng máy in sidebar
    await page.locator('.side-menu').getByText('Phòng máy').click();
    await expect(page).toHaveURL(/.*\/management\/computers/);

    // Page title and summary tags
    await expect(page.locator('.page-title h1')).toContainText('Phòng máy');
    await expect(page.locator('.page-title p')).toContainText('Nhìn rõ từng máy');

    // Machine grid should contain cards
    const cards = page.locator('.machine-card');
    await expect(cards.first()).toBeVisible({ timeout: 10000 });
    const count = await cards.count();
    expect(count).toBeGreaterThan(0);

    // First card should display machine details
    const firstCard = cards.first();
    await expect(firstCard.locator('.machine-name')).toBeVisible();
    await expect(firstCard.locator('.machine-footer strong')).toContainText('/ giờ');
  });

  test('TC-PC-02: Filter machines by search input', async ({ page }) => {
    await page.goto('/management/computers');
    await expect(page.locator('.machine-grid')).toBeVisible();

    const searchInput = page.getByPlaceholder('Tìm tên máy…');
    await searchInput.fill('01');

    // Filtered results should contain '01'
    const cards = page.locator('.machine-card');
    const firstCardName = await cards.first().locator('.machine-name').textContent();
    expect(firstCardName).toContain('01');
  });

  test('TC-PC-03: Click machine card opens detail drawer with live info', async ({ page }) => {
    await page.goto('/management/computers');
    const firstCard = page.locator('.machine-card').first();
    await firstCard.click();

    // Drawer opens
    const drawer = page.locator('.ant-drawer');
    await expect(drawer).toBeVisible();
    await expect(drawer.locator('.ant-descriptions')).toBeVisible();
    await expect(drawer.getByText('Khu vực')).toBeVisible();
    await expect(drawer.getByText('Đơn giá')).toBeVisible();

    // Close drawer
    await drawer.locator('.ant-drawer-close').click();
    await expect(page.locator('.ant-drawer-open')).not.toBeVisible();
  });

});
