**English** | [Bahasa Indonesia](docs/README.id.md) | [ไทย](docs/README.th.md) | [简体中文](docs/README.zh-CN.md) | [繁體中文](docs/README.zh-TW.md) | [日本語](docs/README.ja.md) | [한국어](docs/README.ko.md) | [Deutsch](docs/README.de.md)

# Lost and Found Co. - Split-Screen Co-op Mod

A BepInEx 5 local split-screen co-op mod for **Lost and Found Co.** (Windows x64) supporting 2 to 4 players on the same screen, with independent cameras, virtual cursors, device input isolation, automatic cutscene fullscreen handling, and multi-language mod UI.

> **Tested Game Version**: **Lost and Found Co. Patch 1.2.2** (27 Oct 2026, Steam Build `26480281`, Unity `6000.0.84f1`, Windows x64). Minor patches typically remain compatible unless camera, input, or lighting internals change; if new levels or events are added later, the companion **[Indonesian Localization Mod](https://github.com/AkmalHidayat/LostAndFound-IndonesianMod)** will also need a dictionary update to cover newly added text.

## Features

- **2 to 4 Local Players**:
  - **2 Players**: Vertical (Left/Right) or Horizontal (Top/Bottom) split, toggled anytime with `F11`.
  - **3 Players**: 2x2 grid with a dedicated P4 "Ready to Join" standby slot.
  - **4 Players**: Full 2x2 quad split-screen.
- **Input Isolation & Mixed Devices**:
  - Player 1 uses **Mouse + Keyboard** by default while connected gamepads automatically control P2-P4, or press `F7` so Gamepad 1 drives Player 1.
  - Press `Start` on any connected gamepad to hot-join co-op, or toggle `F6` for single-keyboard P2 testing (`IJKL`, Arrow keys, `U`/`O`, `Enter`).
- **Independent Cameras & Raycast Interaction**:
  - Each viewport renders its own `WorldCamera` with independent pan, zoom, `Physics2D` click/drag raycasts, click ripple feedback, and UI overlays (hold `Y` or `L3` to reach full-screen HUD buttons and the To-Do List).
- **Automatic Cutscene Fullscreen (`AutoFullscreenOnCutscene`)**:
  - Temporarily returns to single fullscreen whenever timelines, snapshot events, dialogue camera locks, floor transitions, or camera tweens take over, then restores each player's split-screen viewport afterward.
- **URP HDR Bloom & 2D Lighting Compatibility**:
  - Preserves HDR Bloom (`RGB111110Float`) and synchronizes `FunkyCode` SmartLighting2D lightmaps across all active player viewports.
- **Multi-Language In-Game UI**:
  - Automatically follows the game's active language setting across official languages (`en`, `th`, `zh-CN`, `zh-TW`, `ja`, `ko`, `de`), **Bahasa Indonesia (`id`)** when the Indonesian Localization Mod is active, plus fallback tables for community/reserved I2 slots (`fr`, `es-ES`, `it`, `ru`, `sv-SE`, `tr`, `pt-BR`, `vi`).
  - Uses per-script Windows OS font lookup so Thai, CJK, Cyrillic, and Vietnamese text render cleanly without missing glyphs.

## Other Mods by AkmalHidayat & Compatibility

We also built a companion localization mod for this game: **[Lost and Found Co. - Indonesian Localization Mod](https://github.com/AkmalHidayat/LostAndFound-IndonesianMod)** (`LostAndFound_Indonesian.dll`, GUID `com.teteh.lostandfoundco.indonesian`):

- **Full Bahasa Indonesia Translation (Updated Through Patch 1.2.2)**: Activates I2 Localization Slot 14 (`Bahasa Indonesia`) at runtime with **16,718 translated entries** covering UI, dialogue, items, objectives, and To-Do lists without modifying original game files. *(Note: if future game updates add new levels, events, or dialogue beyond Patch 1.2.2, newly added text is not covered until the dictionary is updated.)*
- **Automatic Bahasa Indonesia Co-op UI**: When the Indonesian Localization Mod sets the game language to Bahasa Indonesia, the split-screen panel, badges, standby tile, and toasts automatically switch to Bahasa Indonesia using consistent terms (such as `Daftar Tugas` for the To-Do List).
- **No Hotkey Conflicts**: Two-player layout switching is bound strictly to `F11`, leaving `F9` free for the Indonesian mod's dictionary hot-reload.
- **Safe Coexistence Installer**: Both mods share BepInEx 5 cleanly. Running `Install_SplitScreenCoop.bat` or `Uninstall_SplitScreenCoop.bat` never overwrites or removes `LostAndFound_Indonesian.dll`, `runtime_id.json`, or its Doorstop proxy loader.

---

## Installation

Choose the method that fits your setup.

### Method 1: Auto-Install ZIP (Recommended)

1. Download **`LostAndFound-SplitScreenCoop-v1.0.4-AutoInstall.zip`** from the [GitHub Releases](https://github.com/AkmalHidayat/LostAndFound-SplitScreenCoop/releases) page.
2. Extract all files from the ZIP directly into your main **Lost and Found Co.** game folder (the folder containing `Lost and Found Co.exe`).
   - Default Steam path: `C:\Program Files (x86)\Steam\steamapps\common\Lost and Found Co`
3. Double-click **`Install_SplitScreenCoop.bat`**.
   - If BepInEx 5 is not installed yet, the installer places Unity Doorstop (`winhttp.dll`) without overwriting existing files and installs BepInEx 5.4.23.5 + this mod.
   - If BepInEx 5 (or the Indonesian Localization Mod) is already installed, the installer keeps your existing loader untouched and only adds `BepInEx\plugins\LostAndFound_SplitScreenCoop.dll`.
4. Launch **Lost and Found Co.** through Steam.

To remove the mod later, double-click **`Uninstall_SplitScreenCoop.bat`** inside the game folder. It verifies SHA-256 hashes before deleting anything and keeps BepInEx intact if other plugins are still installed.

### Method 2: Manual DLL Drag-and-Drop

Use this option if you already have **BepInEx 5.4.x (x64)** set up for **Lost and Found Co.**:

1. Download **`LostAndFound_SplitScreenCoop.dll`** from the [GitHub Releases](https://github.com/AkmalHidayat/LostAndFound-SplitScreenCoop/releases) page.
2. Copy `LostAndFound_SplitScreenCoop.dll` into:
   ```text
   <Game Folder>\BepInEx\plugins\LostAndFound_SplitScreenCoop.dll
   ```
3. Launch the game via Steam.

### Method 3: Build from Source

Compile the DLL yourself from `src/` against your local Steam copy of the game:

```powershell
git clone https://github.com/AkmalHidayat/LostAndFound-SplitScreenCoop.git
cd LostAndFound-SplitScreenCoop
powershell -ExecutionPolicy Bypass -File ".\scripts\build.ps1" -Deploy
```

Or with `.NET SDK 8.0+`:

```powershell
dotnet build .\src\LostAndFound_SplitScreenCoop.csproj -c Release -p:GameDir="C:\Program Files (x86)\Steam\steamapps\common\Lost and Found Co"
```

See [docs/BUILDING.md](docs/BUILDING.md) for prerequisites, environment variable options, and troubleshooting.

---

## Controls & Hotkeys

### Keyboard Shortcuts

| Key | Action |
| --- | --- |
| `F8` | Toggle Split-Screen Co-op ON / OFF |
| `F11` | Switch 2-Player layout (Left/Right or Top/Bottom) |
| `F7` | Switch Player 1 between Mouse + Keyboard and Gamepad 1 |
| `F10` | Open or close the Split-Screen Control Panel |
| `F6` | Toggle P2 Keyboard Test Mode (`IJKL` cursor, Arrows pan, `U`/`O` zoom, `Enter` click) |

### Gamepad Controls (P2-P4, and P1 when Gamepad 1 mode is enabled)

| Input | Action |
| --- | --- |
| `Left Stick` | Move player cursor |
| `Right Stick` | Pan camera smoothly inside world bounds |
| `LT` / `LB` and `RT` / `RB` | Zoom out / Zoom in anchored to player cursor |
| `A` (South Button) | Click / Drag objects in the scene |
| Hold `Y` or `L3` | Temporarily unlock cursor across full screen to click HUD / To-Do List |
| Press `R3` | Reset camera position and zoom to Player 1's view |
| `Start` | Hot-join split-screen from any connected gamepad |

---

## Configuration

After launching the game once with the mod active, BepInEx generates:

```text
<Game Folder>\BepInEx\config\com.teteh.lostandfound.splitscreencoop.cfg
```

| Section | Key | Default | Description |
| --- | --- | --- | --- |
| `Coop` | `AutoEnableMultiGamepad` | `true` | Automatically enable split-screen when 2 or more gamepads are detected |
| `Coop` | `AutoFullscreenOnCutscene` | `true` | Switch temporarily to fullscreen during cutscenes or camera locks |
| `Coop` | `Player1UseGamepad` | `false` | `false`: P1 uses Mouse + Keyboard; `true`: P1 uses Gamepad 1 |
| `Coop` | `TwoPlayerLayout` | `VerticalLeftRight` | Default 2P split (`VerticalLeftRight` or `HorizontalTopBottom`) |
| `UI` | `UiLanguage` | `Auto` | `Auto` follows game language, or set `en`, `id`, `th`, `zh-CN`, `zh-TW`, `ja`, `ko`, `de`, `fr`, `es-ES`, `it`, `ru`, `sv-SE`, `tr`, `pt-BR`, `vi` |
| `Controls` | `GamepadCursorSpeed` | `780` | Left-stick cursor speed scaled for 1080p height |
| `Controls` | `GamepadCameraPanSpeed` | `1.75` | Right-stick camera pan multiplier |
| `Controls` | `GamepadZoomSpeed` | `18` | Trigger and bumper zoom speed for P2-P4 |
| `Visual` | `CursorScale` | `1.0` | Visual scale multiplier for P2-P4 cursors |
| `Performance` | `OptimizeMainCameraCull` | `true` | Disable redundant world pass on `Camera.main` during split-screen |

---

## Troubleshooting & FAQ

- **My controller does not move any cursor**: Check Windows USB Game Controllers (`joy.cpl`) or Steam Input settings for the game, and press `Start` on the controller while in-game. Press `F10` to check which slots see connected gamepads.
- **Only 1 gamepad is connected, how do I test split-screen?**: Press `F8` then `F6` (or press `Start` on your controller) to activate P2 Keyboard Test Mode or pair Gamepad 1 to P2 while P1 stays on Mouse + Keyboard.
- **Cutscene stays fullscreen or UI looks stuck**: Press `F8` twice to reset split-screen viewports, or toggle `AutoFullscreenOnCutscene = false` in the `.cfg` file.
- **Is this safe to use with Steam achievements / save files?**: Yes. *Lost and Found Co.* is a local single-player puzzle adventure with no anti-cheat, and this mod runs purely in memory without modifying save data or game asset archives.

---

## License & Credits

- Mod source code, scripts, and installers are licensed under the [MIT License](LICENSE) (Copyright (c) 2026 AkmalHidayat).
- The Auto-Install ZIP redistributes **BepInEx 5**, **HarmonyX**, and **Unity Doorstop** under their respective open-source licenses. See [docs/THIRD_PARTY_NOTICES.md](docs/THIRD_PARTY_NOTICES.md).
- Built with community contributions welcome: see [CONTRIBUTING.md](CONTRIBUTING.md) and [CHANGELOG.md](CHANGELOG.md).

> **Disclaimer**: This is an unofficial community mod and is not affiliated with, endorsed by, or supported by **Bit Egg Inc.**, the developers of *Lost and Found Co.*
