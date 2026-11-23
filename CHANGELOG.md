# Changelog

All notable changes to the **Lost and Found Co. Split-Screen Co-op Mod** are documented in this file.

## [1.0.4] - 2026-11-23

### Added
- **In-game UI multi-language support (`SplitScreenLocalization`)**: Control panel, player badges, P4 standby screen, and toast notifications now follow the active game language automatically (`en`, `id`, `th`, `zh-CN`, `zh-TW`, `ja`, `ko`, `de`, plus fallback definitions for `fr`, `es-ES`, `it`, `ru`, `sv-SE`, `tr`, `pt-BR`, `vi`).
- **Indonesian Localization Mod awareness**: Automatically detects `com.teteh.lostandfoundco.indonesian` (`LostAndFound_Indonesian.dll`), follows Bahasa Indonesia terminology when active, and adds `UiLanguage = Auto` override under `[UI]` in the BepInEx config.
- **Per-script OS font resolution**: Added dynamic font fallback (`Leelawadee UI`, `Microsoft YaHei UI`, `Microsoft JhengHei UI`, `Yu Gothic UI`, `Malgun Gothic`, `Segoe UI`) so Thai, Chinese, Japanese, Korean, Cyrillic, and Vietnamese characters render properly instead of tofu boxes.
- **Public release toolchain**: Added portable `.csproj` game detection, `scripts/build.ps1`, `scripts/package_release.ps1`, non-destructive English `Install_SplitScreenCoop.bat` / `Uninstall_SplitScreenCoop.bat`, multi-language GitHub READMEs, and `docs/BUILDING.md`.

### Changed
- Removed `F9` from the 2-player layout hotkey binding (now `F11` only) so `F9` stays dedicated to the Indonesian Localization Mod's dictionary hot-reload.
- Widened panel buttons, badge containers, and toast text bounds with best-fit text sizing for longer localized strings.
- Updated BepInEx configuration descriptions and startup logs to English.

## [1.0.3] - 2026-11-19

### Fixed
- Restored URP HDR Bloom and `FunkyCode` SmartLighting2D world illumination across all split-screen viewports (`RenderTextureFormat.RGB111110Float`, `ForceOpaqueAlphaRenderPass`, multi-camera `LightingManager2D` lightmap sync, and `LightingMeshRenderer.Reset` texture preservation).

## [1.0.2] - 2026-11-18

### Added
- Automatic fullscreen switching (`AutoFullscreenOnCutscene`) whenever timelined cutscenes, snapshot events, dialogues, floor transitions, or camera tweens lock the camera, returning cleanly to split-screen afterward.

### Fixed
- Removed expensive per-frame `FindObjectsByType` scans and eliminated synchronous GPU readback stalls causing severe frame-rate drops when multiplayer viewports were active.

## [1.0.1] - 2026-11-18

### Fixed
- Eliminated bright white HDR box artifacts around lit sprites and stage transitions when rendering split-screen cameras into offscreen RenderTextures.

## [1.0.0] - 2026-11-14

### Added
- Initial release supporting 2 to 4 local players in *Lost and Found Co.*:
  - Per-player independent cameras, panning, zooming, virtual cursors, clickable 2D raycasts, and click ripples.
  - Player 1 support for either Mouse + Keyboard or Gamepad 1 (`F7`).
  - Gamepad hot-join with `Start`, 2-player vertical/horizontal layouts, 3-4 player grid with P4 standby tile, `F10` control panel, and `F6` P2 keyboard test mode.
