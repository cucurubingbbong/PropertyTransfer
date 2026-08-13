using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UIImageStudio
{
    public sealed class UIImageStudioWindow : EditorWindow
    {
        private enum ToolMode { Select, Hand }
        private enum DragMode { None, Move, Pan, Resize, Rotate }
        private enum ResizeHandle { None, TopLeft, Top, TopRight, Right, BottomRight, Bottom, BottomLeft, Left }

        private const float TopHeight = 44f;
        private const float LeftWidth = 88f;
        private const float InspectorWidth = 300f;
        private const float LayersWidth = 248f;
        private const float BottomHeight = 190f;
        private const float MinZoom = 0.05f;
        private const float MaxZoom = 4f;
        private const int ImagePickerId = 917311;

        private UIStudioDocument document;
        private string selectedLayerId;
        private ToolMode toolMode = ToolMode.Select;
        private DragMode dragMode;
        private ResizeHandle resizeHandle;
        private Vector2 pan;
        private float zoom = 0.5f;
        private bool hasInitialFit;
        private Vector2 dragMouseStart;
        private Vector2 panStart;
        private Rect dragOriginalRect;
        private float dragOriginalRotation;
        private int activeUndoGroup = -1;
        private float? smartGuideX;
        private float? smartGuideY;
        private string currentDocumentPath;
        private bool dirty;
        private Vector2 inspectorScroll;
        private Vector2 layersScroll;
        private Vector2 assetsScroll;
        private string assetSearch = string.Empty;
        private double lastAssetRefreshTime;
        private readonly List<Object> assetEntries = new List<Object>();
        private readonly Dictionary<string, Object> assetCache = new Dictionary<string, Object>();
        private readonly Dictionary<string, Texture2D> shapePreviewCache = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, int> shapePreviewHashes = new Dictionary<string, int>();
        private Texture2D checkerTexture;
        private string layerClipboard;
        private int canvasPresetIndex;

        [MenuItem("Tools/UI Image Studio %#i")]
        public static void Open()
        {
            UIImageStudioWindow window = GetWindow<UIImageStudioWindow>();
            window.titleContent = new GUIContent("UI Image Studio");
            window.minSize = new Vector2(1100f, 650f);
            window.Show();
        }

        private void OnEnable()
        {
            EnsureDocument();
            checkerTexture = CreateCheckerTexture();
            Undo.undoRedoPerformed += OnUndoRedo;
            RefreshAssets();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            if (document != null) DestroyImmediate(document);
            if (checkerTexture != null) DestroyImmediate(checkerTexture);
            ClearShapePreviewCache();
        }

        private void OnUndoRedo()
        {
            dirty = true;
            Repaint();
        }

        private void EnsureDocument()
        {
            if (document != null) return;
            document = CreateInstance<UIStudioDocument>();
            document.hideFlags = HideFlags.HideAndDontSave;
            NewDocument(false);
        }

        private void OnGUI()
        {
            EnsureDocument();
            HandleObjectPicker(Event.current);

            Rect topRect = new Rect(0f, 0f, position.width, TopHeight);
            Rect leftRect = new Rect(0f, TopHeight, LeftWidth, Mathf.Max(0f, position.height - TopHeight));
            Rect inspectorRect = new Rect(position.width - LayersWidth - InspectorWidth, TopHeight, InspectorWidth, Mathf.Max(0f, position.height - TopHeight));
            Rect layersRect = new Rect(position.width - LayersWidth, TopHeight, LayersWidth, Mathf.Max(0f, position.height - TopHeight));
            Rect bottomRect = new Rect(LeftWidth, position.height - BottomHeight, Mathf.Max(0f, position.width - LeftWidth - InspectorWidth - LayersWidth), BottomHeight);
            Rect workspaceRect = new Rect(LeftWidth, TopHeight, bottomRect.width, Mathf.Max(0f, position.height - TopHeight - BottomHeight));

            DrawTopToolbar(topRect);
            DrawLeftToolbar(leftRect);
            DrawWorkspace(workspaceRect);
            DrawAssetPanel(bottomRect);
            DrawInspector(inspectorRect);
            DrawLayersPanel(layersRect);

            HandleGlobalShortcuts(Event.current);

            if (!hasInitialFit && workspaceRect.width > 100f && workspaceRect.height > 100f)
            {
                FitCanvas(workspaceRect);
                hasInitialFit = true;
                Repaint();
            }
        }

        private void DrawTopToolbar(Rect rect)
        {
            GUILayout.BeginArea(rect, EditorStyles.toolbar);
            GUILayout.BeginHorizontal();

            if (ToolbarButton("새로 만들기", 72f)) NewDocument(true);
            if (ToolbarButton("열기", 50f)) LoadDocument();
            if (ToolbarButton("저장", 50f)) SaveDocument(false);
            GUILayout.Space(8f);
            if (ToolbarButton("↶", 28f, "실행 취소")) Undo.PerformUndo();
            if (ToolbarButton("↷", 28f, "다시 실행")) Undo.PerformRedo();
            if (ToolbarButton("복제", 48f)) DuplicateSelected();
            GUILayout.Space(8f);

            if (ToolbarButton("−", 24f)) SetZoom(zoom / 1.15f);
            GUILayout.Label(Mathf.RoundToInt(zoom * 100f) + "%", EditorStyles.toolbarButton, GUILayout.Width(50f));
            if (ToolbarButton("+", 24f)) SetZoom(zoom * 1.15f);
            if (ToolbarButton("맞춤", 42f, "캔버스를 작업 영역에 맞춤 (F)")) hasInitialFit = false;
            GUILayout.Space(8f);

            document.data.showGrid = GUILayout.Toggle(document.data.showGrid, "격자", EditorStyles.toolbarButton, GUILayout.Width(48f));
            document.data.snapEnabled = GUILayout.Toggle(document.data.snapEnabled, "스냅", EditorStyles.toolbarButton, GUILayout.Width(52f));
            GUILayout.Space(8f);

            EditorGUI.BeginDisabledGroup(SelectedLayer == null);
            if (ToolbarButton("L", 24f, "왼쪽 맞춤")) AlignSelected(0);
            if (ToolbarButton("C", 24f, "가로 가운데")) AlignSelected(1);
            if (ToolbarButton("R", 24f, "오른쪽 맞춤")) AlignSelected(2);
            if (ToolbarButton("T", 24f, "위쪽 맞춤")) AlignSelected(3);
            if (ToolbarButton("M", 24f, "세로 가운데")) AlignSelected(4);
            if (ToolbarButton("B", 24f, "아래쪽 맞춤")) AlignSelected(5);
            if (ToolbarButton("↑", 24f, "맨앞으로")) BringSelectedToFront();
            if (ToolbarButton("↓", 24f, "맨뒤로")) SendSelectedToBack();
            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();
            GUILayout.Label("캔버스", GUILayout.Width(44f));
            string[] presets = { "1920×1080", "1080×1920", "1280×720", "Custom" };
            int newPreset = EditorGUILayout.Popup(canvasPresetIndex, presets, EditorStyles.toolbarPopup, GUILayout.Width(104f));
            if (newPreset != canvasPresetIndex)
            {
                canvasPresetIndex = newPreset;
                ApplyCanvasPreset(newPreset);
            }

            if (canvasPresetIndex == 3)
            {
                int width = EditorGUILayout.IntField(document.data.canvasWidth, EditorStyles.toolbarTextField, GUILayout.Width(52f));
                GUILayout.Label("×", GUILayout.Width(12f));
                int height = EditorGUILayout.IntField(document.data.canvasHeight, EditorStyles.toolbarTextField, GUILayout.Width(52f));
                if (width != document.data.canvasWidth || height != document.data.canvasHeight)
                    SetCanvasSize(width, height);
            }

            GUILayout.Space(6f);
            GUI.backgroundColor = new Color(0.18f, 0.45f, 0.95f, 1f);
            if (GUILayout.Button("PNG 내보내기", EditorStyles.toolbarButton, GUILayout.Width(104f), GUILayout.Height(24f))) ExportPng();
            GUI.backgroundColor = Color.white;

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private static bool ToolbarButton(string text, float width, string tooltip = null)
        {
            return GUILayout.Button(new GUIContent(text, tooltip ?? text), EditorStyles.toolbarButton, GUILayout.Width(width));
        }

        private void DrawLeftToolbar(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            GUILayout.BeginArea(new Rect(rect.x + 6f, rect.y + 8f, rect.width - 12f, rect.height - 16f));

            DrawToolToggle("선택", ToolMode.Select, "V");
            DrawToolToggle("손", ToolMode.Hand, "H");
            GUILayout.Space(8f);

            if (GUILayout.Button(new GUIContent("텍스트\nT", "텍스트 레이어 추가"), GUILayout.Height(48f))) AddTextLayer();
            if (GUILayout.Button(new GUIContent("도형\n▣", "도형 레이어 추가"), GUILayout.Height(48f))) ShowShapeAddMenu();
            if (GUILayout.Button(new GUIContent("이미지\n▧", "프로젝트 이미지 선택"), GUILayout.Height(48f))) OpenImagePicker();
            GUILayout.Space(8f);
            GUILayout.Label("이미지는\nProject나 아래\n에셋에서 드래그", EditorStyles.miniLabel);

            GUILayout.EndArea();
        }

        private void DrawToolToggle(string label, ToolMode mode, string hotkey)
        {
            bool selected = toolMode == mode;
            Color previous = GUI.backgroundColor;
            if (selected) GUI.backgroundColor = new Color(0.25f, 0.55f, 1f, 1f);
            if (GUILayout.Button(label + "\n" + hotkey, GUILayout.Height(48f))) toolMode = mode;
            GUI.backgroundColor = previous;
        }

        private void DrawWorkspace(Rect workspaceRect)
        {
            EditorGUI.DrawRect(workspaceRect, new Color(0.10f, 0.105f, 0.115f, 1f));

            Rect localViewport = new Rect(0f, 0f, workspaceRect.width, workspaceRect.height);
            Rect canvasRect = GetCanvasScreenRect(localViewport);

            Event e = Event.current;
            HandleProjectDragAndDrop(e, workspaceRect, canvasRect);
            HandleWorkspaceEvents(e, workspaceRect, canvasRect);

            GUI.BeginClip(workspaceRect);
            DrawCanvas(canvasRect);
            GUI.EndClip();
        }

        private Rect GetCanvasScreenRect(Rect localViewport)
        {
            Vector2 canvasSize = new Vector2(document.data.canvasWidth, document.data.canvasHeight) * zoom;
            Vector2 center = localViewport.center + pan;
            return new Rect(center - canvasSize * 0.5f, canvasSize);
        }

        private void DrawCanvas(Rect canvasRect)
        {
            GUI.DrawTextureWithTexCoords(canvasRect, checkerTexture, new Rect(0f, 0f, canvasRect.width / 16f, canvasRect.height / 16f));
            if (document.data.canvasBackground.a > 0f)
                EditorGUI.DrawRect(canvasRect, document.data.canvasBackground);

            GUI.BeginClip(canvasRect);
            Rect canvasLocal = new Rect(0f, 0f, canvasRect.width, canvasRect.height);

            if (document.data.showGrid) DrawGrid(canvasLocal);

            foreach (UIStudioLayer layer in document.data.layers)
            {
                if (layer != null && layer.visible) DrawLayer(layer);
            }

            if (document.data.smartGuidesEnabled && smartGuideX.HasValue)
                EditorGUI.DrawRect(new Rect(smartGuideX.Value * zoom, 0f, 1f, canvasLocal.height), new Color(0.1f, 0.85f, 1f, 0.95f));
            if (document.data.smartGuidesEnabled && smartGuideY.HasValue)
                EditorGUI.DrawRect(new Rect(0f, smartGuideY.Value * zoom, canvasLocal.width, 1f), new Color(0.1f, 0.85f, 1f, 0.95f));

            DrawSelection();
            GUI.EndClip();

            DrawCanvasBorder(canvasRect);
        }

        private void DrawGrid(Rect canvasLocal)
        {
            float step = document.data.gridSize * zoom;
            if (step < 8f) return;
            Color color = new Color(1f, 1f, 1f, 0.08f);
            for (float x = step; x < canvasLocal.width; x += step)
                EditorGUI.DrawRect(new Rect(Mathf.Round(x), 0f, 1f, canvasLocal.height), color);
            for (float y = step; y < canvasLocal.height; y += step)
                EditorGUI.DrawRect(new Rect(0f, Mathf.Round(y), canvasLocal.width, 1f), color);
        }

        private void DrawLayer(UIStudioLayer layer)
        {
            Rect rect = new Rect(layer.rect.x * zoom, layer.rect.y * zoom, layer.rect.width * zoom, layer.rect.height * zoom);
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUIUtility.RotateAroundPivot(layer.rotation, rect.center);

            if (layer.type == UIStudioLayerType.Shape)
            {
                DrawShapePreview(rect, layer);
            }
            else if (layer.type == UIStudioLayerType.Text)
            {
                DrawTextPreview(rect, layer);
            }
            else
            {
                DrawImagePreview(rect, layer);
            }

            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }

        private void DrawShapePreview(Rect rect, UIStudioLayer layer)
        {
            if (layer.shadowEnabled)
            {
                Rect shadow = rect;
                shadow.position += layer.shadowOffset * zoom;
                shadow = shadow.Expand(layer.shadowBlur * zoom * 0.35f);
                EditorGUI.DrawRect(shadow, WithOpacity(layer.shadowColor, layer.opacity * 0.45f));
            }

            if (document.data.lightweightPreview && (zoom < 0.16f || rect.width * rect.height > 480000f))
            {
                EditorGUI.DrawRect(rect, WithOpacity(layer.fillColor, layer.opacity));
                if (layer.strokeWidth > 0f)
                    DrawSimpleOutline(rect, WithOpacity(layer.strokeColor, layer.opacity));
                return;
            }

            Texture2D preview = GetShapePreviewTexture(layer);
            if (preview != null) GUI.DrawTexture(rect, preview, ScaleMode.StretchToFill, true);
            else EditorGUI.DrawRect(rect, WithOpacity(layer.fillColor, layer.opacity));
        }

        private void DrawTextPreview(Rect rect, UIStudioLayer layer)
        {
            GUIStyle style = new GUIStyle(EditorStyles.label)
            {
                alignment = ToGuiAlignment(layer.textAlignment),
                fontSize = Mathf.Max(1, Mathf.RoundToInt(layer.fontSize * zoom)),
                fontStyle = layer.fontStyle,
                wordWrap = true
            };
            Font font = LoadFont(layer.fontPath);
            if (font != null) style.font = font;
            style.normal.textColor = WithOpacity(layer.textColor, layer.opacity);
            GUI.Label(rect, layer.text ?? string.Empty, style);
        }

        private void DrawImagePreview(Rect rect, UIStudioLayer layer)
        {
            Object asset = LoadImageAssetCached(layer);
            if (asset == null)
            {
                EditorGUI.DrawRect(rect, new Color(0.35f, 0.08f, 0.08f, 0.6f));
                GUI.Label(rect, "이미지 없음", CenteredMiniStyle());
                return;
            }

            Color previous = GUI.color;
            GUI.color = WithOpacity(layer.imageTint, layer.opacity);

            Sprite sprite = asset as Sprite;
            if (sprite != null)
            {
                Texture2D texture = sprite.texture;
                Rect source = sprite.rect;
                Rect uv = new Rect(source.x / texture.width, source.y / texture.height, source.width / texture.width, source.height / texture.height);
                Rect target = layer.preserveAspect ? FitAspect(rect, source.width / source.height) : rect;
                GUI.DrawTextureWithTexCoords(target, texture, uv, true);
            }
            else if (asset is Texture texture)
            {
                Rect target = layer.preserveAspect && texture.height > 0 ? FitAspect(rect, (float)texture.width / texture.height) : rect;
                GUI.DrawTexture(target, texture, ScaleMode.StretchToFill, true);
            }

            GUI.color = previous;
        }

        private void DrawSelection()
        {
            UIStudioLayer layer = SelectedLayer;
            if (layer == null || !layer.visible) return;

            Rect rect = new Rect(layer.rect.x * zoom, layer.rect.y * zoom, layer.rect.width * zoom, layer.rect.height * zoom);
            DrawBorder(rect, 1f, new Color(0.2f, 0.65f, 1f, 1f));

            foreach (ResizeHandle handle in Enum.GetValues(typeof(ResizeHandle)))
            {
                if (handle == ResizeHandle.None) continue;
                Rect handleRect = GetResizeHandleRect(rect, handle);
                EditorGUI.DrawRect(handleRect, Color.white);
                DrawBorder(handleRect, 1f, new Color(0.1f, 0.45f, 0.9f, 1f));
            }

            Vector2 rotationPoint = new Vector2(rect.center.x, rect.y - 24f);
            EditorGUI.DrawRect(new Rect(rect.center.x - 0.5f, rect.y - 20f, 1f, 20f), new Color(0.2f, 0.65f, 1f, 1f));
            EditorGUI.DrawRect(new Rect(rotationPoint.x - 5f, rotationPoint.y - 5f, 10f, 10f), Color.white);
        }

        private static void DrawCanvasBorder(Rect rect)
        {
            DrawBorder(rect, 1f, new Color(0.35f, 0.36f, 0.4f, 1f));
        }

        private static void DrawBorder(Rect rect, float thickness, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private void HandleWorkspaceEvents(Event e, Rect workspaceRect, Rect canvasRect)
        {
            Vector2 localMouse = e.mousePosition - workspaceRect.position;
            bool insideWorkspace = new Rect(Vector2.zero, workspaceRect.size).Contains(localMouse);

            if (e.type == EventType.ScrollWheel && insideWorkspace)
            {
                float previousZoom = zoom;
                Vector2 canvasBefore = UIStudioMath.ScreenToCanvasPoint(canvasRect, localMouse, previousZoom);
                SetZoom(zoom * (e.delta.y > 0f ? 0.9f : 1.1f));
                Rect newCanvasRect = GetCanvasScreenRect(new Rect(Vector2.zero, workspaceRect.size));
                Vector2 screenAfter = newCanvasRect.position + canvasBefore * zoom;
                pan += localMouse - screenAfter;
                e.Use();
                return;
            }

            if ((e.type == EventType.MouseDown && e.button == 2 && insideWorkspace) ||
                (e.type == EventType.MouseDown && e.button == 0 && insideWorkspace && toolMode == ToolMode.Hand))
            {
                dragMode = DragMode.Pan;
                dragMouseStart = localMouse;
                panStart = pan;
                e.Use();
                return;
            }

            if (dragMode == DragMode.Pan && e.type == EventType.MouseDrag)
            {
                pan = panStart + (localMouse - dragMouseStart);
                Repaint();
                e.Use();
                return;
            }

            if (dragMode == DragMode.Pan && e.type == EventType.MouseUp)
            {
                dragMode = DragMode.None;
                e.Use();
                return;
            }

            if (toolMode != ToolMode.Select) return;

            UIStudioLayer selected = SelectedLayer;
            Rect selectedScreen = selected != null ? UIStudioMath.CanvasToScreenRect(canvasRect, selected.rect, zoom) : default(Rect);

            if (e.type == EventType.MouseDown && e.button == 0 && insideWorkspace)
            {
                smartGuideX = null;
                smartGuideY = null;

                if (selected != null && !selected.locked)
                {
                    Vector2 rotationPoint = new Vector2(selectedScreen.center.x, selectedScreen.y - 24f);
                    if (Vector2.Distance(localMouse, rotationPoint) <= 9f)
                    {
                        BeginDragUndo("Rotate Layer");
                        dragMode = DragMode.Rotate;
                        dragMouseStart = localMouse;
                        dragOriginalRotation = selected.rotation;
                        e.Use();
                        return;
                    }

                    ResizeHandle hitHandle = HitResizeHandle(selectedScreen, localMouse);
                    if (hitHandle != ResizeHandle.None)
                    {
                        BeginDragUndo("Resize Layer");
                        dragMode = DragMode.Resize;
                        resizeHandle = hitHandle;
                        dragMouseStart = localMouse;
                        dragOriginalRect = selected.rect;
                        e.Use();
                        return;
                    }
                }

                UIStudioLayer hit = HitTestLayer(canvasRect, localMouse);
                if (hit != null)
                {
                    selectedLayerId = hit.id;
                    if (!hit.locked)
                    {
                        BeginDragUndo("Move Layer");
                        dragMode = DragMode.Move;
                        dragMouseStart = localMouse;
                        dragOriginalRect = hit.rect;
                    }
                    e.Use();
                    Repaint();
                    return;
                }

                if (canvasRect.Contains(localMouse))
                {
                    selectedLayerId = null;
                    Repaint();
                    e.Use();
                }
            }

            if (e.type == EventType.MouseDrag && selected != null)
            {
                if (dragMode == DragMode.Move)
                {
                    Undo.RecordObject(document, "Move Layer");
                    Vector2 delta = (localMouse - dragMouseStart) / zoom;
                    Rect candidate = dragOriginalRect;
                    candidate.position += delta;
                    candidate = ApplyMoveSnapping(selected, candidate);
                    selected.rect = UIStudioMath.ClampToCanvas(candidate, CanvasSize);
                    dirty = true;
                    Repaint();
                    e.Use();
                    return;
                }

                if (dragMode == DragMode.Resize)
                {
                    Undo.RecordObject(document, "Resize Layer");
                    Vector2 delta = (localMouse - dragMouseStart) / zoom;
                    selected.rect = ResizeRect(dragOriginalRect, delta, resizeHandle, e.shift);
                    selected.rect = UIStudioMath.ClampToCanvas(selected.rect, CanvasSize);
                    dirty = true;
                    Repaint();
                    e.Use();
                    return;
                }

                if (dragMode == DragMode.Rotate)
                {
                    Undo.RecordObject(document, "Rotate Layer");
                    Vector2 center = selectedScreen.center;
                    float startAngle = Mathf.Atan2(dragMouseStart.y - center.y, dragMouseStart.x - center.x) * Mathf.Rad2Deg;
                    float currentAngle = Mathf.Atan2(localMouse.y - center.y, localMouse.x - center.x) * Mathf.Rad2Deg;
                    float rotation = dragOriginalRotation + (currentAngle - startAngle);
                    if (e.shift) rotation = Mathf.Round(rotation / 15f) * 15f;
                    selected.rotation = NormalizeAngle(rotation);
                    dirty = true;
                    Repaint();
                    e.Use();
                    return;
                }
            }

            if (e.type == EventType.MouseUp && dragMode != DragMode.None)
            {
                EndDragUndo();
                dragMode = DragMode.None;
                resizeHandle = ResizeHandle.None;
                smartGuideX = null;
                smartGuideY = null;
                e.Use();
                Repaint();
            }
        }

        private UIStudioLayer HitTestLayer(Rect canvasRect, Vector2 localMouse)
        {
            for (int i = document.data.layers.Count - 1; i >= 0; i--)
            {
                UIStudioLayer layer = document.data.layers[i];
                if (layer == null || !layer.visible) continue;
                Rect screen = UIStudioMath.CanvasToScreenRect(canvasRect, layer.rect, zoom);
                if (screen.Contains(localMouse)) return layer;
            }
            return null;
        }

        private Rect ApplyMoveSnapping(UIStudioLayer moving, Rect candidate)
        {
            smartGuideX = null;
            smartGuideY = null;
            if (document.data.snapEnabled)
                candidate = UIStudioMath.SnapPosition(candidate, document.data.gridSize);
            if (!document.data.smartGuidesEnabled) return candidate;

            float threshold = 6f / Mathf.Max(zoom, 0.01f);

            List<float> xTargets = new List<float> { 0f, document.data.canvasWidth * 0.5f, document.data.canvasWidth };
            List<float> yTargets = new List<float> { 0f, document.data.canvasHeight * 0.5f, document.data.canvasHeight };

            foreach (UIStudioLayer layer in document.data.layers)
            {
                if (layer == null || layer == moving || !layer.visible) continue;
                xTargets.Add(layer.rect.xMin);
                xTargets.Add(layer.rect.center.x);
                xTargets.Add(layer.rect.xMax);
                yTargets.Add(layer.rect.yMin);
                yTargets.Add(layer.rect.center.y);
                yTargets.Add(layer.rect.yMax);
            }

            float[] movingX = { candidate.xMin, candidate.center.x, candidate.xMax };
            float[] movingY = { candidate.yMin, candidate.center.y, candidate.yMax };
            float bestX = threshold + 1f;
            float bestY = threshold + 1f;
            float xOffset = 0f;
            float yOffset = 0f;

            foreach (float source in movingX)
            foreach (float target in xTargets)
            {
                float distance = Mathf.Abs(source - target);
                if (distance < bestX && distance <= threshold)
                {
                    bestX = distance;
                    xOffset = target - source;
                    smartGuideX = target;
                }
            }

            foreach (float source in movingY)
            foreach (float target in yTargets)
            {
                float distance = Mathf.Abs(source - target);
                if (distance < bestY && distance <= threshold)
                {
                    bestY = distance;
                    yOffset = target - source;
                    smartGuideY = target;
                }
            }

            candidate.position += new Vector2(xOffset, yOffset);
            return candidate;
        }

        private Rect ResizeRect(Rect original, Vector2 delta, ResizeHandle handle, bool keepAspect)
        {
            Rect result = original;
            float minSize = 8f;
            bool left = handle == ResizeHandle.Left || handle == ResizeHandle.TopLeft || handle == ResizeHandle.BottomLeft;
            bool right = handle == ResizeHandle.Right || handle == ResizeHandle.TopRight || handle == ResizeHandle.BottomRight;
            bool top = handle == ResizeHandle.Top || handle == ResizeHandle.TopLeft || handle == ResizeHandle.TopRight;
            bool bottom = handle == ResizeHandle.Bottom || handle == ResizeHandle.BottomLeft || handle == ResizeHandle.BottomRight;

            if (left) { result.xMin = Mathf.Min(original.xMax - minSize, original.xMin + delta.x); }
            if (right) { result.xMax = Mathf.Max(original.xMin + minSize, original.xMax + delta.x); }
            if (top) { result.yMin = Mathf.Min(original.yMax - minSize, original.yMin + delta.y); }
            if (bottom) { result.yMax = Mathf.Max(original.yMin + minSize, original.yMax + delta.y); }

            if (keepAspect && original.height > 0f)
            {
                float aspect = original.width / original.height;
                if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                {
                    float targetHeight = result.width / aspect;
                    if (top && !bottom) result.yMin = result.yMax - targetHeight;
                    else result.height = targetHeight;
                }
                else
                {
                    float targetWidth = result.height * aspect;
                    if (left && !right) result.xMin = result.xMax - targetWidth;
                    else result.width = targetWidth;
                }
            }

            if (document.data.snapEnabled)
            {
                result.xMin = UIStudioMath.Snap(result.xMin, document.data.gridSize);
                result.yMin = UIStudioMath.Snap(result.yMin, document.data.gridSize);
                result.xMax = UIStudioMath.Snap(result.xMax, document.data.gridSize);
                result.yMax = UIStudioMath.Snap(result.yMax, document.data.gridSize);
                if (result.width < minSize) result.width = minSize;
                if (result.height < minSize) result.height = minSize;
            }

            return result;
        }

        private static ResizeHandle HitResizeHandle(Rect rect, Vector2 point)
        {
            foreach (ResizeHandle handle in Enum.GetValues(typeof(ResizeHandle)))
            {
                if (handle == ResizeHandle.None) continue;
                if (GetResizeHandleRect(rect, handle).Contains(point)) return handle;
            }
            return ResizeHandle.None;
        }

        private static Rect GetResizeHandleRect(Rect rect, ResizeHandle handle)
        {
            Vector2 point;
            switch (handle)
            {
                case ResizeHandle.TopLeft: point = new Vector2(rect.xMin, rect.yMin); break;
                case ResizeHandle.Top: point = new Vector2(rect.center.x, rect.yMin); break;
                case ResizeHandle.TopRight: point = new Vector2(rect.xMax, rect.yMin); break;
                case ResizeHandle.Right: point = new Vector2(rect.xMax, rect.center.y); break;
                case ResizeHandle.BottomRight: point = new Vector2(rect.xMax, rect.yMax); break;
                case ResizeHandle.Bottom: point = new Vector2(rect.center.x, rect.yMax); break;
                case ResizeHandle.BottomLeft: point = new Vector2(rect.xMin, rect.yMax); break;
                case ResizeHandle.Left: point = new Vector2(rect.xMin, rect.center.y); break;
                default: return Rect.zero;
            }
            return new Rect(point.x - 5f, point.y - 5f, 10f, 10f);
        }

        private void HandleProjectDragAndDrop(Event e, Rect workspaceRect, Rect canvasRect)
        {
            if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform) return;
            Vector2 localMouse = e.mousePosition - workspaceRect.position;
            if (!canvasRect.Contains(localMouse)) return;

            Object valid = DragAndDrop.objectReferences.FirstOrDefault(IsSupportedImageAsset);
            if (valid == null) return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                Vector2 canvasPoint = UIStudioMath.ScreenToCanvasPoint(canvasRect, localMouse, zoom);
                AddImageLayer(valid, canvasPoint);
            }
            e.Use();
        }

        private void DrawInspector(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            GUILayout.BeginArea(new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, rect.height - 16f));
            GUILayout.Label("인스펙터", EditorStyles.boldLabel);
            inspectorScroll = GUILayout.BeginScrollView(inspectorScroll);

            UIStudioLayer layer = SelectedLayer;
            if (layer == null) DrawCanvasInspector();
            else DrawLayerInspector(layer);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawCanvasInspector()
        {
            GUILayout.Label("Canvas", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            int width = EditorGUILayout.IntField("Width", document.data.canvasWidth);
            int height = EditorGUILayout.IntField("Height", document.data.canvasHeight);
            Color background = EditorGUILayout.ColorField("Background", document.data.canvasBackground);
            float gridSize = EditorGUILayout.FloatField("Grid Size", document.data.gridSize);
            bool showGrid = EditorGUILayout.Toggle("Show Grid", document.data.showGrid);
            bool snap = EditorGUILayout.Toggle("Snap", document.data.snapEnabled);
            bool smartGuides = EditorGUILayout.Toggle("Smart Guides", document.data.smartGuidesEnabled);
            bool lightweightPreview = EditorGUILayout.Toggle("빠른 미리보기", document.data.lightweightPreview);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(document, "Edit Canvas");
                document.data.canvasWidth = Mathf.Max(1, width);
                document.data.canvasHeight = Mathf.Max(1, height);
                document.data.canvasBackground = background;
                document.data.gridSize = Mathf.Max(1f, gridSize);
                document.data.showGrid = showGrid;
                document.data.snapEnabled = snap;
                document.data.smartGuidesEnabled = smartGuides;
                document.data.lightweightPreview = lightweightPreview;
                canvasPresetIndex = DetectPreset();
                dirty = true;
            }

            GUILayout.Space(10f);
            EditorGUILayout.HelpBox("캔버스에서 Project의 Sprite/Texture를 바로 드래그할 수 있습니다. F 키는 화면 맞춤, 빠른 미리보기는 큰 캔버스에서 렉을 줄여줍니다.", MessageType.Info);
        }

        private void DrawLayerInspector(UIStudioLayer layer)
        {
            EditorGUI.BeginChangeCheck();
            string name = EditorGUILayout.TextField("이름", layer.name);
            bool visible = EditorGUILayout.Toggle("보이기", layer.visible);
            bool locked = EditorGUILayout.Toggle("잠금", layer.locked);
            float x = EditorGUILayout.FloatField("X", layer.rect.x);
            float y = EditorGUILayout.FloatField("Y", layer.rect.y);
            float width = EditorGUILayout.FloatField("W", layer.rect.width);
            float height = EditorGUILayout.FloatField("H", layer.rect.height);
            float rotation = EditorGUILayout.FloatField("회전", layer.rotation);
            float opacity = EditorGUILayout.Slider("불투명도", layer.opacity, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(document, "Edit Layer Transform");
                layer.name = name;
                layer.visible = visible;
                layer.locked = locked;
                layer.rect = UIStudioMath.ClampToCanvas(new Rect(x, y, Mathf.Max(8f, width), Mathf.Max(8f, height)), CanvasSize);
                layer.rotation = NormalizeAngle(rotation);
                layer.opacity = opacity;
                dirty = true;
            }

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("복제")) DuplicateSelected();
            if (GUILayout.Button("맨앞")) BringSelectedToFront();
            if (GUILayout.Button("맨뒤")) SendSelectedToBack();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("가운데 X")) AlignSelected(1);
            if (GUILayout.Button("가운데 Y")) AlignSelected(4);
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);

            if (layer.type == UIStudioLayerType.Shape) DrawShapeInspector(layer);
            else if (layer.type == UIStudioLayerType.Text) DrawTextInspector(layer);
            else DrawImageInspector(layer);
        }

        private void DrawShapeInspector(UIStudioLayer layer)
        {
            GUILayout.Label("Shape", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            UIStudioShapeType shapeType = (UIStudioShapeType)EditorGUILayout.EnumPopup("종류", layer.shapeType);
            Color fill = EditorGUILayout.ColorField("채우기", layer.fillColor);
            Color stroke = EditorGUILayout.ColorField("외곽선", layer.strokeColor);
            float strokeWidth = EditorGUILayout.FloatField("외곽선 두께", layer.strokeWidth);
            float radius = layer.cornerRadius;
            if (shapeType == UIStudioShapeType.RoundedRectangle)
                radius = EditorGUILayout.Slider("모서리 반경", layer.cornerRadius, 0f, Mathf.Min(layer.rect.width, layer.rect.height) * 0.5f);
            bool shadow = EditorGUILayout.Toggle("그림자", layer.shadowEnabled);
            Color shadowColor = layer.shadowColor;
            Vector2 shadowOffset = layer.shadowOffset;
            float shadowBlur = layer.shadowBlur;
            if (shadow)
            {
                shadowColor = EditorGUILayout.ColorField("그림자 색", layer.shadowColor);
                shadowOffset = EditorGUILayout.Vector2Field("오프셋", layer.shadowOffset);
                shadowBlur = EditorGUILayout.Slider("블러", layer.shadowBlur, 0f, 64f);
            }
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(document, "Edit Shape");
                layer.shapeType = shapeType;
                layer.fillColor = fill;
                layer.strokeColor = stroke;
                layer.strokeWidth = Mathf.Max(0f, strokeWidth);
                layer.cornerRadius = Mathf.Max(0f, radius);
                layer.shadowEnabled = shadow;
                layer.shadowColor = shadowColor;
                layer.shadowOffset = shadowOffset;
                layer.shadowBlur = shadowBlur;
                dirty = true;
            }
        }

        private void DrawTextInspector(UIStudioLayer layer)
        {
            GUILayout.Label("Text", EditorStyles.boldLabel);
            Font currentFont = LoadFont(layer.fontPath);
            EditorGUI.BeginChangeCheck();
            string text = EditorGUILayout.TextArea(layer.text ?? string.Empty, GUILayout.MinHeight(56f));
            Font font = (Font)EditorGUILayout.ObjectField("폰트", currentFont, typeof(Font), false);
            int fontSize = EditorGUILayout.IntField("크기", layer.fontSize);
            FontStyle fontStyle = (FontStyle)EditorGUILayout.EnumPopup("스타일", layer.fontStyle);
            UIStudioTextAlignment alignment = (UIStudioTextAlignment)EditorGUILayout.EnumPopup("정렬", layer.textAlignment);
            Color color = EditorGUILayout.ColorField("색상", layer.textColor);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(document, "Edit Text");
                layer.text = text;
                layer.fontPath = font != null ? AssetDatabase.GetAssetPath(font) : string.Empty;
                layer.fontSize = Mathf.Max(1, fontSize);
                layer.fontStyle = fontStyle;
                layer.textAlignment = alignment;
                layer.textColor = color;
                dirty = true;
            }
        }

        private void DrawImageInspector(UIStudioLayer layer)
        {
            GUILayout.Label("Image", EditorStyles.boldLabel);
            Object current = LoadImageAssetCached(layer);
            EditorGUI.BeginChangeCheck();
            Object image = EditorGUILayout.ObjectField("이미지", current, typeof(Object), false);
            bool preserve = EditorGUILayout.Toggle("비율 유지", layer.preserveAspect);
            Color tint = EditorGUILayout.ColorField("Tint", layer.imageTint);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(document, "Edit Image");
                if (image == null || IsSupportedImageAsset(image)) SetImageAsset(layer, image);
                layer.preserveAspect = preserve;
                layer.imageTint = tint;
                assetCache.Clear();
                dirty = true;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("원본 크기")) ResetImageToNativeSize(layer);
            if (GUILayout.Button("캔버스 중앙")) CenterSelectedLayer();
            GUILayout.EndHorizontal();
        }

        private void DrawLayersPanel(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            GUILayout.BeginArea(new Rect(rect.x + 6f, rect.y + 8f, rect.width - 12f, rect.height - 16f));
            GUILayout.BeginHorizontal();
            GUILayout.Label("레이어", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+", GUILayout.Width(24f))) ShowAddLayerMenu();
            if (GUILayout.Button("⧉", GUILayout.Width(24f))) DuplicateSelected();
            if (GUILayout.Button("×", GUILayout.Width(24f))) DeleteSelected();
            GUILayout.EndHorizontal();

            layersScroll = GUILayout.BeginScrollView(layersScroll);
            for (int i = document.data.layers.Count - 1; i >= 0; i--)
            {
                UIStudioLayer layer = document.data.layers[i];
                if (layer == null) continue;
                DrawLayerRow(layer, i);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawLayerRow(UIStudioLayer layer, int index)
        {
            Rect row = GUILayoutUtility.GetRect(10f, 26f, GUILayout.ExpandWidth(true));
            bool selected = layer.id == selectedLayerId;
            if (selected) EditorGUI.DrawRect(row, new Color(0.16f, 0.38f, 0.72f, 0.7f));

            Rect eyeRect = new Rect(row.x + 2f, row.y + 3f, 22f, 20f);
            Rect lockRect = new Rect(row.x + 26f, row.y + 3f, 22f, 20f);
            Rect nameRect = new Rect(row.x + 52f, row.y + 2f, Mathf.Max(40f, row.width - 104f), 22f);
            Rect upRect = new Rect(row.xMax - 48f, row.y + 3f, 22f, 20f);
            Rect downRect = new Rect(row.xMax - 24f, row.y + 3f, 22f, 20f);

            if (GUI.Button(eyeRect, layer.visible ? "●" : "○", EditorStyles.miniButton))
            {
                Undo.RecordObject(document, "Toggle Layer Visibility");
                layer.visible = !layer.visible;
                dirty = true;
            }
            if (GUI.Button(lockRect, layer.locked ? "L" : "-", EditorStyles.miniButton))
            {
                Undo.RecordObject(document, "Toggle Layer Lock");
                layer.locked = !layer.locked;
                dirty = true;
            }
            if (GUI.Button(nameRect, LayerIcon(layer.type) + "  " + layer.name, selected ? EditorStyles.miniButtonMid : EditorStyles.label))
                selectedLayerId = layer.id;
            if (GUI.Button(upRect, "↑", EditorStyles.miniButtonLeft)) MoveLayer(index, +1);
            if (GUI.Button(downRect, "↓", EditorStyles.miniButtonRight)) MoveLayer(index, -1);
        }

        private void DrawAssetPanel(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            GUILayout.BeginArea(new Rect(rect.x + 8f, rect.y + 6f, rect.width - 16f, rect.height - 12f));
            GUILayout.BeginHorizontal();
            GUILayout.Label("에셋", EditorStyles.boldLabel, GUILayout.Width(40f));
            string newSearch = GUILayout.TextField(assetSearch, EditorStyles.toolbarSearchField, GUILayout.MinWidth(120f));
            if (newSearch != assetSearch) { assetSearch = newSearch; Repaint(); }
            if (GUILayout.Button("새로고침", GUILayout.Width(72f))) RefreshAssets();
            GUILayout.Label("더블클릭 = 추가 / Project에서 캔버스로 드래그 가능", EditorStyles.miniLabel);
            GUILayout.EndHorizontal();

            assetsScroll = GUILayout.BeginScrollView(assetsScroll);
            float cellWidth = 84f;
            int columns = Mathf.Max(1, Mathf.FloorToInt((rect.width - 32f) / cellWidth));
            List<Object> filtered = assetEntries
                .Where(asset => asset != null && (string.IsNullOrEmpty(assetSearch) || asset.name.IndexOf(assetSearch, StringComparison.OrdinalIgnoreCase) >= 0))
                .Take(document.data.lightweightPreview ? 120 : 240)
                .ToList();

            int rows = Mathf.CeilToInt(filtered.Count / (float)columns);
            for (int row = 0; row < rows; row++)
            {
                GUILayout.BeginHorizontal();
                for (int col = 0; col < columns; col++)
                {
                    int index = row * columns + col;
                    if (index >= filtered.Count) break;
                    DrawAssetCell(filtered[index], cellWidth);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawAssetCell(Object asset, float width)
        {
            GUILayout.BeginVertical(GUILayout.Width(width - 4f));
            Rect previewRect = GUILayoutUtility.GetRect(width - 10f, 62f, GUILayout.Width(width - 10f), GUILayout.Height(62f));
            Texture preview = AssetPreview.GetAssetPreview(asset) ?? AssetPreview.GetMiniThumbnail(asset);
            if (preview != null) GUI.DrawTexture(previewRect, preview, ScaleMode.ScaleToFit, true);
            else EditorGUI.DrawRect(previewRect, new Color(0.16f, 0.16f, 0.18f, 1f));

            GUI.Label(new Rect(previewRect.x, previewRect.yMax - 18f, previewRect.width, 18f), asset.name, CenteredMiniStyle());
            if (Event.current.type == EventType.MouseDown && previewRect.Contains(Event.current.mousePosition) && Event.current.clickCount == 2)
            {
                AddImageLayer(asset, CanvasSize * 0.5f);
                Event.current.Use();
            }
            GUILayout.EndVertical();
        }

        private void HandleGlobalShortcuts(Event e)
        {
            if (e.type != EventType.KeyDown || EditorGUIUtility.editingTextField) return;
            bool action = e.control || e.command;

            if (action && e.keyCode == KeyCode.S)
            {
                SaveDocument(false);
                e.Use();
                return;
            }
            if (action && e.keyCode == KeyCode.D)
            {
                DuplicateSelected();
                e.Use();
                return;
            }
            if (action && e.keyCode == KeyCode.C)
            {
                CopySelected();
                e.Use();
                return;
            }
            if (action && e.keyCode == KeyCode.V)
            {
                PasteLayer();
                e.Use();
                return;
            }
            if (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace)
            {
                DeleteSelected();
                e.Use();
                return;
            }
            if (e.keyCode == KeyCode.V) { toolMode = ToolMode.Select; e.Use(); return; }
            if (e.keyCode == KeyCode.H) { toolMode = ToolMode.Hand; e.Use(); return; }
            if (e.keyCode == KeyCode.T) { AddTextLayer(); e.Use(); return; }
            if (e.keyCode == KeyCode.F) { hasInitialFit = false; e.Use(); return; }

            UIStudioLayer layer = SelectedLayer;
            if (layer == null || layer.locked) return;
            Vector2 nudge = Vector2.zero;
            float amount = e.shift ? 10f : 1f;
            if (e.keyCode == KeyCode.LeftArrow) nudge.x = -amount;
            if (e.keyCode == KeyCode.RightArrow) nudge.x = amount;
            if (e.keyCode == KeyCode.UpArrow) nudge.y = -amount;
            if (e.keyCode == KeyCode.DownArrow) nudge.y = amount;
            if (nudge != Vector2.zero)
            {
                Undo.RecordObject(document, "Nudge Layer");
                Rect rect = layer.rect;
                rect.position += nudge;
                layer.rect = UIStudioMath.ClampToCanvas(rect, CanvasSize);
                dirty = true;
                e.Use();
                Repaint();
            }
        }

        private void AddShapeLayer()
        {
            AddShapeLayer(UIStudioShapeType.RoundedRectangle);
        }

        private void AddShapeLayer(UIStudioShapeType shapeType)
        {
            Rect rect = shapeType == UIStudioShapeType.Ellipse ? CenteredRect(180f, 180f) : CenteredRect(360f, 160f);
            if (shapeType == UIStudioShapeType.Diamond || shapeType == UIStudioShapeType.Triangle) rect = CenteredRect(220f, 220f);
            UIStudioLayer layer = new UIStudioLayer
            {
                name = NextName(shapeType.ToString()),
                type = UIStudioLayerType.Shape,
                shapeType = shapeType,
                rect = rect,
                fillColor = new Color(0.18f, 0.42f, 0.88f, 1f),
                cornerRadius = 20f
            };
            AddLayer(layer, "Add Shape");
        }

        private void AddTextLayer()
        {
            UIStudioLayer layer = new UIStudioLayer
            {
                name = NextName("Text"),
                type = UIStudioLayerType.Text,
                rect = CenteredRect(520f, 100f),
                text = "텍스트",
                fontSize = 54,
                textAlignment = UIStudioTextAlignment.Center,
                textColor = Color.white
            };
            AddLayer(layer, "Add Text");
        }

        private void AddImageLayer(Object asset, Vector2? canvasPosition = null)
        {
            if (!IsSupportedImageAsset(asset)) return;
            Vector2 nativeSize = GetAssetNativeSize(asset);
            float maxWidth = document.data.canvasWidth * 0.5f;
            float maxHeight = document.data.canvasHeight * 0.5f;
            float scale = Mathf.Min(1f, Mathf.Min(maxWidth / Mathf.Max(1f, nativeSize.x), maxHeight / Mathf.Max(1f, nativeSize.y)));
            Vector2 size = nativeSize * scale;
            size.x = Mathf.Max(32f, size.x);
            size.y = Mathf.Max(32f, size.y);

            Vector2 center = canvasPosition ?? CanvasSize * 0.5f;
            UIStudioLayer layer = new UIStudioLayer
            {
                name = NextName(asset.name),
                type = UIStudioLayerType.Image,
                rect = new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y),
                preserveAspect = true
            };
            SetImageAsset(layer, asset);
            layer.rect = UIStudioMath.ClampToCanvas(layer.rect, CanvasSize);
            AddLayer(layer, "Add Image");
        }

        private void AddLayer(UIStudioLayer layer, string undoName)
        {
            Undo.RecordObject(document, undoName);
            document.data.layers.Add(layer);
            selectedLayerId = layer.id;
            toolMode = ToolMode.Select;
            dirty = true;
            Repaint();
        }

        private void DeleteSelected()
        {
            UIStudioLayer layer = SelectedLayer;
            if (layer == null) return;
            Undo.RecordObject(document, "Delete Layer");
            document.data.layers.Remove(layer);
            selectedLayerId = null;
            dirty = true;
            Repaint();
        }

        private void DuplicateSelected()
        {
            UIStudioLayer layer = SelectedLayer;
            if (layer == null) return;
            AddLayer(layer.Clone(), "Duplicate Layer");
        }

        private void CopySelected()
        {
            UIStudioLayer layer = SelectedLayer;
            if (layer == null) return;
            layerClipboard = JsonUtility.ToJson(layer);
        }

        private void PasteLayer()
        {
            if (string.IsNullOrEmpty(layerClipboard)) return;
            UIStudioLayer layer = JsonUtility.FromJson<UIStudioLayer>(layerClipboard);
            layer.id = Guid.NewGuid().ToString("N");
            layer.name = NextName(layer.name);
            layer.rect.position += new Vector2(16f, 16f);
            AddLayer(layer, "Paste Layer");
        }

        private void MoveLayer(int index, int direction)
        {
            int target = Mathf.Clamp(index + direction, 0, document.data.layers.Count - 1);
            if (target == index) return;
            Undo.RecordObject(document, "Reorder Layer");
            UIStudioLayer layer = document.data.layers[index];
            document.data.layers.RemoveAt(index);
            document.data.layers.Insert(target, layer);
            dirty = true;
            Repaint();
        }

        private void BringSelectedToFront()
        {
            UIStudioLayer layer = SelectedLayer;
            if (layer == null) return;
            int index = document.data.layers.IndexOf(layer);
            if (index < 0 || index == document.data.layers.Count - 1) return;
            Undo.RecordObject(document, "Bring Layer To Front");
            document.data.layers.RemoveAt(index);
            document.data.layers.Add(layer);
            dirty = true;
            Repaint();
        }

        private void SendSelectedToBack()
        {
            UIStudioLayer layer = SelectedLayer;
            if (layer == null) return;
            int index = document.data.layers.IndexOf(layer);
            if (index <= 0) return;
            Undo.RecordObject(document, "Send Layer To Back");
            document.data.layers.RemoveAt(index);
            document.data.layers.Insert(0, layer);
            dirty = true;
            Repaint();
        }

        private void CenterSelectedLayer()
        {
            UIStudioLayer layer = SelectedLayer;
            if (layer == null) return;
            Undo.RecordObject(document, "Center Layer");
            Rect r = layer.rect;
            r.x = (document.data.canvasWidth - r.width) * 0.5f;
            r.y = (document.data.canvasHeight - r.height) * 0.5f;
            layer.rect = UIStudioMath.ClampToCanvas(r, CanvasSize);
            dirty = true;
            Repaint();
        }

        private void ResetImageToNativeSize(UIStudioLayer layer)
        {
            if (layer == null || layer.type != UIStudioLayerType.Image) return;
            Object asset = LoadImageAssetCached(layer);
            if (asset == null) return;
            Vector2 nativeSize = GetAssetNativeSize(asset);
            Undo.RecordObject(document, "Reset Image Size");
            Rect rect = layer.rect;
            rect.width = Mathf.Max(8f, nativeSize.x);
            rect.height = Mathf.Max(8f, nativeSize.y);
            rect.x = Mathf.Clamp(rect.x, -rect.width + 8f, document.data.canvasWidth - 8f);
            rect.y = Mathf.Clamp(rect.y, -rect.height + 8f, document.data.canvasHeight - 8f);
            layer.rect = rect;
            dirty = true;
            Repaint();
        }

        private void AlignSelected(int mode)
        {
            UIStudioLayer layer = SelectedLayer;
            if (layer == null || layer.locked) return;
            Undo.RecordObject(document, "Align Layer");
            Rect r = layer.rect;
            if (mode == 0) r.x = 0f;
            if (mode == 1) r.x = (document.data.canvasWidth - r.width) * 0.5f;
            if (mode == 2) r.x = document.data.canvasWidth - r.width;
            if (mode == 3) r.y = 0f;
            if (mode == 4) r.y = (document.data.canvasHeight - r.height) * 0.5f;
            if (mode == 5) r.y = document.data.canvasHeight - r.height;
            layer.rect = r;
            dirty = true;
            Repaint();
        }

        private void ShowShapeAddMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("둥근 사각형"), false, () => AddShapeLayer(UIStudioShapeType.RoundedRectangle));
            menu.AddItem(new GUIContent("사각형"), false, () => AddShapeLayer(UIStudioShapeType.Rectangle));
            menu.AddItem(new GUIContent("원형-타원"), false, () => AddShapeLayer(UIStudioShapeType.Ellipse));
            menu.AddItem(new GUIContent("필"), false, () => AddShapeLayer(UIStudioShapeType.Pill));
            menu.AddItem(new GUIContent("다이아"), false, () => AddShapeLayer(UIStudioShapeType.Diamond));
            menu.AddItem(new GUIContent("삼각형"), false, () => AddShapeLayer(UIStudioShapeType.Triangle));
            menu.ShowAsContext();
        }

        private void ShowAddLayerMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("도형/둥근 사각형"), false, () => AddShapeLayer(UIStudioShapeType.RoundedRectangle));
            menu.AddItem(new GUIContent("도형/사각형"), false, () => AddShapeLayer(UIStudioShapeType.Rectangle));
            menu.AddItem(new GUIContent("도형/원형-타원"), false, () => AddShapeLayer(UIStudioShapeType.Ellipse));
            menu.AddItem(new GUIContent("도형/필"), false, () => AddShapeLayer(UIStudioShapeType.Pill));
            menu.AddItem(new GUIContent("도형/다이아"), false, () => AddShapeLayer(UIStudioShapeType.Diamond));
            menu.AddItem(new GUIContent("도형/삼각형"), false, () => AddShapeLayer(UIStudioShapeType.Triangle));
            menu.AddItem(new GUIContent("텍스트"), false, AddTextLayer);
            menu.AddItem(new GUIContent("이미지 선택..."), false, OpenImagePicker);
            menu.ShowAsContext();
        }

        private void OpenImagePicker()
        {
            EditorGUIUtility.ShowObjectPicker<Object>(null, false, "t:Sprite", ImagePickerId);
        }

        private void HandleObjectPicker(Event e)
        {
            if (e.type != EventType.ExecuteCommand || e.commandName != "ObjectSelectorClosed") return;
            if (EditorGUIUtility.GetObjectPickerControlID() != ImagePickerId) return;
            Object picked = EditorGUIUtility.GetObjectPickerObject();
            if (IsSupportedImageAsset(picked)) AddImageLayer(picked, CanvasSize * 0.5f);
            e.Use();
        }

        private static bool IsSupportedImageAsset(Object asset)
        {
            return asset is Sprite || asset is Texture2D;
        }

        private static Vector2 GetAssetNativeSize(Object asset)
        {
            Sprite sprite = asset as Sprite;
            if (sprite != null) return sprite.rect.size;
            Texture2D texture = asset as Texture2D;
            if (texture != null) return new Vector2(texture.width, texture.height);
            return new Vector2(256f, 256f);
        }

        private void SetImageAsset(UIStudioLayer layer, Object asset)
        {
            if (asset == null)
            {
                layer.assetPath = string.Empty;
                layer.subAssetName = string.Empty;
                layer.assetIsSprite = false;
                return;
            }

            layer.assetPath = AssetDatabase.GetAssetPath(asset);
            layer.assetIsSprite = asset is Sprite;
            layer.subAssetName = layer.assetIsSprite ? asset.name : string.Empty;
        }

        private Object LoadImageAssetCached(UIStudioLayer layer)
        {
            if (layer == null || string.IsNullOrEmpty(layer.assetPath)) return null;
            string key = layer.assetPath + "|" + layer.subAssetName + "|" + layer.assetIsSprite;
            if (assetCache.TryGetValue(key, out Object cached) && cached != null) return cached;
            Object asset = UIStudioExporter.LoadImageAsset(layer);
            assetCache[key] = asset;
            return asset;
        }

        private static Font LoadFont(string path)
        {
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Font>(path);
        }

        private void RefreshAssets()
        {
            assetEntries.Clear();
            HashSet<int> seen = new HashSet<int>();
            foreach (string filter in new[] { "t:Sprite", "t:Texture2D" })
            {
                string[] guids = AssetDatabase.FindAssets(filter, new[] { "Assets" });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (filter == "t:Sprite")
                    {
                        foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(path))
                        {
                            if (!(obj is Sprite) || !seen.Add(obj.GetInstanceID())) continue;
                            assetEntries.Add(obj);
                        }
                    }
                    else
                    {
                        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                        if (texture == null || !seen.Add(texture.GetInstanceID())) continue;
                        assetEntries.Add(texture);
                    }
                }
            }

            assetEntries.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            lastAssetRefreshTime = EditorApplication.timeSinceStartup;
            Repaint();
        }

        private void SaveDocument(bool saveAs)
        {
            if (saveAs || string.IsNullOrEmpty(currentDocumentPath))
            {
                string suggested = string.IsNullOrEmpty(currentDocumentPath) ? "UI_Layout.json" : Path.GetFileName(currentDocumentPath);
                string path = EditorUtility.SaveFilePanel("UI Image Studio 저장", Application.dataPath, suggested, "json");
                if (string.IsNullOrEmpty(path)) return;
                currentDocumentPath = path;
            }

            File.WriteAllText(currentDocumentPath, JsonUtility.ToJson(document.data, true));
            dirty = false;
            AssetDatabase.Refresh();
            ShowNotification(new GUIContent("저장됨"));
        }

        private void LoadDocument()
        {
            string path = EditorUtility.OpenFilePanel("UI Image Studio 열기", Application.dataPath, "json");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                UIStudioDocumentData loaded = JsonUtility.FromJson<UIStudioDocumentData>(File.ReadAllText(path));
                if (loaded == null) throw new InvalidDataException("JSON을 읽을 수 없습니다.");
                Undo.RecordObject(document, "Load UI Studio Document");
                document.data = loaded;
                currentDocumentPath = path;
                selectedLayerId = null;
                canvasPresetIndex = DetectPreset();
                assetCache.Clear();
                dirty = false;
                hasInitialFit = false;
                Repaint();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("UI Image Studio", "파일을 불러오지 못했습니다.\n" + exception.Message, "확인");
            }
        }

        private void ExportPng()
        {
            string path = EditorUtility.SaveFilePanel("PNG 내보내기", Application.dataPath, "UI_Export.png", "png");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                UIStudioExporter.ExportPng(document.data, path);
                ShowNotification(new GUIContent("PNG 내보내기 완료"));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("UI Image Studio", "PNG 내보내기에 실패했습니다.\n" + exception.Message, "확인");
            }
        }

        private void NewDocument(bool recordUndo)
        {
            if (recordUndo) Undo.RecordObject(document, "New UI Studio Document");
            document.data = new UIStudioDocumentData();
            selectedLayerId = null;
            currentDocumentPath = null;
            canvasPresetIndex = 0;
            pan = Vector2.zero;
            zoom = 0.5f;
            dirty = false;
            assetCache.Clear();
            hasInitialFit = false;
            Repaint();
        }

        private void ApplyCanvasPreset(int index)
        {
            if (index == 0) SetCanvasSize(1920, 1080);
            else if (index == 1) SetCanvasSize(1080, 1920);
            else if (index == 2) SetCanvasSize(1280, 720);
        }

        private void SetCanvasSize(int width, int height)
        {
            width = Mathf.Clamp(width, 1, 8192);
            height = Mathf.Clamp(height, 1, 8192);
            if (width == document.data.canvasWidth && height == document.data.canvasHeight) return;
            Undo.RecordObject(document, "Resize Canvas");
            document.data.canvasWidth = width;
            document.data.canvasHeight = height;
            dirty = true;
            hasInitialFit = false;
            Repaint();
        }

        private int DetectPreset()
        {
            if (document.data.canvasWidth == 1920 && document.data.canvasHeight == 1080) return 0;
            if (document.data.canvasWidth == 1080 && document.data.canvasHeight == 1920) return 1;
            if (document.data.canvasWidth == 1280 && document.data.canvasHeight == 720) return 2;
            return 3;
        }

        private void FitCanvas(Rect workspaceRect)
        {
            Rect local = new Rect(Vector2.zero, workspaceRect.size);
            float fitZoom;
            UIStudioMath.FitRect(local, CanvasSize, 48f, out fitZoom);
            zoom = fitZoom;
            pan = Vector2.zero;
        }

        private void SetZoom(float value)
        {
            zoom = Mathf.Clamp(value, MinZoom, MaxZoom);
            Repaint();
        }

        private void BeginDragUndo(string name)
        {
            Undo.IncrementCurrentGroup();
            activeUndoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(name);
        }

        private void EndDragUndo()
        {
            if (activeUndoGroup < 0) return;
            Undo.CollapseUndoOperations(activeUndoGroup);
            activeUndoGroup = -1;
        }

        private Rect CenteredRect(float width, float height)
        {
            width = Mathf.Min(width, document.data.canvasWidth * 0.8f);
            height = Mathf.Min(height, document.data.canvasHeight * 0.8f);
            return new Rect((document.data.canvasWidth - width) * 0.5f, (document.data.canvasHeight - height) * 0.5f, width, height);
        }

        private string NextName(string baseName)
        {
            if (string.IsNullOrWhiteSpace(baseName)) baseName = "Layer";
            string candidate = baseName;
            int index = 2;
            HashSet<string> names = new HashSet<string>(document.data.layers.Where(x => x != null).Select(x => x.name));
            while (names.Contains(candidate)) candidate = baseName + " " + index++;
            return candidate;
        }

        private UIStudioLayer SelectedLayer
        {
            get
            {
                if (string.IsNullOrEmpty(selectedLayerId)) return null;
                return document.data.layers.FirstOrDefault(layer => layer != null && layer.id == selectedLayerId);
            }
        }

        private Vector2 CanvasSize => new Vector2(document.data.canvasWidth, document.data.canvasHeight);

        private static string LayerIcon(UIStudioLayerType type)
        {
            if (type == UIStudioLayerType.Text) return "T";
            if (type == UIStudioLayerType.Image) return "▧";
            return "▣";
        }

        private static TextAnchor ToGuiAlignment(UIStudioTextAlignment alignment)
        {
            if (alignment == UIStudioTextAlignment.Left) return TextAnchor.MiddleLeft;
            if (alignment == UIStudioTextAlignment.Right) return TextAnchor.MiddleRight;
            return TextAnchor.MiddleCenter;
        }

        private static GUIStyle CenteredMiniStyle()
        {
            GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip
            };
            return style;
        }

        private static Rect FitAspect(Rect rect, float aspect)
        {
            if (aspect <= 0f) return rect;
            float rectAspect = rect.width / Mathf.Max(1f, rect.height);
            if (rectAspect > aspect)
            {
                float width = rect.height * aspect;
                return new Rect(rect.center.x - width * 0.5f, rect.y, width, rect.height);
            }
            else
            {
                float height = rect.width / aspect;
                return new Rect(rect.x, rect.center.y - height * 0.5f, rect.width, height);
            }
        }

        private static Color WithOpacity(Color color, float opacity)
        {
            color.a *= Mathf.Clamp01(opacity);
            return color;
        }

        private static void DrawSimpleOutline(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }

        private static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            if (angle < -180f) angle += 360f;
            return angle;
        }


        private Texture2D GetShapePreviewTexture(UIStudioLayer layer)
        {
            int hash = ComputeShapePreviewHash(layer);
            Texture2D cached;
            int cachedHash;
            if (shapePreviewCache.TryGetValue(layer.id, out cached) && cached != null &&
                shapePreviewHashes.TryGetValue(layer.id, out cachedHash) && cachedHash == hash)
                return cached;

            if (cached != null) DestroyImmediate(cached);

            float sourceWidth = Mathf.Max(2f, layer.rect.width);
            float sourceHeight = Mathf.Max(2f, layer.rect.height);
            float maxPreview = document.data.lightweightPreview ? 256f : 512f;
            float previewScale = Mathf.Min(1f, maxPreview / Mathf.Max(sourceWidth, sourceHeight));
            int width = Mathf.Max(2, Mathf.RoundToInt(sourceWidth * previewScale));
            int height = Mathf.Max(2, Mathf.RoundToInt(sourceHeight * previewScale));

            Texture2D texture = UIStudioExporter.CreateShapeTexture(
                width,
                height,
                layer.shapeType,
                WithOpacity(layer.fillColor, layer.opacity),
                WithOpacity(layer.strokeColor, layer.opacity),
                layer.strokeWidth * previewScale,
                layer.cornerRadius * previewScale);

            shapePreviewCache[layer.id] = texture;
            shapePreviewHashes[layer.id] = hash;
            return texture;
        }

        private static int ComputeShapePreviewHash(UIStudioLayer layer)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Mathf.RoundToInt(layer.rect.width);
                hash = hash * 31 + Mathf.RoundToInt(layer.rect.height);
                hash = hash * 31 + (int)layer.shapeType;
                hash = hash * 31 + layer.fillColor.GetHashCode();
                hash = hash * 31 + layer.strokeColor.GetHashCode();
                hash = hash * 31 + layer.strokeWidth.GetHashCode();
                hash = hash * 31 + layer.cornerRadius.GetHashCode();
                hash = hash * 31 + layer.opacity.GetHashCode();
                return hash;
            }
        }

        private void ClearShapePreviewCache()
        {
            foreach (Texture2D texture in shapePreviewCache.Values)
                if (texture != null) DestroyImmediate(texture);
            shapePreviewCache.Clear();
            shapePreviewHashes.Clear();
        }

        private static Texture2D CreateCheckerTexture()
        {
            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point
            };
            Color a = new Color(0.19f, 0.195f, 0.21f, 1f);
            Color b = new Color(0.235f, 0.24f, 0.255f, 1f);
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
                texture.SetPixel(x, y, ((x < 8) ^ (y < 8)) ? a : b);
            texture.Apply();
            return texture;
        }
    }

    internal static class RectExtensions
    {
        public static Rect Expand(this Rect rect, float amount)
        {
            return new Rect(rect.x - amount, rect.y - amount, rect.width + amount * 2f, rect.height + amount * 2f);
        }
    }
}
