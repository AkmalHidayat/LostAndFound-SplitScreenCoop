using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;
using BITEGG;
using BITEGG.Hidden;
using BITEGG.Hidden.CoreUI.Cinematic;
using BITEGG.Hidden.CoreUI.PersistentLauncher;
using BITEGG.Hidden.Dialogue;
using BITEGG.Hidden.GameInput;
using BITEGG.Hidden.GameInput.GameCamera;
using BITEGG.Hidden.Settings;
using BITEGG.Utility;
using BITEGG.Utility.InputEvents;
using EPOOutline;
using FunkyCode;
using FunkyCode.LightingSettings;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using GameCameraSettings = BITEGG.Hidden.GameInput.GameCamera.CameraSettings;
using LightingCameraSettings = FunkyCode.CameraSettings;
using Object = UnityEngine.Object;

namespace LostAndFoundSplitScreenCoop
{
    /// <summary>
    /// Coordinates split-screen viewports, per-player cameras, input isolation, cursors, and overlay UI.
    /// </summary>
    internal sealed class SplitScreenCoopManager : MonoBehaviour
    {
        public static SplitScreenCoopManager Instance { get; private set; }

        private static readonly AccessTools.FieldRef<CameraBackground, SpriteRenderer> BgRendererRef =
            AccessTools.FieldRefAccess<CameraBackground, SpriteRenderer>("_background");

        private static readonly AccessTools.FieldRef<PersistentUILauncher, Canvas> PersistentCanvasRef =
            AccessTools.FieldRefAccess<PersistentUILauncher, Canvas>("canvas");

        private static readonly AccessTools.FieldRef<LightingManager2D> LightingManagerInstanceRef =
            AccessTools.StaticFieldRefAccess<LightingManager2D>(AccessTools.Field(typeof(LightingManager2D), "instance"));

        private static readonly AccessTools.FieldRef<CutsceneManager, PlayableDirector> CutsceneCurrentCsRef =
            AccessTools.FieldRefAccess<CutsceneManager, PlayableDirector>("currentCS");

        private static readonly AccessTools.FieldRef<FillScreenSprite, SpriteRenderer> FillSpriteRendererRef =
            AccessTools.FieldRefAccess<FillScreenSprite, SpriteRenderer>("renderer");

        private static readonly AccessTools.FieldRef<CameraController, bool> CcIsTweeningRef =
            AccessTools.FieldRefAccess<CameraController, bool>("_isTweening");

        private static readonly AccessTools.FieldRef<CinematicSignalObserver, bool> CsoTransitioningRef =
            AccessTools.FieldRefAccess<CinematicSignalObserver, bool>("_transitioning");

        private static readonly AccessTools.FieldRef<UniversalAdditionalCameraData, int> UacdRendererIndexRef =
            AccessTools.FieldRefAccess<UniversalAdditionalCameraData, int>("m_RendererIndex");

        private delegate void PairDeviceDelegate(GameInputController instance, InputDevice device);
        private static readonly PairDeviceDelegate GicPairDeviceMethod =
            AccessTools.MethodDelegate<PairDeviceDelegate>(AccessTools.Method(typeof(GameInputController), "PairDevice"));

        private static readonly Action<bool> SetCimIsOverUi =
            AccessTools.MethodDelegate<Action<bool>>(AccessTools.PropertySetter(typeof(CustomInputModule), "IsOverUI"));

        private static readonly List<FillScreenSprite> ActiveFillSprites = new List<FillScreenSprite>(8);

        private ManualLogSource _log;
        private ConfigEntry<bool> _cfgAutoEnableMultiGamepad;
        private ConfigEntry<bool> _cfgAutoFullscreenCutscene;
        private ConfigEntry<bool> _cfgPlayer1UseGamepad;
        private ConfigEntry<TwoPlayerLayoutMode> _cfgLayout2P;
        private ConfigEntry<string> _cfgUiLanguage;
        private ConfigEntry<float> _cfgCursorSpeed;
        private ConfigEntry<float> _cfgPanSpeed;
        private ConfigEntry<float> _cfgZoomSpeed;
        private ConfigEntry<float> _cfgCursorScale;
        private ConfigEntry<bool> _cfgOptimizeMainCamCull;

        private readonly PlayerCoopState[] _players = new PlayerCoopState[4];
        private readonly HashSet<InputDevice> _modBlacklistedDevices = new HashSet<InputDevice>();
        private readonly List<RaycastResult> _uiRaycastScratch = new List<RaycastResult>(16);
        private readonly GameObject[] _lastUiDownTarget = new GameObject[4];

        private bool _coopEnabled;
        private bool _manuallyDisabled;
        private bool _p2KeyboardTestMode;
        private bool _player1UsesFirstGamepad;
        private TwoPlayerLayoutMode _layout2P = TwoPlayerLayoutMode.VerticalLeftRight;
        private int _activePlayerCount = 1;
        private int _lastKnownGamepadCount = -1;

        private bool _cutsceneFullscreenActive;
        private float _cutsceneReleaseTimer;
        private const float CutsceneReleaseGraceSeconds = 0.35f;
        private int _preCutsceneFloorIndex = -1;
        private int _preCutsceneDimensionIndex = -1;
        private bool _wasSplitScreenRendering;

        private Canvas _viewportCanvas;
        private Canvas _overlayCanvas;
        private Canvas _panelCanvas;
        private GraphicRaycaster _panelRaycaster;
        private Camera _boundUiCamera;

        private GameObject _quad4StandbyRoot;
        private Text _standbyTitleText;
        private Text _standbySubText;
        private GameObject _divVertical;
        private GameObject _divHorizontal;
        private GameObject _divBottomHalf;
        private GameObject _divRightHalf;

        private GameObject _toastRoot;
        private Text _toastText;
        private float _toastTimer;

        private GameObject _panelRoot;
        private bool _panelExpanded;
        private Text _panelHeaderText;
        private Text _panelGuideText;
        private Text _btnCloseText;
        private Text[] _slotStatusTexts;
        private Text _btnModeText;
        private Text _btnLayoutText;
        private Text _btnP1InputText;
        private Text _btnP2TestText;
        private float _nextLanguageCheckTime;
        private string _appliedUiLanguageCode;

        private Camera _trackedMainCamera;
        private UniversalAdditionalCameraData _cachedMainUacd;
        private Outliner _cachedMainOutliner;
        private bool _mainCameraModified;
        private int _savedMainCullingMask;
        private CameraClearFlags _savedMainClearFlags;
        private Color _savedMainBgColor;
        private bool _savedMainOutlinerEnabled;

        private LightingManager2D _trackedLightingManager;
        private LightingCameraSettings[] _savedOriginalLightingCameras;
        private int _lastConfiguredLightingCamCount = -1;
        private bool _loggedLightingDiagnosticsForManager;
        private int _lastCameraSyncFrame = -1;
        private bool _initialFillSpritesScanned;
        private PointerEventData _fallbackPointerEventData;
        private ForceOpaqueAlphaRenderPass _opaqueAlphaPass;

        public int ActiveRaycastPlayerIndex { get; private set; }
        public bool Player1UsesFirstGamepad => _player1UsesFirstGamepad;
        public int ActivePlayerCount => _activePlayerCount;

        public bool IsSplitScreenRendering =>
            _coopEnabled &&
            _activePlayerCount >= 2 &&
            !_cutsceneFullscreenActive &&
            CameraController.Current != null &&
            CameraController.Current.MainCamera != null &&
            UICamera.Current != null &&
            UICamera.Current.Camera != null;

