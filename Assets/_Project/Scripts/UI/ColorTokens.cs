using UnityEngine;
using ZeroGCargo.Data;

namespace ZeroGCargo.UI
{
    /// <summary>Shared palette from the design system.</summary>
    public static class ColorTokens
    {
        /// <summary>Station background.</summary>
        public static readonly Color32 Background = new Color32(10, 14, 26, 255);
        /// <summary>Panel background.</summary>
        public static readonly Color32 Panel = new Color32(18, 24, 40, 255);
        /// <summary>Interactive cyan accent.</summary>
        public static readonly Color32 Accent = new Color32(0, 240, 255, 255);
        /// <summary>Primary text.</summary>
        public static readonly Color32 Text = new Color32(232, 240, 255, 255);
        /// <summary>Secondary text.</summary>
        public static readonly Color32 Muted = new Color32(138, 155, 184, 255);
        /// <summary>Maps cargo identity to its display colour.</summary>
        public static Color Cargo(CargoColor cargo)
        {
            switch (cargo)
            {
                case CargoColor.Lime: return new Color32(182, 255, 60, 255);
                case CargoColor.Amber: return new Color32(255, 184, 48, 255);
                case CargoColor.Violet: return new Color32(168, 85, 247, 255);
                default: return new Color32(255, 46, 154, 255);
            }
        }
    }
}
