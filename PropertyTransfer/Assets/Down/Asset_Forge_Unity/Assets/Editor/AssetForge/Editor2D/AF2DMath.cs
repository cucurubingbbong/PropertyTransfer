using UnityEngine;

namespace AssetForge
{
    public static class AF2DMath
    {
        public static float Snap(float value, float step)
        {
            if (step <= 0f) return value;
            return Mathf.Round(value / step) * step;
        }

        public static Rect SnapPosition(Rect rect, float step)
        {
            rect.x = Snap(rect.x, step);
            rect.y = Snap(rect.y, step);
            return rect;
        }

        public static Rect ClampToCanvas(Rect rect, Vector2 canvasSize, float minSize = 8f)
        {
            rect.width = Mathf.Clamp(rect.width, minSize, Mathf.Max(minSize, canvasSize.x));
            rect.height = Mathf.Clamp(rect.height, minSize, Mathf.Max(minSize, canvasSize.y));
            rect.x = Mathf.Clamp(rect.x, -rect.width + minSize, canvasSize.x - minSize);
            rect.y = Mathf.Clamp(rect.y, -rect.height + minSize, canvasSize.y - minSize);
            return rect;
        }

        public static Rect CanvasToScreenRect(Rect canvasRect, Rect layerRect, float zoom)
        {
            return new Rect(
                canvasRect.x + layerRect.x * zoom,
                canvasRect.y + layerRect.y * zoom,
                layerRect.width * zoom,
                layerRect.height * zoom);
        }

        public static Vector2 ScreenToCanvasPoint(Rect canvasRect, Vector2 screenPoint, float zoom)
        {
            if (zoom <= 0f) return Vector2.zero;
            return (screenPoint - canvasRect.position) / zoom;
        }

        public static Rect FitRect(Rect viewport, Vector2 canvasSize, float padding, out float zoom)
        {
            float availableWidth = Mathf.Max(1f, viewport.width - padding * 2f);
            float availableHeight = Mathf.Max(1f, viewport.height - padding * 2f);
            zoom = Mathf.Min(availableWidth / canvasSize.x, availableHeight / canvasSize.y);
            zoom = Mathf.Clamp(zoom, 0.05f, 4f);

            Vector2 size = canvasSize * zoom;
            Vector2 position = viewport.center - size * 0.5f;
            return new Rect(position, size);
        }

        public static bool Nearly(float a, float b, float threshold)
        {
            return Mathf.Abs(a - b) <= threshold;
        }
    }
}
