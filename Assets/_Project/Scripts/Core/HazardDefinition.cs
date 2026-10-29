using System;
namespace ZeroGCargo.Core
{
    /// <summary>Bounded obstacle/force parameters; no scripts or asset paths.</summary>
    [Serializable]
    public sealed class HazardDefinition
    {
        /// <summary>0 meteorite, 1 energy barrier, 2 gravity well.</summary>
        public int kind;
        /// <summary>Base position.</summary>
        public PointData position;
        /// <summary>Collision radius or field radius.</summary>
        public float radius = 0.4f;
        /// <summary>Barrier horizontal half-size.</summary>
        public float halfWidth = 0.5f;
        /// <summary>Barrier vertical half-size.</summary>
        public float halfHeight = 0.08f;
        /// <summary>Field acceleration strength.</summary>
        public float strength = 1;
        /// <summary>Horizontal meteorite drift amplitude.</summary>
        public float drift;
        /// <summary>Drift angular speed.</summary>
        public float driftSpeed = 0.5f;
        /// <summary>Deterministic current obstacle location.</summary>
        public Float2 At(float elapsed) => position.Vector + new Float2((float)Math.Sin(elapsed * driftSpeed) * drift, 0);
        /// <summary>External acceleration at a shuttle's location.</summary>
        public Float2 ForceAt(Float2 point, float elapsed)
        {
            if (kind == 0) return default;
            Float2 center = At(elapsed), delta = center - point;
            if (kind == 1)
            {
                Float2 nearest = new Float2(Math.Max(center.X - halfWidth, Math.Min(center.X + halfWidth, point.X)), Math.Max(center.Y - halfHeight, Math.Min(center.Y + halfHeight, point.Y)));
                delta = point - nearest;
            }
            float distance = delta.Magnitude;
            return distance >= radius ? default : delta.Normalized * (strength * (1 - distance / radius));
        }
    }
}
