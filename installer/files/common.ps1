$script:FixVersion = '1.0.0'
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
