using System;
using System.Collections.Generic;
using BITEGG;
using BITEGG.Hidden;
using BITEGG.Hidden.GameInput;
using BITEGG.Hidden.GameInput.GameCamera;
using BITEGG.Utility;
using FunkyCode;
using FunkyCode.LightingSettings;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LostAndFoundSplitScreenCoop
{
    /// <summary>
    /// Harmony patches adapting camera projection, 2D raycasting, tap particles, and device isolation for split-screen co-op.
    /// </summary>
    internal static class SplitScreenPatches
    {
        private static readonly AccessTools.FieldRef<InteractiveTapParticle, PointerEventData> TapLastEventDataRef =
            AccessTools.FieldRefAccess<InteractiveTapParticle, PointerEventData>("lastEventData");

        private static readonly AccessTools.FieldRef<GameMouseController, Vector2> GmcStickInputRef =
            AccessTools.FieldRefAccess<GameMouseController, Vector2>("_stickInput");
        private static readonly AccessTools.FieldRef<GameMouseController, bool> GmcLeftClickRef =
            AccessTools.FieldRefAccess<GameMouseController, bool>("_leftClickInput");
        private static readonly AccessTools.FieldRef<GameMouseController, bool> GmcRightClickRef =
            AccessTools.FieldRefAccess<GameMouseController, bool>("_rightClickInput");
        private static readonly AccessTools.FieldRef<GameMouseController, bool> GmcMiddleClickRef =
            AccessTools.FieldRefAccess<GameMouseController, bool>("_middleClickInput");
        private static readonly AccessTools.FieldRef<GameMouseController, Vector2> GmcScrollInputRef =
            AccessTools.FieldRefAccess<GameMouseController, Vector2>("_scrollInput");

        private static readonly AccessTools.FieldRef<CameraInputSystem, InputAction> CisPanActionRef =
            AccessTools.FieldRefAccess<CameraInputSystem, InputAction>("_panAction");
        private static readonly AccessTools.FieldRef<CameraInputSystem, Vector2> CisRawPanMkbRef =
            AccessTools.FieldRefAccess<CameraInputSystem, Vector2>("_rawPanMKB");
        private static readonly AccessTools.FieldRef<CameraInputSystem, bool> CisPointerPressedRef =
            AccessTools.FieldRefAccess<CameraInputSystem, bool>("_pointerPressed");
        private static readonly AccessTools.FieldRef<CameraInputSystem, bool> CisDraggingRef =
            AccessTools.FieldRefAccess<CameraInputSystem, bool>("_dragging");
        private static readonly AccessTools.FieldRef<CameraInputSystem, float> CisEdgeSizeRef =
            AccessTools.FieldRefAccess<CameraInputSystem, float>("edgeSize");

        private static readonly AccessTools.FieldRef<CameraController, Vector2> CcPositionRef =
            AccessTools.FieldRefAccess<CameraController, Vector2>("_position");

        private delegate void SubmitZoomDelegate(CameraInputSystem instance, float zoomAxis, bool fromPinch);
        private static readonly SubmitZoomDelegate CisSubmitMethod =
            AccessTools.MethodDelegate<SubmitZoomDelegate>(AccessTools.Method(typeof(CameraInputSystem), "Submit"));

        private static bool TryGetActiveP1Viewport(CameraController instance, out Rect p1Viewport)
        {
            SplitScreenCoopManager mgr = SplitScreenCoopManager.Instance;
            if (mgr != null && mgr.IsSplitScreenRendering && instance == CameraController.Current)
            {
                p1Viewport = mgr.GetPlayerViewport(0);
                return true;
            }
            p1Viewport = default;
            return false;
        }

        private struct SavedEventPositionState
        {
            public PointerEventData EventData;
            public Vector2 OriginalPos;
            public bool Restorable;
        }

        [HarmonyPatch(typeof(CameraController), "GetAspect")]
        private static class CameraControllerGetAspectPatch
        {
            private static bool Prefix(CameraController __instance, ref float __result)
            {
                if (!TryGetActiveP1Viewport(__instance, out Rect vp))
                {
                    return true;
                }
                __result = vp.width / Mathf.Max(1f, vp.height);
                return false;
            }
        }

        [HarmonyPatch(typeof(CameraController), "CalculateMaxZoomForWorldRect")]
        private static class CameraControllerMaxZoomPatch
        {
            private static bool Prefix(CameraController __instance, ref float __result)
            {
                if (!TryGetActiveP1Viewport(__instance, out Rect vp) || __instance.settings == null)
                {
                    return true;
                }

                float aspect = vp.width / Mathf.Max(1f, vp.height);
                Rect wr = __instance.settings.worldRect;
                float halfH = wr.height * 0.5f;
                float halfW = wr.width / Mathf.Max(0.1f, 2f * aspect);
                // Keep P1 viewport inside world bounds even when narrow vertical split-screen makes aspect < 16:9.
                __result = Mathf.Min(halfH, halfW);
                return false;
            }
        }

        [HarmonyPatch(typeof(CameraController), "ScreenToWorldPoint")]
        private static class CameraControllerScreenToWorldPatch
        {
            private static bool Prefix(CameraController __instance, Vector2 screenPos, ref Vector2 __result)
            {
                if (!TryGetActiveP1Viewport(__instance, out Rect vp))
                {
                    return true;
                }

                float aspect = vp.width / Mathf.Max(1f, vp.height);
                float halfH = __instance.CameraZoom;
                float halfW = halfH * aspect;
                float ndcX = ((screenPos.x - vp.x) / Mathf.Max(1f, vp.width) - 0.5f) * 2f;
                float ndcY = ((screenPos.y - vp.y) / Mathf.Max(1f, vp.height) - 0.5f) * 2f;
                Vector2 pos = CcPositionRef(__instance);
                __result = pos + new Vector2(ndcX * halfW, ndcY * halfH);
                return false;
            }
        }

        [HarmonyPatch(typeof(CameraController), "GetWorldPerPixel")]
        private static class CameraControllerWorldPerPixelPatch
        {
            private static bool Prefix(CameraController __instance, ref Vector2 __result)
            {
                if (!TryGetActiveP1Viewport(__instance, out Rect vp))
                {
                    return true;
                }

                float aspect = vp.width / Mathf.Max(1f, vp.height);
                float zoom = __instance.CameraZoom;
                __result = new Vector2(
                    (zoom * aspect * 2f) / Mathf.Max(1f, vp.width),
                    (zoom * 2f) / Mathf.Max(1f, vp.height));
                return false;
            }
        }

        [HarmonyPatch(typeof(CameraController), "AnchorZoomToScreenPoint")]
        private static class CameraControllerAnchorZoomPatch
        {
            private static bool Prefix(CameraController __instance, Vector2 screenPos, float prevZoom, float newZoom)
            {
                if (!TryGetActiveP1Viewport(__instance, out Rect vp))
                {
                    return true;
                }

                float aspect = vp.width / Mathf.Max(1f, vp.height);
                float ndcX = ((screenPos.x - vp.x) / Mathf.Max(1f, vp.width) - 0.5f) * 2f;
                float ndcY = ((screenPos.y - vp.y) / Mathf.Max(1f, vp.height) - 0.5f) * 2f;
                Vector2 pos = CcPositionRef(__instance);
                Vector2 before = pos + new Vector2(ndcX * prevZoom * aspect, ndcY * prevZoom);
                Vector2 after = pos + new Vector2(ndcX * newZoom * aspect, ndcY * newZoom);
                CcPositionRef(__instance) = pos + (before - after);
                return false;
            }
        }

        [HarmonyPatch(typeof(CameraController), "SetCamera")]
        private static class CameraControllerSetCameraPatch
        {
            private static void Postfix(Vector2 position, float orthoSize)
            {
                SplitScreenCoopManager.Instance?.OnMainCameraReset(position, orthoSize);
            }
        }

        [HarmonyPatch(typeof(CameraController), "LateUpdate")]
        private static class CameraControllerLateUpdatePatch
        {
            [HarmonyPriority(Priority.Low)]
            private static void Postfix()
            {
                SplitScreenCoopManager.Instance?.SyncCamerasAndCursorsAfterCameraUpdate();
            }
        }

        [HarmonyPatch(typeof(CustomInputModule), "RaycastAll")]
        private static class CustomInputModuleRaycastAllPatch
        {
            private static bool Prefix(
                CustomInputModule __instance,
                PointerEventData _eventData,
                List<RaycastResult> _resultAppendList,
                RaycastHit2D[] ___hits,
                int ___CombinedMask)
            {
                SplitScreenCoopManager mgr = SplitScreenCoopManager.Instance;
                if (mgr == null || !mgr.IsSplitScreenRendering)
                {
                    return true;
                }

                if (!ScreenUtility.IsPointInGameScreen(_eventData.position))
                {
                    return false;
                }

                _resultAppendList.Clear();

                int playerIdx = mgr.ActiveRaycastPlayerIndex;
                PlayerCoopState player = mgr.GetPlayerState(playerIdx);
                if (player == null || !player.IsActive)
                {
                    return false;
                }

                // Prevent Player 1 from blindly raycasting hidden objects through P2-P4 viewports.
                if (playerIdx == 0 && !player.PixelViewport.Contains(_eventData.position))
                {
                    return false;
                }

                Camera cam = player.WorldCamera != null ? player.WorldCamera : Camera.main;
                if (cam == null)
                {
                    return false;
                }

                Vector2 worldPos = player.ScreenToPlayerWorld(_eventData.position, Camera.main);
                Array.Clear(___hits, 0, ___hits.Length);
                Physics2D.queriesHitTriggers = true;
                int hitCount = Physics2D.RaycastNonAlloc(worldPos, Vector2.zero, ___hits, float.MaxValue, ___CombinedMask);
                if (hitCount <= 0)
                {
                    return false;
                }

                Vector3 camPos = cam.transform.position;
                for (int i = 0; i < hitCount; i++)
                {
                    RaycastHit2D hit = ___hits[i];
                    if (!hit)
                    {
                        continue;
                    }

                    GameObject colliderGo = hit.collider.gameObject;
                    Renderer renderer = colliderGo.GetComponent<Renderer>();
                    if (renderer == null)
                    {
                        Transform parent = colliderGo.transform.parent;
                        if (parent != null)
                        {
                            renderer = parent.GetComponentInChildren<Renderer>();
                        }
                    }

                    var result = new RaycastResult
                    {
                        gameObject = hit.transform.gameObject,
                        distance = Vector3.Distance(camPos, hit.point),
                        worldPosition = hit.point,
                        worldNormal = hit.normal,
                        screenPosition = _eventData.position,
                        index = _resultAppendList.Count,
                        sortingLayer = renderer != null ? renderer.sortingLayerID : 0,
                        sortingOrder = renderer != null ? renderer.sortingOrder : 0
                    };
                    _resultAppendList.Add(result);
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(InteractiveTapParticle), "PlayParticle")]
        private static class InteractiveTapParticlePlayPatch
        {
            private static void Prefix(InteractiveTapParticle __instance, out SavedEventPositionState __state)
            {
                __state = default;
                SplitScreenCoopManager mgr = SplitScreenCoopManager.Instance;
                if (mgr == null || !mgr.IsSplitScreenRendering)
                {
                    return;
                }

                PointerEventData evt = TapLastEventDataRef(__instance);
                if (evt == null)
                {
                    return;
                }

                __state = BeginMainCameraScreenRemap(mgr, evt);
            }

            private static void Postfix(ref SavedEventPositionState __state)
            {
                EndMainCameraScreenRemap(ref __state);
            }
        }

        [HarmonyPatch(typeof(StageManager), "OnClickEmpty")]
        private static class StageManagerOnClickEmptyPatch
        {
            private static void Prefix(PointerEventData _eventData, out SavedEventPositionState __state)
            {
                __state = default;
                SplitScreenCoopManager mgr = SplitScreenCoopManager.Instance;
                if (mgr == null || !mgr.IsSplitScreenRendering || _eventData == null)
                {
                    return;
                }

                __state = BeginMainCameraScreenRemap(mgr, _eventData);
            }

            private static void Postfix(ref SavedEventPositionState __state)
            {
                EndMainCameraScreenRemap(ref __state);
            }
        }

        private static SavedEventPositionState BeginMainCameraScreenRemap(SplitScreenCoopManager mgr, PointerEventData evt)
        {
            Camera mainCam = Camera.main;
            PlayerCoopState player = mgr.GetPlayerState(mgr.ActiveRaycastPlayerIndex);
            if (mainCam == null || player == null || !player.IsActive)
            {
                return default;
            }

            var state = new SavedEventPositionState
            {
                EventData = evt,
                OriginalPos = evt.position,
                Restorable = true
            };
            evt.position = player.MapViewportScreenToMainCameraScreen(evt.position, mainCam);
            return state;
        }

        private static void EndMainCameraScreenRemap(ref SavedEventPositionState state)
        {
            if (state.Restorable && state.EventData != null)
            {
                state.EventData.position = state.OriginalPos;
            }
        }

        [HarmonyPatch(typeof(GameInputController), "PairDevice")]
        private static class GameInputControllerPairDevicePatch
        {
            private static bool Prefix(InputDevice device)
            {
                SplitScreenCoopManager mgr = SplitScreenCoopManager.Instance;
                return mgr == null || !mgr.ShouldBlockDeviceFromMainUser(device);
            }
        }

        [HarmonyPatch(typeof(GameMouseController), "ReadVirtualMouseInputs")]
        private static class GameMouseControllerReadInputsPatch
        {
            private static void Postfix(GameMouseController __instance)
            {
                SplitScreenCoopManager mgr = SplitScreenCoopManager.Instance;
                if (mgr == null || !mgr.IsSplitScreenRendering)
                {
                    return;
                }

                // When co-op is active, only P1's assigned gamepad may drive GameMouseController's virtual cursor.
                if (!mgr.Player1UsesFirstGamepad)
                {
                    GmcStickInputRef(__instance) = Vector2.zero;
                    GmcLeftClickRef(__instance) = false;
                    GmcRightClickRef(__instance) = false;
                    GmcMiddleClickRef(__instance) = false;
                    GmcScrollInputRef(__instance) = Vector2.zero;
                    return;
                }

                Gamepad p1Pad = mgr.GetPlayerState(0)?.AssignedGamepad;
                if (p1Pad == null)
                {
                    GmcStickInputRef(__instance) = Vector2.zero;
                    GmcLeftClickRef(__instance) = false;
                    GmcRightClickRef(__instance) = false;
                    GmcMiddleClickRef(__instance) = false;
                    GmcScrollInputRef(__instance) = Vector2.zero;
                    return;
                }

                GmcStickInputRef(__instance) = SplitScreenCoopManager.ApplyRadialDeadzone(p1Pad.leftStick.ReadValue(), 0.16f);
                if (__instance.enableVirtualClicks)
                {
                    GmcLeftClickRef(__instance) = p1Pad.buttonSouth.isPressed;
                    GmcRightClickRef(__instance) = p1Pad.buttonEast.isPressed;
                    GmcMiddleClickRef(__instance) = p1Pad.buttonWest.isPressed;
                }
                GmcScrollInputRef(__instance) = Vector2.zero;
            }
        }

        [HarmonyPatch(typeof(GameMouseController), "ConstrainToScreen")]
        private static class GameMouseControllerConstrainPatch
        {
            private static void Postfix(ref Vector2 __result)
            {
                SplitScreenCoopManager mgr = SplitScreenCoopManager.Instance;
                if (mgr == null || !mgr.IsSplitScreenRendering || !mgr.Player1UsesFirstGamepad)
                {
                    return;
                }

                PlayerCoopState p1 = mgr.GetPlayerState(0);
                if (p1 == null || p1.HudUnlockHeld)
                {
                    return;
                }

                const float margin = 4f;
                Rect vp = p1.PixelViewport;
                __result.x = Mathf.Clamp(__result.x, vp.xMin + margin, Mathf.Max(vp.xMin + margin, vp.xMax - margin));
                __result.y = Mathf.Clamp(__result.y, vp.yMin + margin, Mathf.Max(vp.yMin + margin, vp.yMax - margin));
            }
        }

        [HarmonyPatch(typeof(CameraInputSystem), "GetPanVector")]
        private static class CameraInputSystemGetPanPatch
        {
            private static void Postfix(CameraInputSystem __instance, ref Vector2 __result)
            {
                SplitScreenCoopManager mgr = SplitScreenCoopManager.Instance;
                if (mgr == null || !mgr.IsSplitScreenRendering)
                {
                    return;
                }

                PlayerCoopState p1 = mgr.GetPlayerState(0);
                if (p1 == null)
                {
                    return;
                }

                if (mgr.Player1UsesFirstGamepad)
                {
                    Vector2 stick = p1.AssignedGamepad != null
                        ? SplitScreenCoopManager.ApplyRadialDeadzone(p1.AssignedGamepad.rightStick.ReadValue(), 0.16f)
                        : Vector2.zero;
                    __result = stick;
                    return;
                }

                // Isolate P1 keyboard+mouse pan from P2-P4 gamepads and P2 keyboard test arrows.
                Vector2 kbPan = mgr.ReadPlayer1KeyboardPan(CisRawPanMkbRef(__instance));
                if (kbPan.sqrMagnitude > 0.0001f)
                {
                    __result = kbPan;
                    return;
                }

                InputAction panAction = CisPanActionRef(__instance);
                bool panningWithAction = panAction != null && panAction.IsPressed();
                bool useEdge = !panningWithAction && Application.isFocused && mgr.IsEdgeScrollAllowedForP1();
                __result = useEdge ? mgr.ComputeP1ViewportEdgeScroll(CisEdgeSizeRef(__instance)) : Vector2.zero;
            }
        }

        [HarmonyPatch(typeof(CameraInputSystem), "GetEdgeScrollingVector")]
        private static class CameraInputSystemEdgeScrollPatch
        {
            private static bool Prefix(CameraInputSystem __instance, ref Vector2 __result)
            {
                SplitScreenCoopManager mgr = SplitScreenCoopManager.Instance;
                if (mgr == null || !mgr.IsSplitScreenRendering)
                {
                    return true;
                }

                __result = mgr.ComputeP1ViewportEdgeScroll(CisEdgeSizeRef(__instance));
                return false;
            }
        }

        [HarmonyPatch(typeof(CameraInputSystem), "ProcessPointerDrag")]
        private static class CameraInputSystemDragPatch
        {
            private static bool Prefix(CameraInputSystem __instance)
            {
                SplitScreenCoopManager mgr = SplitScreenCoopManager.Instance;
                if (mgr == null || !mgr.IsSplitScreenRendering || mgr.Player1UsesFirstGamepad)
                {
                    return true;
                }

                // Do not initiate P1 map drag while mouse cursor sits over another player's viewport.
                if (!CisPointerPressedRef(__instance) && !CisDraggingRef(__instance) && Mouse.current != null)
                {
                    Vector2 mousePos = Mouse.current.position.ReadValue();
                    Rect p1Vp = mgr.GetPlayerViewport(0);
                    if (!p1Vp.Contains(mousePos))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(CameraInputSystem), "ProcessZoomInput")]
        private static class CameraInputSystemZoomPatch
        {
            private static bool Prefix(CameraInputSystem __instance)
            {
                SplitScreenCoopManager mgr = SplitScreenCoopManager.Instance;
                if (mgr == null || !mgr.IsSplitScreenRendering)
                {
                    return true;
                }

                if (CustomInputModule.IsOverUI)
                {
                    return false;
                }

                float zoomAxis = 0f;
                if (!mgr.Player1UsesFirstGamepad)
                {
                    bool mouseInP1 = Mouse.current == null || mgr.GetPlayerViewport(0).Contains(Mouse.current.position.ReadValue());
                    if (mouseInP1 && __instance.zoomScroll != null && __instance.zoomScroll.action != null)
                    {
                        zoomAxis = __instance.zoomScroll.action.ReadValue<float>();
                    }
                    if (Mathf.Abs(zoomAxis) <= 0.0001f)
                    {
                        zoomAxis = mgr.ReadPlayer1KeyboardZoom();
                    }
                }
                else
                {
                    zoomAxis = mgr.ReadPlayer1GamepadZoom();
                }

                if (Mathf.Abs(zoomAxis) > 0.0001f)
                {
                    CisSubmitMethod(__instance, zoomAxis, false);
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(LightingManager2D), "LateUpdate")]
        private static class LightingManager2DLateUpdatePatch
        {
            private static void Prefix()
            {
                SplitScreenCoopManager.Instance?.OnBeforeLightingManagerLateUpdate();
            }
        }

        [HarmonyPatch(typeof(FillScreenSprite), "OnEnable")]
        private static class FillScreenSpriteOnEnablePatch
        {
            private static void Postfix(FillScreenSprite __instance)
            {
                SplitScreenCoopManager.RegisterFillScreenSprite(__instance);
            }
        }

        [HarmonyPatch(typeof(FillScreenSprite), "OnDisable")]
        private static class FillScreenSpriteOnDisablePatch
        {
            private static void Postfix(FillScreenSprite __instance)
            {
                SplitScreenCoopManager.UnregisterFillScreenSprite(__instance);
            }
        }

        [HarmonyPatch(typeof(FillScreenSprite), "ResizeSprite")]
        private static class FillScreenSpriteResizePatch
        {
            private static bool Prefix(FillScreenSprite __instance, float _zoom)
            {
                SplitScreenCoopManager mgr = SplitScreenCoopManager.Instance;
                if (mgr == null || !mgr.IsSplitScreenRendering)
                {
                    return true;
                }

                PlayerCoopState p1 = mgr.GetPlayerState(0);
                if (p1 != null && p1.WorldCamera != null)
                {
                    SplitScreenCoopManager.ResizeFillScreenSpriteForCamera(__instance, p1.WorldCamera, _zoom);
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(LightingMeshRenderer), "Reset")]
        private static class LightingMeshRendererResetPatch
        {
            private static bool Prefix(
                LightingMeshRenderer __instance,
                MeshRenderer ___meshRenderer,
                MeshModeShader ___meshModeShader)
            {
                // Preserve shared custom light material mainTexture when off-screen lights cull themselves.
                if (___meshModeShader != MeshModeShader.Custom)
                {
                    return true;
                }

                __instance.owner = null;
                if (___meshRenderer != null)
                {
                    ___meshRenderer.enabled = false;
                }
                return false;
            }
        }
    }
}
