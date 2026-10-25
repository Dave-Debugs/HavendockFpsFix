param([string]$GameDir = '')
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
. (Join-Path $here 'common.ps1')

try {
    Write-Title "Havendock FPS Fix $($script:FixVersion) - install"
    $GameDir = Find-GameDir $GameDir
    Write-Host "  Game folder: $GameDir"
    $pkg = Get-PackagedMod $here
    if (-not $pkg) { throw 'Some files are missing. Please unzip the whole download again.' }
    Assert-NotRunning
    if (-not (Test-ModSupport $GameDir)) {
        if (-not (Test-Path (Join-Path $GameDir 'BepInEx\core\BepInEx.dll'))) {
            throw 'Havendock''s mod loader is not installed. Get the free Havendock Mod Manager from tools.havendock.online, let it set up the mod loader once, then run Install.cmd again.'
        }
        Write-Host '  Havendock''s mod loader is missing. Downloading the official one from mods.havendock.online...'
        Install-ModLoader $GameDir
    }

    $mods = Join-Path $GameDir 'Mods'
    $dest = Join-Path $mods $pkg.Id
    $recordPath = Join-Path $mods ($pkg.Id + '.install.json')
    $record = $null
    if (Test-Path $recordPath) { try { $record = Get-Content -Raw $recordPath | ConvertFrom-Json } catch { } }
    if (-not $record) { $record = [pscustomobject]@{ createdModsFolder = $false; createdSwitch = $false; version = ''; installedAt = '' } }

    if (-not (Test-Path $mods)) {
        New-Item -ItemType Directory -Force -Path $mods | Out-Null
        $record.createdModsFolder = $true
    }
    $isUpdate = Test-Path (Join-Path $dest 'havendock-mod.json')
    New-Item -ItemType Directory -Force -Path $dest | Out-Null

    if (Test-BepInExVersion $GameDir) {
        Write-Host '  Found the older BepInEx version of this mod. Replacing it.'
        $oldCfg = Join-Path $GameDir ('BepInEx\config\' + $script:ConfigName)
        $newCfg = Join-Path $dest 'legacy-settings.cfg'
        if ((Test-Path $oldCfg) -and -not (Test-Path $newCfg)) {
            Copy-Item $oldCfg $newCfg
            Write-Host '  Kept your settings.'
        }
        Remove-BepInExVersion $GameDir
    }

    foreach ($other in Get-InstalledCopies $mods) {
        if ($other.FullName -eq (Get-Item $dest).FullName) { continue }
        $otherRecord = Join-Path $mods ($other.Name + '.install.json')
        if (Test-Path $otherRecord) {
            try {
                $o = Get-Content -Raw $otherRecord | ConvertFrom-Json
                if ($o.createdSwitch) { $record.createdSwitch = $true }
                if ($o.createdModsFolder) { $record.createdModsFolder = $true }
            } catch { }
            Remove-Item $otherRecord -Force
        }
        $otherCfg = Join-Path $other.FullName 'config.json'
        if ((Test-Path $otherCfg) -and -not (Test-Path (Join-Path $dest 'config.json'))) { Copy-Item $otherCfg (Join-Path $dest 'config.json') }
        Remove-Item $other.FullName -Recurse -Force
        Write-Host "  Replaced another copy of this mod ($($other.Name))."
    }

    Get-ChildItem $pkg.Path -File | ForEach-Object { Copy-Item $_.FullName (Join-Path $dest $_.Name) -Force }

    $switch = Join-Path $mods 'mods.json'
    if (-not (Test-Path $switch)) {
        Set-Content -Path $switch -Value "{`n    `"enabled`": true`n}" -Encoding ASCII
        $record.createdSwitch = $true
    } else {
        try {
            $s = Get-Content -Raw $switch | ConvertFrom-Json
            if ($s.PSObject.Properties.Name -contains 'enabled' -and -not $s.enabled) {
                Write-Host '  Note: mods are switched off in this copy of Havendock. Turn them on in the Havendock Mod Manager or the Co-op Patcher.' -ForegroundColor Yellow
            }
        } catch { }
    }

    $record.version = $script:FixVersion
    $record.installedAt = (Get-Date).ToString('s')
    $record | ConvertTo-Json | Set-Content -Path $recordPath -Encoding UTF8

    Write-Host ''
    if ($isUpdate) { Write-Host '  Updated.' -ForegroundColor Green } else { Write-Host '  Installed.' -ForegroundColor Green }
    Write-Host "  Mod folder: $dest"
    Write-Host '  Start Havendock as usual.'
    Write-Host '  In a world, press M to switch the fixes off and on, and Pause/Break for the mod menu'
    Write-Host '  (FPS counter, settings, and to change the key).'
    Write-Host '  To remove it, run Uninstall.cmd.'
} catch {
    Write-Host ''
    Write-Host "  Install failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host '  If it stopped halfway, run Uninstall.cmd to clean up, then try again.'
    exit 1
}
