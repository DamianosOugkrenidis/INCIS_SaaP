@echo off
setlocal
cd /d "%~dp0"
echo ============================================
echo  INCIS SaaP - Local server (build + run)
echo  URL: https://localhost:5001
echo ============================================
set DOTNET_ROLL_FORWARD=Major
set ASPNETCORE_ENVIRONMENT=Production
set ASPNETCORE_URLS=https://localhost:5001;http://localhost:5000
set ConnectionStrings__DefaultConnection=Server=.\SQLEXPRESS02;Database=INCIS_SaaP;Trusted_Connection=True;TrustServerCertificate=True;Max Pool Size=100;
set INCIS_DB_CONNECTION_STRING=Server=.\SQLEXPRESS02;Database=INCIS_SaaP;Trusted_Connection=True;TrustServerCertificate=True;Max Pool Size=100;
set Jwt__Secret=INCIS-l0cal-JWT-Secret-2026-ChangeMe-32chars!
set AdminSeed__Email=admin@incis.local
set AdminSeed__Password=
set Security__AllowSelfRegistration=false
set Api__Csrf__Enforce=false
set Api__WritePolicy__Enforce=false
set Api__DemoToken__Enabled=false
dotnet run --project "INCIS_SaaP\INCIS_SaaP.csproj" -c Release
