using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace LostAndFoundSplitScreenCoop
{
    /// <summary>
    /// URP Compatibility-Mode render pass that stamps opaque alpha (1.0) onto a camera's LDR RenderTexture without GPU sync stalls.
    /// </summary>
    internal sealed class ForceOpaqueAlphaRenderPass : ScriptableRenderPass
    {
        private static readonly Matrix4x4 OrthoZeroToOne = Matrix4x4.Ortho(0f, 1f, 0f, 1f, -1f, 1f);
        private CommandBuffer _cmd;

        public ForceOpaqueAlphaRenderPass()
        {
            renderPassEvent = RenderPassEvent.AfterRendering + 10;
            _cmd = new CommandBuffer { name = "SplitCoop_ForceOpaqueAlpha" };
        }

#pragma warning disable 0618
        [Obsolete("Compatibility mode render pass for URP.", false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Camera cam = renderingData.cameraData.camera;
            RenderTexture targetRt = cam != null ? cam.targetTexture : null;
            if (targetRt == null || !targetRt.IsCreated() || _cmd == null)
            {
                return;
            }

            Material mat = SplitScreenTextures.ForceOpaqueAlphaMaterial;
            Mesh quad = SplitScreenTextures.FullScreenUnitQuadMesh;
            if (mat == null || quad == null)
            {
                return;
            }

            _cmd.Clear();
            _cmd.SetRenderTarget(targetRt);
            _cmd.SetViewport(new Rect(0f, 0f, targetRt.width, targetRt.height));
            _cmd.SetViewProjectionMatrices(Matrix4x4.identity, OrthoZeroToOne);
            _cmd.DrawMesh(quad, Matrix4x4.identity, mat, 0, 0);
            context.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
        }
#pragma warning restore 0618

        public void Dispose()
        {
            if (_cmd != null)
            {
                _cmd.Release();
                _cmd = null;
            }
        }
    }

    /// <summary>
    /// Shared cozy-desk color palette and procedural UI sprites for split-screen overlays.
    /// </summary>
    internal static class SplitScreenTextures
    {
        public static readonly Color32 SurfaceDark = new Color32(35, 28, 24, 232);
        public static readonly Color32 SurfaceCard = new Color32(48, 39, 33, 242);
        public static readonly Color32 SurfaceStandby = new Color32(30, 24, 21, 255);
        public static readonly Color32 DividerLine = new Color32(58, 47, 40, 255);
        public static readonly Color32 BorderMuted = new Color32(216, 207, 194, 165);
        public static readonly Color32 TextPrimary = new Color32(255, 248, 236, 255);
        public static readonly Color32 TextSecondary = new Color32(216, 207, 194, 255);

        public static readonly Color32[] PlayerColors =
        {
            new Color32(244, 185, 66, 255),
            new Color32(232, 93, 74, 255),
            new Color32(42, 157, 143, 255),
            new Color32(74, 111, 227, 255)
        };

        private static Sprite _solidWhiteSprite;
        private static Sprite _cursorSprite;
        private static Sprite _ringSprite;
        private static readonly Dictionary<string, Font> ScriptFonts = new Dictionary<string, Font>(StringComparer.Ordinal);
        private static readonly HashSet<Font> OwnedDynamicFonts = new HashSet<Font>();
        private static Font _fallbackBuiltinFont;
        private static Material _forceOpaqueAlphaMat;
        private static Mesh _fullScreenUnitQuadMesh;

        internal static Material ForceOpaqueAlphaMaterial
        {
            get
            {
                if (_forceOpaqueAlphaMat == null)
                {
                    Shader shader = Shader.Find("Hidden/Internal-Colored");
                    if (shader == null)
                    {
                        return null;
                    }

                    _forceOpaqueAlphaMat = new Material(shader)
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    _forceOpaqueAlphaMat.SetColor("_Color", new Color(0f, 0f, 0f, 1f));
                    _forceOpaqueAlphaMat.SetInt("_SrcBlend", (int)BlendMode.One);
                    _forceOpaqueAlphaMat.SetInt("_DstBlend", (int)BlendMode.One);
                    _forceOpaqueAlphaMat.SetInt("_ZWrite", 0);
                    _forceOpaqueAlphaMat.SetInt("_ZTest", (int)CompareFunction.Always);
                    _forceOpaqueAlphaMat.SetInt("_Cull", (int)CullMode.Off);
                }
                return _forceOpaqueAlphaMat;
            }
        }

        internal static Mesh FullScreenUnitQuadMesh
        {
            get
            {
                if (_fullScreenUnitQuadMesh == null)
                {
                    var mesh = new Mesh
                    {
                        name = "SplitCoop_UnitQuad",
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    mesh.vertices = new[]
                    {
                        new Vector3(0f, 0f, 0f),
                        new Vector3(1f, 0f, 0f),
                        new Vector3(1f, 1f, 0f),
                        new Vector3(0f, 1f, 0f)
                    };
                    var white = new Color32(255, 255, 255, 255);
                    mesh.colors32 = new[] { white, white, white, white };
                    mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                    mesh.UploadMeshData(true);
                    _fullScreenUnitQuadMesh = mesh;
                }
                return _fullScreenUnitQuadMesh;
            }
        }

        /// <summary>
        /// Releases cached procedural textures, sprites, meshes, and materials created by the mod.
        /// </summary>
        public static void DestroyAll()
        {
            if (_forceOpaqueAlphaMat != null)
            {
                Object.Destroy(_forceOpaqueAlphaMat);
                _forceOpaqueAlphaMat = null;
            }
            if (_fullScreenUnitQuadMesh != null)
            {
                Object.Destroy(_fullScreenUnitQuadMesh);
                _fullScreenUnitQuadMesh = null;
            }
            if (_solidWhiteSprite != null)
            {
                if (_solidWhiteSprite.texture != null)
                {
                    Object.Destroy(_solidWhiteSprite.texture);
                }
                Object.Destroy(_solidWhiteSprite);
                _solidWhiteSprite = null;
            }
            if (_cursorSprite != null)
            {
                if (_cursorSprite.texture != null)
                {
                    Object.Destroy(_cursorSprite.texture);
                }
                Object.Destroy(_cursorSprite);
                _cursorSprite = null;
            }
            if (_ringSprite != null)
            {
                if (_ringSprite.texture != null)
                {
                    Object.Destroy(_ringSprite.texture);
                }
                Object.Destroy(_ringSprite);
                _ringSprite = null;
            }
            foreach (Font font in OwnedDynamicFonts)
            {
                if (font != null)
                {
                    Object.Destroy(font);
                }
            }
            OwnedDynamicFonts.Clear();
            ScriptFonts.Clear();
            _fallbackBuiltinFont = null;
        }

        public static Sprite SolidWhiteSprite
        {
            get
            {
                if (_solidWhiteSprite == null)
                {
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                    {
                        hideFlags = HideFlags.HideAndDontSave,
                        filterMode = FilterMode.Bilinear
                    };
                    var white = new Color32(255, 255, 255, 255);
                    tex.SetPixels32(new[] { white, white, white, white });
                    tex.Apply(false, true);
                    _solidWhiteSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
                    _solidWhiteSprite.hideFlags = HideFlags.HideAndDontSave;
                }
                return _solidWhiteSprite;
            }
        }

        public static Sprite CursorSprite
        {
            get
            {
                if (_cursorSprite == null)
                {
                    _cursorSprite = BuildPointerCursorSprite();
                }
                return _cursorSprite;
            }
        }

        public static Sprite RingSprite
        {
            get
            {
                if (_ringSprite == null)
                {
                    _ringSprite = BuildRingSprite(64, 5f);
                }
                return _ringSprite;
            }
        }

        public static Font UiFont => GetUiFont(SplitScreenLocalization.CurrentLanguageCode);

        public static Font GetUiFont(string langCode)
        {
            string scriptGroup = SplitScreenLocalization.GetFontScriptGroup(langCode);
            if (ScriptFonts.TryGetValue(scriptGroup, out Font cached) && cached != null)
            {
                return cached;
            }

            string[] candidates = GetOsFontCandidates(scriptGroup);
            Font created = null;
            try
            {
                created = Font.CreateDynamicFontFromOSFont(candidates, 16);
                if (created != null)
                {
                    created.hideFlags = HideFlags.HideAndDontSave;
                    OwnedDynamicFonts.Add(created);
                    ScriptFonts[scriptGroup] = created;
                    return created;
                }
            }
            catch (Exception)
            {
                created = null;
            }

            if (_fallbackBuiltinFont == null)
            {
                try
                {
                    _fallbackBuiltinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                catch (Exception)
                {
                    Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
                    if (fonts != null && fonts.Length > 0)
                    {
                        _fallbackBuiltinFont = fonts[0];
                    }
                }
            }

            return _fallbackBuiltinFont;
        }

        private static string[] GetOsFontCandidates(string scriptGroup)
        {
            switch (scriptGroup)
            {
                case "th":
                    return new[] { "Leelawadee UI", "Tahoma", "Segoe UI", "Arial" };
                case "zh-CN":
                    return new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Segoe UI", "Arial" };
                case "zh-TW":
                    return new[] { "Microsoft JhengHei UI", "Microsoft JhengHei", "PMingLiU", "Segoe UI", "Arial" };
                case "ja":
                    return new[] { "Yu Gothic UI", "Meiryo UI", "Meiryo", "MS UI Gothic", "Segoe UI", "Arial" };
                case "ko":
                    return new[] { "Malgun Gothic", "Gulim", "Segoe UI", "Arial" };
                default:
                    return new[] { "Segoe UI", "Arial" };
            }
        }

        private static Sprite BuildPointerCursorSprite()
        {
            const int size = 36;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color32[size * size];
            var outline = new Color32(35, 28, 24, 255);
            var fill = new Color32(255, 255, 255, 255);

            // Polygon triangle + stem oriented with hot-spot at top-left (pixel x=2, y=size-3).
            Vector2 pTip = new Vector2(2f, size - 3f);
            Vector2 pLeft = new Vector2(2f, size - 29f);
            Vector2 pNotch = new Vector2(10f, size - 22f);
            Vector2 pTailA = new Vector2(15f, size - 33f);
            Vector2 pTailB = new Vector2(19f, size - 31f);
            Vector2 pShoulder = new Vector2(14f, size - 20f);
            Vector2 pRight = new Vector2(23f, size - 20f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var pt = new Vector2(x + 0.5f, y + 0.5f);
                    bool insideMain = PointInTriangle(pt, pTip, pLeft, pRight);
                    bool insideTail = PointInQuad(pt, pNotch, pTailA, pTailB, pShoulder);
                    if (insideMain || insideTail)
                    {
                        pixels[y * size + x] = fill;
                    }
                }
            }

            // Dilate dark border around fill so cursor remains crisp on pale backgrounds.
            var finalPixels = (Color32[])pixels.Clone();
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int idx = y * size + x;
                    if (pixels[idx].a > 0)
                    {
                        continue;
                    }

                    bool touchesFill = false;
                    for (int dy = -2; dy <= 2 && !touchesFill; dy++)
                    {
                        for (int dx = -2; dx <= 2; dx++)
                        {
                            if (dx * dx + dy * dy > 5)
                            {
                                continue;
                            }
                            int nx = x + dx;
                            int ny = y + dy;
                            if (nx >= 0 && nx < size && ny >= 0 && ny < size && pixels[ny * size + nx].a > 0)
                            {
                                touchesFill = true;
                                break;
                            }
                        }
                    }

                    if (touchesFill)
                    {
                        finalPixels[idx] = outline;
                    }
                }
            }

            tex.SetPixels32(finalPixels);
            tex.Apply(false, true);

            // Pivot aligns tip near top-left corner so visual tip matches pointer coordinates.
            var pivot = new Vector2(2f / size, (size - 3f) / size);
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), pivot, 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static Sprite BuildRingSprite(int size, float thickness)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            float outerRadius = center - 1.5f;
            float innerRadius = outerRadius - thickness;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = 0f;
                    if (dist <= outerRadius && dist >= innerRadius)
                    {
                        float edgeOuter = Mathf.Clamp01(outerRadius - dist + 0.7f);
                        float edgeInner = Mathf.Clamp01(dist - innerRadius + 0.7f);
                        alpha = Mathf.Min(edgeOuter, edgeInner);
                    }
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = CrossSign(p, a, b);
            float d2 = CrossSign(p, b, c);
            float d3 = CrossSign(p, c, a);
            bool hasNeg = (d1 < 0f) || (d2 < 0f) || (d3 < 0f);
            bool hasPos = (d1 > 0f) || (d2 > 0f) || (d3 > 0f);
            return !(hasNeg && hasPos);
        }

        private static bool PointInQuad(Vector2 p, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            return PointInTriangle(p, a, b, c) || PointInTriangle(p, a, c, d);
        }

        private static float CrossSign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }
    }
}
