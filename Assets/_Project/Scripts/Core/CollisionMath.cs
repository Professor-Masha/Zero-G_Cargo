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
    }
}
