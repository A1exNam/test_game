using UnityEngine;
using UnityEngine.UI;
using GW.Gameplay;

namespace GW.UI
{
    [DisallowMultipleComponent]
    public sealed class HUDRoot : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField]
        private ConveyorLineController trackedLine;

        [SerializeField]
        private LineFocusController focusController;

        [SerializeField]
        private bool autoLocateFocusController = true;

        [SerializeField]
        private Text scoreText;

        [SerializeField]
        private Text comboText;

        [SerializeField]
        private Text multiplierText;

        [SerializeField]
        private Image blissFillImage;

        [SerializeField]
        private string scoreFormat = "{0:N0}";

        [SerializeField]
        private string comboFormat = "COMBO {0}";

        [SerializeField]
        private string multiplierFormat = "×{0:F1}";

        private ConveyorLineController boundLine;
        private bool focusSubscribed;

        private void OnEnable()
        {
            if (autoLocateFocusController && focusController == null)
            {
                focusController = FindObjectOfType<LineFocusController>();
            }

            if (focusController != null)
            {
                SubscribeFocus();
                HandleFocusChanged(focusController.CurrentLine);
            }
            else if (trackedLine != null)
            {
                BindLine(trackedLine);
            }
        }

        private void OnDisable()
        {
            UnsubscribeFocus();
            UnbindLine();
        }

        public void BindLine(ConveyorLineController line)
        {
            if (line == boundLine)
            {
                return;
            }

            UnbindLine();
            boundLine = line;

            if (boundLine == null)
            {
                return;
            }

            boundLine.ScoreChanged += HandleScoreChanged;
            boundLine.ComboChanged += HandleComboChanged;
            boundLine.MultiplierChanged += HandleMultiplierChanged;
            boundLine.BlissChanged += HandleBlissChanged;

            HandleScoreChanged(boundLine.Score);
            HandleComboChanged(boundLine.Judge?.Combo ?? 0);
            HandleMultiplierChanged(boundLine.Judge?.MultiplierLevel ?? 0);
            HandleBlissChanged(boundLine.Judge?.Bliss ?? 0f);
        }

        public void UnbindLine()
        {
            if (boundLine == null)
            {
                return;
            }

            boundLine.ScoreChanged -= HandleScoreChanged;
            boundLine.ComboChanged -= HandleComboChanged;
            boundLine.MultiplierChanged -= HandleMultiplierChanged;
            boundLine.BlissChanged -= HandleBlissChanged;
            boundLine = null;
        }

        private void HandleScoreChanged(int value)
        {
            if (scoreText != null)
            {
                scoreText.text = string.Format(scoreFormat, value);
            }
        }

        private void HandleComboChanged(int combo)
        {
            if (comboText != null)
            {
                comboText.text = string.Format(comboFormat, Mathf.Max(0, combo));
            }
        }

        private void HandleMultiplierChanged(int level)
        {
            if (multiplierText == null)
            {
                return;
            }

            var step = boundLine?.Judge?.MultiplierStep ?? 0.5f;
            var multiplierValue = 1f + level * step;
            multiplierText.text = string.Format(multiplierFormat, multiplierValue);
        }

        private void HandleBlissChanged(float value)
        {
            if (blissFillImage == null)
            {
                return;
            }

            blissFillImage.fillAmount = Mathf.Clamp01(value);
        }

        public void BindFocusController(LineFocusController controller)
        {
            if (focusController == controller)
            {
                return;
            }

            UnsubscribeFocus();
            focusController = controller;

            if (isActiveAndEnabled)
            {
                if (focusController != null)
                {
                    SubscribeFocus();
                    HandleFocusChanged(focusController.CurrentLine);
                }
                else if (trackedLine != null)
                {
                    BindLine(trackedLine);
                }
            }
        }

        private void SubscribeFocus()
        {
            if (focusController == null || focusSubscribed)
            {
                return;
            }

            focusController.FocusChanged += HandleFocusChanged;
            focusSubscribed = true;
        }

        private void UnsubscribeFocus()
        {
            if (!focusSubscribed || focusController == null)
            {
                focusSubscribed = false;
                return;
            }

            focusController.FocusChanged -= HandleFocusChanged;
            focusSubscribed = false;
        }

        private void HandleFocusChanged(ConveyorLineController line)
        {
            if (line == null)
            {
                UnbindLine();
                return;
            }

            BindLine(line);
        }
    }
}
