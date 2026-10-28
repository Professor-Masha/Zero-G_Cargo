using System;

namespace ZeroGCargo.Core
{
    /// <summary>Immutable, validated simulation parameters copied from an InertiaConfig asset.</summary>
    public readonly struct FlightSettings
    {
        /// <summary>Acceleration in world units per second squared.</summary>
        public float Acceleration { get; }
        /// <summary>Maximum velocity magnitude.</summary>
        public float MaxSpeed { get; }
        /// <summary>Linear damping coefficient.</summary>
        public float Drag { get; }
        /// <summary>Braking deceleration.</summary>
        public float Braking { get; }
        /// <summary>Endpoint drift distance per unit of velocity.</summary>
        public float Drift { get; }
        /// <summary>Steering lookahead distance.</summary>
        public float Lookahead { get; }
        /// <summary>Creates finite positive parameters.</summary>
        public FlightSettings(float acceleration, float maxSpeed, float drag, float braking, float drift, float lookahead)
        {
            if (!PositiveFinite(acceleration) || !PositiveFinite(maxSpeed) || !PositiveFinite(drag) || !PositiveFinite(braking) || !PositiveFinite(drift) || !PositiveFinite(lookahead))
                throw new ArgumentOutOfRangeException(nameof(acceleration));
            Acceleration = acceleration; MaxSpeed = maxSpeed; Drag = drag; Braking = braking; Drift = drift; Lookahead = lookahead;
        }
        private static bool PositiveFinite(float value) => value > 0 && !float.IsInfinity(value) && !float.IsNaN(value);
    }
}
