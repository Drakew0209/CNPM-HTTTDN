const fs = require('fs');
const run = JSON.parse(fs.readFileSync('testing/postman/InternetCafe Management System API - Test Suite.postman_test_run.json', 'utf8'));

for (let i = 24; i < 35; i++) {
  const r = run.results[i];
  if (!r) continue;
  console.log(`[Result ${i+1}] ${r.name}`);
  console.log('   url:', r.url);
  console.log('   responseCode:', r.responseCode);
}
