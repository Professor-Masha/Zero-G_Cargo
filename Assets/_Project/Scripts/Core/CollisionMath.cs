using System;

namespace ZeroGCargo.Core
{
    /// <summary>Swept collision queries that prevent tunnelling at low frame rates.</summary>
    public static class CollisionMath
    {
        /// <summary>Tests whether a swept circle intersects another circle.</summary>
        public static bool SegmentCircle(Float2 start, Float2 end, Float2 center, float radius)
        {
            Float2 edge = end - start;
            float t = edge.SqrMagnitude < 0.000001f ? 0 : Math.Max(0, Math.Min(1, Float2.Dot(center - start, edge) / edge.SqrMagnitude));
            return (center - (start + edge * t)).SqrMagnitude <= radius * radius;
        }
        /// <summary>Tests a swept point against an expanded axis-aligned barrier.</summary>
        public static bool SegmentBox(Float2 start, Float2 end, Float2 center, float halfWidth, float halfHeight)
        {
            Float2 d = end - start; float near = 0, far = 1;
            if (!Slab(start.X, d.X, center.X - halfWidth, center.X + halfWidth, ref near, ref far)) return false;
            return Slab(start.Y, d.Y, center.Y - halfHeight, center.Y + halfHeight, ref near, ref far);
        }
        private static bool Slab(float position, float direction, float min, float max, ref float near, ref float far)
        {
            if (Math.Abs(direction) < 0.000001f) return position >= min && position <= max;
            float a = (min - position) / direction, b = (max - position) / direction;
            if (a > b) { float swap = a; a = b; b = swap; }
            near = Math.Max(near, a); far = Math.Min(far, b); return near <= far;
        }
    }
}
