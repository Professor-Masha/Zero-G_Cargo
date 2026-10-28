using System;
using UnityEngine;
using ZeroGCargo.Core;
using ZeroGCargo.Data;

namespace ZeroGCargo.Gameplay
{
    /// <summary>Single-shuttle tutorial loop: fly, collide, recover, dock and complete.</summary>
    public sealed class LevelManager
    {
        private readonly InertiaPhysics physics = new InertiaPhysics();
        private readonly GameConfig config;
        private readonly InertiaConfig inertia;
        private readonly StationView view;
        private LevelData level;
        private Vector2 position;
        private Vector2 recoveryStart;
        private float animation;
        private float hintRemaining;
        private float angle;
        private Phase phase;
        private enum Phase { Ready, Flying, Recovering, Docking, Complete }
        /// <summary>Failed flight attempts in the current level.</summary>
        public int Attempts { get; private set; }
        /// <summary>Current shuttle location.</summary>
        public Vector2 Position => position;
        /// <summary>Whether a new path can start.</summary>
        public bool CanDraw => phase == Phase.Ready;
        /// <summary>Raised once per collision.</summary>
        public event Action OnShuttleCollided;
        /// <summary>Raised once when cargo reaches its matching bay.</summary>
        public event Action OnShuttleDocked;
        /// <summary>Raised after the docking animation with earned stars.</summary>
        public event Action<int> OnLevelComplete;
        /// <summary>Creates a level controller sharing reusable art and physics settings.</summary>
        public LevelManager(GameConfig config, InertiaConfig inertia, StationView view)
        { this.config = config; this.inertia = inertia; this.view = view; }
        /// <summary>Loads a validated level and resets attempts.</summary>
        public void Load(LevelData data)
        {
            if (data == null || !data.IsValid) throw new ArgumentException("Invalid tutorial level.", nameof(data));
            level = data; position = level.Spawn; physics.Reset(new Float2(position.x, position.y));
            Attempts = 0; phase = Phase.Ready; hintRemaining = 0; angle = 0;
            view.Load(level);
        }
        /// <summary>Launches a released input route.</summary>
        public void Follow(PathData path)
        {
            if (!CanDraw) return;
            bool dockApproach = (path.EndPoint - new Float2(level.Bay.x, level.Bay.y)).Magnitude <= config.DockRadius;
            physics.Follow(path, inertia.ToSettings(config.Lookahead), dockApproach); phase = Phase.Flying;
            hintRemaining = 0; view.ShowHint(level.GhostPath, false);
        }
        /// <summary>Advances deterministic simulation and feedback on the Unity fixed step.</summary>
        public void Step(float dt)
        {
            if (level == null) return;
            if (hintRemaining > 0)
            {
                hintRemaining -= dt;
                if (hintRemaining <= 0) view.ShowHint(level.GhostPath, false);
            }
            if (phase == Phase.Recovering)
            {
                animation += dt; float t = Mathf.Clamp01(animation / config.CollisionDuration);
                float ease = t * t * (3 - 2 * t);
                position = Vector2.Lerp(recoveryStart, level.Spawn, ease);
                view.Move(position, angle + 360 * ease);
                if (t >= 1)
                {
                    physics.Reset(new Float2(position.x, position.y)); phase = Phase.Ready; angle = 0; view.Move(position, angle);
                    if (Attempts >= config.HintAfterFailures) { hintRemaining = config.HintDuration; view.ShowHint(level.GhostPath, true); }
                }
                return;
            }
            if (phase == Phase.Docking)
            {
                animation += dt; float t = Mathf.Clamp01(animation / config.DockingDuration);
                position = Vector2.Lerp(recoveryStart, level.Bay, t * (2 - t)); view.Move(position, angle, 1 - t);
                if (t >= 1) { phase = Phase.Complete; OnLevelComplete?.Invoke(Attempts == 0 ? 3 : Attempts <= 2 ? 2 : 1); }
                return;
            }
            if (phase != Phase.Flying) return;
            Float2 previous = physics.Position;
            physics.Step(dt, default); position = new Vector2(physics.Position.X, physics.Position.Y);
            if (physics.Velocity.SqrMagnitude > 0.001f) angle = Mathf.Atan2(physics.Velocity.Y, physics.Velocity.X) * Mathf.Rad2Deg;
            view.Move(position, angle);
            if (CollisionMath.SegmentCircle(previous, physics.Position, new Float2(level.Meteorite.x, level.Meteorite.y), level.MeteoriteRadius + config.ShuttleRadius)
                || Mathf.Abs(position.x) > config.BoundaryX || Mathf.Abs(position.y) > config.BoundaryY)
            { Collide(); return; }
            int docking = DockingRules.Evaluate(physics.Position, new Float2(level.Bay.x, level.Bay.y), config.DockRadius, physics.Velocity.Magnitude, config.DockingSpeed, (int)level.Cargo, (int)level.BayCargo);
            if (docking < 0) { Collide(); return; }
            if (docking > 0)
            { phase = Phase.Docking; animation = 0; recoveryStart = position; OnShuttleDocked?.Invoke(); return; }
            if (!physics.IsMoving)
            {
                // A short route is not a failure: pick up the stationary shuttle and continue.
                phase = Phase.Ready;
            }
        }
        private void Collide()
        {
            Attempts++; phase = Phase.Recovering; animation = 0; recoveryStart = position;
            OnShuttleCollided?.Invoke();
        }
        /// <summary>Updates the shared route display without creating objects.</summary>
        public void DrawPath(PathData path) => view.DrawPath(path, phase == Phase.Flying ? physics.Progress : 0);
    }
}
