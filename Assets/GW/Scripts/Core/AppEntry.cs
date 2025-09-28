using UnityEngine;
using UnityEngine.SceneManagement;

namespace GW.Core
{
    public sealed class AppEntry : MonoBehaviour
    {
        [SerializeField]
        private string mainMenuSceneName = "MainMenu";

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            SaveSystem.Load();
            SettingsService.Initialise();
        }

        private void Start()
        {
            if (!string.IsNullOrEmpty(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
            else
            {
                Debug.LogError("Main menu scene name is not configured on AppEntry.");
            }
        }
    }
}
