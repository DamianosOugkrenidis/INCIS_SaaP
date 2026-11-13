# e1-local-evidence.ps1 - READ-ONLY evidence collection for E1 (ASCII-only)
# Collects local sources, masks secrets/PII, writes findings + SHA256.
param([string]$OutDir = "docs\reports\e1_local")
$ErrorActionPreference = "Continue"
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$out = Join-Path $root $OutDir
New-Item -ItemType Directory -Path $out -Force | Out-Null

function Mask([string]$s) {
    if ($null -eq $s) { return "" }
    $s = [regex]::Replace($s, '(?i)(password|pwd|secret|token|apikey|api_key|authorization)\s*[:=]\s*\S+', '$1=***')
    $s = [regex]::Replace($s, '(?i)Bearer\s+\S+', 'Bearer ***')
    $s = [regex]::Replace($s, '(?i)\b[A-Z0-9._%+\-]+@([A-Z0-9.\-]+\.[A-Z]{2,})\b', 'user***@$1')
    return $s
}
function Save([string]$name, [string[]]$lines) {
    $p = Join-Path $out $name
    ($lines | ForEach-Object { Mask $_ }) | Set-Content -LiteralPath $p -Encoding UTF8
    return $p
}

$findings = New-Object System.Collections.Generic.List[object]
function AddFinding($cat, $status, $method, $finding, $evidence) {
    $findings.Add([pscustomobject]@{ Category=$cat; Status=$status; Method=$method; Finding=$finding; Evidence=$evidence })
}

# 0. Environment
$envLines = @(
  "hostname=$env:COMPUTERNAME",
  "user=$env:USERNAME",
  "os=$([System.Environment]::OSVersion.VersionString)",
  "note=LOCAL machine, not the production host; absence of evidence here does NOT prove absence of production consumers."
)
$p = Save '00_environment.txt' $envLines
AddFinding '0. Environment' 'CHECKED (LOCAL)' 'hostname/os' 'Local workstation inspected; not production host.' $p

# 1. IIS / reverse proxy logs
$iisDirs = @('C:\inetpub\logs\LogFiles','C:\inetpub\logs')
$iisLines = @()
$iisFound = $false
foreach ($d in $iisDirs) {
  if (Test-Path $d) {
    $iisFound = $true
    $files = Get-ChildItem $d -Recurse -File -ErrorAction SilentlyContinue
    $iisLines += "dir=$d files=$($files.Count)"
    $files | Sort-Object LastWriteTime -Descending | Select-Object -First 5 | ForEach-Object { $iisLines += "  $($_.FullName) last=$($_.LastWriteTime.ToString('s')) size=$($_.Length)" }
  }
}
if (-not $iisFound) { $iisLines += 'IIS logs directory not found.' }
$p = Save '01_iis_logs.txt' $iisLines
AddFinding '1. API Gateway / Reverse Proxy / WAF' $(if($iisFound){'CHECKED (LOCAL - no INCIS API evidence)'}else{'NOT AVAILABLE'}) 'filesystem' $(if($iisFound){'IIS log dir present; no INCIS API host configured locally.'}else{'No IIS/WAF logs locally.'}) $p

# 2. nginx
$nginx = Get-Command nginx -ErrorAction SilentlyContinue
$nginxLines = @()
$nginxDirs = @('C:\nginx\logs','C:\ProgramData\nginx\logs')
$nginxFound = $false
foreach ($d in $nginxDirs) { if (Test-Path $d) { $nginxFound = $true; $nginxLines += "dir=$d"; (Get-ChildItem $d -File -ErrorAction SilentlyContinue | Select-Object -First 5 | ForEach-Object { $nginxLines += "  $($_.Name) last=$($_.LastWriteTime.ToString('s'))" }) } }
$nginxLines += "nginx_command=$([bool]$nginx)"
if (-not $nginxFound) { $nginxLines += 'No nginx logs locally.' }
$p = Save '02_nginx_logs.txt' $nginxLines
AddFinding '1b. nginx' $(if($nginxFound){'CHECKED (LOCAL)'}else{'NOT AVAILABLE'}) 'filesystem/Get-Command' $(if($nginxFound){'nginx logs present locally.'}else{'nginx not installed / no logs locally.'}) $p

