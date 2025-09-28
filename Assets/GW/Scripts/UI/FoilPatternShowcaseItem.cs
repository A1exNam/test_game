using UnityEngine;
using UnityEngine.UI;
using GW.Core;
using GW.Gameplay;

namespace GW.UI
{
    [DisallowMultipleComponent]
    public sealed class FoilPatternShowcaseItem : MonoBehaviour
    {
        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private Image tintSwatch;

        [SerializeField]
        private Text nameLabel;

        [SerializeField]
        private Text rarityLabel;

        [SerializeField]
        private Text detailLabel;

        [Header("Rarity Colors")]
        [SerializeField]
        private Color commonColor = new Color32(201, 166, 70, 255);

        [SerializeField]
        private Color rareColor = new Color32(255, 211, 110, 255);

        [SerializeField]
        private Color epicColor = new Color32(255, 238, 163, 255);

        [SerializeField]
        private Color legendaryColor = new Color32(255, 249, 220, 255);

        public void Present(FoilPatternRuntime runtime)
        {
            Present(runtime.Definition, runtime);
        }

        public void Present(FoilPatternDef pattern, FoilPatternRuntime? runtime = null)
        {
            var rarity = pattern != null ? pattern.Rarity : FoilPatternRarity.Common;
            var rarityColor = GetRarityColor(rarity);
            var displayName = pattern != null ? pattern.DisplayName : "Pattern";

            if (iconImage != null)
            {
                iconImage.enabled = pattern != null && pattern.Icon != null;
                iconImage.sprite = pattern != null ? pattern.Icon : null;
                iconImage.color = rarityColor;
            }

            if (tintSwatch != null)
            {
                if (runtime.HasValue)
                {
                    tintSwatch.color = Color.Lerp(runtime.Value.PrimaryTint, runtime.Value.SecondaryTint, 0.5f);
                }
                else
                {
                    tintSwatch.color = rarityColor;
                }
            }

            if (nameLabel != null)
            {
                nameLabel.text = displayName;
            }

            if (rarityLabel != null)
            {
                rarityLabel.text = rarity.ToString().ToUpperInvariant();
                rarityLabel.color = rarityColor;
            }

            if (detailLabel != null)
            {
                if (pattern == null)
                {
                    detailLabel.text = string.Empty;
                }
                else
                {
                    var angle = pattern.LineAngle;
                    if (runtime.HasValue)
                    {
                        var dir = runtime.Value.FlowDirection;
                        angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    }

                    detailLabel.text = $"Spec {pattern.Specular:F2} • Freq {pattern.LineFrequency:F1} • Angle {angle:F0}°";
                }
            }
        }

        public void Clear()
        {
            if (iconImage != null)
            {
                iconImage.enabled = false;
                iconImage.sprite = null;
            }

            if (tintSwatch != null)
            {
                tintSwatch.color = Color.clear;
            }

            if (nameLabel != null)
            {
                nameLabel.text = string.Empty;
            }

            if (rarityLabel != null)
            {
                rarityLabel.text = string.Empty;
            }

            if (detailLabel != null)
            {
                detailLabel.text = string.Empty;
            }
        }

        private Color GetRarityColor(FoilPatternRarity rarity)
        {
            return rarity switch
            {
                FoilPatternRarity.Common => commonColor,
                FoilPatternRarity.Rare => rareColor,
                FoilPatternRarity.Epic => epicColor,
                FoilPatternRarity.Legendary => legendaryColor,
                _ => commonColor,
            };
        }
    }
}
