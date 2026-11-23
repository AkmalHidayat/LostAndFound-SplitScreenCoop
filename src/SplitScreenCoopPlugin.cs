using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace LostAndFoundSplitScreenCoop
{
    /// <summary>
    /// BepInEx entrypoint registering configuration, Harmony patches, and the persistent SplitScreenCoopManager.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class SplitScreenCoopPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.teteh.lostandfound.splitscreencoop";
        public const string PluginName = "Lost and Found Co Split-Screen Co-op";
        public const string PluginVersion = "1.0.4";

        private Harmony _harmony;

        private void Awake()
        {
            ConfigEntry<bool> autoEnable = Config.Bind(
                "Coop",
                "AutoEnableMultiGamepad",
                true,
                "Automatically enable split-screen when 2 or more gamepads are detected.");

            ConfigEntry<bool> autoFullscreenCutscene = Config.Bind(
                "Coop",
                "AutoFullscreenOnCutscene",
                true,
                "Automatically switch back to temporary fullscreen during cutscenes or camera locks, then resume split-screen afterward.");

            ConfigEntry<bool> p1UseGamepad = Config.Bind(
                "Coop",
                "Player1UseGamepad",
                false,
                "When false, P1 uses Mouse + Keyboard and Gamepad 1 controls P2. Quick toggle via F7.");

            ConfigEntry<TwoPlayerLayoutMode> layout2P = Config.Bind(
                "Coop",
                "TwoPlayerLayout",
                TwoPlayerLayoutMode.VerticalLeftRight,
                "Default split-screen layout for 2 players (VerticalLeftRight or HorizontalTopBottom). Quick toggle via F11 or F10 panel.");

            ConfigEntry<string> uiLanguage = Config.Bind(
                "UI",
                "UiLanguage",
                "Auto",
                "Mod UI language. Auto follows the game's active language setting (including Bahasa Indonesia when the Indonesian Localization Mod is installed), or force a code: en, id, th, zh-CN, zh-TW, ja, ko, de, fr, es-ES, it, ru, sv-SE, tr, pt-BR, vi.");

            ConfigEntry<float> cursorSpeed = Config.Bind(
                "Controls",
                "GamepadCursorSpeed",
                780f,
                "Left-stick virtual cursor movement speed scaled for 1080p height.");

            ConfigEntry<float> panSpeed = Config.Bind(
                "Controls",
                "GamepadCameraPanSpeed",
                1.75f,
                "Right-stick camera panning speed multiplier.");

            ConfigEntry<float> zoomSpeed = Config.Bind(
                "Controls",
                "GamepadZoomSpeed",
                18f,
                "Trigger and bumper zoom speed for P2-P4.");

            ConfigEntry<float> cursorScale = Config.Bind(
                "Visual",
                "CursorScale",
                1.0f,
                "Visual scale multiplier for P2-P4 cursors.");

            ConfigEntry<bool> optimizeMainCull = Config.Bind(
                "Performance",
                "OptimizeMainCameraCull",
                true,
                "Disable duplicate world rendering on Camera.main while split-screen RenderTextures are active.");

            try
            {
                _harmony = new Harmony(PluginGuid);
                _harmony.PatchAll(typeof(SplitScreenPatches).Assembly);

                var host = new GameObject("SplitScreenCoop_Host")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                DontDestroyOnLoad(host);

                SplitScreenCoopManager mgr = host.AddComponent<SplitScreenCoopManager>();
                mgr.Initialize(
                    Logger,
                    autoEnable,
                    autoFullscreenCutscene,
                    p1UseGamepad,
                    layout2P,
                    uiLanguage,
                    cursorSpeed,
                    panSpeed,
                    zoomSpeed,
                    cursorScale,
                    optimizeMainCull);

                Logger.LogInfo($"{PluginName} v{PluginVersion} ready. Hotkeys: F8 (Split-Screen), F11 (2P Layout), F7 (P1 Input), F10 (Panel), F6 (P2 Keyboard Test).");
                if (SplitScreenLocalization.IsIndonesianModInstalled)
                {
                    Logger.LogInfo("Detected Lost and Found Co. Indonesian Localization Mod; mod UI will follow Bahasa Indonesia when active and keep F9 free for translation hot-reload.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to initialize SplitScreenCoopPlugin: {ex}");
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
