# build-release.ps1 - reproducible release packaging (ASCII-only, PS 5.1)
# Mirrors the repo root layout EXACTLY (so the packaged .sln resolves), with an explicit
# include/exclude manifest. Excludes logs, temp/build artifacts, local data and release ZIPs.
# Produces: <OutputDir>\<Version>.zip, <Version>.zip.sha256, <Version>.MANIFEST.csv
# Then runs scan-release-artifacts.ps1 on the produced ZIP (default: report only).
# NOTE: never deletes or modifies source files. Packaging does not remove stray logs from source.
# Usage:
#   .\build-release.ps1 -Version v18_RC2_20261111
#   .\build-release.ps1 -Version v18_RC2_20261111 -OutputDir .\dist -Scanner
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$Root = (Get-Location).Path,
    [string]$OutputDir = "",
    [switch]$Scanner
)
$ErrorActionPreference = "Stop"
$Root = (Resolve-Path -LiteralPath $Root).Path
if ([string]::IsNullOrWhiteSpace($OutputDir)) { $OutputDir = Join-Path $Root "dist" }
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

# Top-level items to include (repo-relative)
$include = @(
    '.github', '.gitignore', '.dockerignore',
    'INCIS_SaaP', 'INCIS_SaaP.Tests', 'INCIS_SaaP.PlaywrightTests',
    'Database', 'docs', 'tools',
    'INCIS_SaaP.sln', 'run-tests.ps1', 'run-playwright-tests.ps1'
)

# Directory names excluded anywhere
$excludeDirs = @('bin', 'obj', 'node_modules', '.vs', '.git', 'TestResults', 'logs', 'coverage', 'FILES', 'dist')
# File name/glob patterns excluded anywhere
$excludeFiles = @('*.log', '*.bak', '*.bak2', '*.trx', '*.pdb', '*.user', '*.suo', '*.tmp', '*.cache',
                  '*.zip', '*.7z', '*.rar', '*.xls', '*.xlsx', '*.pdf', '*.doc', '*.docx',
                  'launchSettings.json', '.env')

function Test-ExcludedFile([string]$name) {
    foreach ($p in $excludeFiles) { if ($name -like $p) { return $true } }
    return $false
}

$staging = Join-Path $env:TEMP ("incis_release_" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path $staging | Out-Null

$manifest = New-Object System.Collections.ArrayList
function Copy-Tree([string]$relTop) {
    $src = Join-Path $Root $relTop
    if (-not (Test-Path -LiteralPath $src)) { Write-Output "WARN: missing include: $relTop"; return }
    $item = Get-Item -LiteralPath $src
    if (-not $item.PSIsContainer) {
        if (Test-ExcludedFile $item.Name) { return }
        $dst = Join-Path $staging $relTop
        New-Item -ItemType Directory -Force -Path (Split-Path $dst -Parent) | Out-Null
        Copy-Item -LiteralPath $src -Destination $dst -Force
        $h = (Get-FileHash -Algorithm SHA256 -LiteralPath $dst).Hash
        [void]$manifest.Add([pscustomobject]@{ Path = ($relTop -replace '\\', '/'); Size = (Get-Item $dst).Length; SHA256 = $h })
        return
    }
    foreach ($f in Get-ChildItem -LiteralPath $src -Recurse -File -Force) {
        $relative = $f.FullName.Substring($Root.Length).TrimStart('\', '/')
        $segments = $relative -split '\\'
        $skip = $false
        foreach ($seg in $segments[0..([Math]::Max(0, $segments.Count - 2))]) { if ($excludeDirs -contains $seg) { $skip = $true; break } }
        if ($skip) { continue }
        if (Test-ExcludedFile $f.Name) { continue }
        $dst = Join-Path $staging $relative
        New-Item -ItemType Directory -Force -Path (Split-Path $dst -Parent) | Out-Null
        Copy-Item -LiteralPath $f.FullName -Destination $dst -Force
        $h = (Get-FileHash -Algorithm SHA256 -LiteralPath $dst).Hash
        [void]$manifest.Add([pscustomobject]@{ Path = ($relative -replace '\\', '/'); Size = $f.Length; SHA256 = $h })
    }
}

foreach ($inc in $include) { Copy-Tree $inc }

if ($manifest.Count -eq 0) { Write-Output "FAIL: nothing to package"; exit 2 }

# Deterministic output: normalize all staged timestamps so repeated builds hash identically.
$fixedTime = [datetime]::SpecifyKind([datetime]'2026-11-11T00:00:00', [System.DateTimeKind]::Utc)
Get-ChildItem -LiteralPath $staging -Recurse -Force | ForEach-Object { $_.LastWriteTimeUtc = $fixedTime }
(Get-Item -LiteralPath $staging).LastWriteTimeUtc = $fixedTime

$manifestPath = Join-Path $OutputDir ($Version + ".MANIFEST.csv")
$manifest | Sort-Object Path | Export-Csv -LiteralPath $manifestPath -NoTypeInformation -Encoding UTF8

$zipPath = Join-Path $OutputDir ($Version + ".zip")
if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($staging, $zipPath, [System.IO.Compression.CompressionLevel]::Optimal, $false)

$zipHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $zipPath).Hash
$shaPath = Join-Path $OutputDir ($Version + ".zip.sha256")
Set-Content -LiteralPath $shaPath -Value ($zipHash + "  " + ($Version + ".zip")) -Encoding ASCII

$totalBytes = ($manifest | Measure-Object -Property Size -Sum).Sum
Write-Output "BUILT $zipPath"
Write-Output ("  files={0} rawBytes={1} zipSHA256={2}" -f $manifest.Count, $totalBytes, $zipHash)
Write-Output ("  manifest={0}" -f $manifestPath)
Write-Output ("  sha256={0}" -f $shaPath)

if ($Scanner) {
    $scannerPath = Join-Path $Root "tools\scan-release-artifacts.ps1"
    $verifyCmd = 'powershell -File "' + $scannerPath + '" -Path "' + $zipPath + '" -FailOnFindings'
    Write-Output "--- verify the produced ZIP (run as a separate step) ---"
    Write-Output $verifyCmd
}

Remove-Item -LiteralPath $staging -Recurse -Force
exit 0
