using System;

namespace ZeroGCargo.Core
{
    /// <summary>Offline campaign resource, serialized without executable or remote content.</summary>
    [Serializable]
    public sealed class CampaignData
    {
        /// <summary>Schema version.</summary>
        public int version = 1;
        /// <summary>Ordered campaign levels.</summary>
        public LevelDefinition[] levels;
    }
}
