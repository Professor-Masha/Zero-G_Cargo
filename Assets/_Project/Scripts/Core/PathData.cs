using System;

namespace ZeroGCargo.Core
{
    /// <summary>A reusable, length-limited polyline with arc-length queries.</summary>
    public sealed class PathData
    {
        private readonly Float2[] points;
        private readonly float[] distances;
        /// <summary>Number of points currently stored.</summary>
        public int Count { get; private set; }
        /// <summary>Total arc length in world units.</summary>
        public float Length => Count == 0 ? 0 : distances[Count - 1];
        /// <summary>Last point or zero for an empty path.</summary>
        public Float2 EndPoint => Count == 0 ? default : points[Count - 1];
        /// <summary>Creates fixed-capacity storage, allocated once per shuttle.</summary>
        public PathData(int capacity)
        {
            if (capacity < 2) throw new ArgumentOutOfRangeException(nameof(capacity));
            points = new Float2[capacity]; distances = new float[capacity];
        }
        /// <summary>Clears an existing route without allocating.</summary>
        public void Clear() { Count = 0; }
        /// <summary>Reads a stored point.</summary>
        public Float2 this[int index] => index >= 0 && index < Count ? points[index] : throw new ArgumentOutOfRangeException(nameof(index));
        /// <summary>Adds a point, clipping the final segment at the route budget.</summary>
        public bool Add(Float2 point, float maxLength, float spacing)
        {
            if (!point.IsFinite || maxLength <= 0 || spacing < 0 || Count == points.Length) return false;
            float segment = Count == 0 ? 0 : (point - EndPoint).Magnitude;
            if (Count > 0 && (segment < spacing || segment < 0.00001f || Length >= maxLength)) return false;
            float remaining = Math.Max(0, maxLength - Length);
            if (segment > remaining) { point = EndPoint + (point - EndPoint).Normalized * remaining; segment = remaining; }
            distances[Count] = Length + segment; points[Count++] = point;
            return true;
        }
        /// <summary>Samples a point at a clamped distance along the route.</summary>
        public Float2 GetPointAtDistance(float distance)
        {
            if (Count == 0) return default;
            if (distance <= 0) return points[0];
            for (int i = 1; i < Count; i++)
                if (distances[i] >= distance)
                    return Float2.Lerp(points[i - 1], points[i], (distance - distances[i - 1]) / (distances[i] - distances[i - 1]));
            return EndPoint;
        }
        /// <summary>Projects onto a local forward window; avoids jumping across looping routes.</summary>
        public float ProjectDistance(Float2 position, float progress, float window)
        {
            float nearest = float.MaxValue, result = progress;
            for (int i = 1; i < Count; i++)
            {
                if (distances[i] < progress) continue;
                if (distances[i - 1] > progress + window) break;
                Float2 edge = points[i] - points[i - 1];
                float t = Math.Max(0, Math.Min(1, Float2.Dot(position - points[i - 1], edge) / edge.SqrMagnitude));
                float along = distances[i - 1] + t * (distances[i] - distances[i - 1]);
                if (along < progress || along > progress + window) continue;
                float sqr = (position - (points[i - 1] + edge * t)).SqrMagnitude;
                if (sqr < nearest) { nearest = sqr; result = along; }
            }
            return result;
        }
    }
}
