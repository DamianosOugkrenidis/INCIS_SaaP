# load-test.ps1 - OUT-OF-PROCESS load/stability smoke (ASCII-only, PS 5.1 compatible)
param(
    [int]$Requests = 200,
    [int]$Concurrency = 20,
    [int]$SampleCount = 50,
    [int]$DurationSeconds = 0,
    [string]$OutFile = "docs\reports\load_out_of_process.md"
)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$dll = Join-Path $root 'INCIS_SaaP\bin\Debug\net9.0\INCIS_SaaP.dll'
if (-not (Test-Path $dll)) { throw "App DLL not found: $dll (build first)" }

$dbName = 'INCIS_SaaP_Load_' + [guid]::NewGuid().ToString('N').Substring(0,8)
$conn = "Server=.\SQLEXPRESS02;Database=$dbName;Trusted_Connection=True;TrustServerCertificate=True;"

$l = New-Object System.Net.Sockets.TcpListener([System.Net.IPAddress]::Loopback, 0)
$l.Start(); $port = $l.LocalEndpoint.Port; $l.Stop()
$base = "http://127.0.0.1:$port"

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = 'dotnet'
$psi.Arguments = '"' + $dll + '"'
$psi.UseShellExecute = $false
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.WorkingDirectory = Split-Path $dll
$psi.EnvironmentVariables['ASPNETCORE_ENVIRONMENT'] = 'Development'
$psi.EnvironmentVariables['ASPNETCORE_URLS'] = $base
$psi.EnvironmentVariables['ConnectionStrings__DefaultConnection'] = $conn
$psi.EnvironmentVariables['INCIS_DB_CONNECTION_STRING'] = $conn
$psi.EnvironmentVariables['DisableBackgroundServices'] = 'true'
$psi.EnvironmentVariables['AdminSeed__Email'] = 'load.admin@incis.local'
$psi.EnvironmentVariables['AdminSeed__Password'] = 'Load#2026!Aa'
$psi.EnvironmentVariables['Jwt__Secret'] = 'LoadTestSecretKey_1234567890_MustBeLongEnough'
$psi.EnvironmentVariables['DOTNET_ROLL_FORWARD'] = 'Major'

$proc = [System.Diagnostics.Process]::Start($psi)
$proc.BeginOutputReadLine(); $proc.BeginErrorReadLine()

function WaitHealthy([int]$timeoutSec) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $timeoutSec) {
        try {
            $r = Invoke-WebRequest -Uri "$base/health" -UseBasicParsing -TimeoutSec 5
            if ($r.StatusCode -eq 200) { return $true }
        } catch { }
        Start-Sleep -Milliseconds 800
    }
    return $false
}

try {
    if (-not (WaitHealthy 120)) { throw "App did not become healthy within 120s" }
    Start-Sleep -Seconds 2
    $proc.Refresh()
    $cpu0 = $proc.TotalProcessorTime
    $ram0 = $proc.WorkingSet64

    # Sequential latency sample
    $lat = New-Object System.Collections.Generic.List[double]
    $fail = 0
    for ($i = 0; $i -lt $SampleCount; $i++) {
        $sw = [Diagnostics.Stopwatch]::StartNew()
        try { $r = Invoke-WebRequest -Uri "$base/health" -UseBasicParsing -TimeoutSec 30; if ($r.StatusCode -ne 200) { $fail++ } } catch { $fail++ }
        $sw.Stop(); $lat.Add($sw.Elapsed.TotalMilliseconds)
    }
    $sorted = @($lat | Sort-Object)
    $p50 = [math]::Round($sorted[[int]($sorted.Count * 0.50)], 1)
    $p95 = [math]::Round($sorted[[Math]::Min($sorted.Count - 1, [int]($sorted.Count * 0.95) - 1)], 1)

    # Parallel throughput using runspace pool (optionally sustained for DurationSeconds)
    $ok = 0; $totalReq = 0
    $sw = [Diagnostics.Stopwatch]::StartNew()
    do {
        $pool = [runspacefactory]::CreateRunspacePool(1, $Concurrency)
        $pool.Open()
        $handles = New-Object System.Collections.Generic.List[object]
        for ($i = 0; $i -lt $Requests; $i++) {
            $ps = [powershell]::Create()
            $ps.RunspacePool = $pool
            [void]$ps.AddScript({
                param($u)
                try { (Invoke-WebRequest -Uri $u -UseBasicParsing -TimeoutSec 30).StatusCode } catch { 0 }
            }).AddArgument("$base/health")
            $handles.Add([pscustomobject]@{ Ps = $ps; Handle = $ps.BeginInvoke() })
        }
        foreach ($h in $handles) {
            $res = $h.Ps.EndInvoke($h.Handle)
            if ($res -contains 200) { $ok++ }
            $h.Ps.Dispose(); $totalReq++
        }
        $pool.Close(); $pool.Dispose()
    } while ($DurationSeconds -gt 0 -and $sw.Elapsed.TotalSeconds -lt $DurationSeconds)
    $sw.Stop()
    $rps = [math]::Round($totalReq / $sw.Elapsed.TotalSeconds, 1)

    $proc.Refresh()
    $cpu = [math]::Round(($proc.TotalProcessorTime - $cpu0).TotalSeconds, 2)
    $ram = [math]::Round($proc.WorkingSet64 / 1MB, 1)

    $lines = @(
        '# Out-of-process load/stability smoke',
        '',
        ('Date: ' + (Get-Date).ToString('s')),
        ('App: ' + $base + ' (Kestrel, isolated DB ' + $dbName + ')'),
        '',
        '| Metric | Value |',
        '|---|---|',
        ('| Sequential sample | ' + $SampleCount + ' requests to /health |'),
        ('| Latency p50 | ' + $p50 + ' ms |'),
        ('| Latency p95 | ' + $p95 + ' ms |'),
        ('| Sample failures | ' + $fail + ' |'),
        ('| Parallel requests | ' + $totalReq + ' @ concurrency ' + $Concurrency + ' (duration ' + $DurationSeconds + 's) |'),
        ('| Successful | ' + $ok + ' / ' + $totalReq + ' |'),
        ('| Throughput | ' + $rps + ' req/s |'),
        ('| Process CPU (during) | ' + $cpu + ' s |'),
        ('| WorkingSet BEFORE | ' + [math]::Round($ram0/1MB,1) + ' MB |'),
        ('| WorkingSet AFTER | ' + $ram + ' MB |'),
        '',
        'Note: single-node local smoke (includes PowerShell/Invoke-WebRequest overhead); not a substitute for sustained staging soak.'
    )
    New-Item -ItemType Directory -Path (Split-Path $OutFile) -Force | Out-Null
    $lines | Set-Content -LiteralPath $OutFile -Encoding UTF8
    $lines | ForEach-Object { Write-Output $_ }
}
finally {
    try { taskkill /PID $proc.Id /T /F 2>$null | Out-Null } catch { }
    try { sqlcmd -S ".\SQLEXPRESS02" -E -Q "IF DB_ID('$dbName') IS NOT NULL BEGIN ALTER DATABASE [$dbName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$dbName]; END" 2>$null | Out-Null } catch { }
}
