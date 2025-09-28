using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GW.Core;

namespace GW.UI
{
    /// <summary>
    /// Handles binding between the settings UI controls and runtime services.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SettingsPanel : MonoBehaviour
    {
        [SerializeField]
        private GameObject rootObject;

        [Header("Audio")]
        [SerializeField]
        private Slider musicVolumeSlider;

        [SerializeField]
        private Slider sfxVolumeSlider;

        [Header("Accessibility")]
        [SerializeField]
        private Slider effectsIntensitySlider;

        [SerializeField]
        private Toggle reducedFlashesToggle;

        [SerializeField]
        private Toggle colorblindModeToggle;

        [Header("Gameplay")]
        [SerializeField]
        private Slider autoSnapSlider;

        [SerializeField]
        private Toggle showHintsToggle;

        [Header("Language")]
        [SerializeField]
        private Dropdown languageDropdown;

        [SerializeField]
        private List<string> languageCodes = new List<string> { "en", "ru" };

        private bool suppressCallbacks;

        private void Awake()
        {
            if (rootObject == null)
            {
                rootObject = gameObject;
            }

            EnsureLanguageCodes();
            RegisterListeners();
        }

        private void OnEnable()
        {
            SettingsService.SettingsChanged += HandleSettingsChanged;
            LocalizationService.LanguageChanged += HandleLanguageChanged;
            RefreshLanguageOptions();
            ApplySettings(SettingsService.Settings);
        }

        private void OnDisable()
        {
            SettingsService.SettingsChanged -= HandleSettingsChanged;
            LocalizationService.LanguageChanged -= HandleLanguageChanged;
        }

        private void OnDestroy()
        {
            UnregisterListeners();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureLanguageCodes();
        }
#endif

        public void Show()
        {
            if (rootObject != null)
            {
                rootObject.SetActive(true);
            }

            ApplySettings(SettingsService.Settings);
        }

        public void Hide()
        {
            if (rootObject != null)
            {
                rootObject.SetActive(false);
            }
        }

        public void Toggle()
        {
            if (rootObject == null)
            {
                return;
            }

            rootObject.SetActive(!rootObject.activeSelf);

            if (rootObject.activeSelf)
            {
                ApplySettings(SettingsService.Settings);
            }
        }

        private void RegisterListeners()
        {
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.AddListener(HandleMusicVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.AddListener(HandleSfxVolumeChanged);
            }

            if (effectsIntensitySlider != null)
            {
                effectsIntensitySlider.onValueChanged.AddListener(HandleEffectsIntensityChanged);
            }

            if (reducedFlashesToggle != null)
            {
                reducedFlashesToggle.onValueChanged.AddListener(HandleReducedFlashesChanged);
            }

            if (colorblindModeToggle != null)
            {
                colorblindModeToggle.onValueChanged.AddListener(HandleColorblindModeChanged);
            }

            if (autoSnapSlider != null)
            {
                autoSnapSlider.onValueChanged.AddListener(HandleAutoSnapChanged);
            }

            if (showHintsToggle != null)
            {
                showHintsToggle.onValueChanged.AddListener(HandleShowHintsChanged);
            }

            if (languageDropdown != null)
            {
                languageDropdown.onValueChanged.AddListener(HandleLanguageDropdownChanged);
            }
        }

        private void UnregisterListeners()
        {
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.RemoveListener(HandleMusicVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.RemoveListener(HandleSfxVolumeChanged);
            }

            if (effectsIntensitySlider != null)
            {
                effectsIntensitySlider.onValueChanged.RemoveListener(HandleEffectsIntensityChanged);
            }

            if (reducedFlashesToggle != null)
            {
                reducedFlashesToggle.onValueChanged.RemoveListener(HandleReducedFlashesChanged);
            }

            if (colorblindModeToggle != null)
            {
                colorblindModeToggle.onValueChanged.RemoveListener(HandleColorblindModeChanged);
            }

            if (autoSnapSlider != null)
            {
                autoSnapSlider.onValueChanged.RemoveListener(HandleAutoSnapChanged);
            }

            if (showHintsToggle != null)
            {
                showHintsToggle.onValueChanged.RemoveListener(HandleShowHintsChanged);
            }

            if (languageDropdown != null)
            {
                languageDropdown.onValueChanged.RemoveListener(HandleLanguageDropdownChanged);
            }
        }

        private void HandleSettingsChanged(PlayerSettingsData settings)
        {
            ApplySettings(settings);
        }

        private void HandleLanguageChanged(string _)
        {
            RefreshLanguageOptions();
            ApplyLanguageSelection(SettingsService.Settings.language);
        }

        private void ApplySettings(PlayerSettingsData settings)
        {
            suppressCallbacks = true;

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = settings.musicVolume;
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = settings.sfxVolume;
            }

            if (effectsIntensitySlider != null)
            {
                effectsIntensitySlider.value = settings.effectsIntensity;
            }

            if (reducedFlashesToggle != null)
            {
                reducedFlashesToggle.isOn = settings.reducedFlashes;
            }

            if (colorblindModeToggle != null)
            {
                colorblindModeToggle.isOn = settings.colorblindMode;
            }

            if (autoSnapSlider != null)
            {
                autoSnapSlider.value = settings.autoSnapSensitivity;
            }

            if (showHintsToggle != null)
            {
                showHintsToggle.isOn = settings.showHints;
            }

            ApplyLanguageSelection(settings.language);

            suppressCallbacks = false;
        }

