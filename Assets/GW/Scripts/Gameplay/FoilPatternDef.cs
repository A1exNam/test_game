using System;
using UnityEngine;
using GW.Core;

namespace GW.Gameplay
{
    [CreateAssetMenu(menuName = "GW/Foil Patterns/Foil Pattern Definition", fileName = "FoilPatternDef")]
    public sealed class FoilPatternDef : ScriptableObject
    {
        [SerializeField]
        private string id = Guid.NewGuid().ToString();

        [SerializeField]
        private string displayName = "Foil Pattern";

        [SerializeField]
        [Tooltip("Optional icon that can be shown in the showcase UI.")]
        private Sprite icon;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How intense the specular highlight should be when this pattern is active.")]
        private float specular = 0.6f;

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("Relative frequency of the foil ridges. Higher values mean tighter lines.")]
        private float lineFrequency = 6f;

        [SerializeField]
        [Range(-180f, 180f)]
        [Tooltip("Base angle of the foil emboss lines in degrees.")]
        private float lineAngle = 45f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Perceived emboss depth for the foil.")]
        private float embossDepth = 0.25f;

        [SerializeField]
        [Tooltip("Seed used for deterministic procedural details.")]
        private int seed;

        [SerializeField]
        private FoilPatternRarity rarity = FoilPatternRarity.Common;

        [SerializeField]
        [Tooltip("Optional short flavour text shown in the showcase.")]
        [TextArea]
        private string description;

        public string Id => id;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public Sprite Icon => icon;
        public float Specular => Mathf.Clamp01(specular);
        public float LineFrequency => Mathf.Max(0.01f, lineFrequency);
        public float LineAngle => lineAngle;
        public float EmbossDepth => Mathf.Clamp01(embossDepth);
        public int Seed => seed;
        public FoilPatternRarity Rarity => rarity;
        public string Description => description;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = name;
            }
        }
#endif
    }
}
