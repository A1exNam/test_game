using System;
using System.Collections.Generic;
using UnityEngine;
using GW.Core;

namespace GW.Gameplay
{
    [CreateAssetMenu(menuName = "GW/Upgrades/Upgrade Node", fileName = "UpgradeNode")]
    public sealed class UpgradeNode : ScriptableObject
    {
        [SerializeField]
        private string id = Guid.NewGuid().ToString();

        [SerializeField]
        private string title = "Upgrade";

        [SerializeField]
        [TextArea]
        private string description = "";

        [SerializeField]
        private UpgradeCategory category = UpgradeCategory.Accuracy;

        [SerializeField]
        [Range(0, 5)]
        private int tier;

        [SerializeField]
        [Min(0)]
        private int cost = 5;

        [SerializeField]
        private UpgradeNode prerequisite;

        [SerializeField]
        private List<UpgradeEffectDefinition> effects = new();

        public string Id => string.IsNullOrWhiteSpace(id) ? title : id;
        public string Title => title;
        public string Description => description;
        public UpgradeCategory Category => category;
        public int Tier => Mathf.Max(0, tier);
        public int Cost => Mathf.Max(0, cost);
        public UpgradeNode Prerequisite => prerequisite;
        public IReadOnlyList<UpgradeEffectDefinition> Effects => effects;
    }

    [Serializable]
    public sealed class UpgradeEffectDefinition
    {
        [SerializeField]
        private UpgradeEffectType effectType = UpgradeEffectType.IncreasePerfectWindow;

        [SerializeField]
        private float value = 0.01f;

        public UpgradeEffectType EffectType => effectType;
        public float Value => value;
    }
}
