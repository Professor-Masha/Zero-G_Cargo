using System;
using System.IO;

namespace ZeroGCargo.Core
{
    /// <summary>Atomic local persistence with backup recovery and future-version protection.</summary>
    public sealed class SaveStore
    {
        private readonly string main;
        private readonly string backup;
        private readonly Func<string, SaveData> decode;
        private readonly Func<SaveData, string> encode;
        private bool recoveredBackup;
        /// <summary>Prevents older clients from overwriting an unknown future schema.</summary>
        public bool IsReadOnly { get; private set; }
        /// <summary>Whether neither the main save nor backup could be used.</summary>
        public bool WasReset { get; private set; }
        /// <summary>Configures paths and a platform-specific JSON codec.</summary>
        public SaveStore(string directory, Func<string, SaveData> decoder, Func<SaveData, string> encoder)
        {
            main = Path.Combine(directory, "zerog_save.json");
            backup = Path.Combine(directory, "zerog_save.bak.json");
            decode = decoder; encode = encoder;
        }
        private SaveData TryLoad(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                SaveData data = decode(File.ReadAllText(path));
                if (data != null && data.version > 1) IsReadOnly = true;
                return data != null && data.Validate() ? data : null;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is FormatException) { return null; }
        }
        /// <summary>Loads main, then backup, then defaults without overwriting evidence.</summary>
        public SaveData Load()
        {
            IsReadOnly = false; WasReset = false; recoveredBackup = false;
            SaveData data = TryLoad(main);
            if (data != null) return data;
            data = TryLoad(backup);
            if (data != null) { recoveredBackup = true; return data; }
            WasReset = File.Exists(main) || File.Exists(backup);
            return new SaveData();
        }
        /// <summary>Writes a temporary file and atomically replaces the primary save.</summary>
        public bool Write(SaveData data)
        {
            if (IsReadOnly || data == null || !data.Validate()) return false;
            Directory.CreateDirectory(Path.GetDirectoryName(main));
            string temp = main + ".tmp";
            File.WriteAllText(temp, encode(data));
            if (File.Exists(main)) File.Replace(temp, main, recoveredBackup ? null : backup);
            else File.Move(temp, main);
            recoveredBackup = false;
            return true;
        }
    }
}
