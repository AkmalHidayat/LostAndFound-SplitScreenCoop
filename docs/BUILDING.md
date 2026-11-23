# Building from Source

This guide explains how to compile `LostAndFound_SplitScreenCoop.dll` directly from the source code in `src/`. Game binaries and BepInEx assemblies are not included in this repository, so the build links against the copies in your local *Lost and Found Co.* Steam installation.

## Prerequisites

1. **Legitimate copy of *Lost and Found Co.* (Windows x64)** installed via Steam.
2. **BepInEx 5.4.x (x64)** present in your game directory (at minimum `BepInEx/core/BepInEx.dll` and `BepInEx/core/0Harmony.dll`).
   - If you do not have BepInEx installed yet, download BepInEx 5 x64 from the official BepInEx GitHub Releases page and extract it into your game folder, or grab the Auto-Install ZIP from this repository's Releases page.
3. **One of the following C# toolchains:**
   - **.NET SDK 8.0 or newer** (recommended for contributors): `dotnet build`
   - Or **Unity 6 Editor** installed via Unity Hub (the included PowerShell script automatically falls back to Unity's bundled Roslyn compiler if no .NET SDK is installed).

## Step 1: Clone the Repository

```powershell
git clone https://github.com/AkmalHidayat/LostAndFound-SplitScreenCoop.git
cd LostAndFound-SplitScreenCoop
```

## Step 2: Locate Your Game Folder

Find the folder containing `Lost and Found Co.exe` and `Lost and Found Co_Data\Managed\Assembly-CSharp.dll`. Common Steam paths include:

- `C:\Program Files (x86)\Steam\steamapps\common\Lost and Found Co`
- `D:\SteamLibrary\steamapps\common\Lost and Found Co`

Both build methods below check common Steam library locations automatically, so you only need to pass the path explicitly if you use a custom folder.

## Step 3: Compile the Mod

### Option A: Using the PowerShell Build Script (Recommended)

From the repository root, run:

```powershell
powershell -ExecutionPolicy Bypass -File ".\scripts\build.ps1"
```

To specify a custom game path and copy the built DLL straight into `BepInEx\plugins\`:

```powershell
powershell -ExecutionPolicy Bypass -File ".\scripts\build.ps1" -GameDir "D:\Games\Lost and Found Co" -Deploy
```

The script auto-detects Steam libraries via the Windows registry (`libraryfolders.vdf`), selects `dotnet build` when a .NET SDK is installed (or Unity Roslyn as a fallback), and writes the compiled assembly to:

```text
bin\Release\LostAndFound_SplitScreenCoop.dll
```

### Option B: Using `dotnet build` Directly

With `.NET SDK 8.0+` installed:

```powershell
dotnet build .\src\LostAndFound_SplitScreenCoop.csproj -c Release -p:GameDir="C:\Program Files (x86)\Steam\steamapps\common\Lost and Found Co"
```

You can also set an environment variable once instead of passing `-p:GameDir` every time:

```powershell
$env:LOST_AND_FOUND_DIR = "C:\Program Files (x86)\Steam\steamapps\common\Lost and Found Co"
dotnet build .\src\LostAndFound_SplitScreenCoop.csproj -c Release
```

## Step 4: Install the Built DLL

Copy the output file:

```text
bin\Release\LostAndFound_SplitScreenCoop.dll
```

into your game's plugins folder:

```text
<Game Folder>\BepInEx\plugins\LostAndFound_SplitScreenCoop.dll
```

Launch *Lost and Found Co.* via Steam and verify in `<Game Folder>\BepInEx\LogOutput.log` that `Lost and Found Co Split-Screen Co-op v1.0.4` loaded without errors.

## Troubleshooting Builds

- **`Game directory not found or missing Assembly-CSharp.dll`**: Pass `-GameDir` / `-p:GameDir` pointing to the folder containing `Lost and Found Co.exe`, not the `_Data` subfolder.
- **`BepInEx 5 core DLLs were not found`**: Extract BepInEx 5 (x64, not BepInEx 6 IL2CPP) into the game folder so `BepInEx\core\BepInEx.dll` and `BepInEx\core\0Harmony.dll` exist before compiling.
- **Spaces in path**: Always wrap paths containing `"Lost and Found Co"` in double quotes when passing arguments in PowerShell or Command Prompt.
- **Character encoding when editing translations**: Save `src/SplitScreenLocalization.cs` as UTF-8. The build script passes `-codepage:65001` so Thai, CJK, Cyrillic, and accented strings compile identically on all Windows system locales.
