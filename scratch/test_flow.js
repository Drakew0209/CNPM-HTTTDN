const baseUrl = 'http://127.0.0.1:5055/api/v1';

async function run() {
  console.log('=== 1. Logins ===');
  async function login(username, password) {
    const res = await fetch(`${baseUrl}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password })
    });
    const d = await res.json();
    return d.data?.accessToken;
  }

  const adminToken = await login('admin', 'Demo@123');
  const cashierToken = await login('cashier', 'Demo@123');
  const gamerToken = await login('gamer', 'Demo@123');
  console.log('Tokens acquired:', { admin: !!adminToken, cashier: !!cashierToken, gamer: !!gamerToken });

  console.log('\n=== 2. Computers & Start Session ===');
  const compRes = await fetch(`${baseUrl}/computers`, { headers: { 'Authorization': `Bearer ${adminToken}` } });
  const compData = (await compRes.json()).data;
  const availComp = compData.find(c => c.status === 'Available') || compData[0];
  console.log('Selected Computer:', availComp.id, availComp.status);

  // If already in session or occupied, let's check
  const startRes = await fetch(`${baseUrl}/sessions/start`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${gamerToken}` },
    body: JSON.stringify({ computerId: availComp.id })
  });
  console.log('Start session status:', startRes.status);
  const startJson = await startRes.json();
  console.log('Start session body:', startJson);
  const sessionId = startJson.data?.id;

  console.log('\n=== 3. Heartbeat ===');
  const hbRes = await fetch(`${baseUrl}/machines/${availComp.id}/heartbeat`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${gamerToken}` }
  });
  console.log('Heartbeat status:', hbRes.status);

  console.log('\n=== 4. Products & Create Order ===');
  const prodRes = await fetch(`${baseUrl}/products`, { headers: { 'Authorization': `Bearer ${gamerToken}` } });
  const prodData = (await prodRes.json()).data;
  const prod = prodData[0];
  console.log('Selected Product:', prod.id, prod.name, prod.price);

  const orderRes = await fetch(`${baseUrl}/orders`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${gamerToken}` },
    body: JSON.stringify({
      computerId: availComp.id,
      items: [{ productId: prod.id, quantity: 1, note: 'Ít đường đá' }]
    })
  });
  console.log('Order status:', orderRes.status);
  const orderJson = await orderRes.json();
  console.log('Order body:', orderJson);
  const orderId = orderJson.data?.id;

  if (orderId) {
    console.log('\n=== 5. Cashier Transition Order ===');
    const transRes = await fetch(`${baseUrl}/orders/${orderId}/transition`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${cashierToken}` },
      body: JSON.stringify({ status: 'Preparing' })
    });
    console.log('Transition status:', transRes.status);
    console.log('Transition body:', await transRes.json());
  }

  if (sessionId) {
    console.log('\n=== 6. End Session ===');
    const endRes = await fetch(`${baseUrl}/sessions/${sessionId}/end`, {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${gamerToken}` }
    });
    console.log('End session status:', endRes.status);
    console.log('End session body:', await endRes.json());
  }
}

run().catch(console.error);
