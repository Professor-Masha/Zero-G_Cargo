using System;
namespace ZeroGCargo.Core
{
    /// <summary>Validates bounded data before any level is instantiated or simulated.</summary>
    public static class LevelValidation
    {
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Position(PointData p) => p.Vector.IsFinite && Math.Abs(p.x) <= 3 && Math.Abs(p.y) <= 3.5f;
        /// <summary>Returns a diagnostic instead of allowing malformed content to crash gameplay.</summary>
        public static string Error(LevelDefinition level)
        {
            if (level == null || level.id < 1 || level.id > 50 || string.IsNullOrWhiteSpace(level.title) || level.title.Length > 80) return "Invalid level identity";
            if (!Finite(level.timeLimit) || level.timeLimit < 0 || level.timeLimit > 600 || !Finite(level.drift) || level.drift < 0.05f || level.drift > 0.35f || !Finite(level.maxPathLength) || level.maxPathLength < 3 || level.maxPathLength > 10) return "Invalid level tuning";
            if (level.cargo == null || level.cargo.Length < 1 || level.cargo.Length > 4 || level.bays == null || level.bays.Length != level.cargo.Length || level.hazards == null || level.hazards.Length > 16 || level.solutions == null || level.solutions.Length != level.cargo.Length) return "Invalid object counts";
            for (int i = 0; i < level.cargo.Length; i++)
            {
                CargoDefinition cargo = level.cargo[i]; BayDefinition bay = level.bays[i]; SolutionRoute route = level.solutions[i];
                if (cargo == null || cargo.color < 0 || cargo.color > 3 || !Position(cargo.spawn) || bay == null || bay.color != cargo.color || !Position(bay.position) || bay.unlockAfter < -1 || bay.unlockAfter >= i) return "Invalid colour pair or chain dependency";
                if (route == null || route.cargo != i || route.points == null || route.points.Length < 2 || route.points.Length > 256) return "Invalid solution route";
                var path = new PathData(256);
                for (int k = 0; k < route.points.Length; k++)
                { if (!Position(route.points[k])) return "Non-finite route"; path.Add(route.points[k].Vector, level.maxPathLength, 0); }
                if ((path[0] - cargo.spawn.Vector).Magnitude > 0.01f || (path.EndPoint - bay.position.Vector).Magnitude > 0.01f) return "Guide endpoints or route budget invalid";
            }
            foreach (HazardDefinition h in level.hazards)
            {
                if (h == null || h.kind < 0 || h.kind > 2 || !Position(h.position) || !Finite(h.radius) || h.radius < 0.05f || h.radius > 3 || !Finite(h.strength) || h.strength < 0 || h.strength > 20 || !Finite(h.drift) || Math.Abs(h.drift) > 1 || !Finite(h.driftSpeed) || h.driftSpeed < 0 || h.driftSpeed > 5 || !Finite(h.halfWidth) || !Finite(h.halfHeight) || h.halfWidth <= 0 || h.halfHeight <= 0 || h.halfWidth > 2 || h.halfHeight > 2) return "Invalid hazard";
            }
            return null;
        }
    }
}
