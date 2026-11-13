# analyze-phase0-logs.ps1 - H-2/CSRF Phase 0 log analysis (ASCII-only for PS 5.1)
# Usage: .\analyze-phase0-logs.ps1 -LogPath .\logs -SinceDays 14 -OutDir .\phase0-report
# Handles: Serilog/ISO timestamps, 14-day window (by parsed timestamp), malformed lines,
# duplicates, secret/email masking. Produces phase0_summary.csv, phase0_by_endpoint.csv, phase0_report.md.
param(
    [Parameter(Mandatory = $true)][string]$LogPath,
    [int]$SinceDays = 14,
    [string]$OutDir = ".\phase0-report",
    [string[]]$KnownInternalUsers = @(),
    [switch]$ShowMaskedEmails,
    [switch]$NoMaskEmails
)

$ErrorActionPreference = "Stop"
$cutoff = (Get-Date).AddDays(-$SinceDays)
$inv = [System.Globalization.CultureInfo]::InvariantCulture

function Parse-Ts([string]$s) {
    if ([string]::IsNullOrWhiteSpace($s)) { return $null }
    $formats = @(
        'yyyy-MM-dd HH:mm:ss.fff', 'yyyy-MM-dd HH:mm:ss',
        'yyyy-MM-ddTHH:mm:ss.fff', 'yyyy-MM-ddTHH:mm:ss'
    )
    foreach ($f in $formats) {
        $dt = [datetime]::MinValue
        if ([datetime]::TryParseExact($s, $f, $inv, [System.Globalization.DateTimeStyles]::AssumeLocal, [ref]$dt)) { return $dt }
    }
    $dt2 = [datetime]::MinValue
    if ([datetime]::TryParse($s, $inv, [System.Globalization.DateTimeStyles]::AssumeLocal, [ref]$dt2)) { return $dt2 }
    return $null
}

function Mask-Text([string]$v) {
    if ([string]::IsNullOrEmpty($v)) { return $v }
    $r = [regex]::Replace($v, '(?i)\b(token|password|secret|apikey|api_key|authorization)\s*[=:]\s*[^\s,;]+', '$1=***')
    $r = [regex]::Replace($r, '(?i)Bearer\s+[A-Za-z0-9\._\-]+', 'Bearer ***')
    return $r
}

function Mask-Identity([string]$v) {
    if ([string]::IsNullOrWhiteSpace($v)) { return "-" }
    if ($NoMaskEmails) { return $v }
    if ($ShowMaskedEmails -and $v -match '^([^@])[^@]*(@.*)$') { return $Matches[1] + '***' + $Matches[2] }
    # Default: FULL redaction (never expose emails/identities). Stable token for correlation.
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($v)
    $hash = ($sha.ComputeHash($bytes) | ForEach-Object { $_.ToString('x2') }) -join ''
    $sha.Dispose()
    return 'user-' + $hash.Substring(0, 8)
}

$allFiles = if (Test-Path -LiteralPath $LogPath -PathType Container) {
    @(Get-ChildItem -LiteralPath $LogPath -File -Filter 'incis-*.log')
} else { @(Get-Item -LiteralPath $LogPath) }

if ($allFiles.Count -eq 0) {
    New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
    "NO_LOG_FILES: no logs found in $LogPath." | Set-Content -LiteralPath (Join-Path $OutDir 'phase0_report.md') -Encoding UTF8
    Write-Output "NO_LOG_FILES (0)"
    exit 2
}

$policyRx = [regex]'API-write (?<dec>ALLOW|DENY \(enforced\)|PHASE-0 OBSERVATION \(no enforcement\)) (?<method>GET|POST|PUT|PATCH|DELETE) (?<path>/\S+) user=(?<user>\S+) id=(?<id>\S+)(?: matchedRole=(?<role>\S+))?(?: requiredRoles=(?<roles>[^\s]+))?'
$csrfRx   = [regex]'API-CSRF (?<dec>ALLOW|DENY \(enforced\)|PHASE-0 OBSERVATION \(no enforcement\)) (?<method>GET|POST|PUT|PATCH|DELETE) (?<path>/\S+) user=(?<user>\S+) id=(?<id>\S+) header=(?<header>\S+)'
$tsRx     = [regex]'^(?<ts>\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?)'

$rows = New-Object System.Collections.Generic.List[object]
$apiLines = 0; $malformed = 0; $duplicates = 0; $outOfWindow = 0
$seen = @{}

