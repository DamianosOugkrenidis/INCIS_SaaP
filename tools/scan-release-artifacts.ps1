# scan-release-artifacts.ps1 - scan a folder or ZIP for release-hygiene problems (ASCII-only, PS 5.1)
# Two tiers:
#   FAIL - must never ship: stray logs/temp/build/data artifacts, JWT-like strings,
#          private keys, literal password / connection-string credentials.
#   INFO - advisory, reported but not failing: e-mail addresses, internal hostnames,
#          local absolute paths, credential field names (often placeholders/tests/docs).
# Never prints matched secret VALUES. Exit: 0=clean(or INFO only), 1=FAIL (with -FailOnFindings), 2=invalid input.
# Usage:
#   .\scan-release-artifacts.ps1 -Path .\dist\release.zip
#   .\scan-release-artifacts.ps1 -Path .\dist\release.zip -FailOnFindings
param(
    [Parameter(Mandatory = $true)][string]$Path,
    [switch]$FailOnFindings,
    [switch]$Quiet
)
$ErrorActionPreference = "Stop"
if (-not (Test-Path -LiteralPath $Path)) { Write-Output "INVALID_INPUT: not found: $Path"; exit 2 }

$excludedExt = @('.log', '.bak', '.bak2', '.trx', '.pdb', '.user', '.suo', '.tmp', '.cache', '.zip', '.7z', '.rar', '.xls', '.xlsx', '.pdf', '.doc', '.docx')
$tempNames   = @('launchSettings.json', '.env')
$tempDirSegs = @('bin', 'obj', 'node_modules', '.vs', 'TestResults', 'logs', 'coverage', 'FILES', 'dist')

# FAIL patterns (literal secrets only; placeholders like ${VAR} / *** / <...> are ignored)
$failPatterns = [ordered]@{
    'JWT_LIKE'        = 'eyJ[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}'
    'PRIVATE_KEY'     = 'BEGIN (RSA |EC |OPENSSH |)PRIVATE KEY'
    'PASSWORD_LITERAL'= '(?i)(password|pwd)\s*[:=]\s*["'']?(?!\$\{|\$\(|<|\*{3}|example|placeholder|changeme|your[_-]|xxx)[^\s"'',]{6,}'
    'CONN_PWD_LITERAL'= '(?i)(server|data source)\s*=[^;]*(password|pwd)\s*=\s*["'']?(?!\$\{|\$\(|<|\*{3})[^\s;"'']{6,}'
}
# INFO patterns (advisory)
$infoPatterns = [ordered]@{
    'EMAIL_ADDRESS'  = '[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}'
    'LOCAL_ABS_PATH' = '[A-Za-z]:\\[Uu]sers\\'
    'INTERNAL_HOST'  = '(?i)\bPYTHAGORAS\b'
    'SQL_CRED_FIELD' = '(?i)(user\s*id|uid)\s*='
}
$textExt = @('.cs', '.csproj', '.sln', '.json', '.md', '.txt', '.yml', '.yaml', '.ps1', '.sql', '.css', '.js', '.html', '.cshtml', '.config', '.xml', '.editorconfig', '.gitignore', '.dockerignore', '.example', '.template', '')

# Extensions where a literal credential is a blocking concern (config that ships secrets)
$configExt = @('.json', '.yml', '.yaml', '.config', '.xml', '.ini', '.properties')
# Extensions allowed under docs/ (curated test evidence)
$docsAllowedExt = @('.log', '.trx')

$items = @()
$isZip = $Path.ToLower().EndsWith('.zip')
if ($isZip) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $z = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $Path).Path)
    foreach ($e in $z.Entries) { if (-not ($e.Name -eq '' -and $e.Length -eq 0)) { $items += [pscustomobject]@{ Rel = $e.FullName; Len = $e.Length; Zip = $e } } }
} else {
    $root = (Resolve-Path -LiteralPath $Path).Path
    foreach ($f in Get-ChildItem -LiteralPath $root -Recurse -File -Force) {
        $rel = $f.FullName.Substring($root.Length).TrimStart('\', '/')
        $items += [pscustomobject]@{ Rel = $rel; Len = $f.Length; File = $f.FullName }
    }
}

$fails = New-Object System.Collections.ArrayList
$infos = New-Object System.Collections.ArrayList
$scannedText = 0

foreach ($it in $items) {
    $rel = $it.Rel -replace '\\', '/'
    $name = ($rel -split '/')[-1]
    $ext = [System.IO.Path]::GetExtension($name).ToLower()
    $segments = $rel -split '/'
    $isDocs = ($rel -match '^docs/')

    foreach ($seg in $segments[0..([Math]::Max(0, $segments.Count - 2))]) { if ($tempDirSegs -contains $seg) { [void]$fails.Add(("DIR:'{0}' {1}" -f $seg, $rel)) } }
    $extFail = ($excludedExt -contains $ext) -and -not ($isDocs -and ($docsAllowedExt -contains $ext))
    if ($extFail) { [void]$fails.Add(("EXT:'{0}' {1}" -f $ext, $rel)) }
    if ($tempNames -contains $name) { [void]$fails.Add(("TEMP:'{0}' {1}" -f $name, $rel)) }

    if ($it.Len -gt 0 -and $it.Len -lt 5MB -and ($textExt -contains $ext)) {
        try {
            $content = $null
            if ($isZip) { $sr = New-Object System.IO.StreamReader($it.Zip.Open()); $content = $sr.ReadToEnd(); $sr.Close() }
            else { $content = Get-Content -LiteralPath $it.File -Raw }
            if ($null -ne $content) {
                $scannedText++
                # Unconditional blocking secrets
                foreach ($k in @('JWT_LIKE', 'PRIVATE_KEY')) { $n = [regex]::Matches($content, $failPatterns[$k]).Count; if ($n -gt 0) { [void]$fails.Add(("{0}:{1} {2}" -f $k, $n, $rel)) } }
                # Literal credentials: blocking only in config-like files
                foreach ($k in @('PASSWORD_LITERAL', 'CONN_PWD_LITERAL')) {
                    $n = [regex]::Matches($content, $failPatterns[$k]).Count
                    if ($n -gt 0) {
                        if ($configExt -contains $ext) { [void]$fails.Add(("{0}:{1} {2}" -f $k, $n, $rel)) }
                        else { [void]$infos.Add(("{0}:{1} {2}" -f $k, $n, $rel)) }
                    }
                }
                foreach ($k in $infoPatterns.Keys) { $n = [regex]::Matches($content, $infoPatterns[$k]).Count; if ($n -gt 0) { [void]$infos.Add(("{0}:{1} {2}" -f $k, $n, $rel)) } }
            }
        } catch { }
    }
}
if ($isZip) { $z.Dispose() }

if (-not $Quiet) {
    Write-Output ("SCAN path={0} items={1} textScanned={2} FAIL={3} INFO={4}" -f $Path, $items.Count, $scannedText, $fails.Count, $infos.Count)
    if ($fails.Count -eq 0) { Write-Output "CLEAN: no blocking release-hygiene findings" }
    else { Write-Output "FAIL findings:"; $fails | Sort-Object -Unique | ForEach-Object { Write-Output ("  x " + $_) } }
    if ($infos.Count -gt 0) {
        $byCat = $infos | ForEach-Object { ($_ -split ':')[0] } | Group-Object | ForEach-Object { "$($_.Name)=$($_.Count)" }
        Write-Output ("INFO (advisory, non-blocking): " + ($byCat -join ', '))
    }
}
if ($fails.Count -gt 0 -and $FailOnFindings) { exit 1 }
exit 0
