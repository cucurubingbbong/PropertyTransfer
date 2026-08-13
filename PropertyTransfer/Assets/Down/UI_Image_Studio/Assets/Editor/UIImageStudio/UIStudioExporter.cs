using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UIImageStudio
{
    public static class UIStudioExporter
    {
        private const int ExportLayer = 31;

        public static void ExportPng(UIStudioDocumentData data, string absolutePath)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrEmpty(absolutePath)) throw new ArgumentException("Export path is empty.", nameof(absolutePath));
            if (data.canvasWidth <= 0 || data.canvasHeight <= 0) throw new InvalidOperationException("Canvas size must be positive.");

            GameObject root = null;
            RenderTexture renderTexture = null;
            Texture2D output = null;
            RenderTexture previousActive = RenderTexture.active;
            List<Object> generatedObjects = new List<Object>();

            try
            {
                root = new GameObject("__UIStudioExportRoot__") { hideFlags = HideFlags.HideAndDontSave, layer = ExportLayer };

                GameObject cameraObject = new GameObject("Camera") { hideFlags = HideFlags.HideAndDontSave, layer = ExportLayer };
                cameraObject.transform.SetParent(root.transform, false);
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = data.canvasBackground;
                camera.orthographic = true;
                camera.nearClipPlane = -10f;
                camera.farClipPlane = 10f;
                camera.cullingMask = 1 << ExportLayer;

                renderTexture = new RenderTexture(data.canvasWidth, data.canvasHeight, 24, RenderTextureFormat.ARGB32)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    antiAliasing = 1,
                    filterMode = FilterMode.Bilinear
                };
                renderTexture.Create();
                camera.targetTexture = renderTexture;

                GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler))
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    layer = ExportLayer
                };
                canvasObject.transform.SetParent(root.transform, false);

                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;

                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = 1f;
                scaler.referencePixelsPerUnit = 100f;

                foreach (UIStudioLayer layer in data.layers)
                {
                    if (layer == null || !layer.visible) continue;
                    CreateLayerVisual(canvasObject.transform, layer, generatedObjects);
                }

                Canvas.ForceUpdateCanvases();
                camera.Render();

                RenderTexture.active = renderTexture;
                output = new Texture2D(data.canvasWidth, data.canvasHeight, TextureFormat.RGBA32, false, false)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                output.ReadPixels(new Rect(0f, 0f, data.canvasWidth, data.canvasHeight), 0, 0, false);
                output.Apply(false, false);

                byte[] png = output.EncodeToPNG();
                string directory = Path.GetDirectoryName(absolutePath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllBytes(absolutePath, png);

                if (IsInsideAssets(absolutePath))
                {
                    AssetDatabase.Refresh();
                    string assetPath = AbsoluteToAssetPath(absolutePath);
                    Object exported = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                    if (exported != null)
                    {
                        EditorGUIUtility.PingObject(exported);
                        Selection.activeObject = exported;
                    }
                }
            }
            finally
            {
                RenderTexture.active = previousActive;
                if (output != null) Object.DestroyImmediate(output);
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    Object.DestroyImmediate(renderTexture);
                }

                foreach (Object generatedObject in generatedObjects)
                {
                    if (generatedObject != null) Object.DestroyImmediate(generatedObject);
                }

                if (root != null) Object.DestroyImmediate(root);
            }
        }

        private static void CreateLayerVisual(Transform parent, UIStudioLayer layer, List<Object> generatedObjects)
        {
            if (layer.type == UIStudioLayerType.Shape)
            {
                CreateShape(parent, layer, generatedObjects);
                return;
            }

            if (layer.type == UIStudioLayerType.Text)
            {
                CreateText(parent, layer, generatedObjects);
                return;
            }

            CreateImage(parent, layer);
        }

        private static GameObject CreateBaseObject(Transform parent, UIStudioLayer layer, string suffix)
        {
            GameObject go = new GameObject(layer.name + suffix, typeof(RectTransform))
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = ExportLayer
            };
            RectTransform rectTransform = go.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(Mathf.Max(1f, layer.rect.width), Mathf.Max(1f, layer.rect.height));
            rectTransform.anchoredPosition = new Vector2(layer.rect.x + layer.rect.width * 0.5f, -(layer.rect.y + layer.rect.height * 0.5f));
            rectTransform.localEulerAngles = new Vector3(0f, 0f, -layer.rotation);
            return go;
        }

        private static void CreateShape(Transform parent, UIStudioLayer layer, List<Object> generatedObjects)
        {
            if (layer.shadowEnabled && layer.shadowColor.a > 0f)
            {
                int blur = Mathf.RoundToInt(Mathf.Clamp(layer.shadowBlur, 0f, 64f));
                UIStudioLayer shadowLayer = JsonUtility.FromJson<UIStudioLayer>(JsonUtility.ToJson(layer));
                shadowLayer.rect = new Rect(
                    layer.rect.x + layer.shadowOffset.x - blur,
                    layer.rect.y + layer.shadowOffset.y - blur,
                    layer.rect.width + blur * 2f,
                    layer.rect.height + blur * 2f);
                shadowLayer.rotation = layer.rotation;

                GameObject shadowObject = CreateBaseObject(parent, shadowLayer, "_Shadow");
                RawImage shadowImage = shadowObject.AddComponent<RawImage>();
                Texture2D shadowTexture = CreateSoftShadowTexture(
                    Mathf.Max(2, Mathf.CeilToInt(shadowLayer.rect.width)),
                    Mathf.Max(2, Mathf.CeilToInt(shadowLayer.rect.height)),
                    Mathf.Max(1f, layer.rect.width),
                    Mathf.Max(1f, layer.rect.height),
                    layer.shapeType,
                    layer.cornerRadius,
                    blur,
                    WithOpacity(layer.shadowColor, layer.opacity));
                generatedObjects.Add(shadowTexture);
                shadowImage.texture = shadowTexture;
                shadowImage.raycastTarget = false;
            }

            GameObject go = CreateBaseObject(parent, layer, "_Shape");
            RawImage image = go.AddComponent<RawImage>();
            Texture2D texture = CreateShapeTexture(
                Mathf.Max(2, Mathf.CeilToInt(layer.rect.width)),
                Mathf.Max(2, Mathf.CeilToInt(layer.rect.height)),
                layer.shapeType,
                WithOpacity(layer.fillColor, layer.opacity),
                WithOpacity(layer.strokeColor, layer.opacity),
                layer.strokeWidth,
                layer.cornerRadius);
            generatedObjects.Add(texture);
            image.texture = texture;
            image.raycastTarget = false;
        }

        private static void CreateText(Transform parent, UIStudioLayer layer, List<Object> generatedObjects)
        {
            GameObject go = CreateBaseObject(parent, layer, "_Text");
            Text text = go.AddComponent<Text>();
            text.text = layer.text ?? string.Empty;
            text.fontSize = Mathf.Max(1, layer.fontSize);
            text.fontStyle = layer.fontStyle;
            text.alignment = ToTextAnchor(layer.textAlignment);
            text.color = WithOpacity(layer.textColor, layer.opacity);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            Font font = null;
            if (!string.IsNullOrEmpty(layer.fontPath))
                font = AssetDatabase.LoadAssetAtPath<Font>(layer.fontPath);

            if (font == null)
            {
                string[] preferredFonts = { "Pretendard", "SUIT", "Malgun Gothic", "Apple SD Gothic Neo", "Arial" };
                font = Font.CreateDynamicFontFromOSFont(preferredFonts, Mathf.Max(12, layer.fontSize));
                if (font != null) generatedObjects.Add(font);
            }

            if (font == null)
            {
                try
                {
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                catch
                {
                }
            }

            text.font = font;
        }

        private static void CreateImage(Transform parent, UIStudioLayer layer)
        {
            Object asset = LoadImageAsset(layer);
            if (asset == null) return;

            GameObject go = CreateBaseObject(parent, layer, "_Image");

            Sprite sprite = asset as Sprite;
            if (sprite != null)
            {
                Image image = go.AddComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = layer.preserveAspect;
                image.color = WithOpacity(layer.imageTint, layer.opacity);
                image.raycastTarget = false;
                return;
            }

            Texture texture = asset as Texture;
            if (texture != null)
            {
                RawImage image = go.AddComponent<RawImage>();
                image.texture = texture;
                image.color = WithOpacity(layer.imageTint, layer.opacity);
                image.raycastTarget = false;

                if (layer.preserveAspect && texture.width > 0 && texture.height > 0)
                {
                    AspectRatioFitter fitter = go.AddComponent<AspectRatioFitter>();
                    fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                    fitter.aspectRatio = (float)texture.width / texture.height;
                }
            }
        }

        public static Object LoadImageAsset(UIStudioLayer layer)
        {
            if (layer == null || string.IsNullOrEmpty(layer.assetPath)) return null;

            if (layer.assetIsSprite)
            {
                Object[] all = AssetDatabase.LoadAllAssetsAtPath(layer.assetPath);
                Sprite named = all.OfType<Sprite>().FirstOrDefault(sprite => sprite.name == layer.subAssetName);
                if (named != null) return named;

                Sprite first = all.OfType<Sprite>().FirstOrDefault();
                if (first != null) return first;
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(layer.assetPath);
        }

        public static Texture2D CreateShapeTexture(int width, int height, UIStudioShapeType shapeType, Color fill, Color stroke, float strokeWidth, float radius)
        {
            width = Mathf.Max(2, width);
            height = Mathf.Max(2, height);
            strokeWidth = Mathf.Max(0f, strokeWidth);
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color32[] pixels = new Color32[width * height];
            float innerWidth = Mathf.Max(0f, width - strokeWidth * 2f);
            float innerHeight = Mathf.Max(0f, height - strokeWidth * 2f);
            float innerRadius = Mathf.Max(0f, radius - strokeWidth);
            Vector2 half = new Vector2(width, height) * 0.5f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - half;
                    bool outer = ContainsShapePoint(p, width, height, shapeType, radius);
                    Color pixel = Color.clear;
                    if (outer)
                    {
                        bool inner = strokeWidth > 0.001f && innerWidth > 1f && innerHeight > 1f &&
                                     ContainsShapePoint(p, innerWidth, innerHeight, shapeType, innerRadius);
                        pixel = (strokeWidth > 0.001f && !inner) ? stroke : fill;
                    }
                    pixels[y * width + x] = pixel;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateSoftShadowTexture(int width, int height, float shapeWidth, float shapeHeight, UIStudioShapeType shapeType, float radius, int blur, Color color)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color32[] pixels = new Color32[width * height];
            Vector2 half = new Vector2(width, height) * 0.5f;
            int steps = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(1, blur) / 2f), 2, 8);
            float maxExtra = Mathf.Max(0f, blur * 2f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - half;
                    float coverage = 0f;
                    for (int i = 0; i < steps; i++)
                    {
                        float t = i / (float)(steps - 1);
                        float testWidth = shapeWidth + maxExtra * t;
                        float testHeight = shapeHeight + maxExtra * t;
                        float testRadius = shapeType == UIStudioShapeType.Pill
                            ? Mathf.Min(testWidth, testHeight) * 0.5f
                            : Mathf.Max(0f, radius + blur * t * 0.5f);
                        if (ContainsShapePoint(p, testWidth, testHeight, shapeType, testRadius))
                            coverage += 1f - t * 0.75f;
                    }

                    float alpha = Mathf.Clamp01(coverage / steps);
                    pixels[y * width + x] = new Color(color.r, color.g, color.b, color.a * alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static bool ContainsShapePoint(Vector2 p, float width, float height, UIStudioShapeType shapeType, float radius)
        {
            float halfWidth = Mathf.Max(1f, width) * 0.5f;
            float halfHeight = Mathf.Max(1f, height) * 0.5f;
            switch (shapeType)
            {
                case UIStudioShapeType.Rectangle:
                    return Mathf.Abs(p.x) <= halfWidth && Mathf.Abs(p.y) <= halfHeight;

                case UIStudioShapeType.RoundedRectangle:
                {
                    float r = Mathf.Clamp(radius, 0f, Mathf.Min(halfWidth, halfHeight));
                    Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - new Vector2(halfWidth - r, halfHeight - r);
                    if (q.x <= 0f && q.y <= 0f) return true;
                    q = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
                    return q.sqrMagnitude <= r * r;
                }

                case UIStudioShapeType.Pill:
                {
                    float r = Mathf.Min(halfWidth, halfHeight);
                    Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - new Vector2(halfWidth - r, halfHeight - r);
                    if (q.x <= 0f && q.y <= 0f) return true;
                    q = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
                    return q.sqrMagnitude <= r * r;
                }

                case UIStudioShapeType.Ellipse:
                {
                    float nx = p.x / halfWidth;
                    float ny = p.y / halfHeight;
                    return nx * nx + ny * ny <= 1f;
                }

                case UIStudioShapeType.Diamond:
                    return Mathf.Abs(p.x) / halfWidth + Mathf.Abs(p.y) / halfHeight <= 1f;

                case UIStudioShapeType.Triangle:
                    return PointInTriangle(
                        new Vector2((p.x + halfWidth) / (halfWidth * 2f), (p.y + halfHeight) / (halfHeight * 2f)),
                        new Vector2(0.5f, 0f),
                        new Vector2(1f, 1f),
                        new Vector2(0f, 1f));

                default:
                    return Mathf.Abs(p.x) <= halfWidth && Mathf.Abs(p.y) <= halfHeight;
            }
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b);
            float d2 = Sign(p, b, c);
            float d3 = Sign(p, c, a);
            bool hasNeg = (d1 < 0f) || (d2 < 0f) || (d3 < 0f);
            bool hasPos = (d1 > 0f) || (d2 > 0f) || (d3 > 0f);
            return !(hasNeg && hasPos);
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        private static TextAnchor ToTextAnchor(UIStudioTextAlignment alignment)
        {
            switch (alignment)
            {
                case UIStudioTextAlignment.Left: return TextAnchor.MiddleLeft;
                case UIStudioTextAlignment.Right: return TextAnchor.MiddleRight;
                default: return TextAnchor.MiddleCenter;
            }
        }

        private static Color WithOpacity(Color color, float opacity)
        {
            color.a *= Mathf.Clamp01(opacity);
            return color;
        }

        private static bool IsInsideAssets(string absolutePath)
        {
            string assets = Path.GetFullPath(Application.dataPath).Replace('\\', '/').TrimEnd('/');
            string file = Path.GetFullPath(absolutePath).Replace('\\', '/');
            return file.StartsWith(assets + "/", StringComparison.OrdinalIgnoreCase) || string.Equals(file, assets, StringComparison.OrdinalIgnoreCase);
        }

        private static string AbsoluteToAssetPath(string absolutePath)
        {
            string assets = Path.GetFullPath(Application.dataPath).Replace('\\', '/').TrimEnd('/');
            string file = Path.GetFullPath(absolutePath).Replace('\\', '/');
            return "Assets" + file.Substring(assets.Length);
        }
    }
}
