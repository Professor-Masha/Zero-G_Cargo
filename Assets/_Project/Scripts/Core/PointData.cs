using System;
namespace ZeroGCargo.Core
{
    /// <summary>JSON-compatible finite point.</summary>
    [Serializable]
    public struct PointData
    {
        /// <summary>Horizontal position.</summary>
        public float x;
        /// <summary>Vertical position.</summary>
        public float y;
        /// <summary>Converts to simulation coordinates.</summary>
        public Float2 Vector => new Float2(x, y);
    }
}
