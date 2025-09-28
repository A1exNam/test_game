using UnityEngine;
using UnityEngine.SceneManagement;
using GW.Gameplay;

namespace GW.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField]
        private string gameSceneName = "Game";

        [Header("Foil Showcase")]
        [SerializeField]
        private FoilPatternShowcase patternShowcase;

        [SerializeField]
        private FoilPatternLibrary patternLibrary;

        [SerializeField]
        [Tooltip("Resource path used if no library reference is provided explicitly.")]
        private string patternLibraryResourcePath = "FoilPatternLibrary";

        [SerializeField]
        [Tooltip("Refresh the foil pattern showcase automatically on enable.")]
        private bool autoRefreshShowcase = true;

        [Header("Settings")]
        [SerializeField]
        private SettingsPanel settingsPanel;

        private void Awake()
        {
            if (autoRefreshShowcase)
            {
                RefreshPatternShowcase();
            }

            settingsPanel?.Hide();
        }

        private void OnEnable()
        {
            if (autoRefreshShowcase)
            {
                RefreshPatternShowcase();
            }
        }

        public void StartCareer()
        {
            if (string.IsNullOrEmpty(gameSceneName))
            {
                Debug.LogError("Game scene name is not configured on MainMenuController.");
                return;
            }

            SceneManager.LoadScene(gameSceneName);
        }

        public void SetPatternLibrary(FoilPatternLibrary library)
        {
            patternLibrary = library;
            RefreshPatternShowcase();
        }

        public void SetPatternShowcase(FoilPatternShowcase showcase)
        {
            patternShowcase = showcase;
            RefreshPatternShowcase();
        }

        public void ShowSettings()
        {
            settingsPanel?.Show();
        }

        public void HideSettings()
        {
            settingsPanel?.Hide();
        }

        private void RefreshPatternShowcase()
        {
            if (patternShowcase == null)
            {
                patternShowcase = FindObjectOfType<FoilPatternShowcase>(true);
            }

            if (patternShowcase == null)
            {
                return;
            }

            if (patternLibrary == null && !string.IsNullOrWhiteSpace(patternLibraryResourcePath))
            {
                patternLibrary = Resources.Load<FoilPatternLibrary>(patternLibraryResourcePath);
            }

            if (patternLibrary != null)
            {
                patternShowcase.SetLibrary(patternLibrary);
            }
            else
            {
                patternShowcase.Refresh();
            }
        }
    }
}
