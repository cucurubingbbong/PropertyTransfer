using UnityEngine;

namespace AssetForge
{
    public static class AF3DMath
    {
        public static float Snap(float value, float step)
        {
            if (step <= 0f) return value;
            return Mathf.Round(value / step) * step;
        }

        public static Vector3 Snap(Vector3 value, float step)
        {
            return new Vector3(Snap(value.x, step), Snap(value.y, step), Snap(value.z, step));
        }

        public static Vector3 SnapScale(Vector3 value, float step, float minMagnitude = 0.001f)
        {
            if (step <= 0f) return ClampScale(value, minMagnitude);
            Vector3 result = Snap(value, step);
            return ClampScale(result, minMagnitude);
        }

        public static Vector3 ClampScale(Vector3 value, float minMagnitude = 0.001f)
        {
            value.x = ClampSigned(value.x, minMagnitude);
            value.y = ClampSigned(value.y, minMagnitude);
            value.z = ClampSigned(value.z, minMagnitude);
            return value;
        }

        public static Vector3 NormalizeEuler(Vector3 euler)
        {
            euler.x = NormalizeAngle(euler.x);
            euler.y = NormalizeAngle(euler.y);
            euler.z = NormalizeAngle(euler.z);
            return euler;
        }

        public static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            if (angle < -180f) angle += 360f;
            return angle;
        }

        public static Quaternion CameraRotation(Vector2 orbit)
        {
            return Quaternion.Euler(orbit.x, orbit.y, 0f);
        }

        private static float ClampSigned(float value, float minMagnitude)
        {
            if (Mathf.Abs(value) >= minMagnitude) return value;
            return value < 0f ? -minMagnitude : minMagnitude;
        }
    }
}
