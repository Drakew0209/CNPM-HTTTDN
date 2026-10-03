import { afterAll, beforeAll, expect, it } from 'vitest';
import { createMockHttpServer } from './server';
import type { AddressInfo } from 'node:net';

const server = createMockHttpServer(); let base: string;
beforeAll(async () => { await new Promise<void>(resolve => server.listen(0, '127.0.0.1', resolve)); base = `http://127.0.0.1:${(server.address() as AddressInfo).port}`; });
afterAll(async () => { server.closeAllConnections(); await new Promise<void>((resolve, reject) => server.close(error => error ? reject(error) : resolve())); });
async function login(username: string) { const response = await fetch(`${base}/api/v1/auth/login`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ username, password: 'Demo@123' }) }); return (await response.json()).data.accessToken; }

it('shares top-up state between independent desktop and operator HTTP clients', async () => {
  const gamer = await login('gamer'), cashier = await login('cashier');
  const request = async (path: string, token: string, method = 'GET', body?: unknown) => fetch(`${base}/api/v1${path}`, { method, headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' }, ...(body === undefined ? {} : { body: JSON.stringify(body) }) });
  const topupResponse = await request('/topups', gamer, 'POST', { amount: 25000, idempotencyKey: 'http-topup-001' });
  expect(topupResponse.status).toBe(201); expect(Number.isNaN(Date.parse(topupResponse.headers.get('date')!))).toBe(false);
  const topup = (await topupResponse.json()).data;
  expect((await (await request('/topups?status=Pending', cashier)).json()).data.some((row: any) => row.id === topup.id)).toBe(true);
  expect((await request(`/topups/${topup.id}/decision`, cashier, 'POST', { status: 'Approved' })).status).toBe(200);
  expect((await (await request('/me', gamer)).json()).data.balance).toBe(210000);
});
it('exposes Date for local web clients and rejects nonlocal origins', async () => {
  const preflight = await fetch(`${base}/api/v1/workspace`, { method: 'OPTIONS', headers: { Origin: 'http://127.0.0.1:5173', 'Access-Control-Request-Method': 'GET' } });
  expect(preflight.status).toBe(204); expect(preflight.headers.get('access-control-expose-headers')).toBe('Date');
  expect(preflight.headers.get('access-control-allow-origin')).toBe('http://127.0.0.1:5173');
  expect((await fetch(`${base}/api/v1/workspace`, { headers: { Origin: 'https://external.example' } })).status).toBe(403);
});
it('returns envelope errors for malformed JSON, unsupported payloads and unauthenticated requests', async () => {
  const broken = await fetch(`${base}/api/v1/auth/login`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: '{' });
  expect(broken.status).toBe(400); expect((await broken.json()).error.code).toBe('INVALID_JSON');
  expect((await fetch(`${base}/api/v1/auth/login`, { method: 'POST', body: '{}' })).status).toBe(415);
  expect((await fetch(`${base}/api/v1/workspace`)).status).toBe(401);
  expect((await fetch(`${base}/workspace`)).status).toBe(404);
});
