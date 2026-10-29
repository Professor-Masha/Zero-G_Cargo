using System;

namespace ZeroGCargo.Core
{
    /// <summary>Validated, portable level layout and simulation tuning.</summary>
    [Serializable]
    public sealed class LevelDefinition
    {
        /// <summary>One-based campaign identity.</summary>
        public int id;
        /// <summary>Level title.</summary>
        public string title;
        /// <summary>Zero disables the timer for training levels.</summary>
        public float timeLimit;
        /// <summary>Per-level authored drift.</summary>
        public float drift = 0.15f;
        /// <summary>Maximum path budget.</summary>
        public float maxPathLength = 6;
        /// <summary>Cargo launch points.</summary>
        public CargoDefinition[] cargo;
        /// <summary>Colour-specific bays.</summary>
        public BayDefinition[] bays;
        /// <summary>Obstacle and force-field data.</summary>
        public HazardDefinition[] hazards;
        /// <summary>One tested route per cargo, also used for training hints.</summary>
        public SolutionRoute[] solutions;
    }
}
