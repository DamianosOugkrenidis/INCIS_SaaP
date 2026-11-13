# run-tests.ps1 — Ενιαία εκτέλεση δοκιμών INCIS SaaP
param(
    [string]$Category = "",
    [switch]$Coverage,
    [string]$SqlConnection = "Server=.\SQLEXPRESS02;Database=master;Trusted_Connection=True;TrustServerCertificate=True;"
)

$ErrorActionPreference = "Stop"
$env:DOTNET_ROLL_FORWARD = "Major"
$env:INCIS_TEST_SQL_CONNECTION = $SqlConnection

$proj = "INCIS_SaaP.Tests/INCIS_SaaP.Tests.csproj"
$argsList = @("test", $proj)

if ($Category) { $argsList += @("--filter", "Category=$Category") }
if ($Coverage) { $argsList += @("--collect:XPlat Code Coverage", "--results-directory", "./coverage") }

Write-Host "dotnet $($argsList -join ' ')" -ForegroundColor Cyan
& dotnet @argsList
exit $LASTEXITCODE
