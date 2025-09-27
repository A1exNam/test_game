using UnityEngine;

namespace GoldenWrap.Settings
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "GoldenWrap/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [SerializeField] private float perfectWindow = 0.05f;
        [SerializeField] private float goodWindow = 0.12f;
        [SerializeField] private float initialConveyorSpeed = 1.0f;
        [SerializeField] private float blissFillPerfect = 0.08f;
        [SerializeField] private float blissFillGood = 0.02f;
        [SerializeField] private float blissDrainFail = 0.15f;

        public float PerfectWindow => perfectWindow;
        public float GoodWindow => goodWindow;
        public float InitialConveyorSpeed => initialConveyorSpeed;
        public float BlissFillPerfect => blissFillPerfect;
        public float BlissFillGood => blissFillGood;
        public float BlissDrainFail => blissDrainFail;
    }
}
