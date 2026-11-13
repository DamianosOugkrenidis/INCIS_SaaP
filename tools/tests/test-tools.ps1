# test-tools.ps1 - executable tests for tools/*.ps1 (ASCII-only for PS 5.1)
# Usage: powershell -ExecutionPolicy Bypass -File .\tools\tests\test-tools.ps1
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$analyzer = Join-Path $root 'tools\analyze-phase0-logs.ps1'
$verifier = Join-Path $root 'tools\verify-ci-artifacts.ps1'
$tmp = Join-Path $env:TEMP ('incis-tooltests-' + [guid]::NewGuid().ToString('N').Substring(0,8))
New-Item -ItemType Directory -Path $tmp -Force | Out-Null

$failures = 0
function Check([bool]$cond, [string]$msg) {
    if ($cond) { Write-Output "PASS: $msg" } else { Write-Output "FAIL: $msg"; $script:failures++ }
}
function Run-Tool([string]$file, [string[]]$arguments) {
    $out = & powershell -NoProfile -ExecutionPolicy Bypass -File $file @arguments 2>&1 | Out-String
    return @{ Out = $out; Code = $LASTEXITCODE }
}

# ---------------- analyze: missing logs ----------------
$missing = Join-Path $tmp 'nologs'
New-Item -ItemType Directory -Path $missing -Force | Out-Null
$r = Run-Tool $analyzer @('-LogPath', $missing, '-SinceDays', '14', '-OutDir', (Join-Path $tmp 'out-missing'))
Check ($r.Code -eq 2) "analyzer missing logs -> exit 2 (got $($r.Code))"

# ---------------- analyze: malformed/duplicate/out-of-window/masking/timestamps ----------------
$logs = Join-Path $tmp 'logs'
New-Item -ItemType Directory -Path $logs -Force | Out-Null
$now = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss.fff')
$old = (Get-Date).AddDays(-40).ToString('yyyy-MM-dd HH:mm:ss.fff')
$lines = @(
    "$now [WRN] (filter)",
    "  API-write PHASE-0 OBSERVATION (no enforcement) POST /api/v1/incis user=ext@example.com id=1 requiredRoles=Admin,SuperUser",
    "$now [WRN] (filter)",
    "  API-write PHASE-0 OBSERVATION (no enforcement) POST /api/v1/incis user=ext@example.com id=1 requiredRoles=Admin,SuperUser",
    "$now [WRN] (filter)",
    "  API-write THIS IS MALFORMED",
    "$now [WRN] (filter)",
    "  API-write ALLOW POST /api/v1/clients user=admin@incis.local id=2 matchedRole=Admin token=SUPERSECRET",
    "$($now.Replace(' ','T')) [WRN] (filter)",
    "  API-CSRF PHASE-0 OBSERVATION (no enforcement) POST /api/v1/incis user=iso@example.com id=3 header=X-CSRF-TOKEN",
    "$old [WRN] (filter)",
    "  API-write PHASE-0 OBSERVATION (no enforcement) DELETE /api/v1/incis/9 user=old@example.com id=4 requiredRoles=Admin,SuperUser"
)
$lines | Set-Content -LiteralPath (Join-Path $logs 'incis-test.log') -Encoding UTF8
$outDir = Join-Path $tmp 'out'
$r = Run-Tool $analyzer @('-LogPath', $logs, '-SinceDays', '14', '-OutDir', $outDir)
Check ($r.Code -eq 0) "analyzer valid run -> exit 0 (got $($r.Code))"
Check ($r.Out -match 'malformed=1') "analyzer counts malformed=1"
Check ($r.Out -match 'dup=1') "analyzer removes duplicate=1"
Check ($r.Out -match 'outOfWindow=1') "analyzer excludes out-of-window=1"
$csv = Get-Content (Join-Path $outDir 'phase0_summary.csv') -Raw
Check ($csv -notmatch 'SUPERSECRET') "analyzer masks token-like values"
Check ($csv -notmatch '@example\.com') "analyzer does NOT export emails (full redaction)"
Check ($csv -match 'user-[0-9a-f]{8}') "analyzer uses stable redacted identity"
$report = Get-Content (Join-Path $outDir 'phase0_report.md') -Raw
Check ($report -match 'Days covered') "report includes window completeness"

# opt-in masked emails
$outDir2 = Join-Path $tmp 'out-showmasked'
$r = Run-Tool $analyzer @('-LogPath', $logs, '-SinceDays', '14', '-OutDir', $outDir2, '-ShowMaskedEmails')
$csv2 = Get-Content (Join-Path $outDir2 'phase0_summary.csv') -Raw
Check ($csv2 -match 'e\*\*\*@example\.com') "analyzer -ShowMaskedEmails masks local-part only"

