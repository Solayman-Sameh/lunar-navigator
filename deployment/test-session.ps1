[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$ApplicationRoot, [switch]$ForceLauncherExit)
$ErrorActionPreference = 'Stop'
$ApplicationRoot = (Resolve-Path -LiteralPath $ApplicationRoot).Path
$logRoot = Join-Path $env:LOCALAPPDATA 'LunarNavigator\logs'
$before = @(Get-ChildItem $logRoot -Directory -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name)
$launcher = $null
$backendPid = 0
$unityPid = 0
try {
    $launcher = Start-Process -FilePath (Join-Path $ApplicationRoot 'LunarNavigator.exe') -WindowStyle Hidden -PassThru
    $deadline = (Get-Date).AddSeconds(40)
    do {
        Start-Sleep -Milliseconds 200
        $sessionDir = Get-ChildItem $logRoot -Directory | Where-Object Name -NotIn $before | Select-Object -First 1
        if ($sessionDir) {
            $log = Get-Content (Join-Path $sessionDir.FullName 'launcher.log') -Raw -ErrorAction SilentlyContinue
            if ($log -match 'Owned backend PID (\d+)') { $backendPid = [int]$Matches[1] }
            if ($log -match 'Owned Unity PID (\d+)') { $unityPid = [int]$Matches[1] }
        }
    } until ($unityPid -or $launcher.HasExited -or (Get-Date) -gt $deadline)
    if (-not $unityPid -or -not $backendPid) { throw 'Owned session did not start within deadline.' }
    $descriptor = Get-Content (Join-Path $env:LOCALAPPDATA "LunarNavigator\runtime\$($sessionDir.Name)\startup.json") -Raw | ConvertFrom-Json
    $health = Invoke-RestMethod "http://127.0.0.1:$($descriptor.port)/health" -TimeoutSec 3
    if ($health.pid -ne $backendPid -or $health.session_id -ne $sessionDir.Name) { throw 'Installed backend identity mismatch' }
    $sites = Invoke-RestMethod "http://127.0.0.1:$($descriptor.port)/sites" -TimeoutSec 3
    if ($sites.sites.Count -ne 3) { throw 'Installed catalog is incomplete' }
    if ($ForceLauncherExit) {
        Stop-Process -Id $launcher.Id -Force
    } else {
        $unity = Get-Process -Id $unityPid
        $deadline = (Get-Date).AddSeconds(30)
        while ($unity.MainWindowHandle -eq 0 -and -not $unity.HasExited -and (Get-Date) -lt $deadline) {
            Start-Sleep -Milliseconds 250
            $unity.Refresh()
        }
        if (-not $unity.CloseMainWindow()) { throw 'Unity window did not accept normal close' }
    }
    if (-not $launcher.WaitForExit(15000)) { throw 'Launcher did not exit after session close' }
    foreach ($owned in @($backendPid, $unityPid)) {
        $process = Get-Process -Id $owned -ErrorAction SilentlyContinue
        if ($process -and -not $process.WaitForExit(5000)) { throw "Owned PID $owned survived session exit" }
    }
    Write-Output "PASS session: normal-close=$(-not $ForceLauncherExit), health, catalog, backend/Unity cleanup. Logs: $($sessionDir.FullName)"
} finally {
    if ($launcher -and -not $launcher.HasExited) { Stop-Process -Id $launcher.Id -Force }
    # Only processes whose PIDs came from this test launch's new session log.
    foreach ($owned in @($backendPid, $unityPid)) {
        if ($owned) { Stop-Process -Id $owned -Force -ErrorAction SilentlyContinue }
    }
}
