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
$loaderWhitelist = @("winhttp.dll")

$parentGameDir = Split-Path -Parent $repoRoot
$sourceCandidates = New-Object System.Collections.Generic.List[string]
if (-not [string]::IsNullOrWhiteSpace($BepInExSourceDir)) {
    $sourceCandidates.Add($BepInExSourceDir) | Out-Null
}
if (-not [string]::IsNullOrWhiteSpace($GameDir)) {
    $sourceCandidates.Add($GameDir) | Out-Null
}
$sourceCandidates.Add($parentGameDir) | Out-Null
$indonesianInstallerPkg = Join-Path $parentGameDir "IndonesianLocalizationMod\obj\release_staging_auto\IndonesianMod_Installer"
if (Test-Path $indonesianInstallerPkg) {
    $sourceCandidates.Add($indonesianInstallerPkg) | Out-Null
}

$resolvedCoreDir = $null
$resolvedLoadersDir = $null
$resolvedWinhttp = $null
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

    foreach ($winhttpCandidate in @((Join-Path $cand "loaders\winhttp.dll"), (Join-Path $cand "winhttp.dll"))) {
        if ((Test-Path $winhttpCandidate) -and ($null -eq $resolvedWinhttp)) {
            $resolvedWinhttp = $winhttpCandidate
            $resolvedLoadersDir = Split-Path -Parent $winhttpCandidate
        }
    }

    $doorstopTry = Join-Path $cand "doorstop_config.ini"
    if ((Test-Path $doorstopTry) -and ($null -eq $resolvedDoorstopIni)) {
        $resolvedDoorstopIni = $doorstopTry
    }
}

if ($null -eq $resolvedCoreDir) {
    throw "Could not find complete BepInEx 5 core DLL set. Pass -BepInExSourceDir pointing to a folder containing BepInEx\core."
}
if ($null -eq $resolvedWinhttp) {
    throw "Could not find Unity Doorstop winhttp.dll. Pass -BepInExSourceDir pointing to a folder containing winhttp.dll or loaders\winhttp.dll."
}

