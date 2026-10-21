const fs = require('fs');
const col = JSON.parse(fs.readFileSync('testing/postman/InternetCafe_API.postman_collection.json', 'utf8'));

col.item.forEach((folder, fIdx) => {
  folder.item.forEach((req, rIdx) => {
    const auth = req.request.auth;
    const authType = auth ? auth.type : 'none';
    let tokenVar = 'none';
    if (auth && auth.bearer && auth.bearer[0]) {
      tokenVar = auth.bearer[0].value;
    }
    console.log(`[F${fIdx+1}.R${rIdx+1}] ${req.request.method} ${req.request.url.raw} -> Auth: ${authType} (${tokenVar})`);
  });
});
