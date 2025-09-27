using UnityEngine;
using UnityEngine.UI;
using GW.Gameplay;

namespace GW.UI
{
    [DisallowMultipleComponent]
    public sealed class UpgradeNodeView : MonoBehaviour
    {
        [SerializeField]
        private UpgradeNode node;

        [SerializeField]
        private Text titleText;

        [SerializeField]
        private Text descriptionText;

        [SerializeField]
        private Text costText;

        [SerializeField]
        private Button purchaseButton;

        [SerializeField]
        private GameObject purchasedIndicator;

        [SerializeField]
        private GameObject lockedIndicator;

        [SerializeField]
        private string ownedLabel = "OWNED";

        [SerializeField]
        private string lockedLabel = "LOCKED";

        private UpgradeSystem system;

        private void Awake()
        {
            if (purchaseButton != null)
            {
                purchaseButton.onClick.AddListener(HandlePurchaseClicked);
            }

            Refresh();
        }

        private void OnDestroy()
        {
            if (purchaseButton != null)
            {
                purchaseButton.onClick.RemoveListener(HandlePurchaseClicked);
            }
        }

        public void Bind(UpgradeSystem upgradeSystem)
        {
            system = upgradeSystem;
            Refresh();
        }

        public void Refresh()
        {
            if (titleText != null)
            {
                titleText.text = node != null ? node.Title : string.Empty;
            }

            if (descriptionText != null)
            {
                descriptionText.text = node != null ? node.Description : string.Empty;
            }

            var isPurchased = system != null && node != null && system.IsPurchased(node);
            var isUnlocked = system != null && node != null && system.IsUnlocked(node);
            var canPurchase = system != null && node != null && system.CanPurchase(node);

            if (costText != null)
            {
                if (node == null)
                {
                    costText.text = string.Empty;
                }
                else if (isPurchased)
                {
                    costText.text = ownedLabel;
                }
                else if (!isUnlocked)
                {
                    costText.text = lockedLabel;
                }
                else
                {
                    costText.text = node.Cost.ToString();
                }
            }

            if (purchaseButton != null)
            {
                purchaseButton.interactable = canPurchase;
            }

            if (purchasedIndicator != null)
            {
                purchasedIndicator.SetActive(isPurchased);
            }

            if (lockedIndicator != null)
            {
                lockedIndicator.SetActive(node != null && !isPurchased && !isUnlocked);
            }
        }

        private void HandlePurchaseClicked()
        {
            if (system == null || node == null)
            {
                return;
            }

            if (system.TryPurchase(node))
            {
                Refresh();
            }
        }
    }
}
