# Lost and Found Co. Split-Screen Co-op Mod v1.0.4

## Highlights in v1.0.4

- **Automatic Multi-Language In-Game UI**: Control panel (`F10`), player corner badges, P4 standby tile, and toast alerts now follow the game's active language setting (`English`, `Thai`, `Simplified Chinese`, `Traditional Chinese`, `Japanese`, `Korean`, `German`, plus `Bahasa Indonesia` when the Indonesian Localization Mod is installed, and fallback tables for community I2 slots `fr`, `es-ES`, `it`, `ru`, `sv-SE`, `tr`, `pt-BR`, `vi`).
- **Dynamic OS Font Support**: Added per-script dynamic font resolution so Thai, Chinese, Japanese, Korean, Cyrillic, and Vietnamese text renders cleanly in mod overlays.
- **Full Coexistence with Indonesian Localization Mod**: Automatically detects `LostAndFound_Indonesian.dll` (`com.teteh.lostandfoundco.indonesian`), aligns UI labels with its glossary, and moves layout switching solely to `F11` so `F9` remains free for dictionary hot-reload.
- **Three Public Installation Options**: Auto-Install ZIP (with non-destructive English installer/uninstaller), standalone manual DLL, and portable source build scripts.

## Which Download Should You Pick?

1. **`LostAndFound-SplitScreenCoop-v1.0.4-AutoInstall.zip` (Recommended for most players)**
   - Complete package with one-click `Install_SplitScreenCoop.bat` and `Uninstall_SplitScreenCoop.bat`.
   - Includes BepInEx 5.4.23.5 + Doorstop loaders (`LGPL-2.1` / `MIT`, see `THIRD_PARTY_NOTICES.md`).
   - Safe to extract directly on top of an existing BepInEx or Indonesian Localization Mod setup: the installer automatically detects existing loaders and only adds `LostAndFound_SplitScreenCoop.dll` without overwriting anything.

2. **`LostAndFound_SplitScreenCoop.dll` (Manual drag-and-drop)**
   - For players who already have BepInEx 5 (x64) working in *Lost and Found Co.*
   - Drop this file into `<Game Folder>\BepInEx\plugins\`.

3. **`Source code (zip/tar.gz)`**
   - For developers or players who prefer compiling `LostAndFound_SplitScreenCoop.dll` themselves. Follow `docs/BUILDING.md`.

## Checksums (`SHA256SUMS.txt`)

Compare downloaded files against `SHA256SUMS.txt` attached to this release.
