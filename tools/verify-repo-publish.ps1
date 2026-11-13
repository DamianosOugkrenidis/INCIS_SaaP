# verify-repo-publish.ps1 - PRE-PUSH SAFETY GATE (ASCII-only for PS 5.1)
#
# EXIT CODE CONTRACT (machine-readable):
#   0 = PASS              : scan clean AND owner authorized AND inputs valid. No git executed.
#   1 = SECURITY_FINDINGS : secrets/credentials (or tracked-file violations) found. ALWAYS blocks,
#                           even when -OwnerAuthorized is supplied (authorization never bypasses scan).
#   2 = NOT_AUTHORIZED    : scan clean, but -OwnerAuthorized was not supplied. NOT a success.
#   3 = INVALID_INPUT     : Root missing, or invalid RepoUrl/Branch, or OwnerAuthorized without valid inputs.
#
# Usage:
#   .\verify-repo-publish.ps1 -Root .
#   .\verify-repo-publish.ps1 -Root . -OwnerAuthorized -RepoUrl https://github.com/org/repo.git -Branch main
#   .\verify-repo-publish.ps1 -Root . -ManifestPath .\publish_candidates.csv
param(
    [string]$Root = ".",
    [string]$RepoUrl = "",
    [string]$Branch = "main",
    [switch]$OwnerAuthorized,
    [string]$ManifestPath = ""
)

$ErrorActionPreference = "Stop"

function Finish([int]$code, [string]$reason) {
    Write-Output ("EXIT={0} REASON={1}" -f $code, $reason)
    exit $code
}

if (-not (Test-Path -LiteralPath $Root)) { Finish 3 "ROOT_NOT_FOUND" }
$rootFull = (Resolve-Path -LiteralPath $Root).Path

# ---- Input validation (only when provided / required) ----
$urlRx = [regex]'^https://[^/\s]+/[^/\s]+/[^/\s]+?(\.git)?$'
$branchRx = [regex]'^[A-Za-z0-9][A-Za-z0-9._\-/]{0,199}$'
if (-not [string]::IsNullOrWhiteSpace($RepoUrl) -and -not $urlRx.IsMatch($RepoUrl)) { Finish 3 "INVALID_REPO_URL" }
if ([string]::IsNullOrWhiteSpace($Branch) -or $Branch.Contains('..') -or $Branch.StartsWith('-') -or -not $branchRx.IsMatch($Branch)) { Finish 3 "INVALID_BRANCH" }
if ($OwnerAuthorized -and [string]::IsNullOrWhiteSpace($RepoUrl)) { Finish 3 "OWNER_AUTH_WITHOUT_REPO_URL" }

$forbiddenPathRx = [regex]'(?i)(^|\\|/)(bin|obj|logs|coverage|TestResults|\.vs|\.git|publish)(\\|/)|\.(bak|mdf|ldf|zip|rar|trx|cobertura\.xml|pfx|p12|key|pem|db|sqlite3?|log|xls|xlsx|pdf|docx?)$|(^|\\|/)FILES(\\|/)|(^|\\|/)\.env(\.|$)'
$localOnlyRx = [regex]'(?i)(^|/)(appsettings\.Development\.json|\.env(\.|$)|.*\.local\.json)$'
# PII patterns (reported; allowlisted domains/paths are exempt)
$emailRx = [regex]'(?i)\b[A-Z0-9._%+\-]+@([A-Z0-9.\-]+\.[A-Z]{2,})\b'
$phoneRx = [regex]'(?<![\d])(?:\+30[\s\-]?)?69\d{8}(?![\d])'
$ibanRx = [regex]'(?i)\bGR\d{2}[\s]?(?:\d{4}[\s]?){5}\d{3}\b'
$allowedEmailDomains = @('example.com','example.org','incis.local','test.local','ci.local','contoso.com','localhost')
$secretRx = [regex]'(?i)[\"''][^\"'']*\b(Password|Pwd)\s*=\s*(?!\$\{)[^;\"''\s]{3,}|\b(SA_PASSWORD|MSSQL_SA_PASSWORD|AdminSeed__Password)\s*=\s*"?\s*(?!\$\{)[^\s"'']{3,}|gho_[A-Za-z0-9]{20,}|AKIA[0-9A-Z]{16}|-----BEGIN [A-Z ]*PRIVATE KEY-----'
$jwtRx = [regex]'(?i)"Secret"\s*:\s*"(?!REPLACE|YOUR_|CHANGE|PLACEHOLDER)[^"]{16,}"'
$connRx = [regex]'(?i)"DefaultConnection"\s*:\s*"[^"]*(Password|User Id)=[^"]*"'
# Explicit allowlist: tool tests with dummy fixtures and the scanner itself.
# NOTE: CI workflow is intentionally NOT allowlisted; its SQL credential comes from a GitHub secret.
$allowlistRx = [regex]'(?i)^(tools/verify-repo-publish\.ps1|tools/tests/.*)$'

$all = @(Get-ChildItem -LiteralPath $rootFull -Recurse -File -Force -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '(?i)\\(\.git)\\' })