# ---------------- publish safety scanner: exit-code contract ----------------
$pub = Join-Path $root 'tools\verify-repo-publish.ps1'
$clean = Join-Path $tmp 'pub-clean'
New-Item -ItemType Directory -Path (Join-Path $clean 'docs') -Force | Out-Null
'# clean' | Set-Content -LiteralPath (Join-Path $clean 'docs\readme.md') -Encoding UTF8

# 2 = NOT_AUTHORIZED (clean, no authorization) - must NOT be treated as success
$r = Run-Tool $pub @('-Root', $clean)
Check ($r.Code -eq 2) "clean without auth -> exit 2"
Check ($r.Out -match 'EXIT=2 REASON=NOT_AUTHORIZED') "exit contract prints EXIT=2 NOT_AUTHORIZED"

# 0 = PASS (clean + authorized + valid inputs)
$r = Run-Tool $pub @('-Root', $clean, '-OwnerAuthorized', '-RepoUrl', 'https://example.com/org/x.git', '-Branch', 'main')
Check ($r.Code -eq 0) "clean + authorized -> exit 0"
Check ($r.Out -match 'EXIT=0 REASON=PASS') "exit contract prints EXIT=0 PASS"
Check ($r.Out -match 'Suggested commands') "prints commands (does not execute git)"

# 3 = INVALID_INPUT
$r = Run-Tool $pub @('-Root', (Join-Path $tmp 'does-not-exist'))
Check ($r.Code -eq 3) "missing root -> exit 3"
Check ($r.Out -match 'EXIT=3 REASON=ROOT_NOT_FOUND') "exit contract ROOT_NOT_FOUND"
$r = Run-Tool $pub @('-Root', $clean, '-OwnerAuthorized', '-RepoUrl', 'not-a-url', '-Branch', 'main')
Check ($r.Code -eq 3) "invalid repo URL -> exit 3"
Check ($r.Out -match 'INVALID_REPO_URL') "exit contract INVALID_REPO_URL"
$r = Run-Tool $pub @('-Root', $clean, '-OwnerAuthorized', '-RepoUrl', 'https://example.com/org/x.git', '-Branch', 'bad..branch')
Check ($r.Code -eq 3) "invalid branch -> exit 3"
Check ($r.Out -match 'INVALID_BRANCH') "exit contract INVALID_BRANCH"
$r = Run-Tool $pub @('-Root', $clean, '-OwnerAuthorized')
Check ($r.Code -eq 3) "owner auth without repo URL -> exit 3"
Check ($r.Out -match 'OWNER_AUTH_WITHOUT_REPO_URL') "exit contract OWNER_AUTH_WITHOUT_REPO_URL"

# 1 = SECURITY_FINDINGS (always blocks; authorization never bypasses)
$dirty = Join-Path $tmp 'pub-dirty'
New-Item -ItemType Directory -Path $dirty -Force | Out-Null
'{ "ConnectionStrings": { "DefaultConnection": "Server=x;User Id=sa;Password=SuperSecret123;" } }' |
    Set-Content -LiteralPath (Join-Path $dirty 'appsettings.json') -Encoding UTF8
$r = Run-Tool $pub @('-Root', $dirty)
Check ($r.Code -eq 1) "secret without auth -> exit 1"
Check ($r.Out -match 'EXIT=1 REASON=SECURITY_FINDINGS') "exit contract SECURITY_FINDINGS"
$r = Run-Tool $pub @('-Root', $dirty, '-OwnerAuthorized', '-RepoUrl', 'https://example.com/org/x.git', '-Branch', 'main')
Check ($r.Code -eq 1) "authorization does NOT bypass secret scan (still exit 1)"

# forbidden/local-only paths are excluded (bin, logs, backups, dev config)
$forbid = Join-Path $tmp 'pub-forbidden'
New-Item -ItemType Directory -Path (Join-Path $forbid 'bin') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $forbid 'Database') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $forbid 'INCIS_SaaP\logs') -Force | Out-Null
'x' | Set-Content -LiteralPath (Join-Path $forbid 'bin\app.dll') -Encoding UTF8
'x' | Set-Content -LiteralPath (Join-Path $forbid 'Database\x.bak') -Encoding UTF8
'x' | Set-Content -LiteralPath (Join-Path $forbid 'INCIS_SaaP\logs\a.log') -Encoding UTF8
'{}' | Set-Content -LiteralPath (Join-Path $forbid 'appsettings.Development.json') -Encoding UTF8
'x' | Set-Content -LiteralPath (Join-Path $forbid 'data.xls') -Encoding UTF8
'x' | Set-Content -LiteralPath (Join-Path $forbid 'manual.pdf') -Encoding UTF8
'x' | Set-Content -LiteralPath (Join-Path $forbid 'doc.docx') -Encoding UTF8
'x' | Set-Content -LiteralPath (Join-Path $forbid 'root.log') -Encoding UTF8
'ok' | Set-Content -LiteralPath (Join-Path $forbid 'ok.md') -Encoding UTF8
$r = Run-Tool $pub @('-Root', $forbid)
Check ($r.Out -match 'EXCLUDED: 8') "excludes bin/logs/backups/dev-config/xls/pdf/docx/log (8 files)"
Check ($r.Code -eq 2) "excluded-only tree is clean but unauthorized -> exit 2"

