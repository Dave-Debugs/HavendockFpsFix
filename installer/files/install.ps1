param([string]$GameDir = '')
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
. (Join-Path $here 'common.ps1')

try {
    Write-Title "Havendock FPS Fix $($script:FixVersion) - install"
    $GameDir = Find-GameDir $GameDir
    Write-Host "  Game folder: $GameDir"
    if (-not (Test-Path (Join-Path $GameDir 'Havendock_Data\Managed\Assembly-CSharp.dll'))) { throw 'This copy of Havendock is not supported (Mono build not found).' }
    Assert-NotRunning

    $manifestPath = Join-Path $GameDir $script:ManifestName
    $manifest = $null
    if (Test-Path $manifestPath) { $manifest = Get-Content -Raw $manifestPath | ConvertFrom-Json }
    $plugins = Join-Path $GameDir 'BepInEx\plugins'

    if ($manifest) {
        Write-Host '  Updating an existing install.'
        $old = Join-Path $plugins 'HavendockPerf'
        if (Test-Path $old) { Remove-Item $old -Recurse -Force }
        $cfgDir = Join-Path $GameDir 'BepInEx\config'
        if ($manifest.configInstalled -and (Test-Path $cfgDir)) { Get-ChildItem $cfgDir -Filter '*.havendock.perf.cfg' | Remove-Item -Force }
        if (-not ($manifest.PSObject.Properties.Name -contains 'configInstalled')) { $manifest | Add-Member configInstalled $false }
        $manifest.configInstalled = $false
    } elseif (Test-Path (Join-Path $GameDir 'BepInEx\core\BepInEx.dll')) {
        Write-Host '  BepInEx is already installed. It will be left as it is.'
        $manifest = [pscustomobject]@{ installedBepInEx = $false; backupDir = ''; backedUp = @(); topLevel = @(); configInstalled = $false; version = ''; installedAt = '' }
    } else {
        Write-Host '  Installing BepInEx 5.4.23.5 (mod loader)...'
        $zipPath = Join-Path $here 'BepInEx_win_x64_5.4.23.5.zip'
        if (-not (Test-Path $zipPath)) { throw 'BepInEx_win_x64_5.4.23.5.zip is missing. Please unzip the whole download again.' }
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $zip = [IO.Compression.ZipFile]::OpenRead($zipPath)
        $top = @($zip.Entries | ForEach-Object { ($_.FullName -replace '\\', '/').Split('/')[0] } | Where-Object { $_ } | Sort-Object -Unique)
        $zip.Dispose()
        $backup = Join-Path $GameDir 'HavendockFpsFix-backup'
        $backedUp = @()
        foreach ($t in $top) {
            $p = Join-Path $GameDir $t
            if (Test-Path $p) {
                New-Item -ItemType Directory -Force -Path $backup | Out-Null
                Copy-Item -Path $p -Destination $backup -Recurse -Force
                $backedUp += $t
                Write-Host "  Backed up your existing $t" -ForegroundColor Yellow
            }
        }
        Expand-Archive -Path $zipPath -DestinationPath $GameDir -Force
        $manifest = [pscustomobject]@{ installedBepInEx = $true; backupDir = $(if ($backedUp.Count) { $backup } else { '' }); backedUp = $backedUp; topLevel = $top; configInstalled = $false; version = ''; installedAt = '' }
        $manifest | ConvertTo-Json | Set-Content -Path $manifestPath -Encoding UTF8
    }

    $dest = Join-Path $plugins $script:PluginFolder
    New-Item -ItemType Directory -Force -Path $dest | Out-Null
    Copy-Item (Join-Path $here 'HavendockFpsFix.dll') (Join-Path $dest 'HavendockFpsFix.dll') -Force
    if (Test-Path (Join-Path $plugins 'HavendockPerf')) { Write-Host '  Note: another copy of this mod (HavendockPerf) is installed. Remove it so the fixes do not run twice.' -ForegroundColor Yellow }

    $manifest.version = $script:FixVersion
    $manifest.installedAt = (Get-Date).ToString('s')
    $manifest | ConvertTo-Json | Set-Content -Path $manifestPath -Encoding UTF8

    Write-Host ''
    Write-Host '  Installed.' -ForegroundColor Green
    Write-Host '  Start Havendock from Steam as usual. The first start takes a little longer.'
    Write-Host '  In game:  F8 turns the mod off/on to compare,  F7 shows an FPS counter.'
    Write-Host '  To remove it, run Uninstall.cmd.'
} catch {
    Write-Host ''
    Write-Host "  Install failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host '  If it stopped halfway, run Uninstall.cmd to clean up, then try again.'
    exit 1
}