        private void ApplyLanguageSelection(string languageCode)
        {
            if (languageDropdown == null)
            {
                return;
            }

            EnsureLanguageCodes();
            var normalised = languageCode?.Trim().ToLowerInvariant() ?? "en";
            var index = languageCodes.FindIndex(code => code == normalised);
            if (index < 0)
            {
                index = 0;
            }

            suppressCallbacks = true;
            languageDropdown.value = index;
            languageDropdown.RefreshShownValue();
            suppressCallbacks = false;
        }

        private void RefreshLanguageOptions()
        {
            if (languageDropdown == null)
            {
                return;
            }

            EnsureLanguageCodes();

            var options = new List<Dropdown.OptionData>(languageCodes.Count);
            for (var i = 0; i < languageCodes.Count; i++)
            {
                var code = languageCodes[i];
                var displayName = LocalizationService.GetLanguageDisplayName(code);
                options.Add(new Dropdown.OptionData(displayName));
            }

            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(options);
            languageDropdown.RefreshShownValue();
        }

        private void HandleMusicVolumeChanged(float value)
        {
            if (suppressCallbacks)
            {
                return;
            }

            SettingsService.SetMusicVolume(value);
        }

        private void HandleSfxVolumeChanged(float value)
        {
            if (suppressCallbacks)
            {
                return;
            }

            SettingsService.SetSfxVolume(value);
        }

        private void HandleEffectsIntensityChanged(float value)
        {
            if (suppressCallbacks)
            {
                return;
            }

            SettingsService.SetEffectsIntensity(value);
        }

        private void HandleReducedFlashesChanged(bool value)
        {
            if (suppressCallbacks)
            {
                return;
            }

            SettingsService.SetReducedFlashes(value);
        }

        private void HandleColorblindModeChanged(bool value)
        {
            if (suppressCallbacks)
            {
                return;
            }

            SettingsService.SetColorblindMode(value);
        }

        private void HandleAutoSnapChanged(float value)
        {
            if (suppressCallbacks)
            {
                return;
            }

            SettingsService.SetAutoSnapSensitivity(value);
        }

        private void HandleShowHintsChanged(bool value)
        {
            if (suppressCallbacks)
            {
                return;
            }

            SettingsService.SetShowHints(value);
        }

        private void HandleLanguageDropdownChanged(int index)
        {
            if (suppressCallbacks)
            {
                return;
            }

            EnsureLanguageCodes();
            if (index < 0 || index >= languageCodes.Count)
            {
                return;
            }

            var selectedCode = languageCodes[index];
            SettingsService.SetLanguage(selectedCode);
        }

        private void EnsureLanguageCodes()
        {
            if (languageCodes == null)
            {
                languageCodes = new List<string>();
            }

            if (languageCodes.Count == 0)
            {
                languageCodes.AddRange(LocalizationService.SupportedLanguages);
            }
        }
    }
}
