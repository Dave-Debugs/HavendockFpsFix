$script:FixVersion = '2.0.0'
$script:ModDll = 'HavendockFpsFixMod.dll'
$script:ManifestName = 'HavendockFpsFix-install.json'
$script:PluginFolder = 'HavendockFpsFix'
$script:ConfigName = 'havendock.fpsfix.cfg'

function Test-GameDir([string]$d) { return ($d -and (Test-Path (Join-Path $d 'Havendock.exe'))) }

function Get-SteamLibraries {
    $roots = @()
    foreach ($k in @('HKCU:\Software\Valve\Steam', 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam', 'HKLM:\SOFTWARE\Valve\Steam')) {
        try {
            $p = Get-ItemProperty -Path $k -ErrorAction Stop
            foreach ($n in @('SteamPath', 'InstallPath')) { if ($p.$n) { $roots += ($p.$n -replace '/', '\') } }
        } catch { }
    }
    $roots += @("${env:ProgramFiles(x86)}\Steam", "$env:ProgramFiles\Steam")
    $libs = @()
    foreach ($r in ($roots | Select-Object -Unique)) {
        if (-not (Test-Path $r)) { continue }
        $libs += $r
        $vdf = Join-Path $r 'steamapps\libraryfolders.vdf'
        if (Test-Path $vdf) {
            foreach ($m in [regex]::Matches((Get-Content -Raw $vdf), '"path"\s+"([^"]+)"')) { $libs += ($m.Groups[1].Value -replace '\\\\', '\') }
        }
    }
    return ($libs | Select-Object -Unique)
}

function Find-GameDir([string]$given) {
    if ($given) {
        if (Test-GameDir $given) { return (Resolve-Path $given).Path }
        throw "Havendock.exe was not found in '$given'."
    }
    foreach ($lib in Get-SteamLibraries) {
        $d = Join-Path $lib 'steamapps\common\Havendock'
        if (Test-GameDir $d) { return $d }
    }
    Write-Host 'Havendock could not be found automatically.' -ForegroundColor Yellow
    Write-Host 'In Steam, right-click Havendock > Manage > Browse local files, copy the folder path from the address bar and paste it here.'
    $p = (Read-Host 'Havendock folder').Trim('" ')
    if (Test-GameDir $p) { return $p }
    throw "Havendock.exe was not found in '$p'."
}

function Assert-NotRunning {
    if (Get-Process -Name 'Havendock' -ErrorAction SilentlyContinue) { throw 'Havendock is running. Close the game, then try again.' }
}

function Write-Title([string]$t) {
    Write-Host ''
    Write-Host "  $t" -ForegroundColor Cyan
    Write-Host ('  ' + ('-' * $t.Length)) -ForegroundColor Cyan
}

function Get-ModApiPath([string]$d) {
    foreach ($p in @('Havendock_Data\Managed\HavendockModding.dll', 'BepInEx\plugins\HavendockLoader\HavendockModding.dll')) {
        $f = Join-Path $d $p
        if (Test-Path $f) { return $f }
    }
    return $null
}

function Test-ModSupport([string]$d) { return [bool](Get-ModApiPath $d) }

function Test-BepInExVersion([string]$GameDir) {
    if (Test-Path (Join-Path $GameDir $script:ManifestName)) { return $true }
    return (Test-Path (Join-Path $GameDir ('BepInEx\plugins\' + $script:PluginFolder)))
}

function Remove-BepInExVersion([string]$GameDir) {
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
    Write-Host '  Removed the old BepInEx version of the mod.'
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
            Write-Host '  Removed BepInEx (this mod does not need it any more).'
            if ($manifest.backupDir -and (Test-Path $manifest.backupDir)) {
                foreach ($t in $manifest.backedUp) { Copy-Item (Join-Path $manifest.backupDir $t) $GameDir -Recurse -Force; Write-Host "  Restored your original $t" }
                Remove-Item $manifest.backupDir -Recurse -Force
            }
        }
    }
    if (Test-Path $manifestPath) { Remove-Item $manifestPath -Force }
}

function Get-PackagedMod([string]$dir) {
    foreach ($d in @(Get-ChildItem $dir -Directory)) {
        $m = Join-Path $d.FullName 'havendock-mod.json'
        if ((Test-Path $m) -and (Test-Path (Join-Path $d.FullName $script:ModDll))) {
            return [pscustomobject]@{ Id = (Get-Content -Raw $m | ConvertFrom-Json).id; Path = $d.FullName }
        }
    }
    return $null
}

function Get-InstalledCopies([string]$mods) {
    if (-not (Test-Path $mods)) { return @() }
    return @(Get-ChildItem $mods -Directory | Where-Object { Test-Path (Join-Path $_.FullName $script:ModDll) })
}

function Install-ModLoader([string]$GameDir) {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $index = Invoke-RestMethod -Uri 'https://mods.havendock.online/dl/mods/index.json' -UseBasicParsing
    $file = $index.loader.file
    $sha = $index.loader.sha256
    if (-not $file -or -not $sha -or $file -notmatch '^[A-Za-z0-9._-]+\.zip$') { throw 'The Havendock mods site did not list a mod loader download. Install the Havendock Mod Manager from tools.havendock.online instead.' }
    $zipPath = Join-Path ([IO.Path]::GetTempPath()) $file
    Invoke-WebRequest -Uri ('https://mods.havendock.online/dl/mods/' + $file) -OutFile $zipPath -UseBasicParsing
    if ((Get-FileHash $zipPath -Algorithm SHA256).Hash -ne $sha.ToUpperInvariant()) { Remove-Item $zipPath -Force; throw 'The mod loader download did not match its published checksum, so it was not installed.' }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $root = (Resolve-Path $GameDir).Path.TrimEnd('\') + '\'
    $zip = [IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        foreach ($e in $zip.Entries) {
            if (-not $e.Name -or -not $e.FullName.StartsWith('BepInEx/')) { continue }
            $dest = [IO.Path]::GetFullPath((Join-Path $GameDir ($e.FullName -replace '/', '\')))
            if (-not $dest.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) { throw "Unexpected file in the mod loader package: $($e.FullName)" }
            New-Item -ItemType Directory -Force -Path (Split-Path $dest) | Out-Null
            [IO.Compression.ZipFileExtensions]::ExtractToFile($e, $dest, $true)
        }
    } finally { $zip.Dispose(); Remove-Item $zipPath -Force -ErrorAction SilentlyContinue }
    if (-not (Test-ModSupport $GameDir)) { throw 'The mod loader package was incomplete. Install the Havendock Mod Manager from tools.havendock.online instead.' }
    Write-Host "  Installed Havendock's mod loader $($index.loader.version) (shared by all Havendock mods)."
}
