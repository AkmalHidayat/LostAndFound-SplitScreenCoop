# Third-Party Notices

The source code of **Lost and Found Co. Split-Screen Co-op Mod** (`src/`, `scripts/`, `installer/`) is licensed under the **MIT License** (see [LICENSE](../LICENSE)).

The pre-packaged **Auto-Install Release ZIP** (`LostAndFound-SplitScreenCoop-v*-AutoInstall.zip`) distributes the following third-party mod loader components under their respective open-source licenses so new users can install the mod in one step without assembling dependencies manually. These third-party binaries are excluded from the Git source tree.

## 1. BepInEx 5 (5.4.23.5)

- **Project**: https://github.com/BepInEx/BepInEx
- **License**: GNU Lesser General Public License v2.1 (LGPL-2.1)
- **Bundled files in release archive**:
  - `BepInEx/core/BepInEx.dll`
  - `BepInEx/core/BepInEx.Preloader.dll`
  - `BepInEx/core/BepInEx.Harmony.dll`
  - `BepInEx/core/HarmonyXInterop.dll`
  - `BepInEx/core/Mono.Cecil.dll`, `Mono.Cecil.Mdb.dll`, `Mono.Cecil.Pdb.dll`, `Mono.Cecil.Rocks.dll`
  - `BepInEx/core/MonoMod.RuntimeDetour.dll`, `MonoMod.Utils.dll`
- Full LGPL-2.1 license text and source code are available in the upstream BepInEx repository.

## 2. HarmonyX / 0Harmony

- **Project**: https://github.com/BepInEx/HarmonyX
- **License**: MIT License
- **Bundled files in release archive**:
  - `BepInEx/core/0Harmony.dll`
  - `BepInEx/core/0Harmony20.dll`

## 3. Unity Doorstop

- **Project**: https://github.com/NeighTools/UnityDoorstop
- **License**: GNU Lesser General Public License v2.1 (LGPL-2.1)
- **Bundled files in release archive**:
  - `loaders/winhttp.dll`
  - `doorstop_config.ini`
