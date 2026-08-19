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
    public static class AF3DExporter
    {
        private const int ExportLayer = 30;

        public static string SavePrefab(AF3DDocumentData data, string assetPath)
        {
            Validate(data);
            if (string.IsNullOrEmpty(assetPath)) throw new ArgumentException("Prefab path is empty.", nameof(assetPath));
            if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal)) throw new ArgumentException("Prefab must be saved inside Assets.", nameof(assetPath));

            GameObject root = null;
            Scene previewScene = default;
            try
            {
                previewScene = EditorSceneManager.NewPreviewScene();
                root = BuildHierarchy(data, false, previewScene);
                root.name = SafeName(data.modelName, "AssetForgeModel");
                PersistGeneratedAssets(root, assetPath);

                bool success;
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, assetPath, out success);
                if (!success || prefab == null) throw new InvalidOperationException("Unity could not save the Prefab asset.");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorGUIUtility.PingObject(prefab);
                Selection.activeObject = prefab;
                return assetPath;
            }
            finally
            {
                DestroyHierarchy(root);
                if (previewScene.IsValid()) EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        public static string SaveCombinedMesh(AF3DDocumentData data, string assetPath)
        {
            Validate(data);
            if (string.IsNullOrEmpty(assetPath)) throw new ArgumentException("Mesh path is empty.", nameof(assetPath));
            if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal)) throw new ArgumentException("Mesh must be saved inside Assets.", nameof(assetPath));

            GameObject root = null;
            Mesh combined = null;
            Scene previewScene = default;
            try
            {
                previewScene = EditorSceneManager.NewPreviewScene();
                root = BuildHierarchy(data, true, previewScene);
                List<CombineInstance> instances = new List<CombineInstance>();
                foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh == null || !filter.gameObject.activeSelf) continue;
                    for (int subMesh = 0; subMesh < filter.sharedMesh.subMeshCount; subMesh++)
                    {
                        instances.Add(new CombineInstance
                        {
                            mesh = filter.sharedMesh,
                            subMeshIndex = subMesh,
                            transform = filter.transform.localToWorldMatrix
                        });
                    }
                }

                if (instances.Count == 0) throw new InvalidOperationException("There are no visible meshes to combine.");
                combined = new Mesh { name = SafeName(data.modelName, "AssetForgeMesh") };
                combined.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                combined.CombineMeshes(instances.ToArray(), true, true, false);
                combined.RecalculateBounds();

                AssetDatabase.CreateAsset(combined, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Mesh saved = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
                if (saved != null)
                {
                    EditorGUIUtility.PingObject(saved);
                    Selection.activeObject = saved;
                }
                combined = null; // AssetDatabase owns it now.
                return assetPath;
            }
            finally
            {
                if (combined != null) Object.DestroyImmediate(combined);
                DestroyHierarchy(root);
                if (previewScene.IsValid()) EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        public static void RenderPng(AF3DDocumentData data, string absolutePath)
        {
            Validate(data);
            if (string.IsNullOrEmpty(absolutePath)) throw new ArgumentException("PNG path is empty.", nameof(absolutePath));

            GameObject root = null;
            GameObject cameraObject = null;
            GameObject keyObject = null;
            GameObject fillObject = null;
            RenderTexture renderTexture = null;
            Texture2D output = null;
            RenderTexture oldActive = RenderTexture.active;
            Scene previewScene = default;

            try
            {
                previewScene = EditorSceneManager.NewPreviewScene();
                root = BuildHierarchy(data, true, previewScene, ExportLayer);
                SetLayerRecursive(root, ExportLayer);

                cameraObject = new GameObject("AssetForge Export Camera", typeof(Camera)) { hideFlags = HideFlags.HideAndDontSave, layer = ExportLayer };
                SceneManager.MoveGameObjectToScene(cameraObject, previewScene);
                Camera camera = cameraObject.GetComponent<Camera>();
                ConfigureCamera(camera, data.camera);
                camera.clearFlags = CameraClearFlags.SolidColor;
                Color clear = data.camera.background;
                clear.a = 0f;
                camera.backgroundColor = clear;
                camera.cullingMask = 1 << ExportLayer;

                keyObject = CreateLight("Key Light", new Vector3(50f, -35f, 0f), 1.15f, ExportLayer);
                fillObject = CreateLight("Fill Light", new Vector3(25f, 140f, 0f), 0.48f, ExportLayer);
                SceneManager.MoveGameObjectToScene(keyObject, previewScene);
                SceneManager.MoveGameObjectToScene(fillObject, previewScene);

                int width = Mathf.Clamp(data.pngWidth, 16, 4096);
                int height = Mathf.Clamp(data.pngHeight, 16, 4096);
                renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    antiAliasing = 1,
                    filterMode = FilterMode.Bilinear
                };
                renderTexture.Create();
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture.active = renderTexture;
                output = new Texture2D(width, height, TextureFormat.RGBA32, false, false) { hideFlags = HideFlags.HideAndDontSave };
                output.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                output.Apply(false, false);
                byte[] png = output.EncodeToPNG();
                string directory = Path.GetDirectoryName(absolutePath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllBytes(absolutePath, png);

                if (IsInsideAssets(absolutePath))
                {
                    AssetDatabase.Refresh();
                    string assetPath = AbsoluteToAssetPath(absolutePath);
                    Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                    if (asset != null)
                    {
                        EditorGUIUtility.PingObject(asset);
                        Selection.activeObject = asset;
                    }
                }
            }
            finally
            {
                RenderTexture.active = oldActive;
                if (output != null) Object.DestroyImmediate(output);
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    Object.DestroyImmediate(renderTexture);
                }
                if (cameraObject != null) Object.DestroyImmediate(cameraObject);
                if (keyObject != null) Object.DestroyImmediate(keyObject);
                if (fillObject != null) Object.DestroyImmediate(fillObject);
                DestroyHierarchy(root);
                if (previewScene.IsValid()) EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        public static void ConfigureCamera(Camera camera, AF3DCameraState state)
        {
            Quaternion rotation = AF3DMath.CameraRotation(state.orbit);
            camera.transform.rotation = rotation;
            camera.transform.position = state.pivot - rotation * Vector3.forward * Mathf.Max(0.1f, state.distance);
            camera.orthographic = state.orthographic;
            camera.orthographicSize = Mathf.Max(0.05f, state.orthographicSize);
            camera.fieldOfView = 35f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 1000f;
            camera.allowHDR = true;
            camera.allowMSAA = true;
        }

        private static GameObject BuildHierarchy(AF3DDocumentData data, bool hidden, Scene targetScene = default, int layer = 0)
        {
            GameObject root = new GameObject(SafeName(data.modelName, "AssetForgeModel"));
            root.layer = layer;
            if (hidden) root.hideFlags = HideFlags.HideAndDontSave;
            if (targetScene.IsValid()) SceneManager.MoveGameObjectToScene(root, targetScene);
            foreach (AF3DPart part in data.parts)
            {
                if (part == null || !part.visible) continue;
                AF3DPrimitiveFactory.CreateObject(part, root.transform, layer, hidden);
            }
            return root;
        }

        private static void PersistGeneratedAssets(GameObject root, string prefabPath)
        {
            string directory = Path.GetDirectoryName(prefabPath)?.Replace('\\', '/') ?? "Assets";
            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
            HashSet<Object> processed = new HashSet<Object>();

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material material = renderer.sharedMaterial;
                if (material == null || EditorUtility.IsPersistent(material) || !processed.Add(material)) continue;
                material.hideFlags = HideFlags.None;
                string materialPath = AssetDatabase.GenerateUniqueAssetPath(directory + "/" + SafeName(prefabName + "_" + renderer.gameObject.name, "Material") + ".mat");
                AssetDatabase.CreateAsset(material, materialPath);
            }

            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null || EditorUtility.IsPersistent(mesh) || !mesh.name.StartsWith("AssetForge ", StringComparison.Ordinal) || !processed.Add(mesh)) continue;
                mesh.hideFlags = HideFlags.None;
                string meshPath = AssetDatabase.GenerateUniqueAssetPath(directory + "/" + SafeName(prefabName + "_" + filter.gameObject.name, "Mesh") + ".asset");
                AssetDatabase.CreateAsset(mesh, meshPath);
            }
        }

        private static GameObject CreateLight(string name, Vector3 euler, float intensity, int layer)
        {
            GameObject go = new GameObject(name, typeof(Light)) { hideFlags = HideFlags.HideAndDontSave, layer = layer };
            Light light = go.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = Color.white;
            light.cullingMask = 1 << layer;
            go.transform.rotation = Quaternion.Euler(euler);
            return go;
        }

        private static void Validate(AF3DDocumentData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.parts == null) data.parts = new List<AF3DPart>();
        }

        private static void DestroyHierarchy(GameObject root)
        {
            if (root == null) return;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material material = renderer.sharedMaterial;
                if (material != null && !EditorUtility.IsPersistent(material)) Object.DestroyImmediate(material);
            }
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh != null && !EditorUtility.IsPersistent(mesh) && mesh.name.StartsWith("AssetForge ", StringComparison.Ordinal)) Object.DestroyImmediate(mesh);
            }
            Object.DestroyImmediate(root);
        }

        private static void SetLayerRecursive(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform) SetLayerRecursive(child.gameObject, layer);
        }

        private static string SafeName(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) value = fallback;
            foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            return value.Trim();
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
