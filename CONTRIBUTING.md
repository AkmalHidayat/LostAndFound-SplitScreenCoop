# Contributing

Thank you for helping improve **Lost and Found Co. Split-Screen Co-op Mod**.

## Guidelines

1. **Never commit copyrighted game files or third-party binaries.**
   - Do not add `Assembly-CSharp.dll`, Unity engine DLLs, `data.unity3d`, BepInEx binaries, or Doorstop DLLs to any commit or pull request. `.gitignore` blocks these files; do not force-add them.
2. **Keep changes scoped and verifiable.**
   - Follow the instructions in [docs/BUILDING.md](docs/BUILDING.md) to confirm your code compiles cleanly with zero warnings (`-warnaserror+` is enabled in `scripts/build.ps1`).
3. **Preserve multi-mod coexistence.**
   - Avoid hijacking keys already used by companion community mods (notably `F9`, which is reserved for the Indonesian Localization Mod hot-reload) and keep installer changes strictly non-destructive.

## Improving or Adding In-Game Translations

All mod UI strings live in `src/SplitScreenLocalization.cs`.

- Each key in `enum LocKey` must have an entry in every language table.
- Keep format placeholders (`{0}`, `{1}`) identical to the English `"en"` table.
- Save `src/SplitScreenLocalization.cs` as **UTF-8** without changing line endings needlessly.
- Test longer strings in the `F10` control panel and bottom toast banner to confirm they stay readable without clipping.

## Pull Request Checklist

- Built cleanly in `Release` mode against a genuine Steam copy of *Lost and Found Co.*
- Tested affected gameplay features (input isolation, viewport split, cutscene return, or UI localization) in-game.
- Updated `CHANGELOG.md` and relevant `README` / `docs/README.*.md` files when user-facing behavior or hotkeys change.
