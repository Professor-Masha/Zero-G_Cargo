using System;

namespace ZeroGCargo.Core
{
    /// <summary>Fixed-step manual inertia with lookahead, finite braking and endpoint drift.</summary>
    public sealed class InertiaPhysics
    {
        private PathData path;
        private FlightSettings settings;
        private float progress;
        private bool approaching;
        private Float2 stoppingPoint;
        private bool dockingApproach;
        /// <summary>Current world-space position.</summary>
        public Float2 Position { get; private set; }
        /// <summary>Current world-space velocity.</summary>
        public Float2 Velocity { get; private set; }
        /// <summary>Whether a flight is active.</summary>
        public bool IsMoving { get; private set; }
        /// <summary>Monotonic route progress for rendering.</summary>
        public float Progress => progress;
        /// <summary>Derived braking distance including the authored drift factor.</summary>
        public float BrakingDistance => Velocity.SqrMagnitude / (2 * settings.Braking) + settings.Drift * Velocity.Magnitude;
        /// <summary>Resets the simulation at spawn or collision recovery.</summary>
        public void Reset(Float2 position) { Position = position; Velocity = default; IsMoving = false; progress = 0; approaching = false; }
        /// <summary>Starts a route, retaining momentum when re-routed.</summary>
        public void Follow(PathData route, FlightSettings config, bool dockApproach = false)
        {
            if (route == null || route.Count < 2 || route.Length <= 0) throw new ArgumentException("A non-empty route is required.", nameof(route));
            path = route; settings = config; dockingApproach = dockApproach; progress = 0; approaching = false; IsMoving = true;
        }
        /// <summary>Integrates a single fixed step without heap allocations.</summary>
        public void Step(float dt, Float2 externalForce)
        {
            if (dt <= 0 || dt > 0.05f || float.IsNaN(dt) || !externalForce.IsFinite) throw new ArgumentOutOfRangeException(nameof(dt));
            if (!IsMoving) return;
            progress = path.ProjectDistance(Position, progress, settings.Lookahead * 3);
            Float2 toEnd = path.EndPoint - Position;
            float speed = Velocity.Magnitude;
            if (!approaching && progress >= path.Length - Math.Max(settings.Lookahead * 2, BrakingDistance + settings.Lookahead) && toEnd.Magnitude <= BrakingDistance)
            {
                approaching = true;
                Float2 finalTangent = (path.EndPoint - path[path.Count - 2]).Normalized;
                stoppingPoint = path.EndPoint + finalTangent * (dockingApproach ? 0 : settings.Drift * speed);
            }
            Float2 target = approaching ? stoppingPoint : path.GetPointAtDistance(progress + settings.Lookahead);
            Float2 delta = target - Position;
            bool braking = approaching && (delta.Magnitude <= speed * speed / (2 * settings.Braking) || Float2.Dot(delta, Velocity) < 0);
            if (approaching && dockingApproach)
            {
                float desiredSpeed = Math.Min(settings.MaxSpeed, (float)Math.Sqrt(2 * settings.Braking * delta.Magnitude));
                Float2 desiredVelocity = delta.Normalized * desiredSpeed;
                Velocity += Float2.Clamp(desiredVelocity - Velocity, settings.Braking * dt);
            }
            else if (braking)
                Velocity = Velocity.Normalized * Math.Max(0, speed - settings.Braking * dt);
            else
                Velocity += delta.Normalized * (settings.Acceleration * dt);
            Velocity += externalForce * dt;
            Velocity *= Math.Max(0, 1 - settings.Drag * dt);
            Velocity = Float2.Clamp(Velocity, settings.MaxSpeed);
            Position += Velocity * dt;
            if ((braking && !dockingApproach && Velocity.SqrMagnitude < 0.0001f) || (dockingApproach && approaching && delta.Magnitude < 0.03f && Velocity.SqrMagnitude < 0.01f)) { Velocity = default; IsMoving = false; }
        }
    }
}
