# verify-ci-artifacts.ps1 - verify official CI artifacts (ASCII-only for PS 5.1)
# Usage:
#   .\verify-ci-artifacts.ps1 -ResultsDir .\artifacts -MinCoverage 70
#   .\verify-ci-artifacts.ps1 -ResultsDir .\artifacts -MinCoverage 70 -ExpectedCommitSha <sha>
#   .\verify-ci-artifacts.ps1 -ResultsDir .\artifacts -CommitShaFile .\commit.txt
# Exit: 0=PASS, 1=FAIL, 2=missing artifacts.
param(
    [Parameter(Mandatory = $true)][string]$ResultsDir,
    [double]$MinCoverage = 70.0,
    [string]$ExpectedCommitSha = "",
    [string]$CommitShaFile = "",
    [switch]$Strict
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path -LiteralPath $ResultsDir)) { Write-Output "ResultsDir not found: $ResultsDir"; exit 2 }

$allTrx = @(Get-ChildItem -LiteralPath $ResultsDir -Recurse -Filter '*.trx' -ErrorAction SilentlyContinue)
$trx = @($allTrx | Group-Object { (Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName).Hash } | ForEach-Object { $_.Group[0] })
$duplicateTrx = $allTrx.Count - $trx.Count
$cov = Get-ChildItem -LiteralPath $ResultsDir -Recurse -Filter 'ci-coverage.cobertura.xml' -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $cov) { $cov = Get-ChildItem -LiteralPath $ResultsDir -Recurse -Filter 'coverage.cobertura.xml' -ErrorAction SilentlyContinue | Select-Object -First 1 }
if ($trx.Count -eq 0 -or -not $cov) {
    Write-Output "MISSING_ARTIFACTS trx=$($trx.Count) coverage=$([bool]$cov)"
    exit 2
}

$total = 0; $passed = 0; $failed = 0; $notExecuted = 0
foreach ($f in $trx) {
    [xml]$x = Get-Content -LiteralPath $f.FullName -Raw
    $c = $x.TestRun.ResultSummary.Counters
    $total += [int]$c.total
    $passed += [int]$c.passed
    $failed += [int]$c.failed
    $notExecuted += @($x.TestRun.Results.UnitTestResult | Where-Object { $_.outcome -eq 'NotExecuted' }).Count
}

[xml]$cx = Get-Content -LiteralPath $cov.FullName -Raw
$line = [double]$cx.coverage.'line-rate' * 100
$branch = [double]$cx.coverage.'branch-rate' * 100

# TRX consistency: total must equal passed + failed + notExecuted
$consistent = (($passed + $failed + $notExecuted) -eq $total)

# Commit SHA verification (optional but enforced when provided)
$shaOk = $true
$expected = $ExpectedCommitSha
if ([string]::IsNullOrWhiteSpace($expected) -and -not [string]::IsNullOrWhiteSpace($CommitShaFile)) {
    if (Test-Path -LiteralPath $CommitShaFile) { $expected = (Get-Content -LiteralPath $CommitShaFile -Raw).Trim() }
    else { Write-Output "FAIL: CommitShaFile not found: $CommitShaFile"; exit 1 }
}
if (-not [string]::IsNullOrWhiteSpace($expected)) {
    $shaCandidates = @()
    foreach ($t in $trx) {
        $txt = Get-Content -LiteralPath $t.FullName -Raw
        $m = [regex]::Match($txt, '(?i)\b[0-9a-f]{40}\b')
        if ($m.Success) { $shaCandidates += $m.Value }
    }
    if ($shaCandidates.Count -eq 0) {
        Write-Output "NOTE: no 40-hex commit SHA found in TRX (add -ExpectedCommitSha explicitly to enforce)."
    } elseif (-not ($shaCandidates | Where-Object { $_ -eq $expected })) {
        $shaOk = $false
    }
}

Write-Output ("TRX files={0} (duplicates skipped={1}) total={2} passed={3} failed={4} notExecuted={5} consistent={6}" -f $trx.Count, $duplicateTrx, $total, $passed, $failed, $notExecuted, $consistent)
Write-Output ("Coverage file: {0}" -f $cov.Name)
Write-Output ("Coverage line={0:N2}% branch={1:N2}% (min {2:N1}%)" -f $line, $branch, $MinCoverage)
if (-not [string]::IsNullOrWhiteSpace($expected)) { Write-Output ("Commit SHA expected={0} match={1}" -f $expected, $shaOk) }

$ok = ($failed -eq 0) -and ($total -gt 0) -and ($line -ge $MinCoverage) -and $shaOk
if ($Strict -and -not $consistent) { $ok = $false; Write-Output "FAIL(Strict): TRX counters inconsistent" }
if (-not $ok) {
    if ($failed -gt 0) { Write-Output "FAIL: failed tests = $failed" }
    if ($line -lt $MinCoverage) { Write-Output ("FAIL: coverage {0:N2}% < {1:N1}%" -f $line, $MinCoverage) }
    if ($total -eq 0) { Write-Output "FAIL: no tests counted" }
    if (-not $shaOk) { Write-Output "FAIL: commit SHA mismatch" }
    exit 1
}
Write-Output "PASS: artifacts valid (0 failed, coverage >= min, SHA ok)"
exit 0
