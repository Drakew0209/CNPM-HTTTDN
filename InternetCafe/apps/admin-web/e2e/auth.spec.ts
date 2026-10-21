import { test, expect } from '@playwright/test';

test.describe('01. Authentication & Session Management E2E', () => {

  test.beforeEach(async ({ page }) => {
    // Clear localStorage to ensure clean session state
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.reload();
  });

  test('TC-AUTH-01: Login page renders with branding, form and demo role buttons', async ({ page }) => {
    await expect(page).toHaveTitle(/InternetCafe/i);
    await expect(page.locator('.brand')).toContainText('InternetCafe');
    await expect(page.locator('h2')).toContainText('Chào mừng trở lại');
    
    // Verify inputs exist
    const usernameInput = page.locator('input[autocomplete="username"]');
    const passwordInput = page.locator('input[autocomplete="current-password"]');
    const submitBtn = page.getByRole('button', { name: 'Đăng nhập', exact: true });

    await expect(usernameInput).toBeVisible();
    await expect(passwordInput).toBeVisible();
    await expect(submitBtn).toBeVisible();

    // Verify demo quick-login buttons exist
    await expect(page.getByRole('button', { name: 'Quản lý' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Thu ngân' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Admin' })).toBeVisible();
  });

  test('TC-AUTH-02: Login fails with invalid password showing error alert', async ({ page }) => {
    const usernameInput = page.locator('input[autocomplete="username"]');
    const passwordInput = page.locator('input[autocomplete="current-password"]');
    const submitBtn = page.getByRole('button', { name: 'Đăng nhập', exact: true });

    await usernameInput.fill('admin');
    await passwordInput.fill('WrongPassword@999');
    await submitBtn.click();

    // Error alert should be displayed
    const alert = page.locator('.ant-alert-error');
    await expect(alert).toBeVisible();
    await expect(alert).toContainText('Tên đăng nhập hoặc mật khẩu không đúng.');
    await expect(page).toHaveURL(/.*\/login/);
  });

  test('TC-AUTH-03: Customer role login is rejected with client app notice', async ({ page }) => {
    const usernameInput = page.locator('input[autocomplete="username"]');
    const passwordInput = page.locator('input[autocomplete="current-password"]');
    const submitBtn = page.getByRole('button', { name: 'Đăng nhập', exact: true });

    await usernameInput.fill('gamer');
    await passwordInput.fill('Demo@123');
    await submitBtn.click();

    const alert = page.locator('.ant-alert-error');
    await expect(alert).toBeVisible();
    await expect(alert).toContainText('Tài khoản khách sử dụng ứng dụng desktop InternetCafe Client.');
  });

  test('TC-AUTH-04: Login as Manager succeeds and redirects to management overview', async ({ page }) => {
    // Click quick demo login for Manager
    await page.getByRole('button', { name: 'Quản lý' }).click();
    await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();

    // Redirect to management overview
    await expect(page).toHaveURL(/.*\/management\/overview/, { timeout: 10000 });
    
    // Topbar and sidebar branding
    await expect(page.locator('.desktop-sidebar .brand')).toBeVisible();
    await expect(page.locator('.topbar-label')).toContainText('Quản lý vận hành');
    await expect(page.locator('.account-button')).toContainText('Quản lý');
  });

  test('TC-AUTH-05: Logout flow prompts confirmation modal and redirects to /login', async ({ page }) => {
    // Login as Manager first
    await page.getByRole('button', { name: 'Quản lý' }).click();
    await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
    await expect(page).toHaveURL(/.*\/management\/overview/, { timeout: 10000 });

    // Open user dropdown
    await page.locator('.account-button').click();
    await page.locator('.ant-dropdown-menu-item', { hasText: 'Đăng xuất' }).click();

    // Confirm modal
    const modal = page.locator('.ant-modal-confirm');
    await expect(modal).toBeVisible();
    await expect(modal).toContainText('Đăng xuất khỏi InternetCafe?');

    // Click confirm logout
    await modal.getByRole('button', { name: 'Đăng xuất' }).click();

    // Should return to login page
    await expect(page).toHaveURL(/.*\/login/, { timeout: 5000 });
  });

});
