using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace AssetForge
{
    public sealed class AssetForge3DWindow : EditorWindow
    {
        private const float TopHeight = 42f;
        private const float LeftWidth = 104f;
        private const float RightWidth = 330f;
        private const int PreviewLayer = 30;

        private AF3DDocument document;
        private string selectedPartId;
        private AF3DTransformTool transformTool = AF3DTransformTool.Move;
        private string currentDocumentPath;
        private bool dirty;

        private Scene previewScene;
        private GameObject previewRoot;
        private GameObject partsRoot;
        private Camera previewCamera;
        private Light keyLight;
        private Light fillLight;
        private readonly Dictionary<string, GameObject> previewObjects = new Dictionary<string, GameObject>();

        private Vector2 inspectorScroll;
        private Vector2 objectListScroll;
        private bool orbitDragging;
        private bool panDragging;
        private Vector2 lastMousePosition;

        [MenuItem("Tools/Asset Forge/3D Builder %#3")]
        public static void Open()
        {
            AssetForge3DWindow window = GetWindow<AssetForge3DWindow>();
            window.titleContent = new GUIContent("Asset Forge 3D");
            window.minSize = new Vector2(1040f, 620f);
            window.Show();
        }

        private void OnEnable()
        {
            EnsureDocument();
            CreatePreviewEnvironment();
            RebuildPreview();
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            DestroyPreviewEnvironment();
            if (document != null) DestroyImmediate(document);
        }

        private void OnUndoRedo()
        {
            RebuildPreview();
            dirty = true;
            Repaint();
        }

        private void EnsureDocument()
        {
            if (document != null) return;
            document = CreateInstance<AF3DDocument>();
            document.hideFlags = HideFlags.HideAndDontSave;
            document.data = new AF3DDocumentData();
        }

        private void OnGUI()
        {
            EnsureDocument();
            if (!previewScene.IsValid())
            {
                CreatePreviewEnvironment();
                RebuildPreview();
            }

            Rect topRect = new Rect(0f, 0f, position.width, TopHeight);
            Rect leftRect = new Rect(0f, TopHeight, LeftWidth, Mathf.Max(0f, position.height - TopHeight));
            Rect rightRect = new Rect(position.width - RightWidth, TopHeight, RightWidth, Mathf.Max(0f, position.height - TopHeight));
            Rect viewportRect = new Rect(LeftWidth, TopHeight, Mathf.Max(0f, position.width - LeftWidth - RightWidth), Mathf.Max(0f, position.height - TopHeight));

            DrawTopToolbar(topRect);
            DrawLeftToolbar(leftRect);
            DrawViewport(viewportRect);
            DrawRightPanel(rightRect);
            HandleShortcuts(Event.current);
        }

        private void DrawTopToolbar(Rect rect)
        {
            GUILayout.BeginArea(rect, EditorStyles.toolbar);
            GUILayout.BeginHorizontal();

            if (ToolbarButton("새로 만들기", 76f)) NewDocument();
            if (ToolbarButton("열기", 48f)) LoadDocument();
            if (ToolbarButton("저장", 48f)) SaveDocument(false);
            GUILayout.Space(6f);
            if (ToolbarButton("↶", 28f, "Undo")) Undo.PerformUndo();
            if (ToolbarButton("↷", 28f, "Redo")) Undo.PerformRedo();
            GUILayout.Space(6f);

            GUILayout.Label("이름", GUILayout.Width(30f));
            EditorGUI.BeginChangeCheck();
            string newName = EditorGUILayout.TextField(document.data.modelName, EditorStyles.toolbarTextField, GUILayout.Width(145f));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(document, "Rename Model");
                document.data.modelName = string.IsNullOrWhiteSpace(newName) ? "New Model" : newName;
                dirty = true;
            }

            GUILayout.Space(6f);
            document.data.showGrid = GUILayout.Toggle(document.data.showGrid, "Grid", EditorStyles.toolbarButton, GUILayout.Width(48f));
            document.data.positionSnapEnabled = GUILayout.Toggle(document.data.positionSnapEnabled, "Pos Snap", EditorStyles.toolbarButton, GUILayout.Width(68f));
            document.data.rotationSnapEnabled = GUILayout.Toggle(document.data.rotationSnapEnabled, "Rot Snap", EditorStyles.toolbarButton, GUILayout.Width(68f));
            document.data.scaleSnapEnabled = GUILayout.Toggle(document.data.scaleSnapEnabled, "Scale Snap", EditorStyles.toolbarButton, GUILayout.Width(78f));
            document.data.localHandle = GUILayout.Toggle(document.data.localHandle, document.data.localHandle ? "Local" : "Global", EditorStyles.toolbarButton, GUILayout.Width(56f));

            GUILayout.FlexibleSpace();
            if (ToolbarButton("Prefab", 64f)) ExportPrefab();
            if (ToolbarButton("Mesh", 56f)) ExportCombinedMesh();
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.35f, 0.7f, 1f, 1f);
            if (GUILayout.Button("PNG", EditorStyles.toolbarButton, GUILayout.Width(56f), GUILayout.Height(23f))) ExportPng();
            GUI.backgroundColor = previous;

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
            GUILayout.BeginArea(new Rect(rect.x + 7f, rect.y + 8f, rect.width - 14f, rect.height - 16f));

            GUILayout.Label("도구", EditorStyles.boldLabel);
            DrawToolButton("이동", AF3DTransformTool.Move, "W");
            DrawToolButton("회전", AF3DTransformTool.Rotate, "E");
            DrawToolButton("크기", AF3DTransformTool.Scale, "R");

            GUILayout.Space(10f);
            GUILayout.Label("프리미티브", EditorStyles.boldLabel);
            if (GUILayout.Button("Cube", GUILayout.Height(30f))) AddPart(AF3DPrimitiveType.Cube);
            if (GUILayout.Button("Sphere", GUILayout.Height(30f))) AddPart(AF3DPrimitiveType.Sphere);
            if (GUILayout.Button("Capsule", GUILayout.Height(30f))) AddPart(AF3DPrimitiveType.Capsule);
            if (GUILayout.Button("Cylinder", GUILayout.Height(30f))) AddPart(AF3DPrimitiveType.Cylinder);
            if (GUILayout.Button("Plane", GUILayout.Height(30f))) AddPart(AF3DPrimitiveType.Plane);
            if (GUILayout.Button("Cone", GUILayout.Height(30f))) AddPart(AF3DPrimitiveType.Cone);

            GUILayout.Space(10f);
            EditorGUI.BeginDisabledGroup(SelectedPart == null);
            if (GUILayout.Button("복제\nCtrl+D", GUILayout.Height(42f))) DuplicateSelected();
            if (GUILayout.Button("삭제\nDel", GUILayout.Height(42f))) DeleteSelected();
            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();
            GUILayout.Label("RMB: 회전\nMMB: 이동\nWheel: 줌\nF: 선택 포커스", EditorStyles.miniLabel);
            GUILayout.EndArea();
        }

        private void DrawToolButton(string label, AF3DTransformTool tool, string hotkey)
        {
            Color old = GUI.backgroundColor;
            if (transformTool == tool) GUI.backgroundColor = new Color(0.3f, 0.58f, 1f, 1f);
            if (GUILayout.Button(label + "  " + hotkey, GUILayout.Height(34f))) transformTool = tool;
            GUI.backgroundColor = old;
        }

        private void DrawViewport(Rect rect)
        {
            EditorGUI.DrawRect(rect, document.data.camera.background);
            UpdatePreviewCamera();
            if (Event.current.type == EventType.Repaint)
                Handles.DrawCamera(rect, previewCamera, DrawCameraMode.Normal);
            Handles.SetCamera(rect, previewCamera);

            if (document.data.showGrid && Event.current.type == EventType.Repaint) DrawGrid();
            bool overViewportToolbar = GetViewportToolbarRect(rect).Contains(Event.current.mousePosition);
            DrawSelectedOutlineAndHandle(!overViewportToolbar);
            HandleViewportPickingAndNavigation(rect, Event.current);
            DrawViewportOverlay(rect);
        }

        private void DrawGrid()
        {
            const int extent = 20;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            for (int i = -extent; i <= extent; i++)
            {
                bool major = i % 5 == 0;
                Handles.color = major ? new Color(1f, 1f, 1f, 0.16f) : new Color(1f, 1f, 1f, 0.07f);
                Handles.DrawLine(new Vector3(i, 0f, -extent), new Vector3(i, 0f, extent));
                Handles.DrawLine(new Vector3(-extent, 0f, i), new Vector3(extent, 0f, i));
            }
            Handles.color = new Color(0.85f, 0.2f, 0.2f, 0.8f);
            Handles.DrawLine(new Vector3(-extent, 0.002f, 0f), new Vector3(extent, 0.002f, 0f));
            Handles.color = new Color(0.2f, 0.45f, 1f, 0.8f);
            Handles.DrawLine(new Vector3(0f, 0.002f, -extent), new Vector3(0f, 0.002f, extent));
            Handles.color = Color.white;
        }

        private void DrawSelectedOutlineAndHandle(bool allowInteraction)
        {
            AF3DPart part = SelectedPart;
            GameObject go = SelectedPreviewObject;
            if (part == null || go == null || !part.visible) return;

            if (Event.current.type == EventType.Repaint)
            {
                Matrix4x4 oldMatrix = Handles.matrix;
                Color oldColor = Handles.color;
                Handles.color = new Color(0.25f, 0.72f, 1f, 1f);
                Handles.matrix = go.transform.localToWorldMatrix;
                Handles.DrawWireCube(Vector3.zero, Vector3.one);
                Handles.matrix = oldMatrix;
                Handles.color = oldColor;
            }

            if (part.locked || !allowInteraction) return;
            Quaternion orientation = document.data.localHandle ? go.transform.rotation : Quaternion.identity;
            EditorGUI.BeginChangeCheck();

            if (transformTool == AF3DTransformTool.Move)
            {
                Vector3 position = Handles.PositionHandle(go.transform.position, orientation);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(document, "Move 3D Part");
                    if (document.data.positionSnapEnabled) position = AF3DMath.Snap(position, document.data.positionSnap);
                    part.position = position;
                    SyncPreviewPart(part);
                    dirty = true;
                }
                return;
            }

            if (transformTool == AF3DTransformTool.Rotate)
            {
                Quaternion rotation = Handles.RotationHandle(go.transform.rotation, go.transform.position);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(document, "Rotate 3D Part");
                    Vector3 euler = AF3DMath.NormalizeEuler(rotation.eulerAngles);
                    if (document.data.rotationSnapEnabled) euler = AF3DMath.Snap(euler, document.data.rotationSnap);
                    part.rotation = AF3DMath.NormalizeEuler(euler);
                    SyncPreviewPart(part);
                    dirty = true;
                }
                return;
            }

            float size = HandleUtility.GetHandleSize(go.transform.position);
            Vector3 scale = Handles.ScaleHandle(go.transform.localScale, go.transform.position, orientation, size);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(document, "Scale 3D Part");
                part.scale = document.data.scaleSnapEnabled
                    ? AF3DMath.SnapScale(scale, document.data.scaleSnap)
                    : AF3DMath.ClampScale(scale);
                SyncPreviewPart(part);
                dirty = true;
            }
        }

        private void HandleViewportPickingAndNavigation(Rect rect, Event e)
        {
            if (GetViewportToolbarRect(rect).Contains(e.mousePosition)) return;
            if (!rect.Contains(e.mousePosition))
            {
                if (e.type == EventType.MouseUp)
                {
                    orbitDragging = false;
                    panDragging = false;
                }
                return;
            }

            int pickingControl = GUIUtility.GetControlID("AssetForge3DViewport".GetHashCode(), FocusType.Passive, rect);
            if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(pickingControl);

            if (e.type == EventType.ScrollWheel)
            {
                Undo.RecordObject(document, "Zoom 3D Camera");
                float factor = Mathf.Exp(e.delta.y * 0.08f);
                if (document.data.camera.orthographic)
                    document.data.camera.orthographicSize = Mathf.Clamp(document.data.camera.orthographicSize * factor, 0.05f, 100f);
                else
                    document.data.camera.distance = Mathf.Clamp(document.data.camera.distance * factor, 0.15f, 250f);
                e.Use();
                Repaint();
                return;
            }

            bool orbitMouseDown = e.type == EventType.MouseDown && (e.button == 1 || (e.alt && e.button == 0));
            bool panMouseDown = e.type == EventType.MouseDown && (e.button == 2 || (e.alt && e.button == 2));
            if (orbitMouseDown)
            {
                orbitDragging = true;
                panDragging = false;
                lastMousePosition = e.mousePosition;
                GUIUtility.hotControl = pickingControl;
                e.Use();
                return;
            }
            if (panMouseDown)
            {
                panDragging = true;
                orbitDragging = false;
                lastMousePosition = e.mousePosition;
                GUIUtility.hotControl = pickingControl;
                e.Use();
                return;
            }

            if (e.type == EventType.MouseDrag && orbitDragging)
            {
                Vector2 delta = e.mousePosition - lastMousePosition;
                lastMousePosition = e.mousePosition;
                document.data.camera.orbit.y += delta.x * 0.35f;
                document.data.camera.orbit.x = Mathf.Clamp(document.data.camera.orbit.x - delta.y * 0.35f, -89.5f, 89.5f);
                e.Use();
                Repaint();
                return;
            }

            if (e.type == EventType.MouseDrag && panDragging)
            {
                Vector2 delta = e.mousePosition - lastMousePosition;
                lastMousePosition = e.mousePosition;
                float worldPerPixel = document.data.camera.orthographic
                    ? (document.data.camera.orthographicSize * 2f / Mathf.Max(1f, rect.height))
                    : (document.data.camera.distance * 0.0022f);
                document.data.camera.pivot += (-previewCamera.transform.right * delta.x + previewCamera.transform.up * delta.y) * worldPerPixel;
                e.Use();
                Repaint();
                return;
            }

            if (e.type == EventType.MouseUp && (orbitDragging || panDragging))
            {
                orbitDragging = false;
                panDragging = false;
                GUIUtility.hotControl = 0;
                e.Use();
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && HandleUtility.nearestControl == pickingControl)
            {
                selectedPartId = PickPreviewPart(e.mousePosition);
                Repaint();
                e.Use();
            }
        }

        private string PickPreviewPart(Vector2 guiPoint)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(guiPoint);
            float bestDistance = float.PositiveInfinity;
            string bestId = null;
            foreach (KeyValuePair<string, GameObject> entry in previewObjects)
            {
                GameObject go = entry.Value;
                if (go == null || !go.activeInHierarchy) continue;
                Renderer renderer = go.GetComponentInChildren<Renderer>();
                if (renderer == null) continue;
                Bounds bounds = renderer.bounds;
                if (bounds.size.sqrMagnitude < 0.0001f) bounds.Expand(0.02f);
                else bounds.Expand(0.01f);
                if (bounds.IntersectRay(ray, out float distance) && distance >= 0f && distance < bestDistance)
                {
                    bestDistance = distance;
                    bestId = entry.Key;
                }
            }
            return bestId;
        }

        private static Rect GetViewportToolbarRect(Rect rect)
        {
            return new Rect(rect.x + 10f, rect.y + 10f, Mathf.Max(0f, Mathf.Min(460f, rect.width - 20f)), 28f);
        }

        private void DrawViewportOverlay(Rect rect)
        {
            Rect overlay = GetViewportToolbarRect(rect);
            GUILayout.BeginArea(overlay, EditorStyles.toolbar);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Front", EditorStyles.toolbarButton)) SetView(new Vector2(0f, 0f));
            if (GUILayout.Button("Back", EditorStyles.toolbarButton)) SetView(new Vector2(0f, 180f));
            if (GUILayout.Button("Left", EditorStyles.toolbarButton)) SetView(new Vector2(0f, 90f));
            if (GUILayout.Button("Right", EditorStyles.toolbarButton)) SetView(new Vector2(0f, -90f));
            if (GUILayout.Button("Top", EditorStyles.toolbarButton)) SetView(new Vector2(89.5f, 0f));
            if (GUILayout.Button("Bottom", EditorStyles.toolbarButton)) SetView(new Vector2(-89.5f, 0f));
            if (GUILayout.Button(document.data.camera.orthographic ? "Ortho" : "Persp", EditorStyles.toolbarButton, GUILayout.Width(58f)))
            {
                document.data.camera.orthographic = !document.data.camera.orthographic;
                Repaint();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            Rect info = new Rect(rect.x + 10f, rect.yMax - 28f, 250f, 20f);
            GUI.Label(info, transformTool + " | " + (document.data.localHandle ? "Local" : "Global") + " | Parts " + document.data.parts.Count, EditorStyles.miniLabel);
        }

        private void DrawRightPanel(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            GUILayout.BeginArea(new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, rect.height - 16f));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Objects", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+", GUILayout.Width(26f))) ShowAddPrimitiveMenu();
            if (GUILayout.Button("⧉", GUILayout.Width(26f))) DuplicateSelected();
            if (GUILayout.Button("×", GUILayout.Width(26f))) DeleteSelected();
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(Mathf.Min(210f, rect.height * 0.36f)));
            objectListScroll = GUILayout.BeginScrollView(objectListScroll);
            for (int i = document.data.parts.Count - 1; i >= 0; i--)
            {
                AF3DPart part = document.data.parts[i];
                if (part != null) DrawObjectRow(part);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(6f);
            GUILayout.Label(SelectedPart == null ? "Model / Camera" : "Inspector", EditorStyles.boldLabel);
            inspectorScroll = GUILayout.BeginScrollView(inspectorScroll);
            if (SelectedPart == null) DrawModelInspector();
            else DrawPartInspector(SelectedPart);
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void DrawObjectRow(AF3DPart part)
        {
            Rect row = GUILayoutUtility.GetRect(10f, 25f, GUILayout.ExpandWidth(true));
            bool selected = part.id == selectedPartId;
            if (selected) EditorGUI.DrawRect(row, new Color(0.18f, 0.42f, 0.78f, 0.68f));

            Rect visibleRect = new Rect(row.x + 2f, row.y + 2f, 22f, 21f);
            Rect lockRect = new Rect(row.x + 26f, row.y + 2f, 22f, 21f);
            Rect labelRect = new Rect(row.x + 52f, row.y + 2f, row.width - 54f, 21f);

            bool visible = GUI.Toggle(visibleRect, part.visible, part.visible ? "●" : "○", EditorStyles.miniButton);
            bool locked = GUI.Toggle(lockRect, part.locked, part.locked ? "L" : "-", EditorStyles.miniButton);
            if (visible != part.visible || locked != part.locked)
            {
                Undo.RecordObject(document, "Change 3D Part State");
                part.visible = visible;
                part.locked = locked;
                SyncPreviewPart(part);
                dirty = true;
            }

            if (GUI.Button(labelRect, part.primitiveType + "  " + part.name, EditorStyles.miniButton))
            {
                selectedPartId = part.id;
                Repaint();
            }
        }

        private void DrawModelInspector()
        {
            EditorGUI.BeginChangeCheck();
            Color background = EditorGUILayout.ColorField("배경", document.data.camera.background);
            bool orthographic = EditorGUILayout.Toggle("Orthographic", document.data.camera.orthographic);
            float posSnap = EditorGUILayout.FloatField("Position Snap", document.data.positionSnap);
            float rotSnap = EditorGUILayout.FloatField("Rotation Snap", document.data.rotationSnap);
            float scaleSnap = EditorGUILayout.FloatField("Scale Snap", document.data.scaleSnap);
            int pngWidth = EditorGUILayout.IntField("PNG Width", document.data.pngWidth);
            int pngHeight = EditorGUILayout.IntField("PNG Height", document.data.pngHeight);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(document, "Edit 3D Model Settings");
                document.data.camera.background = background;
                document.data.camera.orthographic = orthographic;
                document.data.positionSnap = Mathf.Max(0.001f, posSnap);
                document.data.rotationSnap = Mathf.Max(0.1f, rotSnap);
                document.data.scaleSnap = Mathf.Max(0.001f, scaleSnap);
                document.data.pngWidth = Mathf.Clamp(pngWidth, 16, 4096);
                document.data.pngHeight = Mathf.Clamp(pngHeight, 16, 4096);
                dirty = true;
            }

            GUILayout.Space(8f);
            if (GUILayout.Button("전체 모델 프레임")) FrameAll();
            if (GUILayout.Button("카메라 초기화")) ResetCamera();

            GUILayout.Space(8f);
            EditorGUILayout.HelpBox("W/E/R = 이동/회전/크기, RMB = Orbit, MMB = Pan, 휠 = Zoom, F = 선택 오브젝트 포커스", MessageType.Info);
        }

        private void DrawPartInspector(AF3DPart part)
        {
            AF3DPrimitiveType oldType = part.primitiveType;
            EditorGUI.BeginChangeCheck();
            string name = EditorGUILayout.TextField("이름", part.name);
            AF3DPrimitiveType primitive = (AF3DPrimitiveType)EditorGUILayout.EnumPopup("종류", part.primitiveType);
            Vector3 position = EditorGUILayout.Vector3Field("Position", part.position);
            Vector3 rotation = EditorGUILayout.Vector3Field("Rotation", part.rotation);
            Vector3 scale = EditorGUILayout.Vector3Field("Scale", part.scale);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(document, "Edit 3D Part");
                part.name = string.IsNullOrWhiteSpace(name) ? part.primitiveType.ToString() : name;
                part.primitiveType = primitive;
                part.position = document.data.positionSnapEnabled ? AF3DMath.Snap(position, document.data.positionSnap) : position;
                part.rotation = document.data.rotationSnapEnabled ? AF3DMath.Snap(AF3DMath.NormalizeEuler(rotation), document.data.rotationSnap) : AF3DMath.NormalizeEuler(rotation);
                part.scale = document.data.scaleSnapEnabled ? AF3DMath.SnapScale(scale, document.data.scaleSnap) : AF3DMath.ClampScale(scale);
                if (oldType != primitive) ReplacePreviewPart(part);
                else SyncPreviewPart(part);
                dirty = true;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Transform")) ResetSelectedTransform();
            if (GUILayout.Button("Focus")) FrameSelected();
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            GUILayout.Label("빠른 회전", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("X +90")) RotateSelectedBy(new Vector3(90f, 0f, 0f));
            if (GUILayout.Button("Y +90")) RotateSelectedBy(new Vector3(0f, 90f, 0f));
            if (GUILayout.Button("Z +90")) RotateSelectedBy(new Vector3(0f, 0f, 90f));
            GUILayout.EndHorizontal();

            GUILayout.Label("Mirror", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("X")) MirrorSelected(0);
            if (GUILayout.Button("Y")) MirrorSelected(1);
            if (GUILayout.Button("Z")) MirrorSelected(2);
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);
            GUILayout.Label("Material", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            Color color = EditorGUILayout.ColorField("Color", part.color);
            float metallic = EditorGUILayout.Slider("Metallic", part.metallic, 0f, 1f);
            float smoothness = EditorGUILayout.Slider("Smoothness", part.smoothness, 0f, 1f);
            bool emission = EditorGUILayout.Toggle("Emission", part.emissionEnabled);
            Color emissionColor = part.emissionColor;
            if (emission) emissionColor = EditorGUILayout.ColorField("Emission Color", part.emissionColor);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(document, "Edit 3D Material");
                part.color = color;
                part.metallic = metallic;
                part.smoothness = smoothness;
                part.emissionEnabled = emission;
                part.emissionColor = emissionColor;
                SyncPreviewPart(part);
                dirty = true;
            }

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("복제")) DuplicateSelected();
            if (GUILayout.Button("삭제")) DeleteSelected();
            GUILayout.EndHorizontal();
        }

        private void ShowAddPrimitiveMenu()
        {
            GenericMenu menu = new GenericMenu();
            foreach (AF3DPrimitiveType type in Enum.GetValues(typeof(AF3DPrimitiveType)))
            {
                AF3DPrimitiveType captured = type;
                menu.AddItem(new GUIContent(captured.ToString()), false, () => AddPart(captured));
            }
            menu.ShowAsContext();
        }

        private void AddPart(AF3DPrimitiveType type)
        {
            Undo.RecordObject(document, "Add 3D Part");
            AF3DPart part = new AF3DPart
            {
                primitiveType = type,
                name = NextPartName(type.ToString()),
                position = document.data.camera.pivot
            };
            if (type == AF3DPrimitiveType.Plane) part.scale = new Vector3(2f, 1f, 2f);
            document.data.parts.Add(part);
            selectedPartId = part.id;
            CreatePreviewPart(part);
            dirty = true;
            Repaint();
        }

        private void DuplicateSelected()
        {
            AF3DPart part = SelectedPart;
            if (part == null) return;
            Undo.RecordObject(document, "Duplicate 3D Part");
            AF3DPart clone = part.Clone();
            clone.name = NextPartName(part.name);
            document.data.parts.Add(clone);
            selectedPartId = clone.id;
            CreatePreviewPart(clone);
            dirty = true;
            Repaint();
        }

        private void DeleteSelected()
        {
            AF3DPart part = SelectedPart;
            if (part == null) return;
            Undo.RecordObject(document, "Delete 3D Part");
            document.data.parts.Remove(part);
            DestroyPreviewPart(part.id);
            selectedPartId = null;
            dirty = true;
            Repaint();
        }

        private void MirrorSelected(int axis)
        {
            AF3DPart part = SelectedPart;
            if (part == null || part.locked) return;
            Undo.RecordObject(document, "Mirror 3D Part");
            Vector3 scale = part.scale;
            if (axis == 0) scale.x *= -1f;
            if (axis == 1) scale.y *= -1f;
            if (axis == 2) scale.z *= -1f;
            part.scale = AF3DMath.ClampScale(scale);
            SyncPreviewPart(part);
            dirty = true;
        }

        private void ResetSelectedTransform()
        {
            AF3DPart part = SelectedPart;
            if (part == null || part.locked) return;
            Undo.RecordObject(document, "Reset 3D Transform");
            part.position = Vector3.zero;
            part.rotation = Vector3.zero;
            part.scale = Vector3.one;
            SyncPreviewPart(part);
            dirty = true;
            Repaint();
        }

        private void RotateSelectedBy(Vector3 delta)
        {
            AF3DPart part = SelectedPart;
            if (part == null || part.locked) return;
            Undo.RecordObject(document, "Rotate 3D Part 90");
            part.rotation = AF3DMath.NormalizeEuler(part.rotation + delta);
            SyncPreviewPart(part);
            dirty = true;
            Repaint();
        }

        private void FrameSelected()
        {
            GameObject go = SelectedPreviewObject;
            if (go == null) return;
            Bounds bounds = GetObjectBounds(go);
            document.data.camera.pivot = bounds.center;
            FitCameraToBounds(bounds);
            Repaint();
        }

        private void FrameAll()
        {
            bool hasBounds = false;
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            foreach (GameObject go in previewObjects.Values)
            {
                if (go == null || !go.activeInHierarchy) continue;
                Bounds b = GetObjectBounds(go);
                if (!hasBounds) { bounds = b; hasBounds = true; }
                else bounds.Encapsulate(b);
            }
            if (!hasBounds) return;
            document.data.camera.pivot = bounds.center;
            FitCameraToBounds(bounds);
            Repaint();
        }

        private void FitCameraToBounds(Bounds bounds)
        {
            float radius = Mathf.Max(0.25f, bounds.extents.magnitude);
            document.data.camera.distance = Mathf.Clamp(radius * 2.8f, 0.5f, 250f);
            document.data.camera.orthographicSize = Mathf.Clamp(radius * 1.35f, 0.1f, 100f);
        }

        private static Bounds GetObjectBounds(GameObject go)
        {
            Renderer renderer = go.GetComponentInChildren<Renderer>();
            return renderer != null ? renderer.bounds : new Bounds(go.transform.position, Vector3.one);
        }

        private void SetView(Vector2 orbit)
        {
            document.data.camera.orbit = orbit;
            Repaint();
        }

        private void ResetCamera()
        {
            Undo.RecordObject(document, "Reset 3D Camera");
            document.data.camera = new AF3DCameraState();
            Repaint();
        }

        private void HandleShortcuts(Event e)
        {
            if (e.type != EventType.KeyDown || EditorGUIUtility.editingTextField) return;
            bool action = e.control || e.command;
            if (action && e.keyCode == KeyCode.S) { SaveDocument(false); e.Use(); return; }
            if (action && e.keyCode == KeyCode.D) { DuplicateSelected(); e.Use(); return; }
            if (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace) { DeleteSelected(); e.Use(); return; }
            if (e.keyCode == KeyCode.W) { transformTool = AF3DTransformTool.Move; e.Use(); return; }
            if (e.keyCode == KeyCode.E) { transformTool = AF3DTransformTool.Rotate; e.Use(); return; }
            if (e.keyCode == KeyCode.R) { transformTool = AF3DTransformTool.Scale; e.Use(); return; }
            if (e.keyCode == KeyCode.F) { FrameSelected(); e.Use(); }
        }

        private void CreatePreviewEnvironment()
        {
            DestroyPreviewEnvironment();
            previewScene = EditorSceneManager.NewPreviewScene();
            previewRoot = new GameObject("__AssetForge3DPreview__") { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(previewRoot, previewScene);

            partsRoot = new GameObject("Parts") { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(partsRoot, previewScene);
            partsRoot.transform.SetParent(previewRoot.transform, false);

            GameObject cameraObject = new GameObject("Camera", typeof(Camera)) { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(cameraObject, previewScene);
            cameraObject.transform.SetParent(previewRoot.transform, false);
            previewCamera = cameraObject.GetComponent<Camera>();
            previewCamera.enabled = false;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.cullingMask = ~0;

            GameObject keyObject = new GameObject("Key Light", typeof(Light)) { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(keyObject, previewScene);
            keyObject.transform.SetParent(previewRoot.transform, false);
            keyLight = keyObject.GetComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.15f;
            keyObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            GameObject fillObject = new GameObject("Fill Light", typeof(Light)) { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(fillObject, previewScene);
            fillObject.transform.SetParent(previewRoot.transform, false);
            fillLight = fillObject.GetComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.intensity = 0.42f;
            fillObject.transform.rotation = Quaternion.Euler(25f, 140f, 0f);
        }

        private void DestroyPreviewEnvironment()
        {
            previewObjects.Clear();
            if (previewRoot != null)
            {
                DestroyGeneratedResources(previewRoot);
                DestroyImmediate(previewRoot);
            }
            previewRoot = null;
            partsRoot = null;
            previewCamera = null;
            keyLight = null;
            fillLight = null;
            if (previewScene.IsValid()) EditorSceneManager.ClosePreviewScene(previewScene);
            previewScene = default;
        }

        private void RebuildPreview()
        {
            if (partsRoot == null) return;
            for (int i = partsRoot.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = partsRoot.transform.GetChild(i).gameObject;
                DestroyGeneratedResources(child);
                DestroyImmediate(child);
            }
            previewObjects.Clear();
            foreach (AF3DPart part in document.data.parts)
                if (part != null) CreatePreviewPart(part);

            if (SelectedPart == null) selectedPartId = null;
            Repaint();
        }

        private void CreatePreviewPart(AF3DPart part)
        {
            if (partsRoot == null || part == null) return;
            GameObject go = AF3DPrimitiveFactory.CreateObject(part, partsRoot.transform, PreviewLayer, true);
            previewObjects[part.id] = go;
        }

        private void ReplacePreviewPart(AF3DPart part)
        {
            DestroyPreviewPart(part.id);
            CreatePreviewPart(part);
            Repaint();
        }

        private void DestroyPreviewPart(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (!previewObjects.TryGetValue(id, out GameObject go) || go == null)
            {
                previewObjects.Remove(id);
                return;
            }
            DestroyGeneratedResources(go);
            DestroyImmediate(go);
            previewObjects.Remove(id);
        }

        private void SyncPreviewPart(AF3DPart part)
        {
            if (part == null || !previewObjects.TryGetValue(part.id, out GameObject go) || go == null) return;
            go.name = part.name;
            go.transform.localPosition = part.position;
            go.transform.localRotation = Quaternion.Euler(part.rotation);
            go.transform.localScale = AF3DMath.ClampScale(part.scale);
            go.SetActive(part.visible);
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) AF3DPrimitiveFactory.ApplyMaterialProperties(renderer.sharedMaterial, part);
            Repaint();
        }

        private static void DestroyGeneratedResources(GameObject root)
        {
            if (root == null) return;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material material = renderer.sharedMaterial;
                if (material != null && !EditorUtility.IsPersistent(material)) DestroyImmediate(material);
            }
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh != null && !EditorUtility.IsPersistent(mesh) && mesh.name.StartsWith("AssetForge ", StringComparison.Ordinal)) DestroyImmediate(mesh);
            }
        }

        private void UpdatePreviewCamera()
        {
            if (previewCamera == null) return;
            AF3DExporter.ConfigureCamera(previewCamera, document.data.camera);
            previewCamera.backgroundColor = document.data.camera.background;
        }

        private AF3DPart SelectedPart
        {
            get
            {
                if (string.IsNullOrEmpty(selectedPartId)) return null;
                return document.data.parts.FirstOrDefault(part => part != null && part.id == selectedPartId);
            }
        }

        private GameObject SelectedPreviewObject
        {
            get
            {
                if (string.IsNullOrEmpty(selectedPartId)) return null;
                return previewObjects.TryGetValue(selectedPartId, out GameObject go) ? go : null;
            }
        }

        private string NextPartName(string baseName)
        {
            if (string.IsNullOrWhiteSpace(baseName)) baseName = "Part";
            HashSet<string> names = new HashSet<string>(document.data.parts.Where(p => p != null).Select(p => p.name));
            if (!names.Contains(baseName)) return baseName;
            int index = 2;
            while (names.Contains(baseName + " " + index)) index++;
            return baseName + " " + index;
        }

        private void NewDocument()
        {
            Undo.RecordObject(document, "New Asset Forge 3D Document");
            document.data = new AF3DDocumentData();
            selectedPartId = null;
            currentDocumentPath = null;
            dirty = false;
            RebuildPreview();
        }

        private void SaveDocument(bool saveAs)
        {
            if (saveAs || string.IsNullOrEmpty(currentDocumentPath))
            {
                string suggested = SafeFileName(document.data.modelName) + ".af3d.json";
                string path = EditorUtility.SaveFilePanel("Asset Forge 3D 저장", Application.dataPath, suggested, "json");
                if (string.IsNullOrEmpty(path)) return;
                currentDocumentPath = path;
            }
            File.WriteAllText(currentDocumentPath, JsonUtility.ToJson(document.data, true));
            dirty = false;
            ShowNotification(new GUIContent("3D 프로젝트 저장됨"));
        }

        private void LoadDocument()
        {
            string path = EditorUtility.OpenFilePanel("Asset Forge 3D 열기", Application.dataPath, "json");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                AF3DDocumentData loaded = JsonUtility.FromJson<AF3DDocumentData>(File.ReadAllText(path));
                if (loaded == null) throw new InvalidDataException("JSON을 읽을 수 없습니다.");
                if (loaded.parts == null) loaded.parts = new List<AF3DPart>();
                if (loaded.camera == null) loaded.camera = new AF3DCameraState();
                Undo.RecordObject(document, "Load Asset Forge 3D Document");
                document.data = loaded;
                currentDocumentPath = path;
                selectedPartId = null;
                dirty = false;
                RebuildPreview();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Asset Forge 3D", "파일을 불러오지 못했습니다.\n" + exception.Message, "확인");
            }
        }

        private void ExportPrefab()
        {
            string fileName = SafeFileName(document.data.modelName) + ".prefab";
            string path = EditorUtility.SaveFilePanelInProject("Prefab 저장", fileName, "prefab", "Prefab 저장 위치를 선택하세요.");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                AF3DExporter.SavePrefab(document.data, path);
                ShowNotification(new GUIContent("Prefab 저장 완료"));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Asset Forge 3D", "Prefab 저장 실패\n" + exception.Message, "확인");
            }
        }

        private void ExportCombinedMesh()
        {
            string fileName = SafeFileName(document.data.modelName) + ".asset";
            string path = EditorUtility.SaveFilePanelInProject("Mesh 저장", fileName, "asset", "합쳐진 Mesh 저장 위치를 선택하세요.");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                AF3DExporter.SaveCombinedMesh(document.data, path);
                ShowNotification(new GUIContent("Mesh 저장 완료"));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Asset Forge 3D", "Mesh 저장 실패\n" + exception.Message, "확인");
            }
        }

        private void ExportPng()
        {
            string fileName = SafeFileName(document.data.modelName) + ".png";
            string path = EditorUtility.SaveFilePanel("PNG 렌더", Application.dataPath, fileName, "png");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                AF3DExporter.RenderPng(document.data, path);
                ShowNotification(new GUIContent("PNG 렌더 완료"));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Asset Forge 3D", "PNG 렌더 실패\n" + exception.Message, "확인");
            }
        }

        private static string SafeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) value = "AssetForgeModel";
            foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            return value.Trim();
        }
    }
}
