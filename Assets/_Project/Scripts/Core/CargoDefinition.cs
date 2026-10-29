using System;
namespace ZeroGCargo.Core
{
    /// <summary>Shuttle identity and spawn position.</summary>
    [Serializable]
    public sealed class CargoDefinition
    {
        /// <summary>Cargo colour index, 0 through 3.</summary>
        public int color;
        /// <summary>Initial position.</summary>
        public PointData spawn;
    }
}
