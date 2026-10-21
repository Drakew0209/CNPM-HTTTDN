import { test, expect } from '@playwright/test';

test.describe('03. POS Billing & F&B Operations E2E', () => {

  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.reload();
    await page.getByRole('button', { name: 'Thu ngân' }).click();
    await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
    await expect(page).toHaveURL(/.*\/management\/overview/, { timeout: 10000 });
  });

  test('TC-POS-01: Navigate to Customers list and inspect gamer wallet data', async ({ page }) => {
    // Navigate to customers page
    await page.goto('/management/customers');
    await expect(page.locator('.page-title h1')).toContainText('Khách hàng');

    // Table should render customer data
    const table = page.locator('.ant-table');
    await expect(table).toBeVisible({ timeout: 10000 });
    
    // Check table headers
    await expect(table.locator('thead')).toContainText('Họ tên');
    await expect(table.locator('thead')).toContainText('Tên đăng nhập');
    await expect(table.locator('thead')).toContainText('Số dư');

    // Verify at least one customer row exists
    const rows = table.locator('.ant-table-tbody tr');
    await expect(rows.first()).toBeVisible();
    expect(await rows.count()).toBeGreaterThan(0);
  });

  test('TC-POS-02: Navigate to Orders list and verify F&B table columns', async ({ page }) => {
    // Navigate to orders page
    await page.goto('/management/orders');
    await expect(page.locator('.page-title h1')).toContainText('Đơn dịch vụ');

    const table = page.locator('.ant-table');
    await expect(table).toBeVisible({ timeout: 10000 });

    // Table header columns
    await expect(table.locator('thead')).toContainText('Trạng thái');
    await expect(table.locator('thead')).toContainText('Tổng tiền');
  });

  test('TC-POS-03: Navigate to Products menu and verify active food/drink items', async ({ page }) => {
    await page.goto('/management/products');
    await expect(page.locator('.page-title h1')).toContainText('Thực đơn');

    const table = page.locator('.ant-table');
    await expect(table).toBeVisible({ timeout: 10000 });

    // Check menu items like Trà sữa or Mì
    const bodyText = await table.locator('tbody').textContent();
    expect(bodyText).toMatch(/(Trà sữa|Mì|Nước|Cà phê)/i);
  });

});
