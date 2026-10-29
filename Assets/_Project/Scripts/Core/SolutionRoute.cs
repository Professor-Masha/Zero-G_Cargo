using System;
namespace ZeroGCargo.Core
{
    /// <summary>Authored route fixture for one cargo.</summary>
    [Serializable]
    public sealed class SolutionRoute
    {
        /// <summary>Shuttle index.</summary>
        public int cargo;
        /// <summary>Control points, including spawn and bay.</summary>
        public PointData[] points;
    }
}