$forbidden = @(); $candidates = @()
foreach ($f in $all) {
    $rel = $f.FullName.Substring($rootFull.Length).TrimStart('\','/').Replace('\','/')
    if ($localOnlyRx.IsMatch($rel) -or $forbiddenPathRx.IsMatch($rel)) {
        $forbidden += [pscustomobject]@{ File = $rel; SizeMB = [math]::Round($f.Length/1MB,2) }
    } else { $candidates += $f }
}

$sizeMB = [math]::Round((($candidates | Measure-Object Length -Sum).Sum)/1MB, 1)
Write-Output ("CANDIDATES: {0} files, {1} MB" -f $candidates.Count, $sizeMB)
Write-Output ("EXCLUDED: {0} (bin/obj/logs/coverage/TestResults/backups/archives/local-config)" -f $forbidden.Count)
foreach ($x in ($forbidden | Sort-Object SizeMB -Descending | Select-Object -First 12)) {
    Write-Output ("  EXCLUDED: {0} ({1} MB)" -f $x.File, $x.SizeMB)
}

# ---- Optional review manifest ----
if (-not [string]::IsNullOrWhiteSpace($ManifestPath)) {
    $candidates | ForEach-Object {
        $rel = $_.FullName.Substring($rootFull.Length).TrimStart('\','/').Replace('\','/')
        [pscustomobject]@{ Path = $rel; SizeKB = [math]::Round($_.Length/1KB,1) }
    } | Sort-Object Path | Export-Csv -LiteralPath $ManifestPath -NoTypeInformation -Encoding UTF8
    Write-Output ("MANIFEST: {0}" -f $ManifestPath)
}

# ---- Secret scan (authorization never bypasses this) ----
$findings = @(); $allowlisted = @()
foreach ($f in $candidates) {
    $rel = $f.FullName.Substring($rootFull.Length).TrimStart('\','/').Replace('\','/')
    if ($allowlistRx.IsMatch($rel)) { $allowlisted += $rel; continue }
    if ($f.Length -gt 2MB) { continue }
    if ($f.Extension -notin @('.cs','.json','.yml','.yaml','.ps1','.sql','.md','.config','.cshtml','.xml','.txt')) { continue }
    $text = Get-Content -LiteralPath $f.FullName -Raw -ErrorAction SilentlyContinue
    if ($null -eq $text) { continue }
    if ($secretRx.IsMatch($text)) { $findings += [pscustomobject]@{ File = $rel; Pattern = 'secret' } }
    if ($jwtRx.IsMatch($text)) { $findings += [pscustomobject]@{ File = $rel; Pattern = 'jwt-secret' } }
    if ($connRx.IsMatch($text)) { $findings += [pscustomobject]@{ File = $rel; Pattern = 'connection-string-with-credentials' } }
    # Test fixtures contain fake PII; allowlist PII (but NOT secrets) for test sources.
    $piiAllowedPath = $rel -match '^INCIS_SaaP\.Tests/'
    if ($f.Extension -ne '.cshtml' -and -not $piiAllowedPath) {
        foreach ($m in $emailRx.Matches($text)) {
            $domain = $m.Groups[1].Value.ToLowerInvariant()
            if ($allowedEmailDomains -notcontains $domain) { $findings += [pscustomobject]@{ File = $rel; Pattern = 'pii-email' } }
        }
    }
    if (-not $piiAllowedPath) {
        if ($phoneRx.IsMatch($text)) { $findings += [pscustomobject]@{ File = $rel; Pattern = 'pii-phone' } }
        if ($ibanRx.IsMatch($text)) { $findings += [pscustomobject]@{ File = $rel; Pattern = 'pii-iban' } }
    }
}

# ---- Tracked-file check (when a git repo exists) ----
$trackedFindings = @()
$gitDir = Join-Path $rootFull '.git'
if (Test-Path -LiteralPath $gitDir) {
    $git = Get-Command git -ErrorAction SilentlyContinue
    if ($git) {
        $tracked = & git -C $rootFull ls-files 2>$null
        foreach ($t in @($tracked)) {
            $rel = ($t -replace '\\','/')
            if ($forbiddenPathRx.IsMatch($rel) -or $localOnlyRx.IsMatch($rel)) {
                $trackedFindings += [pscustomobject]@{ File = $rel; Pattern = 'tracked-forbidden-path' }
            }
        }
        Write-Output ("TRACKED_CHECK: {0} tracked files scanned" -f @($tracked).Count)
    } else { Write-Output "TRACKED_CHECK: skipped (git not found)" }
} else {
    Write-Output "TRACKED_CHECK: skipped (not a git repository); full-tree scan applied instead"
}

if ($findings.Count -gt 0 -or $trackedFindings.Count -gt 0) {
    Write-Output ("SECRET FINDINGS: {0} files, TRACKED VIOLATIONS: {1}" -f $findings.Count, $trackedFindings.Count)
    foreach ($g in (@($findings) + @($trackedFindings) | Sort-Object File -Unique)) {
        Write-Output ("  FINDING: {0} [{1}]" -f $g.File, $g.Pattern)
    }
    Finish 1 "SECURITY_FINDINGS"
}
if ($allowlisted.Count -gt 0) {
    Write-Output ("ALLOWLISTED (ephemeral CI credential / tool tests / scanner): {0}" -f (($allowlisted | Sort-Object -Unique) -join ', '))
}

if (-not $OwnerAuthorized) { Finish 2 "NOT_AUTHORIZED" }

Write-Output "RESULT: PASS (clean + authorized). Suggested commands (NOT executed):"
Write-Output "  git init"
Write-Output "  git add .gitignore .github INCIS_SaaP INCIS_SaaP.Tests INCIS_SaaP.PlaywrightTests Database/Scripts docs tools run-tests.ps1 run-playwright-tests.ps1"
Write-Output "  git status"
Write-Output ("  git checkout -b {0}" -f $Branch)
Write-Output "  git commit -m `"INCIS SaaP v16 (sanitized)`""
Write-Output ("  git remote add origin {0}" -f $RepoUrl)
Write-Output ("  git push -u origin {0}" -f $Branch)
Write-Output "Then: verify commit SHA and run the ci.yml workflow."
Finish 0 "PASS"
