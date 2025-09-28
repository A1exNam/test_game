using System;
using UnityEngine;

namespace GW.Core
{
    /// <summary>
    /// Centralised access point for player-configurable settings with change notifications.
    /// </summary>
    public static class SettingsService
    {
        private static PlayerSettingsData settings;
        private static bool initialised;

        private const float MinAutoSnap = 0.1f;
        private const float MaxAutoSnap = 0.35f;

        public static event Action<PlayerSettingsData> SettingsChanged;

        public static bool IsInitialised => initialised;

        public static PlayerSettingsData Settings
        {
            get
            {
                EnsureInitialised();
                return settings;
            }
        }

        public static void Initialise()
        {
            EnsureInitialised();
        }

        public static void SetMusicVolume(float value)
        {
            EnsureInitialised();
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(settings.musicVolume, value))
            {
                return;
            }

            settings.musicVolume = value;
            RaiseChanged();
            SaveSystem.Save();
        }

        public static void SetSfxVolume(float value)
        {
            EnsureInitialised();
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(settings.sfxVolume, value))
            {
                return;
            }

            settings.sfxVolume = value;
            RaiseChanged();
            SaveSystem.Save();
        }

        public static void SetEffectsIntensity(float value)
        {
            EnsureInitialised();
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(settings.effectsIntensity, value))
            {
                return;
            }

            settings.effectsIntensity = value;
            RaiseChanged();
            SaveSystem.Save();
        }

        public static void SetReducedFlashes(bool value)
        {
            EnsureInitialised();
            if (settings.reducedFlashes == value)
            {
                return;
            }

            settings.reducedFlashes = value;
            RaiseChanged();
            SaveSystem.Save();
        }

        public static void SetColorblindMode(bool value)
        {
            EnsureInitialised();
            if (settings.colorblindMode == value)
            {
                return;
            }

            settings.colorblindMode = value;
            RaiseChanged();
            SaveSystem.Save();
        }

        public static void SetAutoSnapSensitivity(float value)
        {
            EnsureInitialised();
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(settings.autoSnapSensitivity, value))
            {
                return;
            }

            settings.autoSnapSensitivity = value;
            RaiseChanged();
            SaveSystem.Save();
        }

        public static void SetShowHints(bool value)
        {
            EnsureInitialised();
            if (settings.showHints == value)
            {
                return;
            }

            settings.showHints = value;
            RaiseChanged();
            SaveSystem.Save();
        }

        public static void SetLanguage(string languageCode)
        {
            EnsureInitialised();
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                return;
            }

            var normalised = languageCode.Trim().ToLowerInvariant();
            if (normalised == settings.language)
            {
                return;
            }

            if (!LocalizationService.SetLanguage(normalised))
            {
                return;
            }

            // Language change will be persisted via HandleLanguageChanged
        }

        public static float ResolveAutoSnapPercentage()
        {
            EnsureInitialised();
            var t = Mathf.Clamp01(settings.autoSnapSensitivity);
            return Mathf.Lerp(MinAutoSnap, MaxAutoSnap, t);
        }

        private static void EnsureInitialised()
        {
            if (initialised)
            {
                return;
            }

            var data = SaveSystem.Current;
            data.EnsureIntegrity();
            settings = data.settings ?? new PlayerSettingsData();
            data.settings = settings;

            LocalizationService.LanguageChanged += HandleLanguageChanged;
            if (!LocalizationService.IsInitialised)
            {
                var requestedLanguage = settings.language;
                if (!LocalizationService.Initialize(requestedLanguage))
                {
                    LocalizationService.Initialize("en");
                }
            }

            initialised = true;
            RaiseChanged();
        }

        private static void HandleLanguageChanged(string languageCode)
        {
            EnsureInitialised();
            if (settings.language == languageCode)
            {
                return;
            }

            settings.language = languageCode;
            RaiseChanged();
            SaveSystem.Save();
        }

        private static void RaiseChanged()
        {
            SettingsChanged?.Invoke(settings);
        }
    }
}
