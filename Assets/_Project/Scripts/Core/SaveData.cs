using System;

namespace ZeroGCargo.Core
{
    /// <summary>Version-one local JSON DTO. Public fields are serialized data, never inspector wiring.</summary>
    [Serializable]
    public sealed class SaveData
    {
        /// <summary>Current schema version.</summary>
        public int version = 1;
        /// <summary>Highest unlocked level, one-based.</summary>
        public int highestUnlockedLevel = 1;
        /// <summary>Best stars for each of the planned fifty levels.</summary>
        public int[] starsPerLevel = new int[50];
        /// <summary>Score accumulated from successful deliveries.</summary>
        public int totalScore;
        /// <summary>Player preferences.</summary>
        public SettingsData settings = new SettingsData();
        /// <summary>Reserved and unused in this prototype.</summary>
        public string lastDailyRewardClaim;
        /// <summary>Reserved completion counter; no ads are integrated.</summary>
        public int adFrequencyCounter;
        /// <summary>Validates schema and normalizes recoverable values.</summary>
        public bool Validate()
        {
            if (version != 1) return false;
            highestUnlockedLevel = Math.Max(1, Math.Min(50, highestUnlockedLevel));
            if (starsPerLevel == null) starsPerLevel = new int[50];
            if (starsPerLevel.Length != 50) Array.Resize(ref starsPerLevel, 50);
            for (int i = 0; i < 50; i++) starsPerLevel[i] = Math.Max(0, Math.Min(3, starsPerLevel[i]));
            totalScore = Math.Max(0, totalScore);
            adFrequencyCounter = Math.Max(0, Math.Min(4, adFrequencyCounter));
            if (settings == null) settings = new SettingsData();
            return true;
        }
        /// <summary>Records a completed level, preserving personal bests.</summary>
        public void Complete(int level, int stars, int score)
        {
            if (level < 1 || level > 50) throw new ArgumentOutOfRangeException(nameof(level));
            starsPerLevel[level - 1] = Math.Max(starsPerLevel[level - 1], Math.Max(0, Math.Min(3, stars)));
            highestUnlockedLevel = Math.Max(highestUnlockedLevel, Math.Min(50, level + 1));
            totalScore = (int)Math.Min(int.MaxValue, (long)totalScore + Math.Max(0, score));
        }
    }
}
