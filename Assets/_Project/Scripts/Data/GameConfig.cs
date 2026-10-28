using UnityEngine;

namespace ZeroGCargo.Data
{
    /// <summary>Prototype input, feedback and presentation tuning.</summary>
    [CreateAssetMenu(menuName = "Zero-G Cargo/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [SerializeField, Min(16)] private int pathCapacity = 256;
        [SerializeField, Min(0.01f)] private float pointSpacing = 0.04f;
        [SerializeField, Min(0.01f)] private float minimumPathLength = 0.15f;
        [SerializeField, Min(0.05f)] private float pickupRadius = 0.48f;
        [SerializeField, Min(0.05f)] private float lookahead = 0.3f;
        [SerializeField, Min(0.01f)] private float shuttleRadius = 0.16f;
        [SerializeField, Min(0.05f)] private float dockRadius = 0.48f;
        [SerializeField, Min(0.05f)] private float dockingSpeed = 1.8f;
        [SerializeField, Min(0.05f)] private float collisionDuration = 1.2f;
        [SerializeField, Min(0.05f)] private float dockingDuration = 0.3f;
        [SerializeField, Min(0.01f)] private float lineWidth = 0.025f;
        [SerializeField, Min(1)] private int hintAfterFailures = 2;
        [SerializeField, Min(0.1f)] private float hintDuration = 3;
        [SerializeField, Min(0.1f)] private float cameraSize = 5.8f;
        [SerializeField, Min(0.1f)] private float boundaryX = 3;
        [SerializeField, Min(0.1f)] private float boundaryY = 3.5f;
        /// <summary>Preallocated route point budget.</summary>
        public int PathCapacity => Mathf.Max(16, pathCapacity);
        /// <summary>Minimum input sample spacing.</summary>
        public float PointSpacing => pointSpacing;
        /// <summary>Minimum accepted route length.</summary>
        public float MinimumPathLength => minimumPathLength;
        /// <summary>Touch pickup radius.</summary>
        public float PickupRadius => pickupRadius;
        /// <summary>Path steering lookahead.</summary>
        public float Lookahead => lookahead;
        /// <summary>Collision radius.</summary>
        public float ShuttleRadius => shuttleRadius;
        /// <summary>Docking capture radius.</summary>
        public float DockRadius => dockRadius;
        /// <summary>Maximum safe docking speed.</summary>
        public float DockingSpeed => dockingSpeed;
        /// <summary>Collision recovery seconds.</summary>
        public float CollisionDuration => collisionDuration;
        /// <summary>Docking ease seconds.</summary>
        public float DockingDuration => dockingDuration;
        /// <summary>Route stroke width.</summary>
        public float LineWidth => lineWidth;
        /// <summary>Failures before a hint.</summary>
        public int HintAfterFailures => hintAfterFailures;
        /// <summary>Hint visibility seconds.</summary>
        public float HintDuration => hintDuration;
        /// <summary>Portrait camera half-height.</summary>
        public float CameraSize => cameraSize;
        /// <summary>Horizontal station limit.</summary>
        public float BoundaryX => boundaryX;
        /// <summary>Vertical station limit.</summary>
        public float BoundaryY => boundaryY;
    }
}