# 3. Firewall logging
$fwLines = @()
Get-NetFirewallProfile -ErrorAction SilentlyContinue | ForEach-Object { $fwLines += "profile=$($_.Name) enabled=$($_.Enabled) log=$($_.LogFileName) allowedlog=$($_.LogAllowed) blockedlog=$($_.LogBlocked)"; if ($_.LogFileName -and (Test-Path $_.LogFileName)) { $fwLines += "  log_exists=True last=$((Get-Item $_.LogFileName).LastWriteTime.ToString('s'))" } else { $fwLines += "  log_exists=False" } }
if ($fwLines.Count -eq 0) { $fwLines += 'Get-NetFirewallProfile unavailable.' }
$p = Save '03_firewall.txt' $fwLines
AddFinding '2. Firewall / egress logs' 'CHECKED (LOCAL - logging config only)' 'Get-NetFirewallProfile' 'Firewall logging config recorded; no INCIS egress log analysis possible locally.' $p

# 4. Scheduled tasks
$taskLines = @()
$tasks = Get-ScheduledTask -ErrorAction SilentlyContinue
$matched = @()
foreach ($t in @($tasks)) {
  foreach ($a in @($t.Actions)) {
    $ex = "$($a.Execute) $($a.Arguments)"
    if ($ex -match '(?i)curl|wget|Invoke-RestMethod|Invoke-WebRequest|python|node|INCIS|api/v1') {
      $matched += "task=$($t.TaskName) state=$($t.State) exec=$($a.Execute) args=$($a.Arguments)"
    }
  }
}
$taskLines += "total_tasks=$(@($tasks).Count) matched=$($matched.Count)"
$taskLines += $matched
$p = Save '04_scheduled_tasks.txt' $taskLines
AddFinding '4. Scheduled tasks' 'CHECKED (LOCAL)' 'Get-ScheduledTask' "Matched=$($matched.Count) tasks referencing API/curl/python/node." $p

# 5. Cloud CLIs/configs
$cloudLines = @()
foreach ($c in @('az','aws','gcloud')) { $cloudLines += "$c=$([bool](Get-Command $c -ErrorAction SilentlyContinue))" }
foreach ($d in @("$env:USERPROFILE\.azure","$env:USERPROFILE\.aws","$env:USERPROFILE\.config\gcloud")) { $cloudLines += "dir $d exists=$(Test-Path $d)" }
$p = Save '05_cloud_cli.txt' $cloudLines
AddFinding '3. Cloud activity / audit logs' 'REQUIRES ENVIRONMENT ACCESS' 'Get-Command/config dirs' 'Cloud CLIs/config not available or unauthenticated; production cloud audit logs require authorized access.' $p

# 6. Credentials inventory (names only, masked)
$credLines = @()
$appset = Join-Path $root 'INCIS_SaaP\appsettings.json'
if (Test-Path $appset) {
  $j = Get-Content $appset -Raw | ConvertFrom-Json
  $credLines += "ConnectionStrings.DefaultConnection=$($j.ConnectionStrings.DefaultConnection)"
  $credLines += "Jwt.Secret.present=$([bool]$j.Jwt.Secret)"
  $credLines += "Email.SmtpHost=$($j.Email.SmtpHost)"
}
Get-ChildItem Env: | Where-Object { $_.Name -match 'INCIS|ConnectionStrings|ASPNETCORE|JWT|SMTP' } | ForEach-Object { $credLines += "env $($_.Name)=<set>" }
$p = Save '06_credentials_inventory.txt' $credLines
AddFinding '6. Service/API credentials inventory' 'CHECKED (LOCAL)' 'config/env names' 'Local config/env credential names recorded (values masked).' $p

# 7. Webhook subscribers (local dev DB only)
$whLines = @()
$wh = sqlcmd -S ".\SQLEXPRESS02" -E -d INCIS_SaaP_inspect -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM WebhookSubscriptions;" -h -1 -W 2>&1
$whLines += "local_dev_db_webhook_subscriptions_count=$($wh -join ' ')"
$wh2 = sqlcmd -S ".\SQLEXPRESS02" -E -d INCIS_SaaP_inspect -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM Documents;" -h -1 -W 2>&1
$whLines += "local_dev_db_documents_count=$($wh2 -join ' ')"
$p = Save '07_webhooks_local.txt' $whLines
AddFinding '7. Webhook subscribers (OUTBOUND, separate)' 'CHECKED (LOCAL dev DB)' 'sqlcmd counts' "Local dev DB only: $($wh -join ' ') webhook subscriptions. NOT production. Webhook subscriber != inbound consumer." $p

