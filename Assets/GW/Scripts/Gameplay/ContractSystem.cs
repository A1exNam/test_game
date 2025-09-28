using System;
using System.Collections.Generic;
using UnityEngine;
using GW.Core;
using GW.UI;

namespace GW.Gameplay
{
    /// <summary>
    /// Tracks active contracts, evaluates progress, and issues rewards.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ContractSystem : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField]
        private List<ConveyorLineController> lines = new();

        [SerializeField]
        private bool autoPopulateLines = true;

        [SerializeField]
        private ContractsPanel contractsPanel;

        [Header("Contracts")]
        [SerializeField]
        private List<ContractDef> contractLibrary = new();

        [SerializeField]
        [Min(1)]
        private int maxActiveContracts = 3;

        [SerializeField]
        [Min(0)]
        private int startingCredits;

        private readonly List<ContractInstance> activeContracts = new();
        private readonly HashSet<string> completedContracts = new();
        private int credits;

        public event Action<int> CreditsChanged;
        public event Action<IReadOnlyList<ContractInstance>> ActiveContractsChanged;

        private void Awake()
        {
            RefreshLines();
            var save = SaveSystem.Current;
            save.EnsureIntegrity();

            credits = Mathf.Max(startingCredits, save.credits);
            if (credits != save.credits)
            {
                save.credits = credits;
            }

            completedContracts.Clear();
            if (save.completedContracts != null)
            {
                for (var i = 0; i < save.completedContracts.Count; i++)
                {
                    var id = save.completedContracts[i];
                    if (!string.IsNullOrEmpty(id))
                    {
                        completedContracts.Add(id);
                    }
                }
            }

            PersistProgress();
        }

        private void OnEnable()
        {
            RefreshLines();

            foreach (var line in lines)
            {
                if (line == null)
                {
                    continue;
                }

                line.SealResolved += HandleSealResolved;
            }

            RefillContracts();
            NotifyStateChanged();
            contractsPanel?.BindSystem(this);
        }

        private void OnDisable()
        {
            foreach (var line in lines)
            {
                if (line == null)
                {
                    continue;
                }

                line.SealResolved -= HandleSealResolved;
            }

            contractsPanel?.UnbindSystem(this);
        }

        public IReadOnlyList<ContractInstance> ActiveContracts => activeContracts;
        public int Credits => credits;

        public void RegisterPanel(ContractsPanel panel)
        {
            if (contractsPanel == panel)
            {
                return;
            }

            contractsPanel = panel;
            contractsPanel?.BindSystem(this);
        }

        public void UnregisterPanel(ContractsPanel panel)
        {
            if (contractsPanel != panel)
            {
                return;
            }

            contractsPanel?.UnbindSystem(this);
            contractsPanel = null;
        }

        public bool TrySpendCredits(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (credits < amount)
            {
                return false;
            }

            credits -= amount;
            CreditsChanged?.Invoke(credits);
            PersistProgress();
            return true;
        }

        private void HandleSealResolved(ConveyorLineController line, SealGrade grade)
        {
            if (line == null || activeContracts.Count == 0)
            {
                return;
            }

            var changed = false;

            for (var i = activeContracts.Count - 1; i >= 0; i--)
            {
                var instance = activeContracts[i];
                if (!MatchesLine(instance.Definition, line.LineId))
                {
                    continue;
                }

                changed |= UpdateContractProgress(instance, grade, line);

                if (instance.IsCompleted)
                {
                    CompleteContract(instance, line);
                    activeContracts.RemoveAt(i);
                    changed = true;
                }
            }

            if (changed)
            {
                RefillContracts();
                NotifyStateChanged();
            }
        }

        private void RefreshLines()
        {
            if (lines == null)
            {
                lines = new List<ConveyorLineController>();
            }

            var unique = new HashSet<ConveyorLineController>();
            for (var i = lines.Count - 1; i >= 0; i--)
            {
                var line = lines[i];
                if (line == null || !unique.Add(line))
                {
                    lines.RemoveAt(i);
                }
            }

            if (!autoPopulateLines)
            {
                return;
            }

            var discovered = FindObjectsOfType<ConveyorLineController>(true);
            for (var i = 0; i < discovered.Length; i++)
            {
                var line = discovered[i];
                if (line == null || !unique.Add(line))
                {
                    continue;
                }

                lines.Add(line);
            }
        }