        public void Initialize(
            ManualLogSource log,
            ConfigEntry<bool> autoEnableMultiGamepad,
            ConfigEntry<bool> autoFullscreenCutscene,
            ConfigEntry<bool> player1UseGamepad,
            ConfigEntry<TwoPlayerLayoutMode> layout2P,
            ConfigEntry<string> uiLanguage,
            ConfigEntry<float> cursorSpeed,
            ConfigEntry<float> panSpeed,
            ConfigEntry<float> zoomSpeed,
            ConfigEntry<float> cursorScale,
            ConfigEntry<bool> optimizeMainCamCull)
        {
            Instance = this;
            _log = log;
            _cfgAutoEnableMultiGamepad = autoEnableMultiGamepad;
            _cfgAutoFullscreenCutscene = autoFullscreenCutscene;
            _cfgPlayer1UseGamepad = player1UseGamepad;
            _cfgLayout2P = layout2P;
            _cfgUiLanguage = uiLanguage;
            _cfgCursorSpeed = cursorSpeed;
            _cfgPanSpeed = panSpeed;
            _cfgZoomSpeed = zoomSpeed;
            _cfgCursorScale = cursorScale;
            _cfgOptimizeMainCamCull = optimizeMainCamCull;

            _player1UsesFirstGamepad = player1UseGamepad.Value;
            _layout2P = layout2P.Value;

            for (int i = 0; i < 4; i++)
            {
                _players[i] = new PlayerCoopState(i);
            }

            SplitScreenLocalization.RefreshCurrentLanguage(_cfgUiLanguage != null ? _cfgUiLanguage.Value : "Auto");
            try
            {
                I2.Loc.LocalizationManager.OnLocalizeEvent += OnGameLanguageChanged;
            }
            catch (Exception)
            {
                // Ignore if I2 Localization is not yet initialized.
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _opaqueAlphaPass = new ForceOpaqueAlphaRenderPass();
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        private void OnDestroy()
        {
            try
            {
                I2.Loc.LocalizationManager.OnLocalizeEvent -= OnGameLanguageChanged;
            }
            catch (Exception)
            {
                // Ignore during teardown.
            }
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            _opaqueAlphaPass?.Dispose();
            _opaqueAlphaPass = null;
            RestoreFunkyCodeLightingCameras();
            RestoreMainCameraState();
            ReleaseAllPlayerResources();
            SplitScreenTextures.DestroyAll();
            ClearAllBlacklistedDevices();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static void RegisterFillScreenSprite(FillScreenSprite fill)
        {
            if (fill != null && !ActiveFillSprites.Contains(fill))
            {
                ActiveFillSprites.Add(fill);
            }
        }

        public static void UnregisterFillScreenSprite(FillScreenSprite fill)
        {
            if (fill != null)
            {
                ActiveFillSprites.Remove(fill);
            }
        }

        public PlayerCoopState GetPlayerState(int index)
        {
            return index >= 0 && index < _players.Length ? _players[index] : null;
        }

        public Rect GetPlayerViewport(int index)
        {
            PlayerCoopState p = GetPlayerState(index);
            return p != null ? p.PixelViewport : new Rect(0f, 0f, Screen.width, Screen.height);
        }

        public bool ShouldBlockDeviceFromMainUser(InputDevice device)
        {
            if (device == null || !_coopEnabled || _activePlayerCount < 2)
            {
                return false;
            }

            if (device is Gamepad pad)
            {
                if (!_player1UsesFirstGamepad)
                {
                    return true;
                }
                Gamepad p1Pad = _players[0]?.AssignedGamepad;
                return p1Pad == null || pad != p1Pad;
            }

            return false;
        }

        private void Update()
        {
            if (Time.unscaledTime >= _nextLanguageCheckTime)
            {
                _nextLanguageCheckTime = Time.unscaledTime + 0.5f;
                RefreshLocalizationIfNeeded();
            }

            HandleKeyboardHotkeys();
            ReconcileGamepadsAndAutoJoin();
            UpdateMainCameraLifecycle();
            UpdateCutsceneFullscreenState();

            bool rendering = IsSplitScreenRendering;
            if (!rendering)
            {
                if (_wasSplitScreenRendering || _mainCameraModified || _trackedLightingManager != null)
                {
                    RestoreFunkyCodeLightingCameras();
                    RestoreMainCameraState();
                    DeactivateOverlayVisuals();
                    ClearHoveredAndDraggedStates();
                }
                _wasSplitScreenRendering = false;
                SyncDeviceBlacklist(_coopEnabled && _activePlayerCount >= 2 && _cutsceneFullscreenActive);
                UpdateToastAndPanelUi(false);
                return;
            }

            _wasSplitScreenRendering = true;
            EnsureCanvasesAttached();
            SyncDeviceBlacklist(true);
            UpdatePlayerViewportsAndCameras();
            UpdatePlayersP2ToP4Input();
            UpdateP1GamepadUnlockFlag();
            ApplyMainCameraOptimization();
            UpdateOverlayVisuals();
            UpdateToastAndPanelUi(true);
        }

        private void HandleKeyboardHotkeys()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null)
            {
                return;
            }

            if (kb.f8Key.wasPressedThisFrame)
            {
                ToggleCoopMode();
            }
            // Keep F9 free for LostAndFound_Indonesian.dll dictionary hot-reload.
            if (kb.f11Key.wasPressedThisFrame)
            {
                ToggleTwoPlayerLayout();
            }
            if (kb.f7Key.wasPressedThisFrame)
            {
                TogglePlayer1InputMode();
            }
            if (kb.f6Key.wasPressedThisFrame)
            {
                ToggleP2KeyboardTestMode();
            }
            if (kb.f10Key.wasPressedThisFrame)
            {
                TogglePanelExpanded();
            }
        }

        public void ToggleCoopMode()
        {
            SetCoopEnabled(!_coopEnabled, fromUserHotkey: true);
        }

        public void ToggleTwoPlayerLayout()
        {
            _layout2P = _layout2P == TwoPlayerLayoutMode.VerticalLeftRight
                ? TwoPlayerLayoutMode.HorizontalTopBottom
                : TwoPlayerLayoutMode.VerticalLeftRight;
            _cfgLayout2P.Value = _layout2P;
            string name = SplitScreenLocalization.Get(_layout2P == TwoPlayerLayoutMode.VerticalLeftRight
                ? LocKey.LayoutVertical
                : LocKey.LayoutHorizontal);
            ShowToast(SplitScreenLocalization.Format(LocKey.ToastLayoutChanged, name));
        }

        public void TogglePlayer1InputMode()
        {
            _player1UsesFirstGamepad = !_player1UsesFirstGamepad;
            _cfgPlayer1UseGamepad.Value = _player1UsesFirstGamepad;
            ReassignSlots();

            if (!_player1UsesFirstGamepad && Mouse.current != null && GameInputController.IsInstanceExist)
            {
                ClearAllBlacklistedDevices();
                GicPairDeviceMethod(GameInputController.Instance, Mouse.current);
            }
            else if (_player1UsesFirstGamepad && _players[0].AssignedGamepad != null && GameInputController.IsInstanceExist)
            {
                ClearAllBlacklistedDevices();
                GicPairDeviceMethod(GameInputController.Instance, _players[0].AssignedGamepad);
            }

            string mode = SplitScreenLocalization.Get(_player1UsesFirstGamepad
                ? LocKey.InputGamepad1
                : LocKey.InputMouseKb);
            ShowToast(SplitScreenLocalization.Format(LocKey.ToastP1InputChanged, mode));
        }

        public void ToggleP2KeyboardTestMode()
        {
            _p2KeyboardTestMode = !_p2KeyboardTestMode;
            if (_p2KeyboardTestMode && !_coopEnabled)
            {
                SetCoopEnabled(true, fromUserHotkey: true);
            }
            ShowToast(SplitScreenLocalization.Get(_p2KeyboardTestMode
                ? LocKey.ToastP2TestOn
                : LocKey.ToastP2TestOff));
        }

        public void TogglePanelExpanded()
        {
            _panelExpanded = !_panelExpanded;
            if (_panelExpanded)
            {
                EnsureCanvasesAttached();
                RefreshLocalizationIfNeeded();
            }
        }

        private void SetCoopEnabled(bool enabled, bool fromUserHotkey)
        {
            _coopEnabled = enabled;
            if (fromUserHotkey)
            {
                _manuallyDisabled = !enabled;
            }

            if (enabled)
            {
                ReassignSlots();
                int count = _activePlayerCount;
                if (count < 2)
                {
                    ShowToast(SplitScreenLocalization.Get(LocKey.ToastCoopReady));
                }
                else
                {
                    ShowToast(SplitScreenLocalization.Format(LocKey.ToastCoopActive, count));
                }
                ResetSecondaryCamerasToMain();
            }
            else
            {
                _cutsceneFullscreenActive = false;
                _cutsceneReleaseTimer = 0f;
                RestoreFunkyCodeLightingCameras();
                RestoreMainCameraState();
                DeactivateOverlayVisuals();
                ClearHoveredAndDraggedStates();
                ClearAllBlacklistedDevices();
                ShowToast(SplitScreenLocalization.Get(LocKey.ToastSoloMode));
            }
        }

        private void ReconcileGamepadsAndAutoJoin()
        {
            var gamepads = Gamepad.all;
            int padCount = gamepads.Count;

            if (padCount != _lastKnownGamepadCount)
            {
                _lastKnownGamepadCount = padCount;
                if (padCount >= 2 && !_coopEnabled && !_manuallyDisabled && _cfgAutoEnableMultiGamepad.Value)
                {
                    SetCoopEnabled(true, fromUserHotkey: false);
                }
                else if (padCount == 1 && !_coopEnabled && CameraController.Current != null)
                {
                    ShowToast(SplitScreenLocalization.Get(LocKey.ToastOneGamepadDetected));
                }
            }

            // Allow any connected gamepad to hot-join by pressing Start or Select+A.
            for (int i = 0; i < padCount; i++)
            {
                Gamepad pad = gamepads[i];
                if (pad == null)
                {
                    continue;
                }

                bool startPressed = pad.startButton.wasPressedThisFrame;
                if (startPressed && (!_coopEnabled || !IsGamepadAssignedToActiveSlot(pad)))
                {
                    _manuallyDisabled = false;
                    if (!_coopEnabled)
                    {
                        _coopEnabled = true;
                        if (i == 0 && _player1UsesFirstGamepad)
                        {
                            _player1UsesFirstGamepad = false;
                            _cfgPlayer1UseGamepad.Value = false;
                        }
                    }

                    ReassignSlots();
                    ResetSecondaryCamerasToMain();
                    ShowToast(SplitScreenLocalization.Format(LocKey.ToastCoopJoined, _activePlayerCount));
                    break;
                }
            }

            ReassignSlots();
        }

        private bool IsGamepadAssignedToActiveSlot(Gamepad pad)
        {
            for (int i = 0; i < 4; i++)
            {
                if (_players[i].IsActive && _players[i].AssignedGamepad == pad)
                {
                    return true;
                }
            }
            return false;
        }

        private void ReassignSlots()
        {
            var gamepads = Gamepad.all;
            for (int i = 0; i < 4; i++)
            {
                _players[i].AssignedGamepad = null;
                _players[i].UseKeyboardFallback = false;
            }

            int padIdx = 0;
            _players[0].IsActive = true;
            if (_player1UsesFirstGamepad && padIdx < gamepads.Count)
            {
                _players[0].AssignedGamepad = gamepads[padIdx++];
            }

            int highestActive = 0;
            if (_coopEnabled)
            {
                for (int slot = 1; slot < 4; slot++)
                {
                    if (slot == 1 && _p2KeyboardTestMode)
                    {
                        _players[slot].UseKeyboardFallback = true;
                        _players[slot].IsActive = true;
                        if (padIdx < gamepads.Count)
                        {
                            _players[slot].AssignedGamepad = gamepads[padIdx++];
                        }
                        highestActive = slot;
                        continue;
                    }

                    if (padIdx < gamepads.Count)
                    {
                        _players[slot].AssignedGamepad = gamepads[padIdx++];
                        _players[slot].IsActive = true;
                        highestActive = slot;
                    }
                    else
                    {
                        _players[slot].IsActive = false;
                    }
                }
            }
            else
            {
                for (int slot = 1; slot < 4; slot++)
                {
                    _players[slot].IsActive = false;
                }
            }

            _activePlayerCount = highestActive + 1;
        }

        private void SyncDeviceBlacklist(bool blockNonP1Gamepads)
        {
            if (!GameInputController.IsInstanceExist)
            {
                return;
            }

            List<InputDevice> list = GameInputController.Instance.BlacklistedDevices;
            if (list == null)
            {
                return;
            }

            if (!blockNonP1Gamepads)
            {
                ClearAllBlacklistedDevices();
                return;
            }

            var gamepads = Gamepad.all;
            Gamepad allowedPad = _player1UsesFirstGamepad ? _players[0].AssignedGamepad : null;

            for (int i = 0; i < gamepads.Count; i++)
            {
                Gamepad pad = gamepads[i];
                if (pad == null)
                {
                    continue;
                }

                if (pad != allowedPad)
                {
                    if (!list.Contains(pad))
                    {
                        list.Add(pad);
                    }
                    _modBlacklistedDevices.Add(pad);
                }
                else if (_modBlacklistedDevices.Remove(pad))
                {
                    list.Remove(pad);
                }
            }
        }

        private void ClearAllBlacklistedDevices()
        {
            if (!GameInputController.IsInstanceExist)
            {
                _modBlacklistedDevices.Clear();
                return;
            }

            List<InputDevice> list = GameInputController.Instance.BlacklistedDevices;
            if (list != null)
            {
                foreach (InputDevice dev in _modBlacklistedDevices)
                {
                    list.Remove(dev);
                }
            }
            _modBlacklistedDevices.Clear();
        }

        private void UpdateMainCameraLifecycle()
        {
            Camera currentMain = CameraController.Current != null ? CameraController.Current.MainCamera : Camera.main;
            if (currentMain != _trackedMainCamera)
            {
                RestoreFunkyCodeLightingCameras();
                _initialFillSpritesScanned = false;
                _loggedLightingDiagnosticsForManager = false;
                RestoreMainCameraState();
                _trackedMainCamera = currentMain;
                _cachedMainUacd = currentMain != null ? currentMain.GetComponent<UniversalAdditionalCameraData>() : null;
                _cachedMainOutliner = currentMain != null ? currentMain.GetComponent<Outliner>() : null;
                for (int i = 0; i < 4; i++)
                {
                    _players[i].CameraSettingsSynced = false;
                }
                for (int i = 1; i < 4; i++)
                {
                    _players[i].CameraInitialized = false;
                    if (_players[i].HasAnyHoverOrDrag)
                    {
                        _players[i].ClearHoverAndDrag(_fallbackPointerEventData);
                    }
                }
            }
        }

        private bool DetectActiveCutsceneOrCameraFocus()
        {
            if (SnapShotManager.Instance != null &&
                (SnapShotManager.Instance.InProgress || SnapShotManager.Instance.IsLockCamera()))
            {
                return true;
            }

            CinematicSignalObserver cso = CinematicSignalObserver.Current;
            if (cso != null &&
                (cso.IsActive || cso.IsLockCamera() || CsoTransitioningRef(cso)))
            {
                return true;
            }

            if (CutsceneManager.IsInstanceExist && CutsceneManager.Instance != null)
            {
                PlayableDirector activeCs = CutsceneCurrentCsRef(CutsceneManager.Instance);
                if (activeCs != null && activeCs.state == PlayState.Playing)
                {
                    return true;
                }
            }

            if (CameraController.Current != null && CcIsTweeningRef(CameraController.Current))
            {
                return true;
            }

            if (DimensionManager.Current != null && DimensionManager.Current.InTransition)
            {
                return true;
            }

            if (DialogueManager.IsInstanceExist &&
                DialogueManager.Instance != null &&
                DialogueManager.Instance.IsLockCamera())
            {
                return true;
            }

            return false;
        }

        private void UpdateCutsceneFullscreenState()
        {
            if (!_coopEnabled ||
                _activePlayerCount < 2 ||
                _cfgAutoFullscreenCutscene == null ||
                !_cfgAutoFullscreenCutscene.Value ||
                CameraController.Current == null)
            {
                _cutsceneFullscreenActive = false;
                _cutsceneReleaseTimer = 0f;
                return;
            }

            bool rawActive = DetectActiveCutsceneOrCameraFocus();
            if (rawActive)
            {
                _cutsceneReleaseTimer = CutsceneReleaseGraceSeconds;
                if (!_cutsceneFullscreenActive)
                {
                    _cutsceneFullscreenActive = true;
                    _preCutsceneFloorIndex = FloorManager.Current != null ? FloorManager.Current.CurrentIndex : -1;
                    _preCutsceneDimensionIndex = DimensionManager.Current != null ? DimensionManager.Current.CurrentDimensionIndex : -1;
                    RestoreFunkyCodeLightingCameras();
                    RestoreMainCameraState();
                    DeactivateOverlayVisuals();
                    ClearHoveredAndDraggedStates();
                }
                return;
            }

            if (!_cutsceneFullscreenActive)
            {
                return;
            }

            _cutsceneReleaseTimer -= Time.unscaledDeltaTime;
            if (_cutsceneReleaseTimer > 0f)
            {
                return;
            }

            _cutsceneFullscreenActive = false;
            _cutsceneReleaseTimer = 0f;

            int curFloor = FloorManager.Current != null ? FloorManager.Current.CurrentIndex : -1;
            int curDim = DimensionManager.Current != null ? DimensionManager.Current.CurrentDimensionIndex : -1;
            if (curFloor != _preCutsceneFloorIndex || curDim != _preCutsceneDimensionIndex)
            {
                ResetSecondaryCamerasToMain();
            }
            else
            {
                CameraController cc = CameraController.Current;
                if (cc != null && cc.settings != null)
                {
                    for (int i = 1; i < 4; i++)
                    {
                        PlayerCoopState p = _players[i];
                        if (!p.IsActive)
                        {
                            continue;
                        }

                        float maxZoom = PlayerCoopState.ComputeMaxZoom(cc, p.Aspect);
                        float minZoom = Mathf.Min(cc.settings.zoomMin, maxZoom);
                        p.TargetZoom = Mathf.Clamp(p.TargetZoom, minZoom, maxZoom);
                        p.Zoom = Mathf.Clamp(p.Zoom, minZoom, maxZoom);
                        p.CamPos = PlayerCoopState.ClampPositionToWorldRect(cc, p.CamPos, p.Zoom, p.Aspect);
                        p.PanVelocity = Vector2.zero;
                    }
                }
            }
        }

        private void ApplyMainCameraOptimization()
        {
            Camera mainCam = CameraController.Current != null ? CameraController.Current.MainCamera : null;
            if (mainCam == null || !_cfgOptimizeMainCamCull.Value)
            {
                return;
            }

            if (!_mainCameraModified)
            {
                _savedMainCullingMask = mainCam.cullingMask;
                _savedMainClearFlags = mainCam.clearFlags;
                _savedMainBgColor = mainCam.backgroundColor;

                Outliner mainOutliner = _cachedMainOutliner != null ? _cachedMainOutliner : mainCam.GetComponent<Outliner>();
                if (mainOutliner != null)
                {
                    _savedMainOutlinerEnabled = mainOutliner.enabled;
                    mainOutliner.enabled = false;
                }
                _mainCameraModified = true;
            }

            // Skip redundant full-screen world render on Camera.main while UICamera composites P1-P4 RenderTextures.
            if (mainCam.cullingMask != 0)
            {
                mainCam.cullingMask = 0;
            }
            if (mainCam.clearFlags != CameraClearFlags.SolidColor)
            {
                mainCam.clearFlags = CameraClearFlags.SolidColor;
            }
            Color optBg = new Color(0.11f, 0.09f, 0.08f, 1f);
            if (mainCam.backgroundColor != optBg)
            {
                mainCam.backgroundColor = optBg;
            }
        }

        private void RestoreMainCameraState()
        {
            if (!_mainCameraModified)
            {
                return;
            }

            if (_trackedMainCamera != null)
            {
                _trackedMainCamera.cullingMask = _savedMainCullingMask;
                _trackedMainCamera.clearFlags = _savedMainClearFlags;
                _trackedMainCamera.backgroundColor = _savedMainBgColor;

                Outliner mainOutliner = _cachedMainOutliner != null ? _cachedMainOutliner : _trackedMainCamera.GetComponent<Outliner>();
                if (mainOutliner != null)
                {
                    mainOutliner.enabled = _savedMainOutlinerEnabled;
                }
            }

            _mainCameraModified = false;
        }

        public void OnMainCameraReset(Vector2 pos, float orthoSize)
        {
            for (int i = 1; i < 4; i++)
            {
                PlayerCoopState p = _players[i];
                p.CamPos = PlayerCoopState.ClampPositionToWorldRect(CameraController.Current, pos, orthoSize, p.Aspect);
                p.Zoom = orthoSize;
                p.TargetZoom = orthoSize;
                p.PanVelocity = Vector2.zero;
                p.CameraInitialized = true;
            }
        }

        private void ResetSecondaryCamerasToMain()
        {
            CameraController cc = CameraController.Current;
            if (cc == null)
            {
                return;
            }
            OnMainCameraReset(cc.CameraPosition, cc.CameraZoom);
            for (int i = 0; i < 4; i++)
            {
                if (_players[i].IsActive)
                {
                    _players[i].NormViewport = PlayerCoopState.GetNormalizedViewport(i, _activePlayerCount, _layout2P);
                    _players[i].PixelViewport = ToPixelRect(_players[i].NormViewport);
                    _players[i].CenterCursorInViewport();
                }
            }
        }

        private void UpdatePlayerViewportsAndCameras()
        {
            CameraController cc = CameraController.Current;
            Camera mainCam = cc.MainCamera;
            int uiLayer = LayerMask.NameToLayer("UI");
            int worldMask = _mainCameraModified ? _savedMainCullingMask : mainCam.cullingMask;
            if (uiLayer >= 0)
            {
                worldMask &= ~(1 << uiLayer);
            }

            Color clearBg = _mainCameraModified ? _savedMainBgColor : mainCam.backgroundColor;
            CameraClearFlags clearFlags = _mainCameraModified ? _savedMainClearFlags : mainCam.clearFlags;

            for (int i = 0; i < 4; i++)
            {
                PlayerCoopState p = _players[i];
                if (!p.IsActive)
                {
                    if (p.WorldCamera != null && p.WorldCamera.enabled)
                    {
                        p.WorldCamera.enabled = false;
                    }
                    if (p.ViewportImage != null && p.ViewportImage.gameObject.activeSelf)
                    {
                        p.ViewportImage.gameObject.SetActive(false);
                    }
                    continue;
                }

                p.NormViewport = PlayerCoopState.GetNormalizedViewport(i, _activePlayerCount, _layout2P);
                p.PixelViewport = ToPixelRect(p.NormViewport);

                EnsurePlayerWorldCamera(p, mainCam, worldMask, clearFlags, clearBg);
                p.EnsureRenderTexture(mainCam.allowHDR);

                if (p.ViewportImage != null)
                {
                    GameObject vpGo = p.ViewportImage.gameObject;
                    if (!vpGo.activeSelf)
                    {
                        vpGo.SetActive(true);
                    }

                    if (p.LastAppliedViewportNorm != p.NormViewport)
                    {
                        RectTransform rt = p.ViewportImage.rectTransform;
                        rt.anchorMin = new Vector2(p.NormViewport.x, p.NormViewport.y);
                        rt.anchorMax = new Vector2(p.NormViewport.xMax, p.NormViewport.yMax);
                        rt.offsetMin = Vector2.zero;
                        rt.offsetMax = Vector2.zero;
                        p.LastAppliedViewportNorm = p.NormViewport;
                    }

                    if (p.ViewportImage.texture != p.TargetTexture)
                    {
                        p.ViewportImage.texture = p.TargetTexture;
                    }
                }

                if (!p.CameraInitialized)
                {
                    p.CamPos = PlayerCoopState.ClampPositionToWorldRect(cc, cc.CameraPosition, cc.CameraZoom, p.Aspect);
                    p.Zoom = cc.CameraZoom;
                    p.TargetZoom = cc.CameraZoom;
                    p.PanVelocity = Vector2.zero;
                    p.CenterCursorInViewport();
                    p.CameraInitialized = true;
                }
            }
        }

        private void EnsurePlayerWorldCamera(
            PlayerCoopState p,
            Camera mainCam,
            int worldMask,
            CameraClearFlags clearFlags,
            Color clearBg)
        {
            if (p.WorldCamera == null)
            {
                var go = new GameObject($"SplitCoop_WorldCam_{p.Label}");
                go.transform.SetParent(transform, false);
                p.WorldCamera = go.AddComponent<Camera>();
                p.WorldCamera.useOcclusionCulling = false;
                p.CameraData = go.AddComponent<UniversalAdditionalCameraData>();
                p.ClonedOutliner = go.AddComponent<Outliner>();
                p.CameraSettingsSynced = false;
            }

            Camera cam = p.WorldCamera;
            if (!cam.enabled)
            {
                cam.enabled = true;
            }

            Outliner srcOutliner = _cachedMainOutliner;
            bool wantOutlinerEnabled = srcOutliner != null && (_mainCameraModified ? _savedMainOutlinerEnabled : srcOutliner.enabled);
            clearBg.a = 1f;

            bool needsSettingsSync = !p.CameraSettingsSynced ||
                p.LastSyncedMainCamera != mainCam ||
                p.LastSyncedCullingMask != worldMask ||
                p.LastSyncedClearFlags != clearFlags ||
                p.LastSyncedBgColor != clearBg ||
                p.LastSyncedOutlinerEnabled != wantOutlinerEnabled;

            if (needsSettingsSync)
            {
                cam.orthographic = true;
                cam.nearClipPlane = mainCam.nearClipPlane;
                cam.farClipPlane = mainCam.farClipPlane;
                cam.cullingMask = worldMask;
                cam.clearFlags = clearFlags;
                cam.backgroundColor = clearBg;
                cam.allowHDR = mainCam.allowHDR;
                cam.allowMSAA = false;
                cam.depth = mainCam.depth - 10f + p.PlayerIndex;
                cam.rect = new Rect(0f, 0f, 1f, 1f);

                UniversalAdditionalCameraData srcUacd = _cachedMainUacd;
                if (srcUacd != null && p.CameraData != null)
                {
                    p.CameraData.SetRenderer(UacdRendererIndexRef(srcUacd));
                    p.CameraData.renderType = CameraRenderType.Base;
                    p.CameraData.renderShadows = srcUacd.renderShadows;
                    p.CameraData.renderPostProcessing = srcUacd.renderPostProcessing;
                    p.CameraData.volumeLayerMask = srcUacd.volumeLayerMask;
                    p.CameraData.volumeTrigger = srcUacd.volumeTrigger;
                    p.CameraData.antialiasing = srcUacd.antialiasing;
                    p.CameraData.antialiasingQuality = srcUacd.antialiasingQuality;
                    p.CameraData.requiresDepthTexture = srcUacd.requiresDepthTexture;
                    p.CameraData.requiresColorTexture = srcUacd.requiresColorTexture;
                    p.CameraData.stopNaN = srcUacd.stopNaN;
                    p.CameraData.dithering = srcUacd.dithering;
                }

                CopyOutlinerSettings(srcOutliner, p.ClonedOutliner, wantOutlinerEnabled);

                p.LastSyncedMainCamera = mainCam;
                p.LastSyncedCullingMask = worldMask;
                p.LastSyncedClearFlags = clearFlags;
                p.LastSyncedBgColor = clearBg;
                p.LastSyncedOutlinerEnabled = wantOutlinerEnabled;
                p.CameraSettingsSynced = true;
            }

            SyncBackgroundClone(p, mainCam);
        }

        private static void CopyOutlinerSettings(Outliner src, Outliner dst, bool desiredEnabled)
        {
            if (dst == null)
            {
                return;
            }

            if (src == null || !desiredEnabled)
            {
                dst.enabled = false;
                return;
            }

            dst.enabled = true;
            dst.PrimarySizeReference = src.PrimarySizeReference;
            dst.PrimaryBufferSizeMode = src.PrimaryBufferSizeMode;
            dst.RenderingStrategy = src.RenderingStrategy;
            dst.RenderStage = src.RenderStage;
            dst.DilateQuality = src.DilateQuality;
            dst.RenderingMode = src.RenderingMode;
            dst.BlurShift = src.BlurShift;
            dst.DilateShift = src.DilateShift;
            dst.OutlineLayerMask = src.OutlineLayerMask;
            dst.PrimaryRendererScale = src.PrimaryRendererScale;
            dst.BlurIterations = src.BlurIterations;
            dst.BlurType = src.BlurType;
            dst.DilateIterations = src.DilateIterations;
        }

        private static void SyncBackgroundClone(PlayerCoopState p, Camera mainCam)
        {
            CameraController cc = CameraController.Current;
            SpriteRenderer srcBg = cc != null && cc.Background != null ? BgRendererRef(cc.Background) : null;
            bool shouldClone = srcBg != null && srcBg.gameObject.activeInHierarchy && srcBg.transform.IsChildOf(mainCam.transform);

            if (!shouldClone)
            {
                if (p.BackgroundClone != null && p.BackgroundClone.gameObject.activeSelf)
                {
                    p.BackgroundClone.gameObject.SetActive(false);
                }
                return;
            }

            if (p.BackgroundClone == null)
            {
                var bgGo = new GameObject($"SplitCoop_Bg_{p.Label}");
                bgGo.transform.SetParent(p.WorldCamera.transform, false);
                p.BackgroundClone = bgGo.AddComponent<SpriteRenderer>();
            }

            SpriteRenderer dst = p.BackgroundClone;
            GameObject dstGo = dst.gameObject;
            if (!dstGo.activeSelf)
            {
                dstGo.SetActive(true);
            }
            if (dst.sprite != srcBg.sprite) dst.sprite = srcBg.sprite;
            if (dst.sharedMaterial != srcBg.sharedMaterial) dst.sharedMaterial = srcBg.sharedMaterial;
            if (dst.color != srcBg.color) dst.color = srcBg.color;
            if (dst.sortingLayerID != srcBg.sortingLayerID) dst.sortingLayerID = srcBg.sortingLayerID;
            if (dst.sortingOrder != srcBg.sortingOrder) dst.sortingOrder = srcBg.sortingOrder;

            Transform dstTr = dst.transform;
            Transform srcTr = srcBg.transform;
            if (dstTr.localPosition != srcTr.localPosition) dstTr.localPosition = srcTr.localPosition;
            if (dstTr.localRotation != srcTr.localRotation) dstTr.localRotation = srcTr.localRotation;
            if (dstTr.localScale != srcTr.localScale) dstTr.localScale = srcTr.localScale;
        }

        public void SyncCamerasAndCursorsAfterCameraUpdate()
        {
            if (!IsSplitScreenRendering)
            {
                return;
            }

            CameraController cc = CameraController.Current;
            Camera mainCam = cc.MainCamera;
            Transform mainTransform = mainCam.transform;
            Vector3 mainPos = mainTransform.position;
            float camZ = mainPos.z;

            PlayableDirector activeCutscene = CutsceneManager.IsInstanceExist && CutsceneManager.Instance != null
                ? CutsceneCurrentCsRef(CutsceneManager.Instance)
                : null;
            bool isTimelinePlaying = activeCutscene != null && activeCutscene.state == PlayState.Playing;

            if (isTimelinePlaying)
            {
                _players[0].CamPos = new Vector2(mainPos.x, mainPos.y);
                _players[0].Zoom = mainCam.orthographicSize;
            }
            else
            {
                _players[0].CamPos = cc.CameraPosition;
                _players[0].Zoom = cc.CameraZoom;
                // Keep Camera.main aligned with P1 gameplay camera so FunkyCode and cutscene listeners never read stale frustums.
                if (Mathf.Abs(mainPos.x - _players[0].CamPos.x) > 1e-5f ||
                    Mathf.Abs(mainPos.y - _players[0].CamPos.y) > 1e-5f)
                {
                    mainTransform.position = new Vector3(_players[0].CamPos.x, _players[0].CamPos.y, camZ);
                }
                if (Mathf.Abs(mainCam.orthographicSize - _players[0].Zoom) > 1e-5f)
                {
                    mainCam.orthographicSize = _players[0].Zoom;
                }
            }

            Quaternion mainRot = mainTransform.rotation;
            for (int i = 0; i < 4; i++)
            {
                PlayerCoopState p = _players[i];
                if (!p.IsActive || p.WorldCamera == null)
                {
                    continue;
                }

                Transform camTr = p.WorldCamera.transform;
                Vector3 curPos = camTr.position;
                if (Mathf.Abs(curPos.x - p.CamPos.x) > 1e-5f ||
                    Mathf.Abs(curPos.y - p.CamPos.y) > 1e-5f ||
                    Mathf.Abs(curPos.z - camZ) > 1e-5f)
                {
                    camTr.position = new Vector3(p.CamPos.x, p.CamPos.y, camZ);
                }

                Quaternion curRot = camTr.rotation;
                if (Mathf.Abs(curRot.x - mainRot.x) > 1e-5f ||
                    Mathf.Abs(curRot.y - mainRot.y) > 1e-5f ||
                    Mathf.Abs(curRot.z - mainRot.z) > 1e-5f ||
                    Mathf.Abs(curRot.w - mainRot.w) > 1e-5f)
                {
                    camTr.rotation = mainRot;
                }

                if (Mathf.Abs(p.WorldCamera.orthographicSize - p.Zoom) > 1e-5f)
                {
                    p.WorldCamera.orthographicSize = p.Zoom;
                }
            }

            int frame = Time.frameCount;
            if (_lastCameraSyncFrame != frame)
            {
                _lastCameraSyncFrame = frame;
                SyncFunkyCodeLightingCameras();
            }
        }

        public void OnBeforeLightingManagerLateUpdate()
        {
            if (!IsSplitScreenRendering)
            {
                return;
            }

            SyncCamerasAndCursorsAfterCameraUpdate();
        }

        private static void ClearFunkyCodeMainBuffersAndRenderModes()
        {
            LightMainBuffer2D.Clear();
            List<OnRenderMode> list = OnRenderMode.List;
            if (list == null || list.Count == 0)
            {
                return;
            }

            OnRenderMode[] snapshot = list.ToArray();
            list.Clear();
            for (int i = 0; i < snapshot.Length; i++)
            {
                OnRenderMode orm = snapshot[i];
                if (orm == null)
                {
                    continue;
                }

                if (orm.meshRenderer != null)
                {
                    orm.meshRenderer.enabled = false;
                }
                orm.mainBuffer = null;
                if (orm.gameObject != null)
                {
                    orm.gameObject.SetActive(false);
                    Destroy(orm.gameObject);
                }
            }
        }

        private void SyncFunkyCodeLightingCameras()
        {
            LightingManager2D lm = LightingManagerInstanceRef();
            if (lm == null || lm.cameras == null)
            {
                return;
            }

            if (_trackedLightingManager != lm)
            {
                LightingCameraSettings[] existing = lm.cameras.cameraSettings;
                bool hasMainCam = false;
                if (existing != null && existing.Length > 0)
                {
                    for (int i = 0; i < existing.Length; i++)
                    {
                        if (existing[i].cameraType == LightingCameraSettings.CameraType.MainCamera)
                        {
                            hasMainCam = true;
                            break;
                        }
                    }
                }

                if (hasMainCam)
                {
                    _savedOriginalLightingCameras = new LightingCameraSettings[existing.Length];
                    for (int i = 0; i < existing.Length; i++)
                    {
                        LightingCameraSettings srcCs = existing[i];
                        srcCs.Lightmaps = CloneCameraLightmaps(srcCs.Lightmaps, forSplitScreen: false);
                        _savedOriginalLightingCameras[i] = srcCs;
                    }
                }
                else
                {
                    _savedOriginalLightingCameras = null;
                }

                _trackedLightingManager = lm;
                _lastConfiguredLightingCamCount = -1;
                _loggedLightingDiagnosticsForManager = false;
            }

            for (int i = 0; i < _activePlayerCount; i++)
            {
                if (!_players[i].IsActive || _players[i].WorldCamera == null)
                {
                    return;
                }
            }

            LightingCameraSettings[] templates = _savedOriginalLightingCameras;
            if (templates == null || templates.Length == 0)
            {
                templates = lm.cameras.cameraSettings;
            }

            int mainCamEntryCount = 0;
            int otherEntryCount = 0;
            if (templates != null)
            {
                for (int i = 0; i < templates.Length; i++)
                {
                    if (templates[i].cameraType == LightingCameraSettings.CameraType.MainCamera)
                    {
                        mainCamEntryCount++;
                    }
                    else
                    {
                        otherEntryCount++;
                    }
                }
            }

            int expectedTotalCount = mainCamEntryCount > 0
                ? mainCamEntryCount * _activePlayerCount + otherEntryCount
                : _activePlayerCount;

            bool needsRebuild = _lastConfiguredLightingCamCount != _activePlayerCount ||
                lm.cameras.cameraSettings == null ||
                lm.cameras.Length != expectedTotalCount;

            if (!needsRebuild)
            {
                if (mainCamEntryCount > 0 && templates != null)
                {
                    int idx = 0;
                    for (int t = 0; t < templates.Length && !needsRebuild; t++)
                    {
                        if (templates[t].cameraType == LightingCameraSettings.CameraType.MainCamera)
                        {
                            for (int pIdx = 0; pIdx < _activePlayerCount; pIdx++)
                            {
                                LightingCameraSettings entry = lm.cameras.cameraSettings[idx++];
                                if (entry.cameraType != LightingCameraSettings.CameraType.Custom ||
                                    entry.customCamera != _players[pIdx].WorldCamera)
                                {
                                    needsRebuild = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            idx++;
                        }
                    }
                }
                else
                {
                    for (int pIdx = 0; pIdx < _activePlayerCount; pIdx++)
                    {
                        LightingCameraSettings entry = lm.cameras.cameraSettings[pIdx];
                        if (entry.cameraType != LightingCameraSettings.CameraType.Custom ||
                            entry.customCamera != _players[pIdx].WorldCamera)
                        {
                            needsRebuild = true;
                            break;
                        }
                    }
                }
            }

            if (!needsRebuild)
            {
                return;
            }

            var expanded = new List<LightingCameraSettings>(expectedTotalCount);
            if (mainCamEntryCount > 0 && templates != null)
            {
                for (int t = 0; t < templates.Length; t++)
                {
                    LightingCameraSettings srcCs = templates[t];
                    if (srcCs.cameraType == LightingCameraSettings.CameraType.MainCamera)
                    {
                        for (int pIdx = 0; pIdx < _activePlayerCount; pIdx++)
                        {
                            var cs = new LightingCameraSettings(expanded.Count)
                            {
                                cameraType = LightingCameraSettings.CameraType.Custom,
                                customCamera = _players[pIdx].WorldCamera,
                                Lightmaps = CloneCameraLightmaps(srcCs.Lightmaps, forSplitScreen: true)
                            };
                            expanded.Add(cs);
                        }
                    }
                    else
                    {
                        LightingCameraSettings copyOther = srcCs;
                        copyOther.id = expanded.Count;
                        copyOther.Lightmaps = CloneCameraLightmaps(srcCs.Lightmaps, forSplitScreen: false);
                        expanded.Add(copyOther);
                    }
                }
            }

            if (expanded.Count == 0)
            {
                LightingCameraSettings tpl = lm.cameras.Length > 0 ? lm.cameras.Get(0) : new LightingCameraSettings(0);
                for (int pIdx = 0; pIdx < _activePlayerCount; pIdx++)
                {
                    var cs = new LightingCameraSettings(expanded.Count)
                    {
                        cameraType = LightingCameraSettings.CameraType.Custom,
                        customCamera = _players[pIdx].WorldCamera,
                        Lightmaps = CloneCameraLightmaps(tpl.Lightmaps, forSplitScreen: true)
                    };
                    expanded.Add(cs);
                }
            }

            lm.cameras.cameraSettings = expanded.ToArray();
            _lastConfiguredLightingCamCount = _activePlayerCount;
            ClearFunkyCodeMainBuffersAndRenderModes();

            for (int i = 0; i < _activePlayerCount; i++)
            {
                if (_players[i].WorldCamera != null)
                {
                    CameraTransform.GetCamera(_players[i].WorldCamera)?.Update(true);
                }
            }

            if (!_loggedLightingDiagnosticsForManager)
            {
                _loggedLightingDiagnosticsForManager = true;
                LogLightingDiagnosticsOnce(lm);
            }
        }

        private void LogLightingDiagnosticsOnce(LightingManager2D lm)
        {
            if (_log == null || lm == null)
            {
                return;
            }

            int mainRendererIdx = _cachedMainUacd != null ? UacdRendererIndexRef(_cachedMainUacd) : -99;
            var lmSummary = new System.Text.StringBuilder();
            CameraLightmap[] srcMaps = _savedOriginalLightingCameras != null && _savedOriginalLightingCameras.Length > 0
                ? _savedOriginalLightingCameras[0].Lightmaps
                : (lm.cameras != null && lm.cameras.Length > 0 ? lm.cameras.Get(0).Lightmaps : null);

            if (srcMaps != null)
            {
                for (int i = 0; i < srcMaps.Length; i++)
                {
                    if (i > 0) lmSummary.Append(';');
                    CameraLightmap m = srcMaps[i];
                    lmSummary.Append($"[{i}:preset={m.presetId},ov={m.overlay},ovMat={m.overlayMaterial},out={m.output},ren={m.rendering}]");
                }
            }

            _log.LogInfo(
                $"[SplitCoop Lighting] RenderingMode={Lighting2D.RenderingMode}, OrigCamSettings={_savedOriginalLightingCameras?.Length ?? 0}, " +
                $"NewCamSettings={lm.cameras?.Length ?? 0}, Lights={Light2D.List?.Count ?? 0}, MeshRenderers={LightingMeshRenderer.GetCount()}, " +
                $"MainHDR={_trackedMainCamera?.allowHDR}, MainRendererIdx={mainRendererIdx}, MainPP={_cachedMainUacd?.renderPostProcessing}, " +
                $"P1RT={_players[0]?.TargetTexture?.format}, Maps={lmSummary}");
        }

        private void RestoreFunkyCodeLightingCameras()
        {
            if (_trackedLightingManager != null &&
                _trackedLightingManager.cameras != null &&
                _savedOriginalLightingCameras != null)
            {
                _trackedLightingManager.cameras.cameraSettings = _savedOriginalLightingCameras;
                ClearFunkyCodeMainBuffersAndRenderModes();
            }

            _trackedLightingManager = null;
            _savedOriginalLightingCameras = null;
            _lastConfiguredLightingCamCount = -1;
            _loggedLightingDiagnosticsForManager = false;
        }

        private static CameraLightmap[] CloneCameraLightmaps(CameraLightmap[] src, bool forSplitScreen)
        {
            if (src == null)
            {
                return null;
            }

            var copy = new CameraLightmap[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                CameraLightmap entry = src[i];
                entry.id = i;
                if (forSplitScreen)
                {
                    entry.sceneView = CameraLightmap.SceneView.Disabled;
                    entry.customMaterialInstance = null;
                    if (entry.overlayMaterial == CameraLightmap.OverlayMaterial.Reference &&
                        entry.customMaterial != null)
                    {
                        entry.overlayMaterial = CameraLightmap.OverlayMaterial.Custom;
                    }
                }
                copy[i] = entry;
            }
            return copy;
        }

        public static void ResizeFillScreenSpriteForCamera(FillScreenSprite fill, Camera cam, float orthoZoomOverride = -1f)
        {
            if (fill == null || cam == null)
            {
                return;
            }

            SpriteRenderer sr = FillSpriteRendererRef(fill);
            if (sr == null)
            {
                sr = fill.GetComponent<SpriteRenderer>();
            }
            if (sr == null || sr.sprite == null)
            {
                return;
            }

            Vector3 camPos = cam.transform.position;
            Vector3 fillPos = fill.transform.position;
            fill.transform.position = new Vector3(camPos.x, camPos.y, fillPos.z);

            Vector3 boundsSize = sr.sprite.bounds.size;
            if (boundsSize.x <= 0.0001f || boundsSize.y <= 0.0001f)
            {
                return;
            }

            float orthoSize = orthoZoomOverride > 0.01f ? orthoZoomOverride : cam.orthographicSize;
            float worldH = orthoSize * 2f;
            float worldW = worldH * cam.aspect;
            fill.transform.localScale = new Vector3(
                (worldW / boundsSize.x) * 1.2f,
                (worldH / boundsSize.y) * 1.2f,
                1f);
        }

        private PlayerCoopState FindActivePlayerByWorldCamera(Camera cam)
        {
            if (cam == null)
            {
                return null;
            }

            for (int i = 0; i < _activePlayerCount && i < _players.Length; i++)
            {
                PlayerCoopState p = _players[i];
                if (p != null && p.IsActive && p.WorldCamera == cam)
                {
                    return p;
                }
            }
            return null;
        }

        private void EnsureInitialFillSpritesScanned()
        {
            if (_initialFillSpritesScanned)
            {
                return;
            }

            _initialFillSpritesScanned = true;
            FillScreenSprite[] found = FindObjectsByType<FillScreenSprite>(FindObjectsSortMode.None);
            if (found == null)
            {
                return;
            }

            for (int i = 0; i < found.Length; i++)
            {
                FillScreenSprite fill = found[i];
                if (fill != null && fill.gameObject.activeInHierarchy && !ActiveFillSprites.Contains(fill))
                {
                    ActiveFillSprites.Add(fill);
                }
            }
        }

        private void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam)
        {
            if (!IsSplitScreenRendering || cam == null)
            {
                return;
            }

            PlayerCoopState activePlayer = FindActivePlayerByWorldCamera(cam);
            if (activePlayer == null)
            {
                return;
            }

            EnsureInitialFillSpritesScanned();
            for (int i = ActiveFillSprites.Count - 1; i >= 0; i--)
            {
                FillScreenSprite fill = ActiveFillSprites[i];
                if (fill == null)
                {
                    ActiveFillSprites.RemoveAt(i);
                    continue;
                }

                if (fill.gameObject.activeInHierarchy)
                {
                    ResizeFillScreenSpriteForCamera(fill, cam);
                }
            }

            if (_opaqueAlphaPass != null &&
                activePlayer.TargetTexture != null &&
                activePlayer.TargetTexture.format != RenderTextureFormat.RGB111110Float &&
                activePlayer.CameraData != null &&
                activePlayer.CameraData.scriptableRenderer != null)
            {
                activePlayer.CameraData.scriptableRenderer.EnqueuePass(_opaqueAlphaPass);
            }

            List<LightMainBuffer2D> mainBuffers = LightMainBuffer2D.List;
            if (mainBuffers != null)
            {
                for (int i = 0; i < mainBuffers.Count; i++)
                {
                    LightMainBuffer2D mb = mainBuffers[i];
                    if (mb == null ||
                        !mb.IsActive ||
                        mb.renderTexture == null ||
                        mb.renderTexture.renderTexture == null ||
                        mb.cameraLightmap.rendering != CameraLightmap.Rendering.Enabled ||
                        mb.cameraSettings.GetCamera() != cam)
                    {
                        continue;
                    }

                    LightmapPreset preset = mb.GetLightmapPreset();
                    if (preset == null)
                    {
                        continue;
                    }

                    if (mb.cameraLightmap.output == CameraLightmap.Output.Materials)
                    {
                        ApplyCameraLightmapMaterials(cam, mb, preset);
                    }
                    else
                    {
                        int passId = ResolveLightmapPassId(mb.cameraLightmap.output);
                        if (passId > 0)
                        {
                            LightmapShaders.SetShaders(false, passId, cam, mb.renderTexture, preset);
                        }
                    }
                }
            }

            List<OnRenderMode> onRenderList = OnRenderMode.List;
            if (onRenderList == null)
            {
                return;
            }

            for (int i = 0; i < onRenderList.Count; i++)
            {
                OnRenderMode onRender = onRenderList[i];
                if (onRender == null || onRender.meshRenderer == null)
                {
                    continue;
                }

                LightMainBuffer2D mb = onRender.mainBuffer;
                bool isValid = mb != null && mb.IsActive && mb.renderTexture != null && mb.renderTexture.renderTexture != null;
                if (!isValid)
                {
                    onRender.meshRenderer.enabled = false;
                    continue;
                }

                bool matchesCam = mb.cameraSettings.GetCamera() == cam;
                if (matchesCam)
                {
                    bool shouldEnable = !Lighting2D.Disable &&
                        mb.cameraLightmap.overlay == CameraLightmap.Overlay.Enabled &&
                        mb.cameraLightmap.rendering == CameraLightmap.Rendering.Enabled;
                    onRender.meshRenderer.enabled = shouldEnable;
                    if (shouldEnable)
                    {
                        onRender.UpdatePosition();
                    }
                }
                else
                {
                    onRender.meshRenderer.enabled = false;
                }
            }
        }

        private static void ApplyCameraLightmapMaterials(Camera cam, LightMainBuffer2D mb, LightmapPreset preset)
        {
            LightmapMaterials lmMats = mb.cameraLightmap.GetMaterials();
            if (lmMats == null || lmMats.materials == null || lmMats.materials.Length == 0)
            {
                return;
            }

            Rect pr = cam.pixelRect;
            float aspect = pr.width / Mathf.Max(1f, pr.height);
            float h = cam.orthographicSize * 2f;
            float w = h * aspect;
            Vector3 pos = cam.transform.position;
            float rot = cam.transform.eulerAngles.z * 0.01745329f;
            var rect = new Vector4(pos.x, pos.y, w, h);

            int passId = (int)mb.cameraLightmap.materialsType;
            if (passId <= 0)
            {
                passId = 1;
            }

            for (int i = 0; i < lmMats.materials.Length; i++)
            {
                Material mat = lmMats.materials[i];
                if (mat == null)
                {
                    continue;
                }

                var mp = new MaterialPass
                {
                    isSceneView = false,
                    material = mat,
                    texture = mb.renderTexture.renderTexture,
                    rect = rect,
                    color = preset.darknessColor,
                    rotation = rot,
                    passId = passId
                };
                LightmapMaterials.SetMaterial(passId, mp);
            }
        }

        private void OnEndCameraRendering(ScriptableRenderContext ctx, Camera cam)
        {
            if (!IsSplitScreenRendering || cam == null)
            {
                return;
            }

            PlayerCoopState activePlayer = FindActivePlayerByWorldCamera(cam);
            if (activePlayer == null)
            {
                return;
            }

            List<OnRenderMode> onRenderList = OnRenderMode.List;
            if (onRenderList == null)
            {
                return;
            }

            for (int i = 0; i < onRenderList.Count; i++)
            {
                OnRenderMode onRender = onRenderList[i];
                if (onRender == null || onRender.meshRenderer == null)
                {
                    continue;
                }

                LightMainBuffer2D mb = onRender.mainBuffer;
                onRender.meshRenderer.enabled = mb != null &&
                    mb.IsActive &&
                    mb.renderTexture != null &&
                    mb.renderTexture.renderTexture != null &&
                    !Lighting2D.Disable &&
                    mb.cameraLightmap.overlay == CameraLightmap.Overlay.Enabled &&
                    mb.cameraLightmap.rendering == CameraLightmap.Rendering.Enabled;
            }
        }

        private static int ResolveLightmapPassId(CameraLightmap.Output output)
        {
            if (output == CameraLightmap.Output.Shaders)
            {
                return 1;
            }

            if (output >= CameraLightmap.Output.Pass1 && output <= CameraLightmap.Output.Pass8)
            {
                return (int)output - (int)CameraLightmap.Output.Pass1 + 1;
            }

            return 0;
        }

        private void UpdateP1GamepadUnlockFlag()
        {
            PlayerCoopState p1 = _players[0];
            if (_player1UsesFirstGamepad && p1.AssignedGamepad != null)
            {
                p1.HudUnlockHeld = p1.AssignedGamepad.buttonNorth.isPressed || p1.AssignedGamepad.leftStickButton.isPressed;
            }
            else
            {
                p1.HudUnlockHeld = false;
            }
        }

        private void UpdatePlayersP2ToP4Input()
        {
            CameraController cc = CameraController.Current;
            if (cc == null || cc.settings == null)
            {
                return;
            }

            float dt = cc.settings.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (dt <= 0.00001f)
            {
                dt = Time.unscaledDeltaTime;
            }

            for (int i = 1; i < 4; i++)
            {
                PlayerCoopState p = _players[i];
                if (!p.IsActive)
                {
                    continue;
                }

                if (p.EventData == null)
                {
                    EventSystem es = EventSystem.current;
                    p.EventData = new PointerEventData(es);
                }

                ReadPlayerControlInputs(
                    p,
                    out Vector2 cursorStick,
                    out Vector2 panStick,
                    out float zoomInput,
                    out bool clickHeld,
                    out bool clickDown,
                    out bool clickUp,
                    out bool hudUnlockHeld,
                    out bool resetCamPressed);

                bool wasUnlock = p.HudUnlockHeld;
                p.HudUnlockHeld = hudUnlockHeld;

                float cursorSpeedPx = _cfgCursorSpeed.Value * (Screen.height / 1080f);
                if (cursorStick.sqrMagnitude > 0.0001f)
                {
                    Vector2 shaped = cursorStick.normalized * Mathf.Pow(cursorStick.magnitude, 1.35f);
                    p.CursorScreenPos += shaped * (cursorSpeedPx * Time.unscaledDeltaTime);
                }

                p.ClampCursorToBounds(p.HudUnlockHeld);
                if (wasUnlock && !p.HudUnlockHeld && !p.PixelViewport.Contains(p.CursorScreenPos))
                {
                    p.ClampCursorToBounds(false);
                }

                if (resetCamPressed)
                {
                    p.TargetZoom = Mathf.Clamp(cc.CameraZoom, cc.settings.zoomMin, PlayerCoopState.ComputeMaxZoom(cc, p.Aspect));
                    p.CamPos = PlayerCoopState.ClampPositionToWorldRect(cc, cc.CameraPosition, p.TargetZoom, p.Aspect);
                    p.PanVelocity = Vector2.zero;
                }
                else
                {
                    UpdateSecondaryCameraMotion(cc, p, panStick, zoomInput, dt);
                }

                if (clickDown)
                {
                    p.ClickRippleTimer = 0.22f;
                }

                DispatchPlayerPointerEvents(p, clickHeld, clickDown, clickUp);
            }
        }

        private void ReadPlayerControlInputs(
            PlayerCoopState p,
            out Vector2 cursorStick,
            out Vector2 panStick,
            out float zoomInput,
            out bool clickHeld,
            out bool clickDown,
            out bool clickUp,
            out bool hudUnlockHeld,
            out bool resetCamPressed)
        {
            cursorStick = Vector2.zero;
            panStick = Vector2.zero;
            zoomInput = 0f;
            clickHeld = false;
            clickDown = false;
            clickUp = false;
            hudUnlockHeld = false;
            resetCamPressed = false;

            Gamepad g = p.AssignedGamepad;
            if (g != null)
            {
                cursorStick = ApplyRadialDeadzone(g.leftStick.ReadValue(), 0.16f);
                panStick = ApplyRadialDeadzone(g.rightStick.ReadValue(), 0.16f);

                // RT/RB zoom in (smaller orthoSize -> negative delta); LT/LB zoom out.
                float zoomIn = Mathf.Clamp01(g.rightTrigger.ReadValue()) + (g.rightShoulder.isPressed ? 0.7f : 0f);
                float zoomOut = Mathf.Clamp01(g.leftTrigger.ReadValue()) + (g.leftShoulder.isPressed ? 0.7f : 0f);
                zoomInput = zoomOut - zoomIn;

                clickHeld = g.buttonSouth.isPressed;
                clickDown = g.buttonSouth.wasPressedThisFrame;
                clickUp = g.buttonSouth.wasReleasedThisFrame;
                hudUnlockHeld = g.buttonNorth.isPressed || g.leftStickButton.isPressed;
                resetCamPressed = g.rightStickButton.wasPressedThisFrame;
            }

            if (p.UseKeyboardFallback && Keyboard.current != null)
            {
                Keyboard kb = Keyboard.current;
                Vector2 kbCursor = new Vector2(
                    (kb.lKey.isPressed ? 1f : 0f) - (kb.jKey.isPressed ? 1f : 0f),
                    (kb.iKey.isPressed ? 1f : 0f) - (kb.kKey.isPressed ? 1f : 0f));
                if (kbCursor.sqrMagnitude > 0.01f)
                {
                    cursorStick = kbCursor.normalized;
                }

                Vector2 kbPan = new Vector2(
                    (kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.leftArrowKey.isPressed ? 1f : 0f),
                    (kb.upArrowKey.isPressed ? 1f : 0f) - (kb.downArrowKey.isPressed ? 1f : 0f));
                if (kbPan.sqrMagnitude > 0.01f)
                {
                    panStick = kbPan.normalized;
                }

                float kbZoom = (kb.uKey.isPressed ? 0.85f : 0f) - (kb.oKey.isPressed ? 0.85f : 0f);
                if (Mathf.Abs(kbZoom) > 0.01f)
                {
                    zoomInput = kbZoom;
                }

                bool kbHeld = kb.enterKey.isPressed || kb.rightShiftKey.isPressed;
                bool kbDown = kb.enterKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame;
                bool kbUp = kb.enterKey.wasReleasedThisFrame || kb.rightShiftKey.wasReleasedThisFrame;
                clickHeld |= kbHeld;
                clickDown |= kbDown;
                clickUp |= kbUp;
                hudUnlockHeld |= kb.rightCtrlKey.isPressed;
            }
        }

        private void UpdateSecondaryCameraMotion(
            CameraController cc,
            PlayerCoopState p,
            Vector2 panStick,
            float zoomInput,
            float dt)
        {
            GameCameraSettings s = cc.settings;
            float maxZoom = PlayerCoopState.ComputeMaxZoom(cc, p.Aspect);
            float minZoom = Mathf.Min(s.zoomMin, maxZoom);

            if (Mathf.Abs(zoomInput) > 0.01f)
            {
                float zoomStepAmount = zoomInput * _cfgZoomSpeed.Value * Mathf.Max(0.35f, p.Zoom * 0.12f) * dt;
                p.TargetZoom = Mathf.Clamp(p.TargetZoom + zoomStepAmount, minZoom, maxZoom);
            }
            else
            {
                p.TargetZoom = Mathf.Clamp(p.TargetZoom, minZoom, maxZoom);
            }

            float prevZoom = p.Zoom;
            p.Zoom = Mathf.Lerp(p.Zoom, p.TargetZoom, 1f - Mathf.Exp(-14f * dt));

            if (s.zoomToCursor && Mathf.Abs(p.Zoom - prevZoom) > 0.0001f && p.PixelViewport.Contains(p.CursorScreenPos))
            {
                float ndcX = ((p.CursorScreenPos.x - p.PixelViewport.x) / Mathf.Max(1f, p.PixelViewport.width) - 0.5f) * 2f;
                float ndcY = ((p.CursorScreenPos.y - p.PixelViewport.y) / Mathf.Max(1f, p.PixelViewport.height) - 0.5f) * 2f;
                Vector2 before = p.CamPos + new Vector2(ndcX * prevZoom * p.Aspect, ndcY * prevZoom);
                Vector2 after = p.CamPos + new Vector2(ndcX * p.Zoom * p.Aspect, ndcY * p.Zoom);
                p.CamPos += before - after;
            }

            float panSpeedWorld = _cfgPanSpeed.Value * Mathf.Max(1f, p.Zoom);
            Vector2 targetVel = panStick * panSpeedWorld;
            p.PanVelocity = Vector2.Lerp(p.PanVelocity, targetVel, 1f - Mathf.Exp(-14f * dt));
            p.CamPos += p.PanVelocity * dt;
            p.CamPos = PlayerCoopState.ClampPositionToWorldRect(cc, p.CamPos, p.Zoom, p.Aspect);
        }

        private void DispatchPlayerPointerEvents(
            PlayerCoopState p,
            bool clickHeld,
            bool clickDown,
            bool clickUp)
        {
            p.EventData.Reset();
            p.EventData.position = p.CursorScreenPos;
            p.EventData.button = PointerEventData.InputButton.Left;

            bool overCoopPanel = _panelExpanded && _panelRoot != null && _panelRoot.activeInHierarchy &&
                RectTransformUtility.RectangleContainsScreenPoint(
                    _panelRoot.GetComponent<RectTransform>(),
                    p.CursorScreenPos,
                    _boundUiCamera);

            bool overUi = overCoopPanel || InputUIChecker.IsOverUI(p.CursorScreenPos);
            if (overUi)
            {
                p.ClearHoverAndDrag(p.EventData);
                DispatchSharedUiClick(p, clickDown, clickUp);
                return;
            }

            if (clickUp && _lastUiDownTarget[p.PlayerIndex] != null)
            {
                _lastUiDownTarget[p.PlayerIndex] = null;
            }

            CustomInputModule cim = CustomInputModule.IsInstanceExist ? CustomInputModule.Instance : null;
            if (cim == null)
            {
                return;
            }

            RaycastResult hit;
            bool prevCimOverUi = CustomInputModule.IsOverUI;
            try
            {
                ActiveRaycastPlayerIndex = p.PlayerIndex;
                SetCimIsOverUi(false);
                hit = cim.GetRaycastResult(p.EventData);
            }
            finally
            {
                ActiveRaycastPlayerIndex = 0;
                SetCimIsOverUi(prevCimOverUi);
            }

            CustomPointerEvent newHover = ResolvePointerEvent(hit);
            p.LastHoverObject = p.CurrentHoverObject;
            p.CurrentHoverObject = newHover;

            try
            {
                ActiveRaycastPlayerIndex = p.PlayerIndex;

                if (p.LastHoverObject != p.CurrentHoverObject)
                {
                    if (p.LastHoverObject != null)
                    {
                        p.LastHoverObject.OnPointerExit(p.EventData);
                    }
                    if (p.CurrentHoverObject != null)
                    {
                        p.CurrentHoverObject.OnPointerEnter(p.EventData);
                    }
                    p.ClickCount = 0;
                }

                if (clickDown)
                {
                    p.IsPressed = true;
                    p.IsDragging = false;
                    p.DownScreenPos = p.CursorScreenPos;
                    p.LastDownObject = p.CurrentHoverObject;
                    if (p.CurrentHoverObject != null)
                    {
                        p.CurrentHoverObject.OnPointerDown(p.EventData);
                    }
                }

                if (clickHeld && p.IsPressed && p.LastDownObject != null)
                {
                    if (!p.IsDragging && Vector2.Distance(p.DownScreenPos, p.CursorScreenPos) > 10f)
                    {
                        p.IsDragging = true;
                        p.LastDownObject.OnBeginDrag(p.EventData);
                    }
                    if (p.IsDragging)
                    {
                        p.LastDownObject.OnDrag(p.EventData);
                    }
                }

                if (clickUp && p.IsPressed)
                {
                    p.IsPressed = false;
                    if (p.CurrentHoverObject != null)
                    {
                        p.CurrentHoverObject.OnPointerUp(p.EventData);
                    }

                    if (p.IsDragging && p.LastDownObject != null)
                    {
                        if (p.LastDownObject != p.CurrentHoverObject)
                        {
                            p.LastDownObject.OnPointerUp(p.EventData);
                        }
                        p.LastDownObject.OnEndDrag(p.EventData);
                    }
                    else if (p.CurrentHoverObject != null && p.CurrentHoverObject == p.LastDownObject)
                    {
                        float now = Time.unscaledTime;
                        p.ClickCount = (now - p.LastClickTime) < 0.4f ? p.ClickCount + 1 : 1;
                        p.LastClickTime = now;
                        p.EventData.clickCount = p.ClickCount;
                        p.CurrentHoverObject.OnPointerClick(p.EventData);
                    }

                    p.IsDragging = false;
                    p.LastDownObject = null;
                }
            }
            finally
            {
                ActiveRaycastPlayerIndex = 0;
            }
        }

        private void DispatchSharedUiClick(PlayerCoopState p, bool clickDown, bool clickUp)
        {
            if (!clickDown && !clickUp)
            {
                return;
            }

            _uiRaycastScratch.Clear();
            if (EventSystem.current != null)
            {
                EventSystem.current.RaycastAll(p.EventData, _uiRaycastScratch);
            }
            else
            {
                InputUIChecker.GraphicRaycastAll(p.EventData, _uiRaycastScratch);
            }

            GameObject topUi = null;
            for (int i = 0; i < _uiRaycastScratch.Count; i++)
            {
                if (_uiRaycastScratch[i].gameObject != null)
                {
                    topUi = _uiRaycastScratch[i].gameObject;
                    break;
                }
            }

            int idx = p.PlayerIndex;
            if (clickDown)
            {
                _lastUiDownTarget[idx] = topUi != null
                    ? ExecuteEvents.ExecuteHierarchy(topUi, p.EventData, ExecuteEvents.pointerDownHandler)
                    : null;
            }

            if (clickUp)
            {
                GameObject downGo = _lastUiDownTarget[idx];
                if (downGo != null)
                {
                    ExecuteEvents.ExecuteHierarchy(downGo, p.EventData, ExecuteEvents.pointerUpHandler);
                }

                if (topUi != null)
                {
                    GameObject clickTarget = ExecuteEvents.GetEventHandler<IPointerClickHandler>(topUi);
                    if (clickTarget != null && (downGo == null || clickTarget == ExecuteEvents.GetEventHandler<IPointerClickHandler>(downGo) || clickTarget == downGo))
                    {
                        ExecuteEvents.Execute(clickTarget, p.EventData, ExecuteEvents.pointerClickHandler);
                    }
                }

                _lastUiDownTarget[idx] = null;
            }
        }

        private static CustomPointerEvent ResolvePointerEvent(RaycastResult hit)
        {
            if (hit.gameObject == null)
            {
                return null;
            }
            if (hit.gameObject.TryGetComponent(out CustomPointerEvent direct))
            {
                return direct;
            }
            return hit.gameObject.GetComponentInParent<CustomPointerEvent>();
        }

        private void ClearHoveredAndDraggedStates()
        {
            bool anyActiveHover = false;
            for (int i = 1; i < 4; i++)
            {
                if (_players[i].HasAnyHoverOrDrag)
                {
                    anyActiveHover = true;
                    break;
                }
            }

            if (!anyActiveHover)
            {
                return;
            }

            if (_fallbackPointerEventData == null && EventSystem.current != null)
            {
                _fallbackPointerEventData = new PointerEventData(EventSystem.current);
            }

            for (int i = 1; i < 4; i++)
            {
                if (_players[i].HasAnyHoverOrDrag)
                {
                    _players[i].ClearHoverAndDrag(_fallbackPointerEventData);
                }
            }
        }

        public Vector2 ReadPlayer1KeyboardPan(Vector2 rawMkbFallback)
        {
            Keyboard kb = Keyboard.current;
            if (kb == null)
            {
                return Vector2.zero;
            }

            Vector2 v = new Vector2(
                (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f),
                (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f));

            if (!_p2KeyboardTestMode)
            {
                v.x += (kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.leftArrowKey.isPressed ? 1f : 0f);
                v.y += (kb.upArrowKey.isPressed ? 1f : 0f) - (kb.downArrowKey.isPressed ? 1f : 0f);
            }

            if (v.sqrMagnitude > 0.01f)
            {
                return v.normalized;
            }

            return _p2KeyboardTestMode ? Vector2.zero : rawMkbFallback;
        }

        public bool IsEdgeScrollAllowedForP1()
        {
            GameplaySettings gs = SettingsSystem.GetSettings<GameplaySettings>("Gameplay");
            if (gs == null || !gs.enableEdgeScrolling)
            {
                return false;
            }
            return Mouse.current != null && _players[0].PixelViewport.Contains(Mouse.current.position.ReadValue());
        }

        public Vector2 ComputeP1ViewportEdgeScroll(float edgeSize)
        {
            if (Mouse.current == null)
            {
                return Vector2.zero;
            }

            Vector2 pos = Mouse.current.position.ReadValue();
            Rect vp = _players[0].PixelViewport;
            if (!vp.Contains(pos))
            {
                return Vector2.zero;
            }

            Vector2 dir = Vector2.zero;
            if (pos.x >= vp.xMax - edgeSize) dir.x = 1f;
            else if (pos.x <= vp.xMin + edgeSize) dir.x = -1f;

            if (pos.y >= vp.yMax - edgeSize) dir.y = 1f;
            else if (pos.y <= vp.yMin + edgeSize) dir.y = -1f;
            return dir;
        }

        public float ReadPlayer1KeyboardZoom()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null)
            {
                return 0f;
            }

            float dir = (kb.minusKey.isPressed || kb.numpadMinusKey.isPressed ? 1f : 0f) -
                        (kb.equalsKey.isPressed || kb.numpadPlusKey.isPressed ? 1f : 0f);
            return dir * Time.deltaTime * 20f;
        }

