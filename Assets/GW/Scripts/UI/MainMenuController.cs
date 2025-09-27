using UnityEngine;
using UnityEngine.SceneManagement;

namespace GW.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField]
        private string gameSceneName = "Game";

        public void StartCareer()
        {
            if (string.IsNullOrEmpty(gameSceneName))
            {
                Debug.LogError("Game scene name is not configured on MainMenuController.");
                return;
            }

            SceneManager.LoadScene(gameSceneName);
        }
    }
}