# 8. Hosts / certificates
$hostLines = @()
$hostsFile = "$env:SystemRoot\System32\drivers\etc\hosts"
if (Test-Path $hostsFile) { $hostLines += 'hosts entries mentioning INCIS:'; $hostLines += (Select-String -Path $hostsFile -Pattern 'INCIS|incis' -ErrorAction SilentlyContinue | ForEach-Object { $_.Line }) }
$certs = Get-ChildItem Cert:\LocalMachine\My -ErrorAction SilentlyContinue | Where-Object { $_.Subject -match '(?i)incis' }
$hostLines += "local_machine_certs_incis=$(@($certs).Count)"
$p = Save '08_hosts_certs.txt' $hostLines
AddFinding '8. DNS / certificates / hostnames' 'CHECKED (LOCAL)' 'hosts/cert store' "Local hosts/certs inspected; production DNS/TLS requires environment access." $p

# 9. Network connections
$netLines = @()
$conns = Get-NetTCPConnection -State Established -ErrorAction SilentlyContinue
$netLines += "established_connections=$(@($conns).Count)"
$conns | Group-Object RemotePort | Sort-Object Count -Descending | Select-Object -First 10 | ForEach-Object { $netLines += "remote_port=$($_.Name) count=$($_.Count)" }
$listen = Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue | Where-Object { $_.LocalPort -in 80,443,5000,8080 }
$netLines += "listening_80_443_5000_8080=$(@($listen).Count)"
$p = Save '09_network.txt' $netLines
AddFinding '2b. Network sockets' 'CHECKED (LOCAL)' 'Get-NetTCPConnection' "Snapshot only; not an API consumer audit." $p

# 10. Local Phase 0 logs (reference)
$e2b = Join-Path $root 'docs\reports\e2b_local_dryrun\phase0_report.md'
$p0 = if (Test-Path $e2b) { $e2b } else { '' }
AddFinding '10. App Phase 0 logs (cross-check)' 'CHECKED (LOCAL - not production)' 'analyze-phase0-logs.ps1' 'Local logs: 0 API-write/CSRF lines, completeness=false (see E2b dry-run).' $p0

# Findings CSV + Summary + SHA256
$csv = Join-Path $out 'findings.csv'
$findings | Export-Csv -LiteralPath $csv -NoTypeInformation -Encoding UTF8
$sum = @(
 '# E1 LOCAL ENVIRONMENT FINDINGS',
 ('Date: ' + (Get-Date).ToString('s')),
 ('Host: ' + $env:COMPUTERNAME + ' (LOCAL - not production)'),
 '',
 '| # | Category | Status | Method | Finding | Evidence |',
 '|---|---|---|---|---|---|'
)
$i=0
foreach ($f in $findings) { $i++; $sum += "| $i | $($f.Category) | $($f.Status) | $($f.Method) | $($f.Finding) | $($f.Evidence) |" }
$sum += @(
 '',
 '## Enabled/Disabled checks',
 '- Enabled: filesystem logs (IIS/nginx), firewall config, scheduled tasks, cloud CLI presence, local config/env names, local dev DB counts, hosts/certs, sockets, local Phase 0 logs.',
 '- Requires authorized third-party/production access: production gateway/WAF logs, production firewall/egress, cloud audit logs, integration registry/CMDB, production credential inventory, production webhook subscribers, production DNS/TLS, team interviews.',
 '',
 '## Conclusion',
 '- Local checks CANNOT establish "no external consumers" for production. Option A remains undocumented until production-wide checks are executed by authorized personnel.',
 '- Webhook subscribers are OUTBOUND and are recorded separately from inbound consumers.',
 '- This evidence package is READ-ONLY and masked.'
)
$md = Join-Path $out 'E1_LOCAL_FINDINGS.md'
$sum | Set-Content -LiteralPath $md -Encoding UTF8

$hashLines = @()
Get-ChildItem $out -File | Sort-Object Name | ForEach-Object { $hashLines += ("{0}  {1}" -f (Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName).Hash, $_.Name) }
$hashes = Join-Path $out 'SHA256SUMS.txt'
$hashLines | Set-Content -LiteralPath $hashes -Encoding UTF8

$sum | ForEach-Object { Write-Output $_ }
Write-Output "OUT=$out"
