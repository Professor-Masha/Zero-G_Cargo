using System;
using System.IO;
using UnityEngine;
using ZeroGCargo.Core;

namespace ZeroGCargo.Data
{
    /// <summary>Unity JSON adapter for the engine-independent save store.</summary>
    public sealed class SaveSystem
    {
        private readonly SaveStore store;
        /// <summary>Current local progress.</summary>
        public SaveData Data { get; private set; }
        /// <summary>True if this client cannot safely update the on-disk schema.</summary>
        public bool IsReadOnly => store.IsReadOnly;
        /// <summary>Loads progress from the platform persistent-data directory.</summary>
        public SaveSystem()
        {
            store = new SaveStore(Application.persistentDataPath, Decode, data => JsonUtility.ToJson(data, true));
            Data = store.Load();
            if (store.WasReset) Debug.LogWarning("save_reset: No valid local save or backup; using defaults.");
        }
        private static SaveData Decode(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{")) throw new FormatException("Expected a JSON object.");
            var data = new SaveData(); JsonUtility.FromJsonOverwrite(json, data); return data;
        }
        /// <summary>Resets progress only after the player's explicit confirmation.</summary>
        public bool Reset()
        {
            if (store.IsReadOnly) return false;
            Data = new SaveData();
            // Replace the backup as well, so corruption cannot resurrect the reset progress.
            return Save() && Save();
        }
        /// <summary>Persists progress without interrupting gameplay on storage failures.</summary>
        public bool Save()
        {
            try { return store.Write(Data); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is PlatformNotSupportedException)
            { Debug.LogWarning("Local save unavailable: " + e.Message); return false; }
        }
    }
}
