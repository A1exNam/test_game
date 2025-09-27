using UnityEngine;
using UnityEngine.UI;
using GW.Gameplay;

namespace GW.UI
{
    [DisallowMultipleComponent]
    public sealed class ContractItemView : MonoBehaviour
    {
        [SerializeField]
        private Text titleText;

        [SerializeField]
        private Text descriptionText;

        [SerializeField]
        private Text progressText;

        [SerializeField]
        private Image progressFill;

        private ContractInstance boundInstance;

        public void Bind(ContractInstance instance)
        {
            boundInstance = instance;
            Refresh();
        }

        public void Refresh()
        {
            if (boundInstance == null || boundInstance.Definition == null)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);

            var definition = boundInstance.Definition;
            if (titleText != null)
            {
                titleText.text = definition.Title;
            }

            if (descriptionText != null)
            {
                descriptionText.text = definition.Description;
            }

            if (progressText != null)
            {
                progressText.text = $"{boundInstance.Progress}/{definition.GoalCount}";
            }

            if (progressFill != null)
            {
                progressFill.fillAmount = boundInstance.NormalizedProgress;
            }
        }

        private void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }
    }
}
