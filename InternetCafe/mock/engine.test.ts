import { beforeEach, describe, expect, it } from 'vitest';
import { createMockEngine, createSeed } from './engine';

describe('shared business API', () => {
  let engine: ReturnType<typeof createMockEngine>;
  let time: Date;
  let customer: string, manager: string, cashier: string, staff: string, admin: string;
  const login = (username: string, password = 'Demo@123') => engine.handle('POST', '/api/v1/auth/login', { username, password });
  const call = (method: string, path: string, body?: unknown, token = customer) => engine.handle(method, `/api/v1${path}`, body, token);
  const row = (resource: keyof ReturnType<typeof engine.snapshot>, id: string) => engine.snapshot()[resource].find(item => item.id === id)!;
  beforeEach(() => {
    time = new Date('2026-10-03T05:00:00Z'); engine = createMockEngine({ now: () => time });
    [customer, manager, cashier, staff, admin] = ['gamer', 'manager', 'cashier', 'staff', 'admin'].map(name => login(name).body.data.accessToken);
  });

  it('authenticates all demo roles, enforces bans, and never returns credentials', () => {
    expect(login('gamer').body.data.user).toMatchObject({ id: 'c-001', role: 'Customer' });
    expect(login('staff').body.data.user.id).toBe('e-001');
    expect(login('owner').body.data.user.role).toBe('Owner');
    expect(login('banned').status).toBe(403); expect(login('admin', 'wrong').status).toBe(401);
    expect(engine.handle('GET', '/api/v1/workspace').status).toBe(401);
    expect(JSON.stringify(engine.snapshot())).not.toMatch(/Demo@123|password|accessToken/);
  });
  it('returns customer DTO after registration and /me, and preserves password rules', () => {
    const registered = call('POST', '/auth/register', { username: 'newplayer', password: 'Password123', fullName: 'New Player', phone: '0987654321', email: 'new@example.test', dateOfBirth: '2000-01-01', hobbies: 'FPS' });
    expect(registered.status).toBe(201); expect(registered.body.data).toMatchObject({ username: 'newplayer', balance: 0, tier: 'Silver', status: 'Active' });
    expect(registered.body.data.accessToken).toBeUndefined();
    const token = login('newplayer', 'Password123').body.data.accessToken;
    expect(call('GET', '/me', undefined, token).body.data).toEqual(registered.body.data);
    expect(call('PATCH', '/me', { balance: 999999 }, token).status).toBe(422);
    expect(call('POST', '/me/password', { currentPassword: 'wrong', newPassword: 'Password456' }, token).status).toBe(422);
    expect(call('POST', '/me/password', { currentPassword: 'Password123', newPassword: 'Password456' }, token).status).toBe(200);
    expect(login('newplayer', 'Password123').status).toBe(401); expect(login('newplayer', 'Password456').status).toBe(200);
  });
  it('filters customer and staff data at collection, detail and bootstrap boundaries', () => {
    const workspace = call('GET', '/workspace').body.data;
    expect(workspace.customers.map((x: any) => x.id)).toEqual(['c-001']);
    expect(workspace.accounts).toEqual([]); expect(workspace.payroll).toEqual([]); expect(workspace.employees).toEqual([]);
    expect(workspace.computers.find((x: any) => x.id === 'pc-02').customerId).toBeUndefined();
    expect(workspace.surveys[0].responses).toEqual([]);
    expect(call('GET', '/customers/c-002').status).toBe(404); expect(call('GET', '/payroll').status).toBe(403);
    const staffWorkspace = call('GET', '/workspace', undefined, staff).body.data;
    expect(staffWorkspace.employees.map((x: any) => x.id)).toEqual(['e-001']);
    expect(staffWorkspace.payroll.every((x: any) => x.employeeId === 'e-001')).toBe(true);
    expect(call('GET', '/payroll/payroll-002', undefined, staff).status).toBe(404);
    expect(call('GET', '/customers', undefined, admin).status).toBe(403);
  });
  it('debits server prices and reserves stock once, then refunds and restores stock once', () => {
    expect(call('POST', '/sessions/start', { computerId: 'pc-01' }).status).toBe(201);
    const input = { computerId: 'pc-01', items: [{ productId: 'p-001', quantity: 2 }], note: '', idempotencyKey: 'order-test-0001' };
    const first = call('POST', '/orders', input);
    expect(first.status).toBe(201); expect(first.body.data.total).toBe(40000);
    expect(row('customers', 'c-001').balance).toBe(145000); expect(row('products', 'p-001').stock).toBe(36);
    expect(call('POST', '/orders', { ...input, items: [{ quantity: 2, productId: 'p-001' }] })).toEqual(first);
    expect(engine.handle('POST', '/orders/', input, customer)).toEqual(first);
    expect(call('POST', '/orders', { ...input, note: 'different' }).body.error.code).toBe('IDEMPOTENCY_CONFLICT');
    const id = first.body.data.id;
    expect(call('POST', `/orders/${id}/transition`, { status: 'Cancelled' }).status).toBe(200);
    expect(call('POST', `/orders/${id}/transition`, { status: 'Cancelled' }).status).toBe(409);
    expect(row('customers', 'c-001').balance).toBe(185000); expect(row('products', 'p-001').stock).toBe(38);
    expect(engine.snapshot().transactions.filter(x => x.referenceId === id).map(x => x.type).sort()).toEqual(['FoodOrder', 'Refund']);
  });
  it('rolls back invalid multi-item orders without partial money or stock changes', () => {
    call('POST', '/sessions/start', { computerId: 'pc-01' }); const before = engine.snapshot();
    const result = call('POST', '/orders', { computerId: 'pc-01', items: [{ productId: 'p-001', quantity: 2 }, { productId: 'p-005', quantity: 1 }] });
    expect(result.body.error.code).toBe('OUT_OF_STOCK'); expect(engine.snapshot()).toEqual(before);
    expect(call('POST', '/orders', { computerId: 'pc-01', items: [{ productId: 'p-001', quantity: 1, expectedUnitPrice: 1 }] }).body.error.code).toBe('PRICE_CHANGED');
    expect(call('POST', '/orders', { computerId: 'pc-01', items: [{ productId: 'p-001', quantity: 1 }], total: 1 }).status).toBe(422);
    expect(engine.snapshot()).toEqual(before);
  });
  it('detects insufficient funds before changing stock', () => {
    call('POST', '/sessions/start', { computerId: 'pc-01' }); const before = engine.snapshot();
    expect(call('POST', '/orders', { computerId: 'pc-01', items: [{ productId: 'p-001', quantity: 10 }] }).body.error.code).toBe('INSUFFICIENT_BALANCE');
    expect(engine.snapshot()).toEqual(before);
  });
  it('limits order transitions by state, operator role, and ownership', () => {
    expect(call('POST', '/orders/order-pending/transition', { status: 'Cancelled' }).status).toBe(404);
    expect(call('POST', '/orders/order-pending/transition', { status: 'Served' }, cashier).status).toBe(409);
    expect(call('POST', '/orders/order-pending/transition', { status: 'Preparing' }, cashier).status).toBe(200);
    expect(call('POST', '/orders/order-pending/transition', { status: 'Cancelled' }, cashier).status).toBe(409);
    expect(call('POST', '/orders/order-pending/transition', { status: 'Cancelled' }, manager).status).toBe(200);
  });
  it('processes top-ups exactly once and revokes access after a manager ban', () => {
    const topup = call('POST', '/topups', { amount: 50000, idempotencyKey: 'topup-test-0001' });
    expect(call('POST', '/topups', { amount: 50000, idempotencyKey: 'topup-test-0001' })).toEqual(topup);
    const path = `/topups/${topup.body.data.id}/decision`;
    expect(call('POST', path, { status: 'Approved' }).status).toBe(403);
    expect(call('POST', path, { status: 'Approved' }, cashier).status).toBe(200);
    expect(call('POST', path, { status: 'Approved' }, cashier).status).toBe(409);
    expect(row('customers', 'c-001').balance).toBe(235000);
    expect(call('PATCH', '/customers/c-001', { status: 'Banned' }, cashier).status).toBe(403);
    expect(call('PATCH', '/customers/c-001', { status: 'Banned' }, manager).status).toBe(200);
    expect(call('GET', '/me').status).toBe(403);
  });
  it('freezes session price, computes elapsed billing, ends once and frees the machine', () => {
    const started = call('POST', '/sessions/start', { computerId: 'pc-01' });
    expect(call('POST', '/sessions/start', { computerId: 'pc-03' }).body.error.code).toBe('ACTIVE_SESSION');
    expect(call('POST', '/computers/pc-01/command', { command: 'Maintenance' }, manager).status).toBe(409);
    call('PATCH', '/computers/pc-01', { hourlyRate: 20000 }, manager); time = new Date(time.getTime() + 90 * 60000);
    const path = `/sessions/${started.body.data.id}/end`; const result = call('POST', path, {});
    expect(result.body.data.amount).toBe(15000); expect(row('customers', 'c-001').balance).toBe(170000);
    expect(call('POST', path, {})).toEqual(result); expect(row('computers', 'pc-01')).toMatchObject({ status: 'Available' });
    expect(row('computers', 'pc-01').sessionId).toBeUndefined();
  });
  it('clamps long session charges to available funds', () => {
    const started = call('POST', '/sessions/start', { computerId: 'pc-01' }); time = new Date(time.getTime() + 48 * 3600000);
    expect(call('POST', `/sessions/${started.body.data.id}/end`, {}).body.data.amount).toBe(185000);
    expect(row('customers', 'c-001').balance).toBe(0);
  });
  it('enforces inventory quantity and server controlled payroll totals', () => {
    const before = engine.snapshot();
    expect(call('POST', '/inventory', { productId: 'p-004', type: 'Export', quantity: 5 }, manager).status).toBe(409); expect(engine.snapshot()).toEqual(before);
    expect(call('POST', '/inventory', { productId: 'p-004', type: 'Import', quantity: 10, supplierId: 'supplier-001' }, manager).status).toBe(201);
    expect(row('products', 'p-004').stock).toBe(14);
    expect(call('PATCH', '/products/p-004', { stock: 99 }, manager).status).toBe(422);
    expect(call('PATCH', '/payroll/payroll-001', { bonus: 1000000, deduction: 200000 }, manager).body.data.total).toBe(7800000);
    expect(call('PATCH', '/payroll/payroll-001', { status: 'Paid' }, manager).status).toBe(409);
    expect(call('PATCH', '/payroll/payroll-003', { bonus: 0 }, manager).status).toBe(409);
  });
  it('scopes staff attendance and synchronizes approved leave with future schedules', () => {
    expect(call('POST', '/attendance/check-in', { scheduleId: 'schedule-cashier' }, staff).status).toBe(404);
    const checked = call('POST', '/attendance/check-in', { scheduleId: 'schedule-today' }, staff);
    expect(checked.status).toBe(201); expect(call('POST', '/attendance/check-in', { scheduleId: 'schedule-today' }, staff).status).toBe(409);
    expect(call('PATCH', '/schedules/schedule-today', { employeeId: 'e-002' }, manager).status).toBe(409);
    expect(call('POST', `/attendance/${checked.body.data.id}/check-out`, {}, staff).status).toBe(200);
    expect(row('schedules', 'schedule-today').status).toBe('Completed');
    const leave = call('POST', '/leaves', { type: 'Annual', startDate: '2026-10-04', endDate: '2026-10-04', reason: 'Family' }, staff);
    expect(leave.status).toBe(201); expect(call('POST', `/leaves/${leave.body.data.id}/decision`, { status: 'Approved' }, manager).status).toBe(200);
    expect(row('schedules', 'schedule-tomorrow').status).toBe('OnLeave');
    expect(call('PATCH', `/leaves/${leave.body.data.id}`, { reason: 'Changed' }, staff).status).toBe(409);
  });
  it('keeps targeted survey responses private and accepts one complete response', () => {
    expect(call('POST', '/surveys/survey-002/responses', { answers: { 'q-1': 'Mì' } }).status).toBe(404);
    const survey = row('surveys', 'survey-001');
    expect(call('POST', '/surveys/survey-001/responses', { answers: {} }).status).toBe(422);
    const answers = Object.fromEntries(survey.questions.map((q: any) => [q.id, q.options[0]]));
    expect(call('POST', '/surveys/survey-001/responses', { answers }).status).toBe(201);
    expect(call('POST', '/surveys/survey-001/responses', { answers }).status).toBe(409);
    expect(call('GET', '/surveys/survey-001').body.data.responses).toHaveLength(1);
    expect(call('PATCH', '/surveys/survey-001', { title: 'Changed' }, manager).status).toBe(409);
  });
  it('filters and paginates consistently and excludes top-ups from sales revenue', () => {
    const page = call('GET', '/products?search=&sort=price&order=desc&page=2&pageSize=2');
    expect(page.body.meta).toEqual({ total: 5, page: 2, pageSize: 2 }); expect(page.body.data).toHaveLength(2);
    expect(call('GET', '/orders?from=2026-10-04&to=2026-10-03').status).toBe(422);
    expect(call('GET', '/products?pageSize=0').status).toBe(422);
    const report = call('GET', '/reports/summary?from=2026-10-03&to=2026-10-03', undefined, cashier).body.data;
    expect(report).toMatchObject({ rentalRevenue: 0, foodRevenue: 20000, netRevenue: 20000, topups: 200000, source: 'mock' });
  });
  it('rejects malformed action paths and invalid identity changes without side effects', () => {
    const before = engine.snapshot();
    expect(call('POST', '/topups/topup-pending/decision/extra', { status: 'Approved' }, manager).status).toBe(404);
    expect(call('POST', '/categories/cat-food', { name: 'Duplicate' }, manager).status).toBe(405);
    expect(call('PATCH', '/accounts/a-owner', { role: 'Customer' }, admin).status).toBe(422);
    expect(call('PATCH', '/accounts/a-owner', { role: 'Staff' }, admin).status).toBe(422);
    expect(engine.snapshot()).toEqual(before);
  });
  it('computes reports beyond the first hundred transactions', () => {
    const seed = createSeed(time); seed.transactions = Array.from({ length: 125 }, (_, i) => ({ id: `tx-${i}`, customerId: 'c-001', type: 'FoodOrder', amount: 1000, createdAt: time.toISOString(), referenceId: `order-${i}` }));
    engine = createMockEngine({ seed, now: () => time }); cashier = login('cashier').body.data.accessToken;
    expect(call('GET', '/reports/summary', undefined, cashier).body.data).toMatchObject({ netRevenue: 125000, transactionCount: 125 });
  });
});
