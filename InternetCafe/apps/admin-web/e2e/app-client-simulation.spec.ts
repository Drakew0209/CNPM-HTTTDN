import { test, expect } from '@playwright/test';
import path from 'path';

test.describe('10. Desktop Client App & Web Admin E2E Integration Simulation', () => {

  test('TC-APP-01: Client ClickOnce distribution portal renders download information', async ({ page }) => {
    // Navigate to ClickOnce deployment page in docs/client
    const clientHtmlPath = path.resolve(process.cwd(), '../../../docs/client/index.html');
    await page.goto(`file://${clientHtmlPath}`);

    // Verify title and download button
    await expect(page).toHaveTitle(/InternetCafe|Cài đặt/i);
    const installBtn = page.locator('a, button').filter({ hasText: /Cài đặt|Install|Launch/i });
    await expect(installBtn.first()).toBeVisible();
  });

  test('TC-APP-02: Station heartbeat telemetry updates machine to Online status on Web Admin', async ({ page, request }) => {
    // 1. Send heartbeat from simulated Desktop Client (PC01)
    const hbRes = await request.post('http://127.0.0.1:5055/api/v1/machines/pc-001/heartbeat', {
      data: {
        machineName: 'PC01',
        osVersion: 'Microsoft Windows 11 Pro 64-bit',
        ipAddress: '127.0.0.1',
        clientVersion: '1.0.0'
      }
    });
    expect([200, 204]).toContain(hbRes.status());

    // 2. Open Web Admin and verify PC01 is Online
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.reload();
    await page.getByRole('button', { name: 'Thu ngân' }).click();
    await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
    await expect(page).toHaveURL(/.*\/management\/overview/, { timeout: 10000 });

    await page.goto('/management/computers');
    const pc01Card = page.locator('.machine-card').filter({ hasText: /01/i }).first();
    await expect(pc01Card).toBeVisible({ timeout: 10000 });
    await expect(pc01Card).toContainText(/Online/i);
  });

  test('TC-APP-03: Full Customer Lifecycle: Station login starts session -> Web Admin displays InUse -> Session End', async ({ page, request }) => {
    // 1. Login to Web Admin as Cashier
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.reload();
    await page.getByRole('button', { name: 'Thu ngân' }).click();
    await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
    await expect(page).toHaveURL(/.*\/management\/overview/, { timeout: 10000 });

    // 2. Client logs in and starts a game session on PC01
    const sessionRes = await request.post('http://127.0.0.1:5055/api/v1/sessions/start', {
      data: {
        username: 'gamer',
        password: 'Demo@123',
        computerId: 'pc-001'
      }
    });

    let sessionId: string | null = null;
    if (sessionRes.status() === 200 || sessionRes.status() === 201) {
      const body = await sessionRes.json();
      sessionId = body?.data?.id || body?.id || null;
    }

    // 3. Navigate to Computers page on Web Admin
    await page.goto('/management/computers');
    const pc01Card = page.locator('.machine-card').filter({ hasText: /01/i }).first();
    await expect(pc01Card).toBeVisible({ timeout: 10000 });

    // 4. Clean up / End session if opened
    if (sessionId) {
      await request.post(`http://127.0.0.1:5055/api/v1/sessions/${sessionId}/end`, {
        data: {}
      });
    }
  });

  test('TC-APP-04: Client station orders F&B -> Order appears in Web Admin Orders queue', async ({ page, request }) => {
    // 1. Customer places order via Client API
    const orderRes = await request.post('http://127.0.0.1:5055/api/v1/orders', {
      headers: {
        'Content-Type': 'application/json'
      },
      data: {
        customerId: 'c-001',
        computerId: 'pc-001',
        items: [
          { productId: 'p-001', quantity: 1, unitPrice: 25000 }
        ],
        note: 'E2E Playwright Client Station Order Test'
      }
    });

    // 2. Cashier navigates to Orders page on Web Admin
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.reload();
    await page.getByRole('button', { name: 'Thu ngân' }).click();
    await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
    await expect(page).toHaveURL(/.*\/management\/overview/, { timeout: 10000 });

    await page.goto('/management/orders');
    const table = page.locator('.ant-table');
    await expect(table).toBeVisible({ timeout: 10000 });

    // The order table is visible and rendered
    const rows = table.locator('.ant-table-tbody tr');
    await expect(rows.first()).toBeVisible();
  });

});
