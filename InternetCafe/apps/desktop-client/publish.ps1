$OutputFolder = "$PSScriptRoot/../../dist/desktop-client"
Write-Host "Dang dong goi Desktop Client sang $OutputFolder..." -ForegroundColor Cyan
dotnet publish "$PSScriptRoot/InternetCafe.Client/InternetCafe.Client.csproj" -c Release -r win-x64 --self-contained false -o $OutputFolder
Write-Host "Hoan tat dong goi Desktop Client!" -ForegroundColor Green
