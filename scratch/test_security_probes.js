const http = require('http');

async function login(username, password) {
  return new Promise((resolve, reject) => {
    const data = JSON.stringify({ username, password });
    const req = http.request({
      hostname: '127.0.0.1',
      port: 5055,
      path: '/api/v1/auth/login',
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'Content-Length': Buffer.byteLength(data) }
    }, (res) => {
      let body = '';
      res.on('data', chunk => body += chunk);
      res.on('end', () => {
        try {
          const json = JSON.parse(body);
          resolve({ status: res.statusCode, data: json });
        } catch (e) {
          resolve({ status: res.statusCode, body });
        }
      });
    });
    req.on('error', reject);
    req.write(data);
    req.end();
  });
}

async function request(path, method, headers, body) {
  return new Promise((resolve, reject) => {
    const data = body ? JSON.stringify(body) : '';
    const reqHeaders = { ...headers };
    if (data) {
      reqHeaders['Content-Type'] = 'application/json';
      reqHeaders['Content-Length'] = Buffer.byteLength(data);
    }
    const req = http.request({
      hostname: '127.0.0.1',
      port: 5055,
      path: encodeURI(`/api/v1${path}`),
      method,
      headers: reqHeaders
    }, (res) => {
      let resBody = '';
      res.on('data', chunk => resBody += chunk);
      res.on('end', () => {
        try {
          const json = JSON.parse(resBody);
          resolve({ status: res.statusCode, data: json, headers: res.headers });
        } catch (e) {
          resolve({ status: res.statusCode, body: resBody, headers: res.headers });
        }
      });
    });
    req.on('error', reject);
    if (data) req.write(data);
    req.end();
  });
}

async function run() {
  console.log('Testing security probes...');
  const customerAuth = await login('gamer', 'Demo@123');
  const customerToken = customerAuth.data.data.accessToken;
  const staffAuth = await login('staff', 'Demo@123');
  const staffToken = staffAuth.data.data.accessToken;
  const cashierAuth = await login('cashier', 'Demo@123');
  const cashierToken = cashierAuth.data.data.accessToken;

  // 1. SQLi login
  const sqli1 = await login("' OR '1'='1", "' OR '1'='1");
  console.log('1. SQLi Login:', sqli1.status, sqli1.data?.error?.code || sqli1.data);

  // 2. SQLi on computers query param
  const sqli2 = await request("/computers?status=' OR '1'='1", "GET", { Authorization: `Bearer ${customerToken}` });
  console.log('2. SQLi query param /computers:', sqli2.status);

  // 3. Stored XSS in /me
  const xssMe = await request("/me", "PATCH", { Authorization: `Bearer ${customerToken}` }, {
    fullName: "Gamer <script>alert('XSS')</script>",
    hobbies: "<img src=x onerror=alert(1)>"
  });
  console.log('3. XSS in /me:', xssMe.status, xssMe.headers['x-content-type-options'], xssMe.headers['x-frame-options']);

  // 4. Stored XSS in order note
  const xssOrder = await request("/orders", "POST", { Authorization: `Bearer ${customerToken}` }, {
    computerId: "pc-001",
    items: [{ productId: "p-001", quantity: 1, note: "<svg/onload=alert('XSS')>" }]
  });
  console.log('4. XSS in Order Note:', xssOrder.status, xssOrder.data?.error?.code || 'Order created');

  // 5. Negative topup
  const negTopup = await request("/customers/c-001/topup", "POST", { Authorization: `Bearer ${cashierToken}` }, {
    amount: -50000
  });
  console.log('5. Negative topup:', negTopup.status, negTopup.data?.error?.code);

  // 6. Negative order quantity
  const negQty = await request("/orders", "POST", { Authorization: `Bearer ${customerToken}` }, {
    computerId: "pc-001",
    items: [{ productId: "p-001", quantity: -5 }]
  });
  console.log('6. Negative order qty:', negQty.status, negQty.data?.error?.code);

  // 7. Zero order quantity
  const zeroQty = await request("/orders", "POST", { Authorization: `Bearer ${customerToken}` }, {
    computerId: "pc-001",
    items: [{ productId: "p-001", quantity: 0 }]
  });
  console.log('7. Zero order qty:', zeroQty.status, zeroQty.data?.error?.code);

  // 8. IDOR / Access Control: Customer calling GET /accounts
  const custAccounts = await request("/accounts", "GET", { Authorization: `Bearer ${customerToken}` });
  console.log('8. Customer GET /accounts:', custAccounts.status);

  // 9. IDOR / Access Control: Customer calling GET /reports/analytics
  const custReports = await request("/reports/analytics", "GET", { Authorization: `Bearer ${customerToken}` });
  console.log('9. Customer GET /reports/analytics:', custReports.status);

  // 10. IDOR / Access Control: Customer calling POST /customers/c-001/topup
  const custTopup = await request("/customers/c-001/topup", "POST", { Authorization: `Bearer ${customerToken}` }, {
    amount: 50000
  });
  console.log('10. Customer POST /customers/c-001/topup:', custTopup.status);

  // 11. Staff calling GET /accounts
  const staffAccounts = await request("/accounts", "GET", { Authorization: `Bearer ${staffToken}` });
  console.log('11. Staff GET /accounts:', staffAccounts.status);

  // 12. No token calling GET /me
  const noToken = await request("/me", "GET", {});
  console.log('12. No token GET /me:', noToken.status);
}

run().catch(console.error);
