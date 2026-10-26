import { test, expect } from '@playwright/test';

test.describe('06. Human Resource Management (HRM) & Staff Portal E2E', () => {

  test('TC-HRM-01: Manager inspects Employees roster', async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.reload();
    await page.getByRole('button', { name: 'Quản lý' }).click();
    await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
    await expect(page).toHaveURL(/.*\/management\/overview/, { timeout: 10000 });

    await page.goto('/management/employees');
    await expect(page.locator('.page-title h1')).toContainText('Hồ sơ nhân sự');

    const table = page.locator('.ant-table');
    await expect(table).toBeVisible({ timeout: 10000 });

    const headerText = await table.locator('thead').textContent();
    expect(headerText).toMatch(/(Họ tên|Bộ phận|Chức vụ|Lương)/i);

    const rows = table.locator('.ant-table-tbody tr');
    await expect(rows.first()).toBeVisible();
    expect(await rows.count()).toBeGreaterThan(0);
  });

  test('TC-HRM-02: Manager inspects Work Shifts definition', async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.reload();
    await page.getByRole('button', { name: 'Quản lý' }).click();
    await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
    await expect(page).toHaveURL(/.*\/management\/overview/, { timeout: 10000 });

    await page.goto('/management/shifts');
    await expect(page.locator('.page-title h1')).toContainText('Ca làm việc');

    const table = page.locator('.ant-table');
    await expect(table).toBeVisible({ timeout: 10000 });

    const bodyText = await table.locator('tbody').textContent();
    expect(bodyText).toMatch(/(Ca sáng|Ca chiều|Ca tối|Ca)/i);
  });

  test('TC-HRM-03: Manager inspects Work Schedules assignment', async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.reload();
    await page.getByRole('button', { name: 'Quản lý' }).click();
    await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
    await expect(page).toHaveURL(/.*\/management\/overview/, { timeout: 10000 });

    await page.goto('/management/schedules');
    await expect(page.locator('.page-title h1')).toContainText('Phân lịch làm');

    const table = page.locator('.ant-table');
    await expect(table).toBeVisible({ timeout: 10000 });
  });

  test('TC-HRM-04: Manager inspects Attendance tracking logs', async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.reload();
    await page.getByRole('button', { name: 'Quản lý' }).click();
    await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
    await expect(page).toHaveURL(/.*\/management\/overview/, { timeout: 10000 });

    await page.goto('/management/attendance');
    await expect(page.locator('.page-title h1')).toContainText('Bảng chấm công');

    const table = page.locator('.ant-table');
    await expect(table).toBeVisible({ timeout: 10000 });
  });

  test('TC-HRM-05: Manager inspects Leave Requests queue', async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.reload();
    await page.getByRole('button', { name: 'Quản lý' }).click();
    await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
    await expect(page).toHaveURL(/.*\/management\/overview/, { timeout: 10000 });

    await page.goto('/management/leaves');
    await expect(page.locator('.page-title h1')).toContainText('Duyệt đơn nghỉ');

    const table = page.locator('.ant-table');
    await expect(table).toBeVisible({ timeout: 10000 });
  });

  test('TC-HRM-06: Staff Portal (/staff/home) displays personal shifts and attendance panel', async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.reload();

    // Login as Staff
    await page.getByRole('button', { name: 'Nhân viên' }).click();
    await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();

    // Staff redirected to /staff/home
    await expect(page).toHaveURL(/.*\/staff\/home/, { timeout: 10000 });

    // Verify Staff Portal branding and context
    await expect(page.locator('.topbar-label')).toContainText('Cổng nhân viên');
    await expect(page.locator('.account-button')).toContainText('Nhân viên');

    // Verify personal management sections exist
    const content = page.locator('.main-content');
    await expect(content).toBeVisible();

    // Profile or action buttons
    await expect(page.getByRole('button', { name: /Chấm công|Cập nhật|Đơn nghỉ/i }).first()).toBeVisible();
  });

});
