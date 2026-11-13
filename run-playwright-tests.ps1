# run-playwright-tests.ps1 — Browser (UI) tests με Playwright
param(
    [switch]$Install,   # εγκατάσταση Chromium (μία φορά)
    [string]$SqlConnection = "Server=.\SQLEXPRESS02;Database=master;Trusted_Connection=True;TrustServerCertificate=True;"
)

$ErrorActionPreference = "Stop"
$env:DOTNET_ROLL_FORWARD = "Major"
$env:INCIS_TEST_SQL_CONNECTION = $SqlConnection

$proj = "INCIS_SaaP.PlaywrightTests/INCIS_SaaP.PlaywrightTests.csproj"

Write-Host "Building Playwright test project..." -ForegroundColor Cyan
& dotnet build $proj -c Debug --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if ($Install) {
    Write-Host "Installing Chromium for Playwright..." -ForegroundColor Cyan
    & powershell -ExecutionPolicy Bypass -File "INCIS_SaaP.PlaywrightTests/bin/Debug/net9.0/playwright.ps1" install chromium
}

Write-Host "Running Playwright browser tests..." -ForegroundColor Cyan
& dotnet test $proj --no-build --nologo
exit $LASTEXITCODE