        public float ReadPlayer1GamepadZoom()
        {
            Gamepad g = _players[0].AssignedGamepad;
            if (g == null)
            {
                return 0f;
            }

            float zoomOut = Mathf.Clamp01(g.leftTrigger.ReadValue()) + (g.leftShoulder.isPressed ? 0.7f : 0f);
            float zoomIn = Mathf.Clamp01(g.rightTrigger.ReadValue()) + (g.rightShoulder.isPressed ? 0.7f : 0f);
            return (zoomOut - zoomIn) * Time.deltaTime * 20f;
        }

        private Rect ToPixelRect(Rect norm)
        {
            return new Rect(
                norm.x * Screen.width,
                norm.y * Screen.height,
                norm.width * Screen.width,
                norm.height * Screen.height);
        }

        public static Vector2 ApplyRadialDeadzone(Vector2 input, float deadzone)
        {
            float mag = input.magnitude;
            if (mag <= deadzone)
            {
                return Vector2.zero;
            }

            float scaled = Mathf.Clamp01((mag - deadzone) / (1f - deadzone));
            return (input / mag) * scaled;
        }

        private void EnsureCanvasesAttached()
        {
            Camera uiCam = UICamera.Current != null ? UICamera.Current.Camera : null;
            if (uiCam == null)
            {
                return;
            }

            if (_viewportCanvas == null)
            {
                BuildAllCanvases();
            }

            if (_boundUiCamera != uiCam)
            {
                _boundUiCamera = uiCam;
                ConfigureCanvasCamera(_viewportCanvas, uiCam, -50);
                ConfigureCanvasCamera(_overlayCanvas, uiCam, 940);
                ConfigureCanvasCamera(_panelCanvas, uiCam, 960);
            }
        }

