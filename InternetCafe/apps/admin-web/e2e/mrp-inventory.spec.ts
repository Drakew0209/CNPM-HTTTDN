import { test, expect } from '@playwright/test';

test.describe('05. MRP & Inventory Management E2E', () => {

  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.reload();
    await page.getByRole('button', { name: 'Quản lý' }).click();
    await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
    await expect(page).toHaveURL(/.*\/management\/overview/, { timeout: 10000 });
  });

  test('TC-MRP-01: Navigate to Raw Materials (Kho NVL) and verify stock & reorder levels', async ({ page }) => {
    await page.goto('/management/materials');
    await expect(page.locator('.page-title h1')).toContainText('Nguyên vật liệu');

    const table = page.locator('.ant-table');
    await expect(table).toBeVisible({ timeout: 10000 });

    // Table header checks
    const headerText = await table.locator('thead').textContent();
    expect(headerText).toMatch(/(Mã|Tên|Tồn kho|Đơn vị|Đơn giá)/i);

    // Verify raw material rows exist
    const rows = table.locator('.ant-table-tbody tr');
    await expect(rows.first()).toBeVisible();
    expect(await rows.count()).toBeGreaterThan(0);
  });

  test('TC-MRP-02: Navigate to Bill of Materials (BOM) and inspect recipe formulas', async ({ page }) => {
    await page.goto('/management/bom');
    await expect(page.locator('.page-title h1')).toContainText('Định mức công thức');

    const table = page.locator('.ant-table');
    await expect(table).toBeVisible({ timeout: 10000 });

    const headerText = await table.locator('thead').textContent();
    expect(headerText).toMatch(/(Món ăn|Nguyên vật liệu|Định lượng)/i);

    const rows = table.locator('.ant-table-tbody tr');
    await expect(rows.first()).toBeVisible();
    expect(await rows.count()).toBeGreaterThan(0);
  });

  test('TC-MRP-03: Navigate to Material Requests and verify requisition status workflow', async ({ page }) => {
    await page.goto('/management/material-requests');
    await expect(page.locator('.page-title h1')).toContainText('Yêu cầu xuất NVL');

    const table = page.locator('.ant-table');
    await expect(table).toBeVisible({ timeout: 10000 });

    const rows = table.locator('.ant-table-tbody tr');
    await expect(rows.first()).toBeVisible();
    expect(await rows.count()).toBeGreaterThan(0);
  });

  test('TC-MRP-04: Navigate to Inventory Stock Movement and inspect import/export logs', async ({ page }) => {
    await page.goto('/management/inventory');
    await expect(page.locator('.page-title h1')).toContainText('Nhập · xuất kho');

    const table = page.locator('.ant-table');
    await expect(table).toBeVisible({ timeout: 10000 });

    // Header checks
    const headerText = await table.locator('thead').textContent();
    expect(headerText).toMatch(/(Sản phẩm|Loại|Số lượng|Thời gian)/i);
  });

  test('TC-MRP-05: Navigate to Suppliers directory and check vendor contact info', async ({ page }) => {
    await page.goto('/management/suppliers');
    await expect(page.locator('.page-title h1')).toContainText('Nhà cung cấp');

    const table = page.locator('.ant-table');
    await expect(table).toBeVisible({ timeout: 10000 });

    const headerText = await table.locator('thead').textContent();
    expect(headerText).toMatch(/(Tên nhà cung cấp|Điện thoại|Email)/i);

    const rows = table.locator('.ant-table-tbody tr');
    await expect(rows.first()).toBeVisible();
    expect(await rows.count()).toBeGreaterThan(0);
  });

});
