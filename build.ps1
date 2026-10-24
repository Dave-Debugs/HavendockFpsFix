param(
    [string]$GameDir = '',
    [string]$BepInExCore = ''
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
. (Join-Path $root 'installer\files\common.ps1')

$bepVersion = '5.4.23.5'
$bepZipName = "BepInEx_win_x64_$bepVersion.zip"
$bepUrl = "https://github.com/BepInEx/BepInEx/releases/download/v$bepVersion/$bepZipName"
$bepSha256 = '82F9878551030F54657792C0740D9D51A09500EEAE1FBA21106B0C441E6732C4'

$GameDir = Find-GameDir $GameDir
if (-not $BepInExCore) { $BepInExCore = Join-Path $GameDir 'BepInEx\core' }
if (-not (Test-Path (Join-Path $BepInExCore 'BepInEx.dll'))) { throw "BepInEx not found in '$BepInExCore'. Run installer\Install.cmd once, or pass -BepInExCore." }

$proj = Join-Path $root 'src\HavendockFpsFix\HavendockFpsFix.csproj'
$version = ([xml](Get-Content $proj)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
$out = Join-Path $root 'dist\build'
dotnet build $proj -c Release -o $out "-p:GameDir=$GameDir" "-p:BepInExCore=$BepInExCore"
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

$cache = Join-Path $root '.cache'
$bepZip = Join-Path $cache $bepZipName
New-Item -ItemType Directory -Force -Path $cache | Out-Null
if (-not (Test-Path $bepZip)) {
    Write-Host "Downloading $bepZipName"
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -Uri $bepUrl -OutFile $bepZip -UseBasicParsing
}
if ((Get-FileHash $bepZip -Algorithm SHA256).Hash -ne $bepSha256) {
    Remove-Item $bepZip -Force
    throw "$bepZipName did not match the expected checksum. Download it from $bepUrl and place it in .cache\"
}

$name = "Havendock FPS Fix $version"
$stage = Join-Path $root "dist\$name"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force -Path (Join-Path $stage 'files') | Out-Null
Copy-Item (Join-Path $root 'installer\Install.cmd'), (Join-Path $root 'installer\Uninstall.cmd'), (Join-Path $root 'installer\README.txt') $stage
Copy-Item (Join-Path $root 'installer\files\*') (Join-Path $stage 'files')
Copy-Item (Join-Path $out 'HavendockFpsFix.dll') (Join-Path $stage 'files')
Copy-Item $bepZip (Join-Path $stage 'files')
$zip = Join-Path $root "dist\HavendockFpsFix-$version.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path $stage -DestinationPath $zip
Write-Host "Release zip: $zip" -ForegroundColor Green
