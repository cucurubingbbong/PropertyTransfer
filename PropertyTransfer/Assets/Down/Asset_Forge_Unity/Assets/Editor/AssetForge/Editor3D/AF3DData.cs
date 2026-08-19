using System;
using System.Collections.Generic;
using UnityEngine;

namespace AssetForge
{
    public enum AF3DPrimitiveType
    {
        Cube,
        Sphere,
        Capsule,
        Cylinder,
        Plane,
        Cone
    }

    public enum AF3DTransformTool
    {
        Move,
        Rotate,
        Scale
    }

    [Serializable]
    public sealed class AF3DPart
    {
        public string id = Guid.NewGuid().ToString("N");
        public string name = "Part";
        public AF3DPrimitiveType primitiveType = AF3DPrimitiveType.Cube;
        public Vector3 position = Vector3.zero;
        public Vector3 rotation = Vector3.zero;
        public Vector3 scale = Vector3.one;
        public bool visible = true;
        public bool locked;

        public Color color = new Color(0.72f, 0.76f, 0.84f, 1f);
        [Range(0f, 1f)] public float metallic;
        [Range(0f, 1f)] public float smoothness = 0.45f;
        public bool emissionEnabled;
        public Color emissionColor = Color.black;

        public AF3DPart Clone()
        {
            AF3DPart clone = JsonUtility.FromJson<AF3DPart>(JsonUtility.ToJson(this));
            clone.id = Guid.NewGuid().ToString("N");
            clone.name = name + " Copy";
            clone.position += new Vector3(0.5f, 0f, 0.5f);
            return clone;
        }
    }

    [Serializable]
    public sealed class AF3DCameraState
    {
        public Vector2 orbit = new Vector2(22f, -35f);
        public Vector3 pivot = Vector3.zero;
        [Min(0.1f)] public float distance = 8f;
        public bool orthographic;
        [Min(0.05f)] public float orthographicSize = 4f;
        public Color background = new Color(0.105f, 0.11f, 0.125f, 1f);
    }

    [Serializable]
    public sealed class AF3DDocumentData
    {
        public string modelName = "New Model";
        public List<AF3DPart> parts = new List<AF3DPart>();
        public AF3DCameraState camera = new AF3DCameraState();

        public bool showGrid = true;
        public bool positionSnapEnabled;
        [Min(0.001f)] public float positionSnap = 0.25f;
        public bool rotationSnapEnabled = true;
        [Min(0.1f)] public float rotationSnap = 15f;
        public bool scaleSnapEnabled;
        [Min(0.001f)] public float scaleSnap = 0.1f;
        public bool localHandle;

        [Min(16)] public int pngWidth = 512;
        [Min(16)] public int pngHeight = 512;
    }

    public sealed class AF3DDocument : ScriptableObject
    {
        public AF3DDocumentData data = new AF3DDocumentData();
    }
}
