using System;
namespace ZeroGCargo.Core
{
    /// <summary>Docking bay and optional delivery dependency.</summary>
    [Serializable]
    public sealed class BayDefinition
    {
        /// <summary>Accepted cargo colour index.</summary>
        public int color;
        /// <summary>Bay position.</summary>
        public PointData position;
        /// <summary>Cargo index that must be delivered first, or -1 for unlocked.</summary>
        public int unlockAfter = -1;
    }
}
