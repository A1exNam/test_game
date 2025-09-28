using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace GW.Core
{
    /// <summary>
    /// Simple JSON-driven localisation service for runtime string lookup.
    /// </summary>
    public static class LocalizationService
    {
        private const string ResourceRoot = "GW/Localization";
        private static readonly Dictionary<string, string> entries = new(StringComparer.OrdinalIgnoreCase);
        private static readonly string[] defaultLanguages = { "en", "ru" };
        private static readonly ReadOnlyCollection<string> supportedLanguages = new(defaultLanguages);

        private static string currentLanguage = "en";
        private static bool initialised;

        public static event Action<string> LanguageChanged;

        public static bool IsInitialised => initialised;
        public static string CurrentLanguage => currentLanguage;
        public static IReadOnlyList<string> SupportedLanguages => supportedLanguages;

        /// <summary>
        /// Initialises the localisation service and loads the requested language.
        /// </summary>
        public static bool Initialize(string languageCode)
        {
            var normalised = NormaliseLanguage(languageCode);
            if (!LoadLanguage(normalised))
            {
                return false;
            }

            currentLanguage = normalised;
            initialised = true;
            LanguageChanged?.Invoke(currentLanguage);
            return true;
        }

        /// <summary>
        /// Attempts to switch to the requested language.
        /// </summary>
        public static bool SetLanguage(string languageCode)
        {
            var normalised = NormaliseLanguage(languageCode);
            if (initialised && string.Equals(normalised, currentLanguage, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!LoadLanguage(normalised))
            {
                return false;
            }

            currentLanguage = normalised;
            initialised = true;
            LanguageChanged?.Invoke(currentLanguage);
            return true;
        }

        /// <summary>
        /// Gets the translation for the specified key, falling back as necessary.
        /// </summary>
        public static string Get(string key, string fallback = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                return fallback ?? string.Empty;
            }

            if (entries.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
            {
                return value;
            }

            return fallback ?? key;
        }

        /// <summary>
        /// Returns a user-friendly display name for the supplied language code.
        /// </summary>
        public static string GetLanguageDisplayName(string languageCode)
        {
            var key = $"language.{NormaliseLanguage(languageCode)}";
            var fallback = languageCode?.ToUpperInvariant() ?? string.Empty;
            return Get(key, fallback);
        }

        private static bool LoadLanguage(string languageCode)
        {
            var assetPath = $"{ResourceRoot}/Localization_{languageCode}";
            var textAsset = Resources.Load<TextAsset>(assetPath);
            if (textAsset == null)
            {
                Debug.LogWarning($"Localization asset not found at path '{assetPath}'.");
                return false;
            }

            try
            {
                var table = JsonUtility.FromJson<LocalizationTable>(textAsset.text);
                entries.Clear();

                if (table?.entries != null)
                {
                    for (var i = 0; i < table.entries.Length; i++)
                    {
                        var entry = table.entries[i];
                        if (entry == null || string.IsNullOrEmpty(entry.key))
                        {
                            continue;
                        }

                        entries[entry.key] = entry.value ?? string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to parse localisation data for '{languageCode}': {ex.Message}");
                entries.Clear();
                return false;
            }

            return true;
        }

        private static string NormaliseLanguage(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return "en";
            }

            return code.Trim().ToLowerInvariant();
        }

        [Serializable]
        private sealed class LocalizationTable
        {
            public LocalizationEntry[] entries;
        }

        [Serializable]
        private sealed class LocalizationEntry
        {
            public string key;
            public string value;
        }
    }
}
