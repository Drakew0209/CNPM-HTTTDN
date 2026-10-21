const fs = require('fs');
let code = fs.readFileSync('InternetCafe/apps/desktop-client/InternetCafe.Client/MainWindow.xaml.cs', 'utf8');

const target = 'Loaded += async (_, _) => {';
const addition = 'MainTabs.SelectionChanged += (s, e) => { if (e.Source == MainTabs && viewModel.IsAuthenticated && !closing && !smokeMode) AudioService.Play("click"); };\n        ' + target;

code = code.replace(target, addition);
fs.writeFileSync('InternetCafe/apps/desktop-client/InternetCafe.Client/MainWindow.xaml.cs', code, 'utf8');
console.log('Tab selection sound hooked up!');
