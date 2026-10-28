using System;

namespace ZeroGCargo.Core
{
    /// <summary>Serialized preferences; all feedback is enabled by default.</summary>
    [Serializable]
    public sealed class SettingsData
    {
        /// <summary>Sound-effect preference.</summary>
        public bool soundOn = true;
        /// <summary>Music preference, reserved until audio implementation.</summary>
        public bool musicOn = true;
        /// <summary>Haptics preference, reserved until device feedback implementation.</summary>
        public bool hapticsOn = true;
    }
}
