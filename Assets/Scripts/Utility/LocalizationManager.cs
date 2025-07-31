using UnityEngine;
using System.Collections.Generic;

namespace RollABall.Utility
{
    /// <summary>
    /// Minimal localization helper used for UI text.
    /// Maps simple keys to translated strings based on the current system language.
    /// </summary>
    public static class LocalizationManager
    {
        [System.Serializable]
        private class LocalizationEntry
        {
            public string key;
            public string value;
        }

        [System.Serializable]
        private class LocalizationFile
        {
            public LocalizationEntry[] entries;
        }

        private static readonly Dictionary<string, string> english = new();
        private static readonly Dictionary<string, string> german = new();
        private static bool loaded;

        private static Dictionary<string, string> Current
        {
            get
            {
                LoadIfNeeded();
                return Application.systemLanguage == SystemLanguage.German ? german : english;
            }
        }

        private static void LoadIfNeeded()
        {
            if (loaded) return;

            LoadLanguageFile("en", english);
            LoadLanguageFile("de", german);
            loaded = true;
        }

        private static void LoadLanguageFile(string code, Dictionary<string, string> target)
        {
            TextAsset asset = Resources.Load<TextAsset>($"Localization/{code}");
            if (asset == null) return;

            var file = JsonUtility.FromJson<LocalizationFile>(asset.text);
            target.Clear();
            if (file != null && file.entries != null)
            {
                foreach (var entry in file.entries)
                {
                    if (!target.ContainsKey(entry.key))
                        target.Add(entry.key, entry.value);
                }
            }
        }

        /// <summary>
        /// Returns the localized string for the given key. If no translation exists, returns the key.
        /// </summary>
        public static string Get(string key)
        {
            return Current.TryGetValue(key, out var value) ? value : key;
        }
    }
}
