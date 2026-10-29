namespace ZeroGCargo.Core
{
    /// <summary>Preallocated simulation state for a single shuttle.</summary>
    public sealed class ShuttleState
    {
        /// <summary>Flight state.</summary>
        public InertiaPhysics Flight { get; } = new InertiaPhysics();
        /// <summary>Reusable flight route.</summary>
        public PathData Route { get; } = new PathData(256);
        /// <summary>0 ready, 1 flying, 2 recovering, 3 docking, 4 delivered.</summary>
        public int Phase { get; internal set; }
        /// <summary>Current position including animation.</summary>
        public Float2 Position { get; internal set; }
        /// <summary>Current display angle in radians.</summary>
        public float Angle { get; internal set; }
        /// <summary>Animation clock.</summary>
        public float Animation { get; internal set; }
        /// <summary>Animation origin.</summary>
        public Float2 AnimationStart { get; internal set; }
        internal uint NearMask;
        /// <summary>Whether the player may begin a new route.</summary>
        public bool CanDraw => Phase == 0;
    }
}