# CI workflow is NOT allowlisted: a secret there must be detected
$ci = Join-Path $tmp 'pub-ci'
New-Item -ItemType Directory -Path (Join-Path $ci '.github\workflows') -Force | Out-Null
'SA_PASSWORD=RealProductionSecret123' | Set-Content -LiteralPath (Join-Path $ci '.github\workflows\ci.yml') -Encoding UTF8
$r = Run-Tool $pub @('-Root', $ci)
Check ($r.Code -eq 1) "secret in .github/workflows/ci.yml detected (no allowlist bypass)"
Check ($r.Out -match 'FINDING: .github/workflows/ci.yml') "reports ci.yml finding"

# manifest for owner review
$man = Join-Path $tmp 'publish_candidates.csv'
$r = Run-Tool $pub @('-Root', $clean, '-ManifestPath', $man)
Check (Test-Path $man) "candidate manifest generated"
Check (((Get-Content $man | Measure-Object).Count) -ge 2) "manifest contains header + candidates"

# .gitignore contains critical exclusions (static check)
$gi = Get-Content (Join-Path $root '.gitignore') -Raw
Check ($gi -match '(?m)^\*\*/bin/') ".gitignore excludes bin"
Check ($gi -match 'appsettings\.Development\.json') ".gitignore excludes dev config"
Check ($gi -match '\*\*/\*\.bak') ".gitignore excludes backups"
Check ($gi -match '\.env') ".gitignore excludes .env"

# ---------------- verifier: fixtures ----------------
function New-CiFixture([int]$total, [int]$passed, [string]$sha) {
    $dir = Join-Path $tmp ('ci-' + [guid]::NewGuid().ToString('N').Substring(0,8))
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    $results = ''
    for ($i = 0; $i -lt $passed; $i++) { $results += '<UnitTestResult outcome="Passed" />' }
    $trx = @"
<?xml version="1.0" encoding="utf-8"?>
<TestRun name="$sha">
  <Results>$results</Results>
  <ResultSummary><Counters total="$total" executed="$total" passed="$passed" failed="0" notExecuted="0" /></ResultSummary>
</TestRun>
"@
    $trx | Set-Content -LiteralPath (Join-Path $dir 'test.trx') -Encoding UTF8
    '<coverage line-rate="0.90" branch-rate="0.50"></coverage>' | Set-Content -LiteralPath (Join-Path $dir 'coverage.cobertura.xml') -Encoding UTF8
    return $dir
}

$sha = '0123456789abcdef0123456789abcdef01234567'
$d1 = New-CiFixture 3 3 $sha
$r = Run-Tool $verifier @('-ResultsDir', $d1, '-MinCoverage', '70', '-ExpectedCommitSha', $sha)
Check ($r.Code -eq 0) "verifier valid artifacts + SHA -> PASS (got $($r.Code))"

# duplicate TRX (same content) must be deduped
Copy-Item (Join-Path $d1 'test.trx') (Join-Path $d1 'test-copy.trx')
$r = Run-Tool $verifier @('-ResultsDir', $d1, '-MinCoverage', '70')
Check ($r.Code -eq 0) "verifier dedupes identical TRX -> PASS"
Check ($r.Out -match 'duplicates skipped=1') "verifier reports duplicate TRX"

# SHA mismatch must fail
$r = Run-Tool $verifier @('-ResultsDir', $d1, '-MinCoverage', '70', '-ExpectedCommitSha', 'ffffffffffffffffffffffffffffffffffffffff')
Check ($r.Code -eq 1) "verifier SHA mismatch -> FAIL (got $($r.Code))"

# inconsistent counters under -Strict must fail
$d2 = New-CiFixture 5 2 $sha
$r = Run-Tool $verifier @('-ResultsDir', $d2, '-MinCoverage', '70', '-Strict')
Check ($r.Code -eq 1) "verifier inconsistent counters + Strict -> FAIL (got $($r.Code))"

# missing artifacts must exit 2
$empty = Join-Path $tmp 'empty'
New-Item -ItemType Directory -Path $empty -Force | Out-Null
$r = Run-Tool $verifier @('-ResultsDir', $empty)
Check ($r.Code -eq 2) "verifier missing artifacts -> exit 2 (got $($r.Code))"

Write-Output ("RESULT: failures=$failures")
try { Remove-Item -Recurse -Force $tmp } catch { }
if ($failures -gt 0) { exit 1 } else { exit 0 }


