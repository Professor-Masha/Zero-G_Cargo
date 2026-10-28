using UnityEngine;
using ZeroGCargo.Core;

namespace ZeroGCargo.Data
{
    /// <summary>Authored inertia defaults and permitted tuning ranges.</summary>
    [CreateAssetMenu(menuName = "Zero-G Cargo/Inertia Config")]
    public sealed class InertiaConfig : ScriptableObject
    {
        [SerializeField, Range(4, 15)] private float acceleration = 8;
        [SerializeField, Range(4, 10)] private float maxSpeed = 6;
        [SerializeField, Range(0.1f, 1)] private float dragCoefficient = 0.4f;
        [SerializeField, Range(8, 20)] private float brakingStrength = 12;
        [SerializeField, Range(0.05f, 0.35f)] private float driftCoefficient = 0.15f;
        [SerializeField, Range(3, 10)] private float maxPathLength = 6;
        /// <summary>Maximum route length.</summary>
        public float MaxPathLength => Mathf.Clamp(maxPathLength, 3, 10);
        /// <summary>Copies authored values to the pure simulation.</summary>
        public FlightSettings ToSettings(float lookahead) => new FlightSettings(Mathf.Clamp(acceleration, 4, 15), Mathf.Clamp(maxSpeed, 4, 10), Mathf.Clamp(dragCoefficient, 0.1f, 1), Mathf.Clamp(brakingStrength, 8, 20), Mathf.Clamp(driftCoefficient, 0.05f, 0.35f), lookahead);
    }
}
