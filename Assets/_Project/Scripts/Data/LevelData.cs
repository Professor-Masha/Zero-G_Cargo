using System;
using UnityEngine;

namespace ZeroGCargo.Data
{
    /// <summary>An authored tutorial layout, with explicit solution guide points.</summary>
    [CreateAssetMenu(menuName = "Zero-G Cargo/Level")]
    public sealed class LevelData : ScriptableObject
    {
        [SerializeField] private int levelId = 1;
        [SerializeField] private string title = "First shift";
        [SerializeField] private CargoColor cargo;
        [SerializeField] private CargoColor bayCargo;
        [SerializeField] private Vector2 spawn = new Vector2(-1.8f, -1.5f);
        [SerializeField] private Vector2 bay = new Vector2(1.8f, 1.5f);
        [SerializeField] private Vector2 meteorite = new Vector2(1.8f, -1.5f);
        [SerializeField, Min(0.01f)] private float meteoriteRadius = 0.42f;
        [SerializeField] private Vector2[] ghostPath = Array.Empty<Vector2>();
        /// <summary>One-based level identifier.</summary>
        public int LevelId => levelId;
        /// <summary>Display title.</summary>
        public string Title => title;
        /// <summary>Required cargo identity.</summary>
        public CargoColor Cargo => cargo;
        /// <summary>Bay identity, independent of the shuttle.</summary>
        public CargoColor BayCargo => bayCargo;
        /// <summary>Shuttle starting point.</summary>
        public Vector2 Spawn => spawn;
        /// <summary>Matching bay centre.</summary>
        public Vector2 Bay => bay;
        /// <summary>Static meteorite centre.</summary>
        public Vector2 Meteorite => meteorite;
        /// <summary>Static meteorite radius.</summary>
        public float MeteoriteRadius => meteoriteRadius;
        /// <summary>Authored tutorial route; immutable at runtime.</summary>
        public Vector2[] GhostPath => ghostPath;
        /// <summary>Checks obvious layout authoring errors.</summary>
        public bool IsValid => levelId > 0 && !string.IsNullOrWhiteSpace(title) && meteoriteRadius > 0 && ghostPath != null && ghostPath.Length >= 2 && Enum.IsDefined(typeof(CargoColor), cargo) && Enum.IsDefined(typeof(CargoColor), bayCargo) && Vector2.Distance(spawn, bay) > 1;
    }
}
