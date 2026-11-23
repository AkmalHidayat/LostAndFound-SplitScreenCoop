[CmdletBinding()]
param(
    [string]$GameDir = "",
    [string]$Configuration = "Release",
    [switch]$Deploy
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Test-ValidGameDir {
    param([string]$Candidate)
    if ([string]::IsNullOrWhiteSpace($Candidate)) {
        return $false
    }
    $managed = Join-Path $Candidate "Lost and Found Co_Data\Managed\Assembly-CSharp.dll"
    return (Test-Path $managed)
}

function Resolve-GameDirectory {
    param([string]$ExplicitDir, [string]$RepoRoot)

    $candidates = New-Object System.Collections.Generic.List[string]
    if (-not [string]::IsNullOrWhiteSpace($ExplicitDir)) {
        $candidates.Add($ExplicitDir) | Out-Null
    }
    if (-not [string]::IsNullOrWhiteSpace($env:LOST_AND_FOUND_DIR)) {
        $candidates.Add($env:LOST_AND_FOUND_DIR) | Out-Null
    }

    $parentDir = Split-Path -Parent $RepoRoot
    if (-not [string]::IsNullOrWhiteSpace($parentDir)) {
        $candidates.Add($parentDir) | Out-Null
    }

    try {
        $steamReg = Get-ItemProperty -Path "HKCU:\Software\Valve\Steam" -ErrorAction SilentlyContinue
        if (($null -ne $steamReg) -and ($null -ne $steamReg.SteamPath)) {
            $steamBase = $steamReg.SteamPath -replace "/", "\"
            $candidates.Add((Join-Path $steamBase "steamapps\common\Lost and Found Co")) | Out-Null
            $vdfPath = Join-Path $steamBase "steamapps\libraryfolders.vdf"
            if (Test-Path $vdfPath) {
                $vdfText = Get-Content -Path $vdfPath -Raw
                $matches = [regex]::Matches($vdfText, '"path"\s+"([^"]+)"')
                foreach ($m in $matches) {
                    $libPath = ($m.Groups[1].Value) -replace "\\\\", "\"
                    $candidates.Add((Join-Path $libPath "steamapps\common\Lost and Found Co")) | Out-Null
                }
            }
        }
    }
    catch {
        # Registry lookup is best-effort.
    }

    $defaultLibs = @(
        "C:\Program Files (x86)\Steam\steamapps\common\Lost and Found Co",
        "C:\SteamLibrary\steamapps\common\Lost and Found Co",
        "D:\SteamLibrary\steamapps\common\Lost and Found Co",
        "E:\SteamLibrary\steamapps\common\Lost and Found Co",
        "F:\SteamLibrary\steamapps\common\Lost and Found Co"
    )
    foreach ($lib in $defaultLibs) {
        $candidates.Add($lib) | Out-Null
    }

    foreach ($c in $candidates) {
        if (Test-ValidGameDir $c) {
            return (Resolve-Path $c).Path
        }
    }

    return $null
}

function Find-BepInExCoreDir {
    param([string]$ResolvedGameDir)
    $primary = Join-Path $ResolvedGameDir "BepInEx\core"
    $secondary = Join-Path $ResolvedGameDir "LostAndFoundCo_Mod_ID\BepInEx\core"
    if ((Test-Path (Join-Path $primary "BepInEx.dll")) -and (Test-Path (Join-Path $primary "0Harmony.dll"))) {
        return $primary
    }
    if ((Test-Path (Join-Path $secondary "BepInEx.dll")) -and (Test-Path (Join-Path $secondary "0Harmony.dll"))) {
        return $secondary
    }
    return $null
}

function Find-UnityRoslynPair {
    $hubBase = "C:\Program Files\Unity\Hub\Editor"
    if (-not (Test-Path $hubBase)) {
        return $null
    }
    $editors = Get-ChildItem -Path $hubBase -Directory | Sort-Object Name -Descending
    foreach ($ed in $editors) {
        $dotnet = Join-Path $ed.FullName "Editor\Data\NetCoreRuntime\dotnet.exe"
        $csc = Join-Path $ed.FullName "Editor\Data\DotNetSdkRoslyn\csc.dll"
        if ((Test-Path $dotnet) -and (Test-Path $csc)) {
            return @{
                Dotnet = $dotnet
                Csc = $csc
            }
        }
    }
    return $null
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$srcDir = Join-Path $repoRoot "src"
$csprojPath = Join-Path $srcDir "LostAndFound_SplitScreenCoop.csproj"
$outDir = Join-Path $repoRoot (Join-Path "bin" $Configuration)
$outDll = Join-Path $outDir "LostAndFound_SplitScreenCoop.dll"

$resolvedGameDir = Resolve-GameDirectory -ExplicitDir $GameDir -RepoRoot $repoRoot
if ([string]::IsNullOrWhiteSpace($resolvedGameDir)) {
    throw "Could not locate 'Lost and Found Co'. Pass -GameDir 'C:\Path\To\Lost and Found Co' or set LOST_AND_FOUND_DIR."
}

$bepCoreDir = Find-BepInExCoreDir -ResolvedGameDir $resolvedGameDir
if ([string]::IsNullOrWhiteSpace($bepCoreDir)) {
    throw "Could not find BepInEx 5 core DLLs (BepInEx.dll, 0Harmony.dll) under '$resolvedGameDir'."
}

if (-not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
}

Write-Output "[i] Game directory : $resolvedGameDir"
Write-Output "[i] BepInEx core   : $bepCoreDir"

$hasDotnetSdk = $false
$dotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -ne $dotnetCmd) {
    $sdks = & dotnet --list-sdks 2>$null
    if ($sdks -and ($sdks.Count -gt 0)) {
        $hasDotnetSdk = $true
    }
}

if ($hasDotnetSdk) {
    Write-Output "[...] Building via dotnet SDK ($Configuration)..."
    & dotnet build $csprojPath -c $Configuration "-p:GameDir=$resolvedGameDir" "-p:BepInExCoreDir=$bepCoreDir"
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE"
    }
}
else {
    $roslyn = Find-UnityRoslynPair
    if ($null -eq $roslyn) {
        throw "Neither .NET SDK nor Unity Hub Roslyn (csc.dll) was found. Install .NET 8 SDK to build from source."
    }

    Write-Output "[...] .NET SDK not found; compiling via Unity Roslyn fallback..."
    $managedDir = Join-Path $resolvedGameDir "Lost and Found Co_Data\Managed"
    $refs = @(
        (Join-Path $managedDir "netstandard.dll"),
        (Join-Path $managedDir "mscorlib.dll"),
        (Join-Path $managedDir "System.dll"),
        (Join-Path $managedDir "System.Core.dll"),
        (Join-Path $bepCoreDir "0Harmony.dll"),
        (Join-Path $bepCoreDir "BepInEx.dll"),
        (Join-Path $managedDir "Assembly-CSharp.dll"),
        (Join-Path $managedDir "UnityCommunity.UnitySingleton.Runtime.dll"),
        (Join-Path $managedDir "UnityEngine.dll"),
        (Join-Path $managedDir "UnityEngine.CoreModule.dll"),
        (Join-Path $managedDir "UnityEngine.UI.dll"),
        (Join-Path $managedDir "UnityEngine.UIModule.dll"),
        (Join-Path $managedDir "UnityEngine.Physics2DModule.dll"),
        (Join-Path $managedDir "UnityEngine.IMGUIModule.dll"),
        (Join-Path $managedDir "UnityEngine.TextRenderingModule.dll"),
        (Join-Path $managedDir "UnityEngine.AudioModule.dll"),
        (Join-Path $managedDir "UnityEngine.DirectorModule.dll"),
        (Join-Path $managedDir "Unity.InputSystem.dll"),
        (Join-Path $managedDir "Unity.RenderPipelines.Universal.Runtime.dll"),
        (Join-Path $managedDir "Unity.RenderPipelines.Core.Runtime.dll"),
        (Join-Path $managedDir "Cinemachine.dll"),
        (Join-Path $managedDir "EPO.dll"),
        (Join-Path $managedDir "ZBase.UnityScreenNavigator.dll")
    )

    $sourceFiles = Get-ChildItem -Path $srcDir -Filter "*.cs" | Sort-Object Name
    if (($null -eq $sourceFiles) -or ($sourceFiles.Count -eq 0)) {
        throw "No C# source files found under '$srcDir'."
    }

    $objDir = Join-Path $repoRoot "obj"
    if (-not (Test-Path $objDir)) {
        New-Item -ItemType Directory -Path $objDir -Force | Out-Null
    }
    $rspPath = Join-Path $objDir "compile.rsp"

    $rspLines = New-Object System.Collections.Generic.List[string]
    $rspLines.Add("-target:library")
    if ($Configuration -eq "Release") {
        $rspLines.Add("-optimize+")
    }
    $rspLines.Add("-deterministic")
    $rspLines.Add("-langversion:latest")
    $rspLines.Add("-nostdlib+")
    $rspLines.Add("-unsafe")
    $rspLines.Add("-codepage:65001")
    $rspLines.Add("-warnaserror+")
    $rspLines.Add("-out:`"$outDll`"")

    foreach ($r in $refs) {
        if (-not (Test-Path $r)) {
            throw "Missing reference DLL: $r"
        }
        $rspLines.Add("-r:`"$r`"")
    }

    foreach ($sf in $sourceFiles) {
        $rspLines.Add("`"$($sf.FullName)`"")
    }

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllLines($rspPath, $rspLines, $utf8NoBom)

    & $roslyn.Dotnet exec $roslyn.Csc -noconfig "@$rspPath"
    if ($LASTEXITCODE -ne 0) {
        throw "csc.dll failed with exit code $LASTEXITCODE"
    }
}

if (-not (Test-Path $outDll)) {
    throw "Build completed without producing '$outDll'."
}

$hash = (Get-FileHash -Path $outDll -Algorithm SHA256).Hash
Write-Output "[OK] Built: $outDll"
Write-Output "[OK] SHA256: $hash"

if ($Deploy) {
    $pluginsDir = Join-Path $resolvedGameDir "BepInEx\plugins"
    if (-not (Test-Path $pluginsDir)) {
        New-Item -ItemType Directory -Path $pluginsDir -Force | Out-Null
    }
    $deployTarget = Join-Path $pluginsDir "LostAndFound_SplitScreenCoop.dll"
    Copy-Item -Path $outDll -Destination $deployTarget -Force
    Write-Output "[OK] Deployed to: $deployTarget"
}
