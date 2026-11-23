[CmdletBinding()]
param(
    [string]$GameDir = "",
    [string]$BepInExSourceDir = "",
    [string]$Version = "1.0.4"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$buildScript = Join-Path $scriptDir "build.ps1"

Write-Output "[1/4] Building Release DLL..."
if (-not [string]::IsNullOrWhiteSpace($GameDir)) {
    & $buildScript -GameDir $GameDir -Configuration "Release"
}
else {
    & $buildScript -Configuration "Release"
}
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

$builtDll = Join-Path $repoRoot "bin\Release\LostAndFound_SplitScreenCoop.dll"
if (-not (Test-Path $builtDll)) {
    throw "Expected compiled DLL not found: $builtDll"
}

# Explicit whitelist prevents any game binaries or third-party mod DLLs (e.g., LostAndFound_Indonesian.dll)
# from ever leaking into the distributable release archive.
$bepCoreWhitelist = @(
    "0Harmony.dll",
    "0Harmony20.dll",
    "BepInEx.dll",
    "BepInEx.Harmony.dll",
    "BepInEx.Preloader.dll",
    "HarmonyXInterop.dll",
    "Mono.Cecil.dll",
    "Mono.Cecil.Mdb.dll",
    "Mono.Cecil.Pdb.dll",
    "Mono.Cecil.Rocks.dll",
    "MonoMod.RuntimeDetour.dll",
    "MonoMod.Utils.dll"
)
$loaderWhitelist = @("winhttp.dll", "version.dll", "winmm.dll")

$parentGameDir = Split-Path -Parent $repoRoot
$sourceCandidates = New-Object System.Collections.Generic.List[string]
if (-not [string]::IsNullOrWhiteSpace($BepInExSourceDir)) {
    $sourceCandidates.Add($BepInExSourceDir) | Out-Null
}
if (-not [string]::IsNullOrWhiteSpace($GameDir)) {
    $sourceCandidates.Add((Join-Path $GameDir "LostAndFoundCo_Mod_ID")) | Out-Null
    $sourceCandidates.Add($GameDir) | Out-Null
}
$sourceCandidates.Add((Join-Path $parentGameDir "LostAndFoundCo_Mod_ID")) | Out-Null
$sourceCandidates.Add($parentGameDir) | Out-Null

$resolvedCoreDir = $null
$resolvedLoadersDir = $null
$resolvedDoorstopIni = $null

foreach ($cand in $sourceCandidates) {
    if ([string]::IsNullOrWhiteSpace($cand) -or (-not (Test-Path $cand))) {
        continue
    }

    $coreTry = Join-Path $cand "BepInEx\core"
    $hasAllCore = $true
    foreach ($dll in $bepCoreWhitelist) {
        if (-not (Test-Path (Join-Path $coreTry $dll))) {
            $hasAllCore = $false
            break
        }
    }
    if ($hasAllCore -and ($null -eq $resolvedCoreDir)) {
        $resolvedCoreDir = $coreTry
    }

    $loadersTry = Join-Path $cand "loaders"
    $hasLoadersSubdir = $true
    foreach ($ldr in $loaderWhitelist) {
        if (-not (Test-Path (Join-Path $loadersTry $ldr))) {
            $hasLoadersSubdir = $false
            break
        }
    }
    if ($hasLoadersSubdir -and ($null -eq $resolvedLoadersDir)) {
        $resolvedLoadersDir = $loadersTry
    }

    $doorstopTry = Join-Path $cand "doorstop_config.ini"
    if ((Test-Path $doorstopTry) -and ($null -eq $resolvedDoorstopIni)) {
        $resolvedDoorstopIni = $doorstopTry
    }
}

if ($null -eq $resolvedCoreDir) {
    throw "Could not find complete BepInEx 5 core DLL set. Pass -BepInExSourceDir pointing to a folder containing BepInEx\core and loaders\."
}
if ($null -eq $resolvedLoadersDir) {
    throw "Could not find Doorstop proxy loaders (winhttp.dll, version.dll, winmm.dll). Pass -BepInExSourceDir pointing to a folder containing loaders\."
}

Write-Output "[2/4] Preparing release staging directory..."
$stagingDir = Join-Path $repoRoot "obj\release_staging"
$distDir = Join-Path $repoRoot "dist"
if (Test-Path $stagingDir) {
    Remove-Item -Path $stagingDir -Recurse -Force
}
if (-not (Test-Path $distDir)) {
    New-Item -ItemType Directory -Path $distDir -Force | Out-Null
}

$pkgRoot = Join-Path $stagingDir "SplitScreenCoop_Installer"
$pkgCore = Join-Path $pkgRoot "BepInEx\core"
$pkgPlugins = Join-Path $pkgRoot "BepInEx\plugins"
$pkgLoaders = Join-Path $pkgRoot "loaders"
New-Item -ItemType Directory -Path $pkgCore -Force | Out-Null
New-Item -ItemType Directory -Path $pkgPlugins -Force | Out-Null
New-Item -ItemType Directory -Path $pkgLoaders -Force | Out-Null

# Top-level files inside ZIP (extracted right next to Lost and Found Co.exe)
Copy-Item -Path (Join-Path $repoRoot "installer\Install_SplitScreenCoop.bat") -Destination $stagingDir -Force
Copy-Item -Path (Join-Path $repoRoot "installer\Uninstall_SplitScreenCoop.bat") -Destination $stagingDir -Force
Copy-Item -Path (Join-Path $repoRoot "installer\INSTALL_GUIDE.txt") -Destination $stagingDir -Force

$licenseFile = Join-Path $repoRoot "LICENSE"
if (Test-Path $licenseFile) {
    Copy-Item -Path $licenseFile -Destination (Join-Path $pkgRoot "LICENSE") -Force
}
$noticesFile = Join-Path $repoRoot "docs\THIRD_PARTY_NOTICES.md"
if (Test-Path $noticesFile) {
    Copy-Item -Path $noticesFile -Destination (Join-Path $pkgRoot "THIRD_PARTY_NOTICES.md") -Force
}

if ($null -ne $resolvedDoorstopIni) {
    Copy-Item -Path $resolvedDoorstopIni -Destination (Join-Path $pkgRoot "doorstop_config.ini") -Force
}
else {
    $defaultDoorstop = @(
        "[General]",
        "enabled = true",
        "target_assembly = BepInEx\core\BepInEx.Preloader.dll",
        "redirect_output_log = false",
        "ignore_disable_switch = false"
    )
    [System.IO.File]::WriteAllLines((Join-Path $pkgRoot "doorstop_config.ini"), $defaultDoorstop)
}

foreach ($dll in $bepCoreWhitelist) {
    Copy-Item -Path (Join-Path $resolvedCoreDir $dll) -Destination $pkgCore -Force
}
foreach ($ldr in $loaderWhitelist) {
    Copy-Item -Path (Join-Path $resolvedLoadersDir $ldr) -Destination $pkgLoaders -Force
}
Copy-Item -Path $builtDll -Destination (Join-Path $pkgPlugins "LostAndFound_SplitScreenCoop.dll") -Force

Write-Output "[3/4] Creating release archives in dist\..."
$zipName = "LostAndFound-SplitScreenCoop-v$Version-AutoInstall.zip"
$zipPath = Join-Path $distDir $zipName
$manualDllName = "LostAndFound_SplitScreenCoop.dll"
$manualDllPath = Join-Path $distDir $manualDllName
$sumsPath = Join-Path $distDir "SHA256SUMS.txt"

if (Test-Path $zipPath) { Remove-Item -Path $zipPath -Force }
if (Test-Path $manualDllPath) { Remove-Item -Path $manualDllPath -Force }

Compress-Archive -Path (Join-Path $stagingDir "*") -DestinationPath $zipPath -CompressionLevel Optimal
Copy-Item -Path $builtDll -Destination $manualDllPath -Force

# Audit ZIP entries to ensure zero forbidden third-party mod or game files leaked in.
Add-Type -AssemblyName System.IO.Compression.FileSystem
$forbiddenSubstrings = @("Indonesian", "runtime_id", "Assembly-CSharp", "UnityEngine", "data.unity3d")
$forbiddenExactNames = @("config.ini", "game_bible.md", "glossary.csv", "decisions_log.md")
$zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    foreach ($entry in $zip.Entries) {
        $entryFile = [System.IO.Path]::GetFileName($entry.FullName)
        foreach ($pat in $forbiddenSubstrings) {
            if ($entry.FullName -like "*$pat*") {
                throw "Security/packaging check failed: unexpected file '$($entry.FullName)' found in release ZIP."
            }
        }
        foreach ($exact in $forbiddenExactNames) {
            if ($entryFile -ieq $exact) {
                throw "Security/packaging check failed: unexpected file '$($entry.FullName)' found in release ZIP."
            }
        }
    }
    $entryCount = $zip.Entries.Count
}
finally {
    $zip.Dispose()
}

Write-Output "[4/4] Generating SHA256SUMS.txt..."
$zipHash = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
$dllHash = (Get-FileHash -Path $manualDllPath -Algorithm SHA256).Hash.ToLowerInvariant()
$sumLines = @(
    "$zipHash *$zipName",
    "$dllHash *$manualDllName"
)
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllLines($sumsPath, $sumLines, $utf8NoBom)

Write-Output "[OK] Auto-Install ZIP ($entryCount entries): $zipPath"
Write-Output "[OK] Manual DLL Asset                 : $manualDllPath"
Write-Output "[OK] Checksums                        : $sumsPath"
