param([string]$GameDir = '')
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
. (Join-Path $here 'common.ps1')

try {
    Write-Title 'Havendock FPS Fix - uninstall'
    $GameDir = Find-GameDir $GameDir
    Write-Host "  Game folder: $GameDir"
    Assert-NotRunning

    $mods = Join-Path $GameDir 'Mods'
    $createdSwitch = $false
    $createdModsFolder = $false
    $copies = @(Get-InstalledCopies $mods)
    foreach ($c in $copies) {
        $recordPath = Join-Path $mods ($c.Name + '.install.json')
        if (Test-Path $recordPath) {
            try {
                $r = Get-Content -Raw $recordPath | ConvertFrom-Json
                if ($r.createdSwitch) { $createdSwitch = $true }
                if ($r.createdModsFolder) { $createdModsFolder = $true }
            } catch { }
            Remove-Item $recordPath -Force
        }
        Remove-Item $c.FullName -Recurse -Force
        Write-Host "  Removed the mod ($($c.Name))."
    }
    if ($copies.Count -eq 0) { Write-Host '  The mod was not in the Mods folder.' }

    if ($createdSwitch -and (Test-Path $mods)) {
        $left = @(Get-ChildItem $mods -Force | Where-Object { $_.Name -ne 'mods.json' })
        if ($left.Count -eq 0) {
            Remove-Item (Join-Path $mods 'mods.json') -Force -ErrorAction SilentlyContinue
            if ($createdModsFolder -and @(Get-ChildItem $mods -Force).Count -eq 0) { Remove-Item $mods -Force }
        }
    }

    if (Test-BepInExVersion $GameDir) { Remove-BepInExVersion $GameDir }

    Write-Host ''
    Write-Host '  Done. Havendock is back to normal.' -ForegroundColor Green
} catch {
    Write-Host ''
    Write-Host "  Uninstall failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
