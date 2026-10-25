param(
    [string]$HavendockDir = $env:HAVENDOCK_DIR,
    [switch]$Zip,
    [switch]$NoInstall,
    [string]$ModId = '',
    [string]$Author = '',
    [string]$Homepage = $null,
    [string]$ReportTo = 'github.com/Dave-Debugs/HavendockFpsFix/issues',
    [string]$OutDir = ''
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
. (Join-Path $root 'installer\files\common.ps1')

$harmonyVersion = '2.4.3'
$harmonyUrl = "https://api.nuget.org/v3-flatcontainer/lib.harmony/$harmonyVersion/lib.harmony.$harmonyVersion.nupkg"
$harmonySha256 = 'F07AF11A0D62447E7E1B6E3FCF1E28FC75D9F8D1ABF2D6EE79AABCDC4DB43E88'

$HavendockDir = Find-GameDir $HavendockDir
if (-not (Test-ModSupport $HavendockDir)) {
    throw "No HavendockModding.dll in $HavendockDir (Havendock_Data\Managed or BepInEx\plugins\HavendockLoader). Patch the game for co-op or install the mod loader with the Havendock Mod Manager, or pass -HavendockDir."
}

$projDir = Join-Path $root 'src\HavendockFpsFix'
$proj = Join-Path $projDir 'HavendockFpsFix.csproj'
$manifest = Get-Content (Join-Path $projDir 'havendock-mod.json') -Raw | ConvertFrom-Json
$code = Get-Content (Join-Path $projDir 'Plugin.cs') -Raw
if ($code -notmatch 'Version\s*=\s*"([^"]+)"' -or $Matches[1] -ne $manifest.version) { throw "Version in Plugin.cs does not match havendock-mod.json ($($manifest.version))." }
$projVersion = ([xml](Get-Content $proj)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
if ($projVersion -ne $manifest.version) { throw "Version in HavendockFpsFix.csproj does not match havendock-mod.json ($($manifest.version))." }
if ($manifest.version -ne $script:FixVersion) { throw "Version in installer\files\common.ps1 does not match havendock-mod.json ($($manifest.version))." }

$manifestText = Get-Content (Join-Path $projDir 'havendock-mod.json') -Raw
if ($ModId) {
    if ($ModId -notmatch '^[a-z0-9-]+\.[a-z0-9-]+$') { throw "ModId '$ModId' must look like yourname.modname (lower-case letters, digits, dashes)." }
    $manifestText = [regex]::Replace($manifestText, '("id"\s*:\s*")[^"]*(")', { param($m) $m.Groups[1].Value + $ModId + $m.Groups[2].Value })
    $manifest.id = $ModId
}
if ($Author) { $manifestText = [regex]::Replace($manifestText, '("author"\s*:\s*")[^"]*(")', { param($m) $m.Groups[1].Value + $Author + $m.Groups[2].Value }) }
if ($PSBoundParameters.ContainsKey('Homepage')) { $manifestText = [regex]::Replace($manifestText, '("homepage"\s*:\s*")[^"]*(")', { param($m) $m.Groups[1].Value + $Homepage + $m.Groups[2].Value }) }
$null = $manifestText | ConvertFrom-Json
if (-not $OutDir) { $OutDir = Join-Path $root 'dist' }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$cache = Join-Path $root '.cache'
New-Item -ItemType Directory -Force -Path $cache | Out-Null
$pkg = Join-Path $cache "lib.harmony.$harmonyVersion.nupkg"
$harmonyDll = Join-Path $cache '0Harmony.dll'
$harmonyLicense = Join-Path $cache 'Harmony-LICENSE.txt'
if (-not (Test-Path $pkg)) {
    Write-Host "Downloading Harmony $harmonyVersion"
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -Uri $harmonyUrl -OutFile $pkg -UseBasicParsing
}
if ((Get-FileHash $pkg -Algorithm SHA256).Hash -ne $harmonySha256) {
    Remove-Item $pkg -Force
    throw "lib.harmony.$harmonyVersion.nupkg did not match the expected checksum. Download it from $harmonyUrl and place it in .cache\"
}
if (-not (Test-Path $harmonyDll) -or -not (Test-Path $harmonyLicense) -or (Get-Item $pkg).LastWriteTime -gt (Get-Item $harmonyDll).LastWriteTime) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $z = [IO.Compression.ZipFile]::OpenRead($pkg)
    try {
        foreach ($pair in @(@('lib/net472/0Harmony.dll', $harmonyDll), @('LICENSE', $harmonyLicense))) {
            $entry = $z.Entries | Where-Object { $_.FullName -eq $pair[0] } | Select-Object -First 1
            if (-not $entry) { throw "$($pair[0]) not found in the Harmony package." }
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $pair[1], $true)
        }
    } finally { $z.Dispose() }
}

$out = Join-Path $root 'dist\build'
dotnet build $proj -c Release -nologo -v q -o $out "-p:HavendockDir=$HavendockDir" "-p:HarmonyDll=$harmonyDll"
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
$dll = Join-Path $out $manifest.assembly
if (-not (Test-Path $dll)) { throw "$($manifest.assembly) was not built." }

$name = "Havendock FPS Fix $($manifest.version)"
$stage = Join-Path $OutDir $name
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
$files = Join-Path $stage 'files'
$modStage = Join-Path $files $manifest.id
New-Item -ItemType Directory -Force -Path $modStage | Out-Null
Copy-Item $dll, $harmonyDll, $harmonyLicense $modStage
[IO.File]::WriteAllText((Join-Path $modStage 'havendock-mod.json'), $manifestText, (New-Object Text.UTF8Encoding $false))
$modReadme = (Get-Content (Join-Path $root 'installer\MOD-README.md') -Raw).Replace('{MODID}', $manifest.id).Replace('{REPORT}', $ReportTo)
[IO.File]::WriteAllText((Join-Path $modStage 'README.md'), $modReadme, (New-Object Text.UTF8Encoding $false))
Copy-Item (Join-Path $root 'installer\Install.cmd'), (Join-Path $root 'installer\Uninstall.cmd') $stage
$readme = (Get-Content (Join-Path $root 'installer\README.txt') -Raw).Replace('{MODID}', $manifest.id).Replace('{REPORT}', $ReportTo)
[IO.File]::WriteAllText((Join-Path $stage 'README.txt'), $readme, (New-Object Text.UTF8Encoding $false))
Copy-Item (Join-Path $root 'installer\files\*') $files
Write-Host "Built: $modStage" -ForegroundColor Green

if (-not $NoInstall) {
    & (Join-Path $files 'install.ps1') -GameDir $HavendockDir
    if ($LASTEXITCODE -ne 0) { throw 'Install failed.' }
}

if ($Zip) {
    $modZip = Join-Path $OutDir "$($manifest.id)-$($manifest.version).zip"
    if (Test-Path $modZip) { Remove-Item $modZip -Force }
    Compress-Archive -Path (Join-Path $modStage '*') -DestinationPath $modZip
    $zipPath = Join-Path $OutDir "HavendockFpsFix-$($manifest.version).zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Compress-Archive -Path $stage -DestinationPath $zipPath
    Write-Host "Release zip: $zipPath" -ForegroundColor Green
    Write-Host "Mod folder zip: $modZip" -ForegroundColor Green
}
