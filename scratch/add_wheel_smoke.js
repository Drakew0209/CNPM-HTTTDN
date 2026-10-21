const fs = require('fs');
let code = fs.readFileSync('InternetCafe/apps/desktop-client/InternetCafe.Client/MainWindow.xaml.cs', 'utf8');

const target = 'viewModel.IsAppLocked = false;';
const addition = 'viewModel.IsAppLocked = false;\n            viewModel.OpenLuckyWheelCommand.Execute(null); await CaptureAsync(this, Path.Combine(outputDirectory, "11-lucky-wheel.png")); viewModel.CloseLuckyWheelCommand.Execute(null);';

code = code.replace(target, addition);
fs.writeFileSync('InternetCafe/apps/desktop-client/InternetCafe.Client/MainWindow.xaml.cs', code, 'utf8');
console.log('MainWindow.xaml.cs updated for lucky wheel capture!');
