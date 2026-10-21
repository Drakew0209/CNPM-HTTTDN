const fs = require('fs');

async function testSuite() {
  const baseUrl = 'http://127.0.0.1:5055/api/v1';

  // 1. Logins
  console.log('--- 1. Testing Logins ---');
  async function login(username, password) {
    const res = await fetch(`${baseUrl}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password })
    });
    const data = await res.json();
    return { status: res.status, token: data.data?.accessToken, role: data.data?.user?.role };
  }

  const admin = await login('admin', 'Demo@123');
  console.log('Admin login:', admin.status, admin.role, admin.token ? 'OK' : 'NO TOKEN');

  const manager = await login('manager', 'Demo@123');
  console.log('Manager login:', manager.status, manager.role, manager.token ? 'OK' : 'NO TOKEN');

  const cashier = await login('cashier', 'Demo@123');
  console.log('Cashier login:', cashier.status, cashier.role, cashier.token ? 'OK' : 'NO TOKEN');

  const staff = await login('staff', 'Demo@123');
  console.log('Staff login:', staff.status, staff.role, staff.token ? 'OK' : 'NO TOKEN');

  const gamer = await login('gamer', 'Demo@123');
  console.log('Gamer login:', gamer.status, gamer.role, gamer.token ? 'OK' : 'NO TOKEN');

  // Test HRM endpoints
  console.log('\n--- 2. Testing HRM Endpoints ---');
  async function testGet(name, url, token) {
    const res = await fetch(url, { headers: { 'Authorization': `Bearer ${token}` } });
    console.log(`${name}: HTTP ${res.status}`);
    if (res.status !== 200) {
      console.log('  Error body:', await res.text());
    } else {
      const json = await res.json();
      console.log('  Items count:', Array.isArray(json.data) ? json.data.length : 'Object');
    }
  }

  await testGet('Employees (manager)', `${baseUrl}/employees`, manager.token);
  await testGet('Employees (admin)', `${baseUrl}/employees`, admin.token);
  await testGet('Shifts (manager)', `${baseUrl}/shifts`, manager.token);
  await testGet('Schedules (staff)', `${baseUrl}/schedules`, staff.token);
  await testGet('Schedules (manager)', `${baseUrl}/schedules`, manager.token);
  await testGet('Attendance (manager)', `${baseUrl}/attendance`, manager.token);

  // Test Reports endpoints
  console.log('\n--- 3. Testing Reports Endpoints ---');
  await testGet('Summary (cashier)', `${baseUrl}/reports/summary?from=2026-10-01&to=2026-10-31`, cashier.token);
  await testGet('Summary (manager)', `${baseUrl}/reports/summary?from=2026-10-01&to=2026-10-31`, manager.token);
  await testGet('Analytics (manager)', `${baseUrl}/reports/analytics?from=2026-10-01&to=2026-10-31`, manager.token);
  await testGet('Invoices (cashier)', `${baseUrl}/invoices`, cashier.token);
}

testSuite().catch(console.error);