        private bool UpdateContractProgress(ContractInstance instance, SealGrade grade, ConveyorLineController line)
        {
            var def = instance.Definition;
            var progressChanged = false;

            switch (def.Type)
            {
                case ContractType.PerfectStreak:
                    if (grade == SealGrade.Perfect)
                    {
                        instance.Increment();
                        progressChanged = true;
                    }
                    else if (def.ResetProgressOnFail && instance.Progress > 0)
                    {
                        instance.Reset();
                        progressChanged = true;
                    }
                    break;

                case ContractType.PatternBatch:
                    if (grade == SealGrade.Perfect && PatternMatches(def, line))
                    {
                        instance.Increment();
                        progressChanged = true;
                    }
                    else if (grade == SealGrade.Fail && def.ResetProgressOnFail && instance.Progress > 0)
                    {
                        instance.Reset();
                        progressChanged = true;
                    }
                    break;

                case ContractType.SuccessfulSeals:
                    if (grade != SealGrade.Fail)
                    {
                        instance.Increment();
                        progressChanged = true;
                    }
                    else if (def.ResetProgressOnFail && instance.Progress > 0)
                    {
                        instance.Reset();
                        progressChanged = true;
                    }
                    break;
            }

            return progressChanged;
        }

        private static bool MatchesLine(ContractDef definition, LineId lineId)
        {
            return definition.TargetLine == LineId.Any || definition.TargetLine == lineId;
        }

        private static bool PatternMatches(ContractDef definition, ConveyorLineController line)
        {
            if (line == null)
            {
                return false;
            }

            var target = definition.TargetPatternId;
            if (string.IsNullOrEmpty(target))
            {
                return true;
            }

            return string.Equals(target, line.ActivePatternId, StringComparison.OrdinalIgnoreCase);
        }

        private void CompleteContract(ContractInstance instance, ConveyorLineController line)
        {
            if (instance?.Definition == null)
            {
                return;
            }

            var def = instance.Definition;
            completedContracts.Add(def.Id);

            if (def.RewardCredits > 0)
            {
                credits += def.RewardCredits;
                CreditsChanged?.Invoke(credits);
            }

            if (def.RewardBliss > 0f)
            {
                ApplyBlissReward(line, def.RewardBliss);
            }

            PersistProgress();
        }

        private void ApplyBlissReward(ConveyorLineController line, float amount)
        {
            if (line?.Judge == null)
            {
                return;
            }

            line.Judge.AddBliss(amount);
        }

        private void RefillContracts()
        {
            if (contractLibrary.Count == 0)
            {
                return;
            }

            while (activeContracts.Count < maxActiveContracts)
            {
                var next = DrawRandomContract();
                if (next == null)
                {
                    break;
                }

                activeContracts.Add(new ContractInstance(next));
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RefreshLines();
        }
#endif

        private ContractDef DrawRandomContract()
        {
            var available = new List<ContractDef>();
            foreach (var def in contractLibrary)
            {
                if (def == null)
                {
                    continue;
                }

                if (completedContracts.Contains(def.Id))
                {
                    continue;
                }

                var alreadyActive = activeContracts.Exists(c => c.Definition == def);
                if (alreadyActive)
                {
                    continue;
                }

                available.Add(def);
            }

            if (available.Count == 0)
            {
                return null;
            }

            var index = UnityEngine.Random.Range(0, available.Count);
            return available[index];
        }

        private void NotifyStateChanged()
        {
            ActiveContractsChanged?.Invoke(activeContracts);
            CreditsChanged?.Invoke(credits);
        }

        internal void ResetSystem()
        {
            activeContracts.Clear();
            RefillContracts();
            NotifyStateChanged();
        }

        private void PersistProgress()
        {
            var save = SaveSystem.Current;
            save.credits = credits;

            if (save.completedContracts == null)
            {
                save.completedContracts = new List<string>();
            }
            else
            {
                save.completedContracts.Clear();
            }

            foreach (var id in completedContracts)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    save.completedContracts.Add(id);
                }
            }

            SaveSystem.Save();
        }
    }

    [Serializable]
    public sealed class ContractInstance
    {
        public ContractInstance(ContractDef definition)
        {
            Definition = definition;
        }

        public ContractDef Definition { get; }
        public int Progress { get; private set; }
        public bool IsCompleted => Definition != null && Progress >= Definition.GoalCount;

        public float NormalizedProgress => Definition == null || Definition.GoalCount == 0
            ? 0f
            : Mathf.Clamp01((float)Progress / Definition.GoalCount);

        public void Increment()
        {
            Progress = Mathf.Min(Definition.GoalCount, Progress + 1);
        }

        public void Reset()
        {
            Progress = 0;
        }
    }
}
