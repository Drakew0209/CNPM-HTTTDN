import { test, expect } from '@playwright/test';

test.describe('04. Financial Reports & Invoices Reconciliation E2E', () => {

  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.reload();
    await page.getByRole('button', { name: 'Quản lý' }).click();
    await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
    await expect(page).toHaveURL(/.*\/management\/overview/, { timeout: 10000 });
  });

  test('TC-REP-01: Navigate to Reports page and render revenue breakdown charts', async ({ page }) => {
    await page.goto('/management/reports');
    await expect(page.locator('.page-title h1')).toContainText('Báo cáo');

    // Preset filter buttons
    await expect(page.getByRole('button', { name: 'Tất cả' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Tháng này' })).toBeVisible();

    // Financial KPI cards or charts
    const chartsOrCards = page.locator('.ant-card');
    await expect(chartsOrCards.first()).toBeVisible({ timeout: 10000 });
    const count = await chartsOrCards.count();
    expect(count).toBeGreaterThan(0);
  });

  test('TC-REP-02: Navigate to Invoices page and inspect bill history', async ({ page }) => {
    await page.goto('/management/invoices');
    await expect(page.locator('.page-title h1')).toContainText('Hóa đơn');

    const table = page.locator('.ant-table');
    await expect(table).toBeVisible({ timeout: 10000 });

    // Verify invoice table headers
    await expect(table.locator('thead')).toContainText('Mã hóa đơn');
    await expect(table.locator('thead')).toContainText('Tổng thanh toán');

    // Click on first invoice to open details drawer if rows exist
    const firstRow = table.locator('tbody tr').first();
    if (await firstRow.isVisible()) {
      const detailBtn = firstRow.getByRole('button', { name: /Xem|Chi tiết/i }).first();
      if (await detailBtn.isVisible()) {
        await detailBtn.click();
        await expect(page.locator('.ant-drawer')).toBeVisible();
      }
    }
  });

});
