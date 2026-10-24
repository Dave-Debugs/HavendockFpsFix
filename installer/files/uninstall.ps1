param([string]$GameDir = '')
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
. (Join-Path $here 'common.ps1')

try {
    Write-Title 'Havendock FPS Fix - uninstall'
    $GameDir = Find-GameDir $GameDir
    Write-Host "  Game folder: $GameDir"
    Assert-NotRunning

    $manifestPath = Join-Path $GameDir $script:ManifestName
    $manifest = $null
    if (Test-Path $manifestPath) { $manifest = Get-Content -Raw $manifestPath | ConvertFrom-Json }
    $plugins = Join-Path $GameDir 'BepInEx\plugins'

    foreach ($p in @((Join-Path $plugins $script:PluginFolder), (Join-Path $GameDir ('BepInEx\config\' + $script:ConfigName)))) {
        if (Test-Path $p) { Remove-Item $p -Recurse -Force }
    }
    if ($manifest) {
        $old = Join-Path $plugins 'HavendockPerf'
        if (Test-Path $old) { Remove-Item $old -Recurse -Force }
    }
    Write-Host '  Removed the mod.'

    if ($manifest -and $manifest.installedBepInEx) {
        $others = @()
        if (Test-Path $plugins) { $others += @(Get-ChildItem $plugins -Force) }
        $patchers = Join-Path $GameDir 'BepInEx\patchers'
        if (Test-Path $patchers) { $others += @(Get-ChildItem $patchers -Force) }
        if ($others.Count -gt 0) {
            Write-Host '  Other BepInEx mods are installed, so BepInEx was kept.' -ForegroundColor Yellow
        } else {
            foreach ($t in $manifest.topLevel) {
                $p = Join-Path $GameDir $t
                if (Test-Path $p) { Remove-Item $p -Recurse -Force }
            }
            Write-Host '  Removed BepInEx.'
            if ($manifest.backupDir -and (Test-Path $manifest.backupDir)) {
                foreach ($t in $manifest.backedUp) { Copy-Item (Join-Path $manifest.backupDir $t) $GameDir -Recurse -Force; Write-Host "  Restored your original $t" }
                Remove-Item $manifest.backupDir -Recurse -Force
            }
        }
    } elseif (-not $manifest) {
        Write-Host '  BepInEx was not installed by this mod, so it was kept.'
    }
    if (Test-Path $manifestPath) { Remove-Item $manifestPath -Force }

    Write-Host ''
    Write-Host '  Done. Havendock is back to normal.' -ForegroundColor Green
} catch {
    Write-Host ''
    Write-Host "  Uninstall failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