$winhttpBytes = [System.IO.File]::ReadAllBytes($resolvedWinhttp)
$winhttpAscii = [System.Text.Encoding]::ASCII.GetString($winhttpBytes)
if (($winhttpAscii.IndexOf("doorstop", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) -or
    ($winhttpAscii.IndexOf("lego", [System.StringComparison]::OrdinalIgnoreCase) -ge 0)) {
    throw "Refusing to package '$resolvedWinhttp': binary signature check failed (not genuine Unity Doorstop)."
}

Write-Output "[2/4] Preparing Auto-Install and Manual staging directories..."
$autoStagingDir = Join-Path $repoRoot "obj\release_staging_auto"
$manualStagingDir = Join-Path $repoRoot "obj\release_staging_manual"
$legacyStagingDir = Join-Path $repoRoot "obj\release_staging"
$distDir = Join-Path $repoRoot "dist"

foreach ($dir in @($autoStagingDir, $manualStagingDir, $legacyStagingDir)) {
    if (Test-Path $dir) {
        Remove-Item -Path $dir -Recurse -Force
    }
}
if (-not (Test-Path $distDir)) {
    New-Item -ItemType Directory -Path $distDir -Force | Out-Null
}

function Write-OrCopyDoorstopConfig {
    param([string]$DestinationPath, [string]$SourceIniPath)
    if (-not [string]::IsNullOrWhiteSpace($SourceIniPath) -and (Test-Path $SourceIniPath)) {
        Copy-Item -Path $SourceIniPath -Destination $DestinationPath -Force
    }
    else {
        $defaultDoorstop = @(
            "[General]",
            "enabled = true",
            "target_assembly = BepInEx\core\BepInEx.Preloader.dll",
            "redirect_output_log = false",
            "ignore_disable_switch = false"
        )
        [System.IO.File]::WriteAllLines($DestinationPath, $defaultDoorstop)
    }
}

$licenseFile = Join-Path $repoRoot "LICENSE"
$noticesFile = Join-Path $repoRoot "docs\THIRD_PARTY_NOTICES.md"

# --- Auto-Install ZIP staging ---
$pkgRoot = Join-Path $autoStagingDir "SplitScreenCoop_Installer"
$pkgCore = Join-Path $pkgRoot "BepInEx\core"
$pkgPlugins = Join-Path $pkgRoot "BepInEx\plugins"
$pkgLoaders = Join-Path $pkgRoot "loaders"
New-Item -ItemType Directory -Path $pkgCore -Force | Out-Null
New-Item -ItemType Directory -Path $pkgPlugins -Force | Out-Null
New-Item -ItemType Directory -Path $pkgLoaders -Force | Out-Null

Copy-Item -Path (Join-Path $repoRoot "installer\Install_SplitScreenCoop.bat") -Destination $autoStagingDir -Force
Copy-Item -Path (Join-Path $repoRoot "installer\Uninstall_SplitScreenCoop.bat") -Destination $autoStagingDir -Force
Copy-Item -Path (Join-Path $repoRoot "installer\INSTALL_GUIDE.txt") -Destination $autoStagingDir -Force

if (Test-Path $licenseFile) {
    Copy-Item -Path $licenseFile -Destination (Join-Path $pkgRoot "LICENSE") -Force
}
if (Test-Path $noticesFile) {
    Copy-Item -Path $noticesFile -Destination (Join-Path $pkgRoot "THIRD_PARTY_NOTICES.md") -Force
}
Write-OrCopyDoorstopConfig -DestinationPath (Join-Path $pkgRoot "doorstop_config.ini") -SourceIniPath $resolvedDoorstopIni

foreach ($dll in $bepCoreWhitelist) {
    Copy-Item -Path (Join-Path $resolvedCoreDir $dll) -Destination $pkgCore -Force
}
foreach ($ldr in $loaderWhitelist) {
    Copy-Item -Path (Join-Path $resolvedLoadersDir $ldr) -Destination $pkgLoaders -Force
}
Copy-Item -Path $builtDll -Destination (Join-Path $pkgPlugins "LostAndFound_SplitScreenCoop.dll") -Force

# --- Manual drag-and-drop ZIP staging (ready-to-drop game root folder structure, no .bat scripts) ---
$manualCore = Join-Path $manualStagingDir "BepInEx\core"
$manualPlugins = Join-Path $manualStagingDir "BepInEx\plugins"
New-Item -ItemType Directory -Path $manualCore -Force | Out-Null
New-Item -ItemType Directory -Path $manualPlugins -Force | Out-Null

Copy-Item -Path (Join-Path $resolvedLoadersDir "winhttp.dll") -Destination (Join-Path $manualStagingDir "winhttp.dll") -Force
Write-OrCopyDoorstopConfig -DestinationPath (Join-Path $manualStagingDir "doorstop_config.ini") -SourceIniPath $resolvedDoorstopIni
Copy-Item -Path (Join-Path $repoRoot "installer\MANUAL_INSTALL_GUIDE.txt") -Destination (Join-Path $manualStagingDir "MANUAL_INSTALL_GUIDE.txt") -Force
if (Test-Path $licenseFile) {
    Copy-Item -Path $licenseFile -Destination (Join-Path $manualStagingDir "LICENSE") -Force
}
if (Test-Path $noticesFile) {
    Copy-Item -Path $noticesFile -Destination (Join-Path $manualStagingDir "THIRD_PARTY_NOTICES.md") -Force
}
foreach ($dll in $bepCoreWhitelist) {
    Copy-Item -Path (Join-Path $resolvedCoreDir $dll) -Destination $manualCore -Force
}
Copy-Item -Path $builtDll -Destination (Join-Path $manualPlugins "LostAndFound_SplitScreenCoop.dll") -Force

Write-Output "[3/4] Creating release archives in dist\..."
$autoZipName = "LostAndFound-SplitScreenCoop-v$Version-AutoInstall.zip"
$autoZipPath = Join-Path $distDir $autoZipName
$manualZipName = "LostAndFound-SplitScreenCoop-v$Version-Manual.zip"
$manualZipPath = Join-Path $distDir $manualZipName
$staleDllPath = Join-Path $distDir "LostAndFound_SplitScreenCoop.dll"
$sumsPath = Join-Path $distDir "SHA256SUMS.txt"

foreach ($old in @($autoZipPath, $manualZipPath, $staleDllPath)) {
    if (Test-Path $old) { Remove-Item -Path $old -Force }
}

Compress-Archive -Path (Join-Path $autoStagingDir "*") -DestinationPath $autoZipPath -CompressionLevel Optimal
Compress-Archive -Path (Join-Path $manualStagingDir "*") -DestinationPath $manualZipPath -CompressionLevel Optimal

# Audit ZIP entries to ensure zero forbidden third-party mod or game files leaked in.
Add-Type -AssemblyName System.IO.Compression.FileSystem
$forbiddenSubstrings = @("Indonesian", "runtime_id", "Assembly-CSharp", "UnityEngine", "data.unity3d")
$forbiddenExactNames = @("config.ini", "game_bible.md", "glossary.csv", "decisions_log.md", "version.dll", "winmm.dll")

function Test-ReleaseZipClean {
    param([string]$ZipFilePath, [bool]$DisallowBatScripts)
    $zip = [System.IO.Compression.ZipFile]::OpenRead($ZipFilePath)
    try {
        foreach ($entry in $zip.Entries) {
            $entryFile = [System.IO.Path]::GetFileName($entry.FullName)
            foreach ($pat in $forbiddenSubstrings) {
                if ($entry.FullName -like "*$pat*") {
                    throw "Security/packaging check failed in '$ZipFilePath': unexpected file '$($entry.FullName)'."
                }
            }
            foreach ($exact in $forbiddenExactNames) {
                if ($entryFile -ieq $exact) {
                    throw "Security/packaging check failed in '$ZipFilePath': unexpected file '$($entry.FullName)'."
                }
            }
            if ($DisallowBatScripts -and ($entryFile -like "*.bat")) {
                throw "Security/packaging check failed in '$ZipFilePath': manual archive must not contain .bat scripts ('$($entry.FullName)')."
            }
        }
        return $zip.Entries.Count
    }
    finally {
        $zip.Dispose()
    }
}

$autoEntryCount = Test-ReleaseZipClean -ZipFilePath $autoZipPath -DisallowBatScripts $false
$manualEntryCount = Test-ReleaseZipClean -ZipFilePath $manualZipPath -DisallowBatScripts $true

Write-Output "[4/4] Generating SHA256SUMS.txt..."
$autoHash = (Get-FileHash -Path $autoZipPath -Algorithm SHA256).Hash.ToLowerInvariant()
$manualHash = (Get-FileHash -Path $manualZipPath -Algorithm SHA256).Hash.ToLowerInvariant()
$sumLines = @(
    "$autoHash *$autoZipName",
    "$manualHash *$manualZipName"
)
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllLines($sumsPath, $sumLines, $utf8NoBom)

Write-Output "[OK] Auto-Install ZIP ($autoEntryCount entries) : $autoZipPath"
Write-Output "[OK] Manual ZIP ($manualEntryCount entries)       : $manualZipPath"
Write-Output "[OK] Checksums                        : $sumsPath"
