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
        private static readonly Dictionary<string, string> english = new()
        {
            {"NewGame", "New Game"},
            {"LastPlayed", "Last played"}
        };

        private static readonly Dictionary<string, string> german = new()
        {
            {"NewGame", "Neues Spiel"},
            {"LastPlayed", "Zuletzt gespielt"}
        };

        private static Dictionary<string, string> Current => Application.systemLanguage == SystemLanguage.German ? german : english;

        /// <summary>
        /// Returns the localized string for the given key. If no translation exists, returns the key.
        /// </summary>
        public static string Get(string key)
        {
            return Current.TryGetValue(key, out var value) ? value : key;
        }
    }
}
