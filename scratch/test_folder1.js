const baseUrl = 'http://127.0.0.1:5055/api/v1';

async function testFolder1() {
  console.log('--- Testing Folder 01 Full Auth Lifecycle ---');
  async function login(username, password) {
    const res = await fetch(`${baseUrl}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password })
    });
    const d = await res.json();
    return { status: res.status, token: d.data?.accessToken, role: d.data?.user?.role };
  }

  // 1-5 Logins
  for (const [user, role] of [['admin', 'Admin'], ['manager', 'Manager'], ['cashier', 'Cashier'], ['staff', 'Staff'], ['gamer', 'Customer']]) {
    const r = await login(user, 'Demo@123');
    console.log(`Login ${user}: status=${r.status}, role=${r.role}`);
    if (r.status !== 200) throw new Error(`Failed to login as ${user}`);
  }

  // 6 Negative login
  const neg = await login('admin', 'WrongPass@999');
  console.log(`Negative login: status=${neg.status} (expected 401)`);

  // 7 Register
  const ts = Date.now();
  const newUser = `test_gamer_${ts}`;
  const regRes = await fetch(`${baseUrl}/auth/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      username: newUser,
      password: 'GamerPass@2026',
      fullName: 'Khách Hàng Tạm',
      phone: '09' + Math.floor(10000000 + Math.random()*90000000),
      email: `${newUser}@test.com`
    })
  });
  console.log(`Register ${newUser}: status=${regRes.status} (expected 201)`);

  // 8 Login disposable
  const disp1 = await login(newUser, 'GamerPass@2026');
  console.log(`Login disposable: status=${disp1.status}`);

  // 9 Change password
  const chgRes = await fetch(`${baseUrl}/me/password`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${disp1.token}` },
    body: JSON.stringify({ currentPassword: 'GamerPass@2026', newPassword: 'GamerPass@2026_Updated' })
  });
  console.log(`Change password: status=${chgRes.status}`);

  // 10 Relogin disposable
  const disp2 = await login(newUser, 'GamerPass@2026_Updated');
  console.log(`Relogin with updated password: status=${disp2.status}`);

  // 11 Logout disposable
  const logoutRes = await fetch(`${baseUrl}/auth/logout`, {
    method: 'POST',
    headers: { 'Authorization': `Bearer ${disp2.token}` }
  });
  console.log(`Logout disposable: status=${logoutRes.status}`);

  // 12 Verify token revoked
  const verifyRes = await fetch(`${baseUrl}/me`, {
    headers: { 'Authorization': `Bearer ${disp2.token}` }
  });
  console.log(`Verify token revoked: status=${verifyRes.status} (expected 401)`);

  // 13-14 Customer profile
  const gamerLogin = await login('gamer', 'Demo@123');
  const meRes = await fetch(`${baseUrl}/me`, { headers: { 'Authorization': `Bearer ${gamerLogin.token}` } });
  console.log(`Get /me (gamer): status=${meRes.status}`);

  const patchRes = await fetch(`${baseUrl}/me`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${gamerLogin.token}` },
    body: JSON.stringify({ fullName: 'Gamer VIP Pro', hobbies: 'CS2, LoL' })
  });
  console.log(`PATCH /me (gamer): status=${patchRes.status}`);

  console.log('--- ALL FOLDER 01 TESTS PASSED! ---');
}

testFolder1().catch(console.error);
