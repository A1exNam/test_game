using System;
using System.Collections.Generic;
using UnityEngine;

namespace GW.Core
{
    /// <summary>
    /// Lightweight save system wrapper around PlayerPrefs storing persistent profile data.
    /// </summary>
    public static class SaveSystem
    {
        private const string SaveKey = "GW_Save_v1";
        private const int CurrentVersion = 1;

        private static SaveData current;
        private static bool loaded;

        /// <summary>
        /// Gets the mutable save data instance, loading it on demand if necessary.
        /// </summary>
        public static SaveData Current
        {
            get
            {
                if (!loaded)
                {
                    Load();
                }

                return current;
            }
        }

        /// <summary>
        /// Loads the save data from PlayerPrefs, returning the in-memory representation.
        /// </summary>
        public static SaveData Load()
        {
            if (loaded)
            {
                return current;
            }

            try
            {
                if (PlayerPrefs.HasKey(SaveKey))
                {
                    var json = PlayerPrefs.GetString(SaveKey, string.Empty);
                    if (!string.IsNullOrEmpty(json))
                    {
                        current = JsonUtility.FromJson<SaveData>(json);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load save data: {ex.Message}");
                current = null;
            }

            if (current == null)
            {
                current = CreateDefault();
            }

            current.version = CurrentVersion;
            current.EnsureIntegrity();

            loaded = true;
            return current;
        }

        /// <summary>
        /// Persists the current save data to PlayerPrefs.
        /// </summary>
        public static void Save()
        {
            if (!loaded)
            {
                Load();
            }

            if (current == null)
            {
                current = CreateDefault();
            }

            current.EnsureIntegrity();

            try
            {
                var json = JsonUtility.ToJson(current);
                PlayerPrefs.SetString(SaveKey, json);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to save data: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes persisted save data and resets the in-memory representation.
        /// </summary>
        public static void Reset()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            current = CreateDefault();
            loaded = true;
        }

        private static SaveData CreateDefault()
        {
            return new SaveData
            {
                version = CurrentVersion,
                credits = 0,
                bestCombo = 0,
                unlockedPatterns = new List<string>(),
                completedContracts = new List<string>(),
                purchasedUpgrades = new List<string>(),
                settings = new PlayerSettingsData(),
            };
        }
    }

    [Serializable]
    public sealed class SaveData
    {
        public int version = 1;
        public int credits;
        public int bestCombo;
        public List<string> unlockedPatterns = new List<string>();
        public List<string> completedContracts = new List<string>();
        public List<string> purchasedUpgrades = new List<string>();
        public PlayerSettingsData settings = new PlayerSettingsData();

        public void EnsureIntegrity()
        {
            unlockedPatterns ??= new List<string>();
            completedContracts ??= new List<string>();
            purchasedUpgrades ??= new List<string>();
            settings ??= new PlayerSettingsData();
        }
    }

    [Serializable]
    public sealed class PlayerSettingsData
    {
        [Range(0f, 1f)]
        public float musicVolume = 0.8f;

        [Range(0f, 1f)]
        public float sfxVolume = 1f;

        [Range(0f, 1f)]
        public float effectsIntensity = 1f;

        public bool reducedFlashes;

        public bool colorblindMode;

        [Range(0f, 1f)]
        public float autoSnapSensitivity = 0.5f;

        public bool showHints = true;

        public string language = "en";
    }
}