foreach ($f in $allFiles) {
    $currentTs = $null
    foreach ($line in (Get-Content -LiteralPath $f.FullName)) {
        if ($tsRx.IsMatch($line)) { $currentTs = Parse-Ts $tsRx.Match($line).Groups['ts'].Value }

        if ($line -notmatch 'API-(write|CSRF) ') { continue }
        $apiLines++

        $m = $policyRx.Match($line); $cat = 'WRITE-POLICY'
        if (-not $m.Success) { $m = $csrfRx.Match($line); $cat = 'CSRF' }
        if (-not $m.Success) { $malformed++; continue }

        $lineTs = $null
        if ($tsRx.IsMatch($line)) { $lineTs = Parse-Ts $tsRx.Match($line).Groups['ts'].Value }
        if (-not $lineTs) { $lineTs = $currentTs }
        if ($lineTs -and $lineTs -lt $cutoff) { $outOfWindow++; continue }

        $key = ($(if ($lineTs) { $lineTs.ToString('s') } else { '' })) + '|' + (Mask-Text $line)
        if ($seen.ContainsKey($key)) { $duplicates++; continue }
        $seen[$key] = $true

        $user = Mask-Text $m.Groups['user'].Value
        $clientCat = if ($KnownInternalUsers -contains $user) { 'internal' } else { 'external-or-unknown' }

        $rows.Add([pscustomobject]@{
            Timestamp = $(if ($lineTs) { $lineTs.ToString('yyyy-MM-dd HH:mm:ss') } else { '' })
            Category  = $cat
            Decision  = $m.Groups['dec'].Value
            Method    = $m.Groups['method'].Value
            Path      = Mask-Text $m.Groups['path'].Value
            User      = (Mask-Identity $user)
            UserId    = Mask-Text $m.Groups['id'].Value
            Role      = $m.Groups['role'].Value
            Required  = $m.Groups['roles'].Value
            Header    = $m.Groups['header'].Value
            ClientCat = $clientCat
        })
    }
}

New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
$rows | Export-Csv -LiteralPath (Join-Path $OutDir 'phase0_summary.csv') -NoTypeInformation -Encoding UTF8

$byEndpoint = $rows | Group-Object Category, Method, Path, Decision |
    ForEach-Object {
        $p = $_.Name -split ', '
        [pscustomobject]@{ Category = $p[0]; Method = $p[1]; Path = $p[2]; Decision = $p[3]; Count = $_.Count }
    } | Sort-Object Category, Method, Path, Decision
$byEndpoint | Export-Csv -LiteralPath (Join-Path $OutDir 'phase0_by_endpoint.csv') -NoTypeInformation -Encoding UTF8

$obs = @($rows | Where-Object { $_.Decision -like 'PHASE-0*' })
$den = @($rows | Where-Object { $_.Decision -like 'DENY*' })
$allow = @($rows | Where-Object { $_.Decision -eq 'ALLOW' })
$tsList = @($rows | Where-Object { $_.Timestamp } | ForEach-Object { $_.Timestamp } | Sort-Object)
$window = if ($tsList.Count) { "$($tsList[0]) -> $($tsList[-1])" } else { "(no parseable timestamps)" }
$daysCovered = if ($tsList.Count -ge 2) {
    ([datetime]::Parse($tsList[-1]) - [datetime]::Parse($tsList[0])).TotalDays
} else { 0 }
$complete = ($daysCovered -ge ($SinceDays - 1))

$md = @(
    "# Phase 0 Log Analysis Report",
    "",
    "Requested window: last $SinceDays days (cutoff $($cutoff.ToString('s')))",
    "Log window (parsed): $window",
    "Days covered by data: $([math]::Round($daysCovered,1)) / $SinceDays (complete=$complete)",
    "",
    "## Data quality",
    "| Metric | Value |",
    "|---|---|",
    "| Files | $($allFiles.Count) |",
    "| API-write/CSRF lines | $apiLines |",
    "| Parsed rows (in window) | $($rows.Count) |",
    "| Malformed (unparsable) | $malformed |",
    "| Duplicates removed | $duplicates |",
    "| Out of window | $outOfWindow |",
    "",
    "## Summary",
    "| Category | Count |",
    "|---|---|",
    "| ALLOW (authorized) | $($allow.Count) |",
    "| PHASE-0 OBSERVATION (would be denied) | $($obs.Count) |",
    "| DENY (enforced) | $($den.Count) |",
    "",
    "## By endpoint / method / decision",
    "| Category | Method | Path | Decision | Count |",
    "|---|---|---|---|---|"
)
foreach ($r in $byEndpoint) { $md += "| $($r.Category) | $($r.Method) | $($r.Path) | $($r.Decision) | $($r.Count) |" }
$md += @(
    "",
    "## Decision criteria",
    "- Cleared for H-2/CSRF Phase 1: ZERO PHASE-0 OBSERVATION from roles outside Admin/SuperUser for the full window AND written PO confirmation.",
    "- Any OBSERVATION => identify client/role before enabling.",
    "- If completeness=false (data do not span $SinceDays days) => EXTEND observation; do not conclude approval.",
    "- ABSENCE of calls does NOT prove absence of external consumers.",
    "",
    "## Privacy / limitations",
    "- Identities are FULLY redacted by default (user-<hash>) - no emails exported. Use -ShowMaskedEmails only if explicitly approved.",
    "- Token-like values masked.",
    "- Duplicates/malformed lines are reported, not silently dropped.",
    "- Analysis is limited to the provided logs (no server-side correlation)."
)
$md | Set-Content -LiteralPath (Join-Path $OutDir 'phase0_report.md') -Encoding UTF8

Write-Output ("OK rows={0} allow={1} observation={2} deny={3} malformed={4} dup={5} outOfWindow={6} days={7:N1} complete={8} out={9}" -f $rows.Count,$allow.Count,$obs.Count,$den.Count,$malformed,$duplicates,$outOfWindow,$daysCovered,$complete,$OutDir)
exit 0
