import { test, expect } from '@playwright/test';

test.describe('07. CRM & Customer Surveys & Feedback E2E', () => {

  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.reload();
    await page.getByRole('button', { name: 'Quản lý' }).click();
    await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
    await expect(page).toHaveURL(/.*\/management\/overview/, { timeout: 10000 });
  });

  test('TC-CRM-01: Navigate to Customer Surveys list and verify questionnaires', async ({ page }) => {
    await page.goto('/management/surveys');
    await expect(page.locator('.page-title h1')).toContainText('Khảo sát khách hàng');

    const table = page.locator('.ant-table');
    await expect(table).toBeVisible({ timeout: 10000 });

    const headerText = await table.locator('thead').textContent();
    expect(headerText).toMatch(/(Tiêu đề|Trạng thái|Người nhận|Phản hồi)/i);
  });

  test('TC-CRM-02: Navigate to Feedback & Helpdesk and verify customer tickets', async ({ page }) => {
    await page.goto('/management/feedback');
    await expect(page.locator('.page-title h1')).toContainText('Phản hồi & hỗ trợ');

    const table = page.locator('.ant-table');
    await expect(table).toBeVisible({ timeout: 10000 });

    const headerText = await table.locator('thead').textContent();
    expect(headerText).toMatch(/(Chủ đề|Nội dung|Trạng thái|Thời gian)/i);
  });

});
