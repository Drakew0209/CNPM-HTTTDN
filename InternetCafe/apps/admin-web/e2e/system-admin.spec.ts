import { test, expect } from '@playwright/test';

test.describe('08. System Administration & RBAC Matrix E2E', () => {

  test('TC-ADM-01: Admin logs in and inspects Accounts management (/system/accounts)', async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.reload();

    // Login as Admin
    await page.getByRole('button', { name: 'Admin' }).click();
    await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();

    await expect(page).toHaveURL(/.*\/system\/accounts/, { timeout: 10000 });
    await expect(page.locator('.page-title h1')).toContainText('Tài khoản & phân quyền');

    const table = page.locator('.ant-table');
    await expect(table).toBeVisible({ timeout: 10000 });

    const headerText = await table.locator('thead').textContent();
    expect(headerText).toMatch(/(Tên đăng nhập|Họ tên|Vai trò|Trạng thái)/i);

    // Verify system roles present in table
    const bodyText = await table.locator('tbody').textContent();
    expect(bodyText).toMatch(/(Admin|Manager|Cashier|Staff|Owner)/i);
  });

  test('TC-ADM-02: Admin inspects Permission Matrix (/system/permissions)', async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.reload();

    await page.getByRole('button', { name: 'Admin' }).click();
    await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
    await expect(page).toHaveURL(/.*\/system\/accounts/, { timeout: 10000 });

    await page.goto('/system/permissions');
    await expect(page.locator('.page-title h1')).toContainText('Ma trận quyền');

    const cards = page.locator('.permission-grid .ant-card');
    await expect(cards.first()).toBeVisible({ timeout: 10000 });
    expect(await cards.count()).toBeGreaterThanOrEqual(4);
  });

  test('TC-ADM-03: Admin inspects Games catalog (/management/games)', async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.reload();

    await page.getByRole('button', { name: 'Admin' }).click();
    await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
    await expect(page).toHaveURL(/.*\/system\/accounts/, { timeout: 10000 });

    await page.goto('/management/games');
    await expect(page.locator('.page-title h1')).toContainText('Danh mục Game');

    const table = page.locator('.ant-table');
    await expect(table).toBeVisible({ timeout: 10000 });

    const bodyText = await table.locator('tbody').textContent();
    expect(bodyText).toMatch(/(League|Valorant|CS|Dota|FIFA|Game)/i);
  });

  test('TC-ADM-04: Unauthorized user (Cashier) is blocked from Admin Accounts with 403', async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.reload();

    // Login as Cashier
    await page.getByRole('button', { name: 'Thu ngân' }).click();
    await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
    await expect(page).toHaveURL(/.*\/management\/overview/, { timeout: 10000 });

    // Attempt direct access to Admin Accounts
    await page.goto('/system/accounts');

    // 403 Result should be displayed
    const forbidden = page.locator('.ant-result-403');
    await expect(forbidden).toBeVisible({ timeout: 5000 });
    await expect(forbidden).toContainText('403');
    await expect(forbidden).toContainText('Bạn không có quyền truy cập khu vực này.');
  });

});