        private static void ConfigureCanvasCamera(Canvas canvas, Camera uiCam, int sortingOrder)
        {
            if (canvas == null || uiCam == null)
            {
                return;
            }

            int layer = uiCam.gameObject.layer;
            int sortingLayerId = 0;
            float planeDist = 100f;

            if (PersistentUILauncher.Current != null)
            {
                Canvas hostCanvas = PersistentCanvasRef(PersistentUILauncher.Current);
                if (hostCanvas != null)
                {
                    layer = hostCanvas.gameObject.layer;
                    sortingLayerId = hostCanvas.sortingLayerID;
                    planeDist = hostCanvas.planeDistance;
                }
            }

            SetLayerRecursively(canvas.gameObject, layer);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCam;
            canvas.planeDistance = planeDist;
            canvas.sortingLayerID = sortingLayerId;
            canvas.sortingOrder = sortingOrder;
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            if (go == null)
            {
                return;
            }
            go.layer = layer;
            Transform t = go.transform;
            for (int i = 0; i < t.childCount; i++)
            {
                SetLayerRecursively(t.GetChild(i).gameObject, layer);
            }
        }

        private void BuildAllCanvases()
        {
            _viewportCanvas = CreateCanvasObject("SplitCoop_ViewportCanvas", addRaycaster: false);
            for (int i = 0; i < 4; i++)
            {
                var vpGo = new GameObject($"Viewport_{_players[i].Label}");
                vpGo.transform.SetParent(_viewportCanvas.transform, false);
                var raw = vpGo.AddComponent<RawImage>();
                raw.raycastTarget = false;
                raw.color = Color.white;
                vpGo.SetActive(false);
                _players[i].ViewportImage = raw;
            }

            BuildStandbyQuadrant4(_viewportCanvas.transform);

            _overlayCanvas = CreateCanvasObject("SplitCoop_OverlayCanvas", addRaycaster: false);
            _divVertical = CreateDivider(_overlayCanvas.transform, "Div_Vertical", new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(3f, 0f));
            _divHorizontal = CreateDivider(_overlayCanvas.transform, "Div_Horizontal", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 3f));
            _divBottomHalf = CreateDivider(_overlayCanvas.transform, "Div_BottomHalf", new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(3f, 0f));
            _divRightHalf = CreateDivider(_overlayCanvas.transform, "Div_RightHalf", new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 3f));

