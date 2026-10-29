using System;
namespace ZeroGCargo.Core
{
    /// <summary>Complete offline level simulation shared by Unity and campaign solvability checks.</summary>
    public sealed class CampaignSimulation
    {
        private readonly ShuttleState[] states = { new ShuttleState(), new ShuttleState(), new ShuttleState(), new ShuttleState() };
        private LevelDefinition level;
        private readonly FlightSettings defaults;
        private float hintTime;
        /// <summary>Loaded level definition.</summary>
        public LevelDefinition Level => level;
        /// <summary>Simulation time, frozen when Step is not called.</summary>
        public float Elapsed { get; private set; }
        /// <summary>Number of collisions.</summary>
        public int Attempts { get; private set; }
        /// <summary>Delivered cargo count.</summary>
        public int Delivered { get; private set; }
        /// <summary>0 running, 1 won, 2 timed out.</summary>
        public int Outcome { get; private set; }
        /// <summary>Remaining seconds, or zero for untimed levels.</summary>
        public float Remaining => level.timeLimit <= 0 ? 0 : Math.Max(0, level.timeLimit - Elapsed);
        /// <summary>Fraction of timer consumed.</summary>
        public float TimerFraction => level.timeLimit <= 0 ? 0 : Math.Min(1, Elapsed / level.timeLimit);
        /// <summary>Whether the early-level guide is visible.</summary>
        public bool HintVisible => hintTime > 0;
        /// <summary>Collision feedback.</summary>
        public event Action<int> Collided;
        /// <summary>Near-miss feedback, once per shuttle/hazard/flight.</summary>
        public event Action<int> NearMiss;
        /// <summary>Delivery feedback.</summary>
        public event Action<int> Docked;
        /// <summary>Win or fail transition, emitted once.</summary>
        public event Action<int> Finished;
        /// <summary>Creates fixed storage and default tuning.</summary>
        public CampaignSimulation(FlightSettings settings) { defaults = settings; }
        /// <summary>Accesses one active shuttle.</summary>
        public ShuttleState Shuttle(int index) => index >= 0 && index < level.cargo.Length ? states[index] : throw new ArgumentOutOfRangeException(nameof(index));
        /// <summary>Whether a chain-delivery bay is unlocked.</summary>
        public bool BayUnlocked(int index) => level.bays[index].unlockAfter < 0 || states[level.bays[index].unlockAfter].Phase == 4;
        /// <summary>Validates and loads an immutable level resource.</summary>
        public void Load(LevelDefinition data)
        {
            string error = LevelValidation.Error(data); if (error != null) throw new ArgumentException(error, nameof(data));
            level = data; Elapsed = 0; Attempts = 0; Delivered = 0; Outcome = 0; hintTime = 0;
            for (int i = 0; i < data.cargo.Length; i++)
            {
                var s = states[i]; s.Phase = 0; s.Position = data.cargo[i].spawn.Vector; s.Angle = 0; s.Animation = 0; s.Route.Clear(); s.Flight.Reset(s.Position);
            }
        }
        /// <summary>Copies a route into shuttle-owned storage and launches it once.</summary>
        public bool Launch(int index, PathData path)
        {
            if (level == null || Outcome != 0 || index < 0 || index >= level.cargo.Length || states[index].Phase != 0 || path == null || path.Count < 2 || path.Length < 0.15f || path.Length > level.maxPathLength + 0.001f) return false;
            var s = states[index]; if ((path[0] - s.Position).Magnitude > 0.5f) return false;
            s.NearMask = 0; s.Route.Clear(); for (int k = 0; k < path.Count; k++) s.Route.Add(path[k], level.maxPathLength, 0);
            bool approachingBay = false;
            for (int b = 0; b < level.bays.Length; b++) if (BayUnlocked(b) && level.bays[b].color == level.cargo[index].color && (path.EndPoint - level.bays[b].position.Vector).Magnitude <= 0.48f) approachingBay = true;
            var settings = new FlightSettings(defaults.Acceleration, defaults.MaxSpeed, defaults.Drag, defaults.Braking, level.drift, defaults.Lookahead);
            s.Flight.Follow(s.Route, settings, approachingBay); s.Phase = 1; return true;
        }
        private void Crash(int index)
        {
            var s = states[index]; if (s.Phase != 1) return;
            s.Phase = 2; s.Animation = 0; s.AnimationStart = s.Position; s.Route.Clear(); Attempts++; Collided?.Invoke(index);
        }
        /// <summary>Advances one bounded fixed simulation tick; never call while paused.</summary>
        public void Step(float dt)
        {
            if (dt <= 0 || dt > 0.05f || float.IsNaN(dt)) throw new ArgumentOutOfRangeException(nameof(dt));
            if (level == null || Outcome != 0) return;
            Elapsed += dt; hintTime = Math.Max(0, hintTime - dt);
            if (level.timeLimit > 0 && Remaining <= 0) { Outcome = 2; Finished?.Invoke(2); return; }
            for (int i = 0; i < level.cargo.Length; i++)
            {
                var s = states[i];
                if (s.Phase == 2)
                {
                    s.Animation += dt; float t = Math.Min(1, s.Animation / 1.2f); float ease = t * t * (3 - 2 * t);
                    s.Position = Float2.Lerp(s.AnimationStart, level.cargo[i].spawn.Vector, ease); s.Angle = ease * (float)Math.PI * 2;
                    if (t >= 1) { s.Phase = 0; s.Angle = 0; s.Flight.Reset(s.Position); if (level.id <= 10 && Attempts >= 2) hintTime = 3; }
                    continue;
                }
                if (s.Phase == 3)
                {
                    s.Animation += dt; float t = Math.Min(1, s.Animation / 0.3f);
                    s.Position = Float2.Lerp(s.AnimationStart, level.bays[i].position.Vector, t * (2 - t));
                    if (t >= 1) { s.Phase = 4; Delivered++; if (Delivered == level.cargo.Length) { Outcome = 1; Finished?.Invoke(1); } }
                    continue;
                }
                if (s.Phase != 1) continue;
                Float2 external = default; foreach (var h in level.hazards) external += h.ForceAt(s.Position, Elapsed);
                Float2 previous = s.Position; s.Flight.Step(dt, external); s.Position = s.Flight.Position;
                if (s.Flight.Velocity.SqrMagnitude > 0.001f) s.Angle = (float)Math.Atan2(s.Flight.Velocity.Y, s.Flight.Velocity.X);
                if (Math.Abs(s.Position.X) > 3 || Math.Abs(s.Position.Y) > 3.5f) { Crash(i); continue; }
                foreach (var h in level.hazards)
                {
                    Float2 center = h.At(Elapsed);
                    bool hit = h.kind == 0 ? CollisionMath.SegmentCircle(previous, s.Position, center, h.radius + 0.16f) : h.kind == 1 && CollisionMath.SegmentBox(previous, s.Position, center, h.halfWidth + 0.16f, h.halfHeight + 0.16f);
                    if (hit) { Crash(i); break; }
                }
                if (s.Phase != 1) continue;
                for (int h = 0; h < level.hazards.Length; h++)
                {
                    var obstacle = level.hazards[h]; if (obstacle.kind != 0 || (s.NearMask & (1u << h)) != 0) continue;
                    float distance = (s.Position-obstacle.At(Elapsed)).Magnitude;
                    if (distance < obstacle.radius+.40f && distance > obstacle.radius+.16f) { s.NearMask |= 1u << h; NearMiss?.Invoke(i); }
                }
                for (int other = 0; other < i; other++)
                    if (states[other].Phase == 1 && CollisionMath.SegmentCircle(previous,s.Position,states[other].Position,.32f)) { Crash(i); Crash(other); break; }
                if (s.Phase != 1) continue;
                for (int b = 0; b < level.bays.Length; b++)
                {
                    if (!BayUnlocked(b)) continue;
                    int docking = DockingRules.Evaluate(s.Position, level.bays[b].position.Vector, 0.48f, s.Flight.Velocity.Magnitude, 1.8f, level.cargo[i].color, level.bays[b].color);
                    if (docking < 0) { Crash(i); break; }
                    if (docking > 0 && b == i) { s.Phase = 3; s.Animation = 0; s.AnimationStart = s.Position; s.Route.Clear(); Docked?.Invoke(i); break; }
                }
                if (s.Phase == 1 && !s.Flight.IsMoving) s.Phase = 0;
            }
        }
        /// <summary>Time/attempt-based star award, only after a completed delivery set.</summary>
        public int Stars => Outcome != 1 ? 0 : Attempts == 0 && (level.timeLimit <= 0 || Remaining >= level.timeLimit * 0.5f) ? 3 : Attempts <= 2 ? 2 : 1;
    }
}
