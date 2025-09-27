using System;
using System.Collections.Generic;
using UnityEngine;
using GW.Core;
using GW.UI;

namespace GW.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class UpgradeSystem : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField]
        private ContractSystem contractSystem;

        [SerializeField]
        private List<ConveyorLineController> lines = new();

        [SerializeField]
        private UpgradePanel upgradePanel;

        [Header("Library")]
        [SerializeField]
        private List<UpgradeNode> upgradeNodes = new();

        [SerializeField]
        private bool autoPopulateFromResources = true;

        [SerializeField]
        private string resourcesPath = "GW/Upgrades";

        private readonly HashSet<string> purchasedNodeIds = new();
        private readonly Dictionary<string, UpgradeNode> nodeLookup = new();

        public event Action<UpgradeNode> UpgradePurchased;
        public event Action StateChanged;

        public int AvailableCredits => contractSystem?.Credits ?? 0;

        private void Awake()
        {
            nodeLookup.Clear();
            BuildLookup(upgradeNodes);

            if (autoPopulateFromResources && !string.IsNullOrEmpty(resourcesPath))
            {
                var loadedNodes = Resources.LoadAll<UpgradeNode>(resourcesPath);
                foreach (var node in loadedNodes)
                {
                    if (node == null)
                    {
                        continue;
                    }

                    if (!nodeLookup.ContainsKey(node.Id))
                    {
                        upgradeNodes.Add(node);
                    }

                    nodeLookup[node.Id] = node;
                }
            }
        }

        private void OnEnable()
        {
            if (contractSystem != null)
            {
                contractSystem.CreditsChanged += HandleCreditsChanged;
            }

            upgradePanel?.BindSystem(this);
            ApplyAllUpgrades();
            RaiseStateChanged();
        }

        private void Start()
        {
            // Ensure upgrades are applied after all lines complete their setup.
            ApplyAllUpgrades();
            RaiseStateChanged();
        }

        private void OnDisable()
        {
            if (contractSystem != null)
            {
                contractSystem.CreditsChanged -= HandleCreditsChanged;
            }

            upgradePanel?.UnbindSystem(this);
        }

        public IReadOnlyCollection<string> PurchasedNodeIds => purchasedNodeIds;

        public bool IsPurchased(UpgradeNode node)
        {
            if (node == null)
            {
                return false;
            }

            return purchasedNodeIds.Contains(node.Id);
        }

        public bool IsUnlocked(UpgradeNode node)
        {
            if (node == null)
            {
                return false;
            }

            var prerequisite = node.Prerequisite;
            if (prerequisite == null)
            {
                return true;
            }

            return IsPurchased(prerequisite);
        }

        public bool CanPurchase(UpgradeNode node)
        {
            if (node == null)
            {
                return false;
            }

            if (!nodeLookup.ContainsKey(node.Id))
            {
                return false;
            }

            if (IsPurchased(node))
            {
                return false;
            }

            if (!IsUnlocked(node))
            {
                return false;
            }

            return AvailableCredits >= node.Cost;
        }

        public bool TryPurchase(UpgradeNode node)
        {
            if (!CanPurchase(node))
            {
                return false;
            }

            if (contractSystem != null && !contractSystem.TrySpendCredits(node.Cost))
            {
                return false;
            }

            purchasedNodeIds.Add(node.Id);
            ApplyAllUpgrades();
            UpgradePurchased?.Invoke(node);
            RaiseStateChanged();
            return true;
        }

        private void HandleCreditsChanged(int _)
        {
            RaiseStateChanged();
        }

        private void RaiseStateChanged()
        {
            StateChanged?.Invoke();
        }

        private void ApplyAllUpgrades()
        {
            var perfectWindowAdd = 0f;
            var goodWindowAdd = 0f;
            var comboStepDelta = 0;
            var maxMultiplierDelta = 0;
            var multiplierStepAdd = 0f;
            var blissPerfectAdd = 0f;
            var blissGoodAdd = 0f;
            var blissFailPenaltyAdd = 0f;
            var beltSpeedMultiplier = 1f;
            var spawnIntervalMultiplier = 1f;
            int? failPenaltyOverride = null;

            foreach (var id in purchasedNodeIds)
            {
                if (!nodeLookup.TryGetValue(id, out var node) || node == null)
                {
                    continue;
                }

                foreach (var effect in node.Effects)
                {
                    if (effect == null)
                    {
                        continue;
                    }

                    var value = effect.Value;

                    switch (effect.EffectType)
                    {
                        case UpgradeEffectType.IncreasePerfectWindow:
                            perfectWindowAdd += value;
                            break;
                        case UpgradeEffectType.IncreaseGoodWindow:
                            goodWindowAdd += value;
                            break;
                        case UpgradeEffectType.AdjustComboStep:
                            comboStepDelta += Mathf.RoundToInt(value);
                            break;
                        case UpgradeEffectType.IncreaseMaxMultiplierLevel:
                            maxMultiplierDelta += Mathf.RoundToInt(value);
                            break;
                        case UpgradeEffectType.IncreaseMultiplierStep:
                            multiplierStepAdd += value;
                            break;
                        case UpgradeEffectType.IncreaseBlissPerfectGain:
                            blissPerfectAdd += value;
                            break;
                        case UpgradeEffectType.IncreaseBlissGoodGain:
                            blissGoodAdd += value;
                            break;
                        case UpgradeEffectType.AdjustBlissFailPenalty:
                            blissFailPenaltyAdd += value;
                            break;
                        case UpgradeEffectType.AdjustBeltSpeedMultiplier:
                            beltSpeedMultiplier *= Mathf.Clamp(value, 0.25f, 4f);
                            break;
                        case UpgradeEffectType.AdjustSpawnIntervalMultiplier:
                            spawnIntervalMultiplier *= Mathf.Clamp(value, 0.25f, 4f);
                            break;
                        case UpgradeEffectType.SetFailPenalty:
                            var penaltyValue = Mathf.Max(0, Mathf.RoundToInt(value));
                            if (failPenaltyOverride.HasValue)
                            {
                                failPenaltyOverride = Mathf.Min(failPenaltyOverride.Value, penaltyValue);
                            }
                            else
                            {
                                failPenaltyOverride = penaltyValue;
                            }
                            break;
                    }
                }
            }

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line == null)
                {
                    continue;
                }

                var perfectWindow = Mathf.Clamp(line.BasePerfectWindow + perfectWindowAdd, 0.01f, 0.5f);
                var goodWindow = Mathf.Clamp(line.BaseGoodWindow + goodWindowAdd, perfectWindow, 0.6f);
                var comboValue = Mathf.Max(1, line.BaseComboStep + comboStepDelta);
                var maxMultiplier = Mathf.Max(0, line.BaseMaxMultiplierLevel + maxMultiplierDelta);
                var multiplierStep = Mathf.Max(0f, line.BaseMultiplierStep + multiplierStepAdd);
                var blissPerfectValue = Mathf.Max(0f, line.BaseBlissPerfect + blissPerfectAdd);
                var blissGoodValue = Mathf.Max(0f, line.BaseBlissGood + blissGoodAdd);
                var blissFailPenaltyValue = Mathf.Max(0f, line.BaseBlissFailPenalty + blissFailPenaltyAdd);
                var failPenalty = failPenaltyOverride.HasValue
                    ? Mathf.Max(0, failPenaltyOverride.Value)
                    : line.BaseFailPenalty;

                line.ApplyJudgeOverrides(
                    perfectWindow,
                    goodWindow,
                    comboValue,
                    maxMultiplier,
                    multiplierStep,
                    blissPerfectValue,
                    blissGoodValue,
                    blissFailPenaltyValue,
                    failPenalty);

                line.SetSpeedMultiplier(Mathf.Clamp(beltSpeedMultiplier, 0.25f, 4f));
                line.SetSpawnIntervalMultiplier(Mathf.Clamp(spawnIntervalMultiplier, 0.25f, 4f));
            }
        }

        private void BuildLookup(IEnumerable<UpgradeNode> source)
        {
            if (source == null)
            {
                return;
            }

            foreach (var node in source)
            {
                if (node == null)
                {
                    continue;
                }

                nodeLookup[node.Id] = node;
            }
        }
    }
}