            for (int i = 0; i < 4; i++)
            {
                BuildCornerBadge(_overlayCanvas.transform, _players[i]);
                BuildPlayerCursor(_overlayCanvas.transform, _players[i]);
            }

            BuildToast(_overlayCanvas.transform);

            _panelCanvas = CreateCanvasObject("SplitCoop_PanelCanvas", addRaycaster: true);
            _panelRaycaster = _panelCanvas.GetComponent<GraphicRaycaster>();
            BuildControlPanel(_panelCanvas.transform);
            ApplyLocalizedUiTextAndFonts();
        }

        private Canvas CreateCanvasObject(string name, bool addRaycaster)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            if (addRaycaster)
            {
                go.AddComponent<GraphicRaycaster>();
            }
            return canvas;
        }

        private void BuildStandbyQuadrant4(Transform parent)
        {
            _quad4StandbyRoot = new GameObject("Standby_P4");
            _quad4StandbyRoot.transform.SetParent(parent, false);
            var rt = _quad4StandbyRoot.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = _quad4StandbyRoot.AddComponent<Image>();
            bg.sprite = SplitScreenTextures.SolidWhiteSprite;
            bg.color = SplitScreenTextures.SurfaceStandby;
            bg.raycastTarget = false;

            _standbyTitleText = CreateTextChild(_quad4StandbyRoot.transform, SplitScreenLocalization.Get(LocKey.StandbyTitle), 24, FontStyle.Bold, SplitScreenTextures.TextPrimary, TextAnchor.MiddleCenter);
            _standbyTitleText.resizeTextForBestFit = true;
            _standbyTitleText.resizeTextMinSize = 14;
            _standbyTitleText.resizeTextMaxSize = 24;
            RectTransform tr = _standbyTitleText.rectTransform;
            tr.anchorMin = new Vector2(0.08f, 0.46f);
            tr.anchorMax = new Vector2(0.92f, 0.7f);
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;

            _standbySubText = CreateTextChild(_quad4StandbyRoot.transform, SplitScreenLocalization.Get(LocKey.StandbySubtitle), 16, FontStyle.Normal, SplitScreenTextures.TextSecondary, TextAnchor.MiddleCenter);
            _standbySubText.resizeTextForBestFit = true;
            _standbySubText.resizeTextMinSize = 11;
            _standbySubText.resizeTextMaxSize = 16;
            RectTransform sr = _standbySubText.rectTransform;
            sr.anchorMin = new Vector2(0.08f, 0.26f);
            sr.anchorMax = new Vector2(0.92f, 0.46f);
            sr.offsetMin = Vector2.zero;
            sr.offsetMax = Vector2.zero;

            _quad4StandbyRoot.SetActive(false);
        }

        private static GameObject CreateDivider(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.sizeDelta = sizeDelta;

            var img = go.AddComponent<Image>();
            img.sprite = SplitScreenTextures.SolidWhiteSprite;
            img.color = SplitScreenTextures.DividerLine;
            img.raycastTarget = false;
            go.SetActive(false);
            return go;
        }

        private void BuildCornerBadge(Transform parent, PlayerCoopState p)
        {
            var badge = new GameObject($"Badge_{p.Label}");
            badge.transform.SetParent(parent, false);
            var rt = badge.AddComponent<RectTransform>();
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(164f, 26f);

            var bg = badge.AddComponent<Image>();
            bg.sprite = SplitScreenTextures.SolidWhiteSprite;
            bg.color = SplitScreenTextures.SurfaceDark;
            bg.raycastTarget = false;

            var accentGo = new GameObject("Accent");
            accentGo.transform.SetParent(badge.transform, false);
            var art = accentGo.AddComponent<RectTransform>();
            art.anchorMin = new Vector2(0f, 0f);
            art.anchorMax = new Vector2(0f, 1f);
            art.pivot = new Vector2(0f, 0.5f);
            art.sizeDelta = new Vector2(4f, 0f);
            art.anchoredPosition = Vector2.zero;
            var accentImg = accentGo.AddComponent<Image>();
            accentImg.sprite = SplitScreenTextures.SolidWhiteSprite;
            accentImg.color = p.AccentColor;
            accentImg.raycastTarget = false;

            Text label = CreateTextChild(badge.transform, p.Label, 13, FontStyle.Bold, SplitScreenTextures.TextPrimary, TextAnchor.MiddleLeft);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 9;
            label.resizeTextMaxSize = 13;
            RectTransform lrt = label.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(10f, 0f);
            lrt.offsetMax = new Vector2(-6f, 0f);

            p.CornerBadgeRoot = badge;
            p.CornerBadgeRect = rt;
            p.CornerBadgeText = label;
            badge.SetActive(false);
        }

        private void BuildPlayerCursor(Transform parent, PlayerCoopState p)
        {
            var root = new GameObject($"Cursor_{p.Label}");
            root.transform.SetParent(parent, false);
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = Vector2.zero;
            rt.sizeDelta = new Vector2(36f, 36f);

            var rippleGo = new GameObject("ClickRipple");
            rippleGo.transform.SetParent(root.transform, false);
            var rrt = rippleGo.AddComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero;
            rrt.anchorMax = Vector2.zero;
            rrt.pivot = new Vector2(0.5f, 0.5f);
            rrt.anchoredPosition = new Vector2(2f, -3f);
            rrt.sizeDelta = new Vector2(28f, 28f);
            var rippleImg = rippleGo.AddComponent<Image>();
            rippleImg.sprite = SplitScreenTextures.RingSprite;
            rippleImg.color = p.AccentColor;
            rippleImg.raycastTarget = false;
            rippleGo.SetActive(false);

            var arrowGo = new GameObject("Arrow");
            arrowGo.transform.SetParent(root.transform, false);
            var art = arrowGo.AddComponent<RectTransform>();
            art.anchorMin = Vector2.zero;
            art.anchorMax = Vector2.zero;
            art.pivot = new Vector2(2f / 36f, 33f / 36f);
            art.anchoredPosition = Vector2.zero;
            art.sizeDelta = new Vector2(36f, 36f);
            var arrowImg = arrowGo.AddComponent<Image>();
            arrowImg.sprite = SplitScreenTextures.CursorSprite;
            arrowImg.color = Color.Lerp(Color.white, p.AccentColor, 0.25f);
            arrowImg.raycastTarget = false;

            var tagBgGo = new GameObject("TagBg");
            tagBgGo.transform.SetParent(root.transform, false);
            var tbrt = tagBgGo.AddComponent<RectTransform>();
            tbrt.anchorMin = Vector2.zero;
            tbrt.anchorMax = Vector2.zero;
            tbrt.pivot = new Vector2(0f, 1f);
            tbrt.anchoredPosition = new Vector2(18f, -20f);
            tbrt.sizeDelta = new Vector2(40f, 17f);
            var tagBg = tagBgGo.AddComponent<Image>();
            tagBg.sprite = SplitScreenTextures.SolidWhiteSprite;
            tagBg.color = SplitScreenTextures.SurfaceDark;
            tagBg.raycastTarget = false;

            Text tagText = CreateTextChild(tagBgGo.transform, p.Label, 11, FontStyle.Bold, p.AccentColor, TextAnchor.MiddleCenter);
            tagText.rectTransform.anchorMin = Vector2.zero;
            tagText.rectTransform.anchorMax = Vector2.one;
            tagText.rectTransform.offsetMin = Vector2.zero;
            tagText.rectTransform.offsetMax = Vector2.zero;

            p.CursorRoot = root;
            p.CursorTransform = rt;
            p.CursorArrowImage = arrowImg;
            p.CursorTagText = tagText;
            p.RippleImage = rippleImg;
            p.RippleTransform = rrt;
            root.SetActive(false);
        }

        private void BuildToast(Transform parent)
        {
            _toastRoot = new GameObject("CoopToast");
            _toastRoot.transform.SetParent(parent, false);
            var rt = _toastRoot.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 16f);
            rt.sizeDelta = new Vector2(740f, 36f);

            var bg = _toastRoot.AddComponent<Image>();
            bg.sprite = SplitScreenTextures.SolidWhiteSprite;
            bg.color = SplitScreenTextures.SurfaceDark;
            bg.raycastTarget = false;

            _toastText = CreateTextChild(_toastRoot.transform, string.Empty, 14, FontStyle.Bold, SplitScreenTextures.TextPrimary, TextAnchor.MiddleCenter);
            _toastText.resizeTextForBestFit = true;
            _toastText.resizeTextMinSize = 10;
            _toastText.resizeTextMaxSize = 14;
            _toastText.rectTransform.anchorMin = Vector2.zero;
            _toastText.rectTransform.anchorMax = Vector2.one;
            _toastText.rectTransform.offsetMin = new Vector2(12f, 2f);
            _toastText.rectTransform.offsetMax = new Vector2(-12f, -2f);

            _toastRoot.SetActive(false);
        }

        private void BuildControlPanel(Transform parent)
        {
            _panelRoot = new GameObject("CoopControlPanel");
            _panelRoot.transform.SetParent(parent, false);
            var rt = _panelRoot.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(500f, 388f);

            var bg = _panelRoot.AddComponent<Image>();
            bg.sprite = SplitScreenTextures.SolidWhiteSprite;
            bg.color = SplitScreenTextures.SurfaceCard;

            var outline = _panelRoot.AddComponent<Outline>();
            outline.effectColor = SplitScreenTextures.BorderMuted;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            _panelHeaderText = CreateTextChild(_panelRoot.transform, SplitScreenLocalization.Get(LocKey.PanelTitle), 18, FontStyle.Bold, SplitScreenTextures.TextPrimary, TextAnchor.MiddleLeft);
            _panelHeaderText.resizeTextForBestFit = true;
            _panelHeaderText.resizeTextMinSize = 12;
            _panelHeaderText.resizeTextMaxSize = 18;
            SetAnchoredBox(_panelHeaderText.rectTransform, new Vector2(18f, -14f), new Vector2(464f, 28f));

            _slotStatusTexts = new Text[4];
            for (int i = 0; i < 4; i++)
            {
                float y = -52f - i * 28f;
                var dotGo = new GameObject($"Dot_{i}");
                dotGo.transform.SetParent(_panelRoot.transform, false);
                var dotImg = dotGo.AddComponent<Image>();
                dotImg.sprite = SplitScreenTextures.SolidWhiteSprite;
                dotImg.color = _players[i].AccentColor;
                SetAnchoredBox(dotImg.rectTransform, new Vector2(20f, y - 6f), new Vector2(10f, 14f));

                _slotStatusTexts[i] = CreateTextChild(_panelRoot.transform, string.Empty, 14, FontStyle.Normal, SplitScreenTextures.TextPrimary, TextAnchor.MiddleLeft);
                _slotStatusTexts[i].resizeTextForBestFit = true;
                _slotStatusTexts[i].resizeTextMinSize = 10;
                _slotStatusTexts[i].resizeTextMaxSize = 14;
                SetAnchoredBox(_slotStatusTexts[i].rectTransform, new Vector2(38f, y), new Vector2(444f, 24f));
            }

            _btnModeText = CreatePanelButton(_panelRoot.transform, new Vector2(18f, -172f), new Vector2(224f, 34f), ToggleCoopMode);
            _btnLayoutText = CreatePanelButton(_panelRoot.transform, new Vector2(258f, -172f), new Vector2(224f, 34f), ToggleTwoPlayerLayout);
            _btnP1InputText = CreatePanelButton(_panelRoot.transform, new Vector2(18f, -214f), new Vector2(224f, 34f), TogglePlayer1InputMode);
            _btnP2TestText = CreatePanelButton(_panelRoot.transform, new Vector2(258f, -214f), new Vector2(224f, 34f), ToggleP2KeyboardTestMode);
            _btnCloseText = CreatePanelButton(_panelRoot.transform, new Vector2(18f, -338f), new Vector2(464f, 32f), TogglePanelExpanded);
            _btnCloseText.text = SplitScreenLocalization.Get(LocKey.PanelClose);

            _panelGuideText = CreateTextChild(_panelRoot.transform, SplitScreenLocalization.Get(LocKey.PanelGuide), 12, FontStyle.Normal, SplitScreenTextures.TextSecondary, TextAnchor.UpperLeft);
            _panelGuideText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _panelGuideText.verticalOverflow = VerticalWrapMode.Truncate;
            SetAnchoredBox(_panelGuideText.rectTransform, new Vector2(18f, -258f), new Vector2(464f, 70f));

            _panelRoot.SetActive(false);
        }

        private Text CreatePanelButton(Transform parent, Vector2 topLeftPos, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            var btnGo = new GameObject("PanelBtn");
            btnGo.transform.SetParent(parent, false);
            var img = btnGo.AddComponent<Image>();
            img.sprite = SplitScreenTextures.SolidWhiteSprite;
            img.color = SplitScreenTextures.SurfaceDark;
            SetAnchoredBox(img.rectTransform, topLeftPos, size);

            var outline = btnGo.AddComponent<Outline>();
            outline.effectColor = SplitScreenTextures.BorderMuted;
            outline.effectDistance = new Vector2(1f, -1f);

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            Text txt = CreateTextChild(btnGo.transform, string.Empty, 13, FontStyle.Bold, SplitScreenTextures.TextPrimary, TextAnchor.MiddleCenter);
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 9;
            txt.resizeTextMaxSize = 13;
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = new Vector2(6f, 0f);
            txt.rectTransform.offsetMax = new Vector2(-6f, 0f);
            return txt;
        }

        private static void SetAnchoredBox(RectTransform rt, Vector2 topLeftPos, Vector2 size)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = topLeftPos;
            rt.sizeDelta = size;
        }

        private static Text CreateTextChild(Transform parent, string text, int fontSize, FontStyle style, Color32 color, TextAnchor alignment)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<Text>();
            label.font = SplitScreenTextures.UiFont;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.text = text;
            return label;
        }

        private void UpdateOverlayVisuals()
        {
            if (_overlayCanvas == null)
            {
                return;
            }

            if (_quad4StandbyRoot != null)
            {
                bool showStandby = _activePlayerCount == 3;
                if (_quad4StandbyRoot.activeSelf != showStandby)
                {
                    _quad4StandbyRoot.SetActive(showStandby);
                }
            }

            bool showVertical = (_activePlayerCount == 2 && _layout2P == TwoPlayerLayoutMode.VerticalLeftRight) || _activePlayerCount >= 3;
            bool showHorizontal = (_activePlayerCount == 2 && _layout2P == TwoPlayerLayoutMode.HorizontalTopBottom) || _activePlayerCount >= 3;
            if (_divVertical.activeSelf != showVertical) _divVertical.SetActive(showVertical);
            if (_divHorizontal.activeSelf != showHorizontal) _divHorizontal.SetActive(showHorizontal);
            if (_divBottomHalf.activeSelf) _divBottomHalf.SetActive(false);
            if (_divRightHalf.activeSelf) _divRightHalf.SetActive(false);

            float scale = Mathf.Clamp(_cfgCursorScale.Value, 0.6f, 2f);
            for (int i = 0; i < 4; i++)
            {
                PlayerCoopState p = _players[i];
                bool showBadge = p.IsActive;
                if (p.CornerBadgeRoot != null)
                {
                    if (p.CornerBadgeRoot.activeSelf != showBadge)
                    {
                        p.CornerBadgeRoot.SetActive(showBadge);
                    }
                    if (showBadge)
                    {
                        if (p.LastAppliedBadgeNorm != p.NormViewport && p.CornerBadgeRect != null)
                        {
                            p.CornerBadgeRect.anchorMin = new Vector2(p.NormViewport.x, p.NormViewport.yMax);
                            p.CornerBadgeRect.anchorMax = new Vector2(p.NormViewport.x, p.NormViewport.yMax);
                            p.CornerBadgeRect.anchoredPosition = new Vector2(10f, -10f);
                            p.LastAppliedBadgeNorm = p.NormViewport;
                        }

                        string badgeLabel;
                        if (i == 0)
                        {
                            badgeLabel = SplitScreenLocalization.Format(
                                _player1UsesFirstGamepad ? LocKey.BadgeGamepad : LocKey.BadgeMouse,
                                p.Label);
                        }
                        else if (p.UseKeyboardFallback && p.AssignedGamepad == null)
                        {
                            badgeLabel = SplitScreenLocalization.Format(LocKey.BadgeKeyTest, p.Label);
                        }
                        else
                        {
                            badgeLabel = SplitScreenLocalization.Format(LocKey.BadgeGamepad, p.Label);
                        }

                        if (p.CornerBadgeText.text != badgeLabel)
                        {
                            p.CornerBadgeText.text = badgeLabel;
                        }
                    }
                }

                // Player 1 keeps native CursorManager visuals; P2-P4 draw dedicated per-player overlay cursors.
                bool showCursor = p.IsActive && i >= 1;
                if (p.CursorRoot != null)
                {
                    if (p.CursorRoot.activeSelf != showCursor)
                    {
                        p.CursorRoot.SetActive(showCursor);
                    }
                    if (showCursor)
                    {
                        UpdateCursorVisual(p, scale);
                    }
                }
            }
        }

        private void UpdateCursorVisual(PlayerCoopState p, float scale)
        {
            RectTransform canvasRt = _overlayCanvas.transform as RectTransform;
            if (canvasRt != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, p.CursorScreenPos, _boundUiCamera, out Vector2 localPt))
            {
                if (float.IsNaN(p.LastAppliedCursorLocalPos.x) || (localPt - p.LastAppliedCursorLocalPos).sqrMagnitude > 0.01f)
                {
                    p.CursorTransform.anchoredPosition = localPt;
                    p.LastAppliedCursorLocalPos = localPt;
                }
            }

            float targetScale = scale * (p.CurrentHoverObject != null ? 1.12f : 1f);
            if (Mathf.Abs(targetScale - p.LastAppliedCursorScale) > 0.001f)
            {
                p.CursorTransform.localScale = Vector3.one * targetScale;
                p.LastAppliedCursorScale = targetScale;
            }

            string tag = p.HudUnlockHeld ? p.LabelPlus : p.Label;
            if (p.CursorTagText.text != tag)
            {
                p.CursorTagText.text = tag;
            }

            if (p.ClickRippleTimer > 0f)
            {
                p.ClickRippleTimer = Mathf.Max(0f, p.ClickRippleTimer - Time.unscaledDeltaTime);
                float progress = 1f - (p.ClickRippleTimer / 0.22f);
                if (!p.RippleImage.gameObject.activeSelf)
                {
                    p.RippleImage.gameObject.SetActive(true);
                }
                p.RippleTransform.localScale = Vector3.one * Mathf.Lerp(0.45f, 1.55f, progress);
                Color32 c = p.AccentColor;
                c.a = (byte)Mathf.RoundToInt((1f - progress) * 230f);
                p.RippleImage.color = c;
            }
            else if (p.RippleImage.gameObject.activeSelf)
            {
                p.RippleImage.gameObject.SetActive(false);
            }
        }

        private void DeactivateOverlayVisuals()
        {
            if (_quad4StandbyRoot != null && _quad4StandbyRoot.activeSelf) _quad4StandbyRoot.SetActive(false);
            if (_divVertical != null && _divVertical.activeSelf) _divVertical.SetActive(false);
            if (_divHorizontal != null && _divHorizontal.activeSelf) _divHorizontal.SetActive(false);
            if (_divBottomHalf != null && _divBottomHalf.activeSelf) _divBottomHalf.SetActive(false);
            if (_divRightHalf != null && _divRightHalf.activeSelf) _divRightHalf.SetActive(false);

            for (int i = 0; i < 4; i++)
            {
                if (_players[i].WorldCamera != null && _players[i].WorldCamera.enabled) _players[i].WorldCamera.enabled = false;
                if (_players[i].ViewportImage != null && _players[i].ViewportImage.gameObject.activeSelf) _players[i].ViewportImage.gameObject.SetActive(false);
                if (_players[i].CornerBadgeRoot != null && _players[i].CornerBadgeRoot.activeSelf) _players[i].CornerBadgeRoot.SetActive(false);
                if (_players[i].CursorRoot != null && _players[i].CursorRoot.activeSelf) _players[i].CursorRoot.SetActive(false);
            }
        }

        private void UpdateToastAndPanelUi(bool coopRendering)
        {
            if (_toastTimer > 0f)
            {
                _toastTimer -= Time.unscaledDeltaTime;
                if (_toastTimer <= 0f && _toastRoot != null && _toastRoot.activeSelf)
                {
                    _toastRoot.SetActive(false);
                }
            }

            if (_panelRoot == null)
            {
                return;
            }

            if (_panelRoot.activeSelf != _panelExpanded)
            {
                _panelRoot.SetActive(_panelExpanded);
            }
            if (_panelRaycaster != null && _panelRaycaster.enabled != _panelExpanded)
            {
                _panelRaycaster.enabled = _panelExpanded;
            }

            if (!_panelExpanded)
            {
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                PlayerCoopState p = _players[i];
                string status;
                if (i == 0)
                {
                    status = _player1UsesFirstGamepad
                        ? (p.AssignedGamepad != null
                            ? SplitScreenLocalization.Format(LocKey.SlotActiveDevice, "P1", p.AssignedGamepad.displayName)
                            : SplitScreenLocalization.Get(LocKey.SlotP1WaitingGamepad))
                        : SplitScreenLocalization.Get(LocKey.SlotP1ActiveMkb);
                }
                else if (p.IsActive)
                {
                    status = p.AssignedGamepad != null
                        ? SplitScreenLocalization.Format(LocKey.SlotActiveDevice, p.Label, p.AssignedGamepad.displayName)
                        : SplitScreenLocalization.Format(LocKey.SlotActiveKeyTest, p.Label);
                }
                else
                {
                    status = SplitScreenLocalization.Format(LocKey.SlotEmpty, p.Label);
                }

                if (_slotStatusTexts[i].text != status)
                {
                    _slotStatusTexts[i].text = status;
                }
            }

            string modeText = _coopEnabled
                ? (coopRendering
                    ? SplitScreenLocalization.Get(LocKey.BtnModeActive)
                    : (_cutsceneFullscreenActive
                        ? SplitScreenLocalization.Get(LocKey.BtnModeCutscene)
                        : SplitScreenLocalization.Get(LocKey.BtnModeReady)))
                : SplitScreenLocalization.Get(LocKey.BtnModeOff);
            if (_btnModeText.text != modeText)
            {
                _btnModeText.text = modeText;
            }

            string layoutText = SplitScreenLocalization.Get(_layout2P == TwoPlayerLayoutMode.VerticalLeftRight
                ? LocKey.BtnLayoutVertical
                : LocKey.BtnLayoutHorizontal);
            if (_btnLayoutText.text != layoutText)
            {
                _btnLayoutText.text = layoutText;
            }

            string p1Text = SplitScreenLocalization.Get(_player1UsesFirstGamepad
                ? LocKey.BtnP1Gamepad
                : LocKey.BtnP1MouseKb);
            if (_btnP1InputText.text != p1Text)
            {
                _btnP1InputText.text = p1Text;
            }

            string p2TestText = SplitScreenLocalization.Get(_p2KeyboardTestMode
                ? LocKey.BtnP2TestOn
                : LocKey.BtnP2TestOff);
            if (_btnP2TestText.text != p2TestText)
            {
                _btnP2TestText.text = p2TestText;
            }
        }

        private void OnGameLanguageChanged()
        {
            RefreshLocalizationIfNeeded(force: true);
        }

        private void RefreshLocalizationIfNeeded(bool force = false)
        {
            string overrideCode = _cfgUiLanguage != null ? _cfgUiLanguage.Value : "Auto";
            bool changed = SplitScreenLocalization.RefreshCurrentLanguage(overrideCode);
            if (changed || force || _appliedUiLanguageCode != SplitScreenLocalization.CurrentLanguageCode)
            {
                ApplyLocalizedUiTextAndFonts();
            }
        }

        private void ApplyLocalizedUiTextAndFonts()
        {
            string langCode = SplitScreenLocalization.CurrentLanguageCode;
            _appliedUiLanguageCode = langCode;
            Font font = SplitScreenTextures.GetUiFont(langCode);

            if (_standbyTitleText != null)
            {
                _standbyTitleText.font = font;
                _standbyTitleText.text = SplitScreenLocalization.Get(LocKey.StandbyTitle);
            }
            if (_standbySubText != null)
            {
                _standbySubText.font = font;
                _standbySubText.text = SplitScreenLocalization.Get(LocKey.StandbySubtitle);
            }
            if (_toastText != null)
            {
                _toastText.font = font;
            }
            if (_panelHeaderText != null)
            {
                _panelHeaderText.font = font;
                _panelHeaderText.text = SplitScreenLocalization.Get(LocKey.PanelTitle);
            }
            if (_panelGuideText != null)
            {
                _panelGuideText.font = font;
                _panelGuideText.text = SplitScreenLocalization.Get(LocKey.PanelGuide);
            }
            if (_btnCloseText != null)
            {
                _btnCloseText.font = font;
                _btnCloseText.text = SplitScreenLocalization.Get(LocKey.PanelClose);
            }

            Text[] dynamicLabels = { _btnModeText, _btnLayoutText, _btnP1InputText, _btnP2TestText };
            for (int i = 0; i < dynamicLabels.Length; i++)
            {
                if (dynamicLabels[i] != null)
                {
                    dynamicLabels[i].font = font;
                }
            }

            if (_slotStatusTexts != null)
            {
                for (int i = 0; i < _slotStatusTexts.Length; i++)
                {
                    if (_slotStatusTexts[i] != null)
                    {
                        _slotStatusTexts[i].font = font;
                    }
                }
            }

            for (int i = 0; i < 4; i++)
            {
                if (_players[i]?.CornerBadgeText != null)
                {
                    _players[i].CornerBadgeText.font = font;
                }
                if (_players[i]?.CursorTagText != null)
                {
                    _players[i].CursorTagText.font = font;
                }
            }
        }

        private void ShowToast(string message)
        {
            EnsureCanvasesAttached();
            RefreshLocalizationIfNeeded();
            if (_toastRoot == null || _toastText == null)
            {
                return;
            }

            _toastText.text = message;
            _toastRoot.SetActive(true);
            _toastTimer = 5.5f;
        }

        private void ReleaseAllPlayerResources()
        {
            for (int i = 0; i < 4; i++)
            {
                if (_players[i] != null)
                {
                    _players[i].ReleaseRenderTexture();
                    if (_players[i].WorldCamera != null)
                    {
                        Destroy(_players[i].WorldCamera.gameObject);
                        _players[i].WorldCamera = null;
                    }
                }
            }
        }
    }
}
