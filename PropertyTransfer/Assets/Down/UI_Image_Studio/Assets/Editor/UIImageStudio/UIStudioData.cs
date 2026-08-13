using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIImageStudio
{
    public enum UIStudioLayerType
    {
        Shape,
        Text,
        Image
    }

    public enum UIStudioTextAlignment
    {
        Left,
        Center,
        Right
    }

    public enum UIStudioShapeType
    {
        Rectangle,
        RoundedRectangle,
        Pill,
        Ellipse,
        Diamond,
        Triangle
    }

    [Serializable]
    public sealed class UIStudioLayer
    {
        public string id = Guid.NewGuid().ToString("N");
        public string name = "Layer";
        public UIStudioLayerType type = UIStudioLayerType.Shape;

        // Canvas-space pixels. Origin is the canvas top-left.
        public Rect rect = new Rect(100f, 100f, 240f, 120f);
        public float rotation;
        [Range(0f, 1f)] public float opacity = 1f;
        public bool visible = true;
        public bool locked;

        // Shape
        public UIStudioShapeType shapeType = UIStudioShapeType.RoundedRectangle;
        public Color fillColor = new Color(0.18f, 0.42f, 0.88f, 1f);
        public Color strokeColor = Color.white;
        [Min(0f)] public float strokeWidth;
        [Min(0f)] public float cornerRadius = 16f;
        public bool shadowEnabled;
        public Color shadowColor = new Color(0f, 0f, 0f, 0.35f);
        public Vector2 shadowOffset = new Vector2(0f, 6f);
        [Range(0f, 64f)] public float shadowBlur = 10f;

        // Text
        [TextArea(1, 6)] public string text = "텍스트";
        public string fontPath = string.Empty;
        [Min(1)] public int fontSize = 48;
        public FontStyle fontStyle = FontStyle.Normal;
        public UIStudioTextAlignment textAlignment = UIStudioTextAlignment.Center;
        public Color textColor = Color.white;

        // Image
        public string assetPath = string.Empty;
        public string subAssetName = string.Empty;
        public bool assetIsSprite;
        public bool preserveAspect = true;
        public Color imageTint = Color.white;

        public UIStudioLayer Clone()
        {
            UIStudioLayer clone = JsonUtility.FromJson<UIStudioLayer>(JsonUtility.ToJson(this));
            clone.id = Guid.NewGuid().ToString("N");
            clone.name = name + " Copy";
            clone.rect.position += new Vector2(16f, 16f);
            return clone;
        }
    }

    [Serializable]
    public sealed class UIStudioDocumentData
    {
        [Min(1)] public int canvasWidth = 1920;
        [Min(1)] public int canvasHeight = 1080;
        public Color canvasBackground = new Color(0f, 0f, 0f, 0f);
        [Min(1f)] public float gridSize = 32f;
        public bool showGrid = true;
        public bool snapEnabled = true;
        public bool smartGuidesEnabled = true;
        public bool lightweightPreview = true;
        public List<UIStudioLayer> layers = new List<UIStudioLayer>();
    }

    public sealed class UIStudioDocument : ScriptableObject
    {
        public UIStudioDocumentData data = new UIStudioDocumentData();
    }
}
