namespace ZeroGCargo.Core
{
    /// <summary>Engine-independent colour and speed gating for docking.</summary>
    public static class DockingRules
    {
        /// <summary>Returns zero outside capture, one for a match, or minus one for a wrong bay.</summary>
        public static int Evaluate(Float2 position, Float2 bay, float radius, float speed, float maxSpeed, int cargo, int bayCargo)
        {
            if ((position - bay).SqrMagnitude > radius * radius) return 0;
            if (cargo != bayCargo) return -1;
            return speed <= maxSpeed ? 1 : 0;
        }
    }
}
