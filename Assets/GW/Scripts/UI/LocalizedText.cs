using UnityEngine;
using UnityEngine.UI;
using GW.Core;

namespace GW.UI
{
    /// <summary>
    /// Binds a UI Text element to a localisation key.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField]
        private string localizationKey = string.Empty;

        [SerializeField]
        private Text targetText;

        [SerializeField]
        [Tooltip("Fallback text used if the localisation key is missing.")]
        private string fallback = string.Empty;

        [SerializeField]
        [Tooltip("Convert the resolved string to uppercase before applying it to the text component.")]
        private bool uppercase;

        private void Awake()
        {
            if (targetText == null)
            {
                targetText = GetComponent<Text>();
            }
        }

        private void OnEnable()
        {
            LocalizationService.LanguageChanged += HandleLanguageChanged;
            RefreshText();
        }

        private void OnDisable()
        {
            LocalizationService.LanguageChanged -= HandleLanguageChanged;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (targetText == null)
            {
                targetText = GetComponent<Text>();
            }

            RefreshText();
        }
#endif

        public void SetKey(string key)
        {
            localizationKey = key;
            RefreshText();
        }

        public void RefreshText()
        {
            if (targetText == null)
            {
                return;
            }

            var resolved = LocalizationService.Get(localizationKey, fallback);
            if (uppercase)
            {
                resolved = resolved.ToUpperInvariant();
            }

            targetText.text = resolved;
        }

        private void HandleLanguageChanged(string _)
        {
            RefreshText();
        }
    }
}
