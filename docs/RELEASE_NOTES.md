# Lost and Found Co. Split-Screen Co-op Mod v1.0.4

**Tested Game Version**: **Lost and Found Co. Patch 1.2.2** (27 Oct 2026, Steam Build `26480281`, Unity `6000.0.84f1`, Windows x64) | **Mod Loader**: BepInEx 5.4.23.5 (x64)

## Other Mods by AkmalHidayat

- **[Lost and Found Co. - Indonesian Localization Mod](https://github.com/AkmalHidayat/LostAndFound-IndonesianMod)**: Companion Bahasa Indonesia translation mod (16,718 entries) tested and updated through **Patch 1.2.2** (future game updates adding new levels after 1.2.2 will require a dictionary update to cover newly added text).

## Highlights in v1.0.4

- **Automatic Multi-Language In-Game UI**: Control panel (`F10`), player corner badges, P4 standby tile, and toast alerts now follow the game's active language setting (`English`, `Thai`, `Simplified Chinese`, `Traditional Chinese`, `Japanese`, `Korean`, `German`, plus `Bahasa Indonesia` when the Indonesian Localization Mod is installed, and fallback tables for community I2 slots `fr`, `es-ES`, `it`, `ru`, `sv-SE`, `tr`, `pt-BR`, `vi`).
- **Dynamic OS Font Support**: Added per-script dynamic font resolution so Thai, Chinese, Japanese, Korean, Cyrillic, and Vietnamese text renders cleanly in mod overlays.
- **Full Coexistence with Indonesian Localization Mod**: Automatically detects [`LostAndFound_Indonesian.dll`](https://github.com/AkmalHidayat/LostAndFound-IndonesianMod) (`com.teteh.lostandfoundco.indonesian`), aligns UI labels with its glossary, and moves layout switching solely to `F11` so `F9` remains free for dictionary hot-reload.
- **Three Public Installation Options**: Auto-Install ZIP (with non-destructive English installer/uninstaller bundling genuine Unity Doorstop `winhttp.dll`), standalone manual DLL, and portable source build scripts.

## Which Download Should You Pick?

1. **`LostAndFound-SplitScreenCoop-v1.0.4-AutoInstall.zip` (Recommended for most players)**
   - Complete package with one-click `Install_SplitScreenCoop.bat` and `Uninstall_SplitScreenCoop.bat`.
   - Includes BepInEx 5.4.23.5 + Doorstop loaders (`LGPL-2.1` / `MIT`, see `THIRD_PARTY_NOTICES.md`).
   - Safe to extract directly on top of an existing BepInEx or Indonesian Localization Mod setup: the installer automatically detects existing loaders and only adds `LostAndFound_SplitScreenCoop.dll` without overwriting anything.

2. **`LostAndFound-SplitScreenCoop-v1.0.4-Manual.zip` (Manual drag-and-drop package)**
   - Contains the full ready-to-drop folder layout (`winhttp.dll`, `doorstop_config.ini`, `BepInEx\core\`, and `BepInEx\plugins\LostAndFound_SplitScreenCoop.dll`) without `.bat` scripts.
   - Extract and drag the contents into `<Game Folder>` (clean setup) or copy only `BepInEx\plugins\LostAndFound_SplitScreenCoop.dll` if BepInEx 5 or the Indonesian Localization Mod is already installed.

3. **`Source code (zip/tar.gz)`**
   - For developers or players who prefer compiling `LostAndFound_SplitScreenCoop.dll` themselves. Follow `docs/BUILDING.md`.

## Checksums (`SHA256SUMS.txt`)

Compare downloaded files against `SHA256SUMS.txt` attached to this release.
