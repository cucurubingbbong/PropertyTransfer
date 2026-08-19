using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace AssetForge
{
    public static class AF3DPrimitiveFactory
    {
        private static Shader cachedShader;
        public static GameObject CreateObject(AF3DPart part, Transform parent, int layer, bool hidden)
        {
            GameObject go;
            switch (part.primitiveType)
            {
                case AF3DPrimitiveType.Sphere:
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    break;
                case AF3DPrimitiveType.Capsule:
                    go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    break;
                case AF3DPrimitiveType.Cylinder:
                    go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    break;
                case AF3DPrimitiveType.Plane:
                    go = CreateCustomMeshObject("Plane", CreatePlaneMesh(), hidden);
                    break;
                case AF3DPrimitiveType.Cone:
                    go = CreateCustomMeshObject("Cone", CreateConeMesh(32), hidden);
                    break;
                default:
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    break;
            }

            Collider collider = go.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);

            go.name = part.name;
            go.layer = layer;
            if (parent != null && parent.gameObject.scene.IsValid() && go.scene != parent.gameObject.scene)
                SceneManager.MoveGameObjectToScene(go, parent.gameObject.scene);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = part.position;
            go.transform.localRotation = Quaternion.Euler(part.rotation);
            go.transform.localScale = AF3DMath.ClampScale(part.scale);
            go.SetActive(part.visible);

            if (hidden)
                SetHideFlagsRecursive(go, HideFlags.HideAndDontSave);

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = CreateMaterial(part, hidden);

            return go;
        }

        public static Material CreateMaterial(AF3DPart part, bool hidden)
        {
            Material material = new Material(ResolveShader());
            material.name = part.name + " Material";
            if (hidden) material.hideFlags = HideFlags.HideAndDontSave;

            ApplyMaterialProperties(material, part);
            return material;
        }

        private static Shader ResolveShader()
        {
            if (cachedShader != null) return cachedShader;
            cachedShader = Shader.Find("Universal Render Pipeline/Lit") ??
                           Shader.Find("HDRP/Lit") ??
                           Shader.Find("Standard") ??
                           Shader.Find("Hidden/InternalErrorShader");
            return cachedShader;
        }

        public static void ApplyMaterialProperties(Material material, AF3DPart part)
        {
            if (material == null || part == null) return;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", part.color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", part.color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", Mathf.Clamp01(part.metallic));
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", Mathf.Clamp01(part.smoothness));
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", Mathf.Clamp01(part.smoothness));

            Color emission = part.emissionEnabled ? part.emissionColor : Color.black;
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", emission);
            if (part.emissionEnabled) material.EnableKeyword("_EMISSION");
            else material.DisableKeyword("_EMISSION");
        }

        public static Mesh CreateConeMesh(int segments)
        {
            segments = Mathf.Clamp(segments, 3, 128);
            int vertexCount = segments * 2 + 2;
            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uv = new Vector2[vertexCount];
            int[] triangles = new int[segments * 6];

            int tip = segments * 2;
            int center = tip + 1;
            vertices[tip] = new Vector3(0f, 0.5f, 0f);
            vertices[center] = new Vector3(0f, -0.5f, 0f);
            uv[tip] = new Vector2(0.5f, 1f);
            uv[center] = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * 0.5f;
                float z = Mathf.Sin(angle) * 0.5f;
                vertices[i] = new Vector3(x, -0.5f, z);
                vertices[segments + i] = vertices[i];
                uv[i] = new Vector2(i / (float)segments, 0f);
                uv[segments + i] = new Vector2(x + 0.5f, z + 0.5f);

                int next = (i + 1) % segments;
                int tri = i * 6;
                triangles[tri] = tip;
                triangles[tri + 1] = i;
                triangles[tri + 2] = next;
                triangles[tri + 3] = center;
                triangles[tri + 4] = segments + next;
                triangles[tri + 5] = segments + i;
            }

            Mesh mesh = new Mesh { name = "AssetForge Cone" };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreatePlaneMesh()
        {
            Mesh mesh = new Mesh { name = "AssetForge Plane" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, 0.5f),
                new Vector3(-0.5f, 0f, 0.5f)
            };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static GameObject CreateCustomMeshObject(string name, Mesh mesh, bool hidden)
        {
            mesh.hideFlags = HideFlags.HideAndDontSave;
            GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            MeshFilter filter = go.GetComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            return go;
        }

        private static void SetHideFlagsRecursive(GameObject root, HideFlags flags)
        {
            root.hideFlags = flags;
            foreach (Transform child in root.transform)
                SetHideFlagsRecursive(child.gameObject, flags);
        }
    }
}
