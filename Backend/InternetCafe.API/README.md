# InternetCafe.API

ASP.NET Core Web API targeting .NET 8. The API uses the existing SQL Server schema and does not run EF migrations automatically.

## Local configuration

Set secrets outside source control before running:

```powershell
dotnet user-secrets set "ConnectionStrings:InternetCafeDB" "Server=localhost;Database=InternetCafeDB;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet user-secrets set "Jwt:SigningKey" "<a random secret of at least 32 UTF-8 bytes>"
```

The project requires `Jwt:Issuer` and `Jwt:Audience` from `appsettings.json`. Do not commit JWT signing keys or SQL credentials.

## Run

```powershell
dotnet run --project InternetCafe.API/InternetCafe.API.csproj
```

Swagger is available in Development. The SignalR hub is mapped at `/cafeHub`.

## Existing database

Entities and `InternetCafeDbContext` are mapped from `InternetCafe Final.sql`. They are not migrations; do not run `database update` against the existing database. EF Core SQL Server, Design, Tools, and the matching `dotnet-ef` local tool are pinned to 8.0.31.
