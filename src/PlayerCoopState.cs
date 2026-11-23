using System;
using BITEGG.Hidden.GameInput.GameCamera;
using BITEGG.Utility.InputEvents;
using EPOOutline;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LostAndFoundSplitScreenCoop
{
    public enum TwoPlayerLayoutMode
    {
        VerticalLeftRight = 0,
        HorizontalTopBottom = 1
    }

    /// <summary>
    /// Holds independent camera, RenderTexture, viewport bounds, and cursor interaction state for one co-op slot.
    /// </summary>
    internal sealed class PlayerCoopState
    {
        public readonly int PlayerIndex;
        public readonly string Label;
        public readonly string LabelPlus;
        public Color32 AccentColor => SplitScreenTextures.PlayerColors[PlayerIndex];

        public bool IsActive;
        public bool CameraInitialized;
        public Gamepad AssignedGamepad;
        public bool UseKeyboardFallback;

        public Rect NormViewport = new Rect(0f, 0f, 1f, 1f);
        public Rect PixelViewport = new Rect(0f, 0f, 1920f, 1080f);

        public Camera WorldCamera;
        public Outliner ClonedOutliner;
        public UniversalAdditionalCameraData CameraData;
        public SpriteRenderer BackgroundClone;
        public RenderTexture TargetTexture;

        public Camera LastSyncedMainCamera;
        public int LastSyncedCullingMask;
        public CameraClearFlags LastSyncedClearFlags;
        public Color LastSyncedBgColor;
        public bool LastSyncedOutlinerEnabled;
        public bool CameraSettingsSynced;

        public RawImage ViewportImage;
        public Rect LastAppliedViewportNorm = new Rect(-1f, -1f, -1f, -1f);
        public GameObject CornerBadgeRoot;
        public RectTransform CornerBadgeRect;
        public Text CornerBadgeText;
        public Rect LastAppliedBadgeNorm = new Rect(-1f, -1f, -1f, -1f);

        public GameObject CursorRoot;
        public RectTransform CursorTransform;
        public Image CursorArrowImage;
        public Text CursorTagText;
        public Image RippleImage;
        public RectTransform RippleTransform;
        public float ClickRippleTimer;
        public Vector2 LastAppliedCursorLocalPos = new Vector2(float.NaN, float.NaN);
        public float LastAppliedCursorScale = -1f;

        public Vector2 CamPos;
        public float Zoom = 5f;
        public float TargetZoom = 5f;
        public Vector2 PanVelocity;

        public Vector2 CursorScreenPos;
        public bool HudUnlockHeld;

        public PointerEventData EventData;
        public CustomPointerEvent CurrentHoverObject;
        public CustomPointerEvent LastHoverObject;
        public CustomPointerEvent LastDownObject;
        public Vector2 DownScreenPos;
        public bool IsPressed;
        public bool IsDragging;
        public float LastClickTime;
        public int ClickCount;

        public bool HasAnyHoverOrDrag =>
            CurrentHoverObject != null ||
            LastHoverObject != null ||
            LastDownObject != null ||
            IsPressed ||
            IsDragging;

        public PlayerCoopState(int index)
        {
            PlayerIndex = index;
            Label = $"P{index + 1}";
            LabelPlus = $"{Label}+";
        }

        public float Aspect => PixelViewport.height > 1f
            ? PixelViewport.width / PixelViewport.height
            : 1f;

        /// <summary>
        /// Converts a full-window screen position into world coordinates through this player's viewport camera.
        /// </summary>
        public Vector2 ScreenToPlayerWorld(Vector2 screenPos, Camera fallbackMainCam)
        {
            Camera cam = WorldCamera != null ? WorldCamera : fallbackMainCam;
            if (cam == null)
            {
                return Vector2.zero;
            }

            float vx = (screenPos.x - PixelViewport.x) / Mathf.Max(1f, PixelViewport.width);
            float vy = (screenPos.y - PixelViewport.y) / Mathf.Max(1f, PixelViewport.height);
            float depth = Mathf.Abs(cam.transform.position.z);
            Vector3 world = cam.ViewportToWorldPoint(new Vector3(vx, vy, depth));
            return new Vector2(world.x, world.y);
        }

        /// <summary>
        /// Maps a viewport screen coordinate into Camera.main screen pixels so vanilla particle/SFX callers resolve the same world point.
        /// </summary>
        public Vector2 MapViewportScreenToMainCameraScreen(Vector2 screenPos, Camera mainCam)
        {
            if (mainCam == null)
            {
                return screenPos;
            }

            Vector2 world = ScreenToPlayerWorld(screenPos, mainCam);
            Vector3 mainScreen = mainCam.WorldToScreenPoint(new Vector3(world.x, world.y, 0f));
            return new Vector2(mainScreen.x, mainScreen.y);
        }

        public void CenterCursorInViewport()
        {
            CursorScreenPos = new Vector2(
                PixelViewport.x + PixelViewport.width * 0.5f,
                PixelViewport.y + PixelViewport.height * 0.5f);
        }

        public void ClampCursorToBounds(bool allowFullScreen)
        {
            const float margin = 4f;
            if (allowFullScreen)
            {
                CursorScreenPos.x = Mathf.Clamp(CursorScreenPos.x, margin, Mathf.Max(margin, Screen.width - margin));
                CursorScreenPos.y = Mathf.Clamp(CursorScreenPos.y, margin, Mathf.Max(margin, Screen.height - margin));
                return;
            }

            float minX = PixelViewport.xMin + margin;
            float maxX = Mathf.Max(minX, PixelViewport.xMax - margin);
            float minY = PixelViewport.yMin + margin;
            float maxY = Mathf.Max(minY, PixelViewport.yMax - margin);
            CursorScreenPos.x = Mathf.Clamp(CursorScreenPos.x, minX, maxX);
            CursorScreenPos.y = Mathf.Clamp(CursorScreenPos.y, minY, maxY);
        }

        public void EnsureRenderTexture(bool hdr)
        {
            int targetW = Mathf.Clamp(Mathf.RoundToInt(PixelViewport.width), 64, Screen.width);
            int targetH = Mathf.Clamp(Mathf.RoundToInt(PixelViewport.height), 64, Screen.height);
            // RGB111110Float preserves full URP HDR + Bloom intensity while having no alpha channel to corrupt RawImage UI blending.
            RenderTextureFormat format = hdr && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float)
                ? RenderTextureFormat.RGB111110Float
                : RenderTextureFormat.Default;

            if (TargetTexture != null &&
                TargetTexture.width == targetW &&
                TargetTexture.height == targetH &&
                TargetTexture.format == format)
            {
                return;
            }

            ReleaseRenderTexture();
            TargetTexture = new RenderTexture(targetW, targetH, 24, format)
            {
                name = $"SplitCoop_RT_{Label}",
                filterMode = FilterMode.Bilinear,
                antiAliasing = 1,
                hideFlags = HideFlags.HideAndDontSave
            };
            TargetTexture.Create();

            if (WorldCamera != null)
            {
                WorldCamera.targetTexture = TargetTexture;
                FunkyCode.CameraTransform.GetCamera(WorldCamera)?.Update(true);
            }
            if (ViewportImage != null)
            {
                ViewportImage.texture = TargetTexture;
            }
        }

        public void ReleaseRenderTexture()
        {
            if (WorldCamera != null)
            {
                WorldCamera.targetTexture = null;
            }
            if (ViewportImage != null)
            {
                ViewportImage.texture = null;
            }
            if (TargetTexture != null)
            {
                TargetTexture.Release();
                Object.Destroy(TargetTexture);
                TargetTexture = null;
            }
        }

        public void ClearHoverAndDrag(PointerEventData fallbackData)
        {
            PointerEventData data = EventData ?? fallbackData;
            if (LastHoverObject != null && data != null)
            {
                LastHoverObject.OnPointerExit(data);
            }
            if (CurrentHoverObject != null && CurrentHoverObject != LastHoverObject && data != null)
            {
                CurrentHoverObject.OnPointerExit(data);
            }
            if (IsDragging && LastDownObject != null && data != null)
            {
                LastDownObject.OnPointerUp(data);
                LastDownObject.OnEndDrag(data);
            }

            CurrentHoverObject = null;
            LastHoverObject = null;
            LastDownObject = null;
            IsPressed = false;
            IsDragging = false;
        }

        /// <summary>
        /// Computes normalized screen rects ([0..1] bottom-left origin) for the active player count and layout mode.
        /// </summary>
        public static Rect GetNormalizedViewport(int index, int activeCount, TwoPlayerLayoutMode twoPlayerLayout)
        {
            if (activeCount <= 1)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            if (activeCount == 2)
            {
                if (twoPlayerLayout == TwoPlayerLayoutMode.VerticalLeftRight)
                {
                    return index == 0
                        ? new Rect(0f, 0f, 0.5f, 1f)
                        : new Rect(0.5f, 0f, 0.5f, 1f);
                }

                return index == 0
                    ? new Rect(0f, 0.5f, 1f, 0.5f)
                    : new Rect(0f, 0f, 1f, 0.5f);
            }

            switch (index)
            {
                case 0: return new Rect(0f, 0.5f, 0.5f, 0.5f);
                case 1: return new Rect(0.5f, 0.5f, 0.5f, 0.5f);
                case 2: return new Rect(0f, 0f, 0.5f, 0.5f);
                default: return new Rect(0.5f, 0f, 0.5f, 0.5f);
            }
        }

        private static readonly Func<CameraController, bool> GetIsClampZoomBounds =
            AccessTools.MethodDelegate<Func<CameraController, bool>>(
                AccessTools.PropertyGetter(typeof(CameraController), "IsClampZoomBounds"));

        /// <summary>
        /// Computes max safe orthographic zoom for a given aspect inside CameraSettings.worldRect.
        /// </summary>
        public static float ComputeMaxZoom(CameraController cc, float aspect)
        {
            if (cc == null || cc.settings == null)
            {
                return 20f;
            }

            CameraSettings s = cc.settings;
            float maxZoom = s.zoomMax;
            if (GetIsClampZoomBounds(cc) && s.useWorldBounds)
            {
                float halfH = s.worldRect.height * 0.5f;
                float halfW = s.worldRect.width / Mathf.Max(0.1f, 2f * aspect);
                float worldMax = Mathf.Min(halfH, halfW);
                if (worldMax > 0.25f)
                {
                    maxZoom = Mathf.Min(maxZoom, worldMax);
                }
            }

            return Mathf.Max(0.25f, maxZoom);
        }

        public static Vector2 ClampPositionToWorldRect(CameraController cc, Vector2 pos, float zoom, float aspect)
        {
            if (cc == null || cc.settings == null || !cc.settings.useWorldBounds || !cc.clampWithWorldRect)
            {
                return pos;
            }

            Rect wr = cc.settings.worldRect;
            float halfH = zoom;
            float halfW = zoom * aspect;

            float x = wr.width > halfW * 2f
                ? Mathf.Clamp(pos.x, wr.xMin + halfW, wr.xMax - halfW)
                : wr.center.x;
            float y = wr.height > halfH * 2f
                ? Mathf.Clamp(pos.y, wr.yMin + halfH, wr.yMax - halfH)
                : wr.center.y;
            return new Vector2(x, y);
        }
    }
}
