using UnityEngine;
using GW.Core;

namespace GW.Gameplay
{
    [CreateAssetMenu(menuName = "GW/Contracts/Contract Definition", fileName = "ContractDef")]
    public sealed class ContractDef : ScriptableObject
    {
        [SerializeField]
        private string id = System.Guid.NewGuid().ToString();

        [SerializeField]
        private string title = "Contract";

        [SerializeField]
        [TextArea]
        private string description = "Complete the objective.";

        [SerializeField]
        private ContractType type = ContractType.PerfectStreak;

        [SerializeField]
        private LineId targetLine = LineId.Any;

        [SerializeField]
        private string targetPatternId = string.Empty;

        [SerializeField]
        [Min(1)]
        private int goalCount = 5;

        [SerializeField]
        [Min(0)]
        private int rewardCredits = 5;

        [SerializeField]
        [Range(0f, 1f)]
        private float rewardBliss = 0.1f;

        [SerializeField]
        private bool resetProgressOnFail = true;

        public string Id => id;
        public string Title => title;
        public string Description => description;
        public ContractType Type => type;
        public LineId TargetLine => targetLine;
        public string TargetPatternId => targetPatternId;
        public int GoalCount => Mathf.Max(1, goalCount);
        public int RewardCredits => Mathf.Max(0, rewardCredits);
        public float RewardBliss => Mathf.Clamp01(rewardBliss);
        public bool ResetProgressOnFail => resetProgressOnFail;
    }
}
