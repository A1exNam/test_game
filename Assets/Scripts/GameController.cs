using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace BlissSeal
{
    public class GameController : MonoBehaviour
    {
        [Header("Core references")]
        [SerializeField] private GameBalance balance;
        [SerializeField] private Transform sealZone;
        [SerializeField] private List<CandyLaneController> lanes = new List<CandyLaneController>();

        [Header("Gameplay state")]
        [SerializeField] private bool enableLaneBAtStart;
        [SerializeField] private bool enableLaneCAtStart;
        [Header("Seal zone visual")]
        [SerializeField] private bool createSealZoneVisual = true;
        [SerializeField] private Color sealZoneColor = new Color(1f, 1f, 1f, 0.2f);
        [SerializeField] private float sealZoneHeightUnits = 6f;

        private int _score;
        private int _credits;
        private int _waste;
        private int _combo;
        private int _comboProgress;
        private float _multiplier;
        private float _bliss;
        private float _blissCooldown;
        private float _blissTimer;
        private bool _blissActive;
        private readonly List<float> _blissActivationHistory = new List<float>();

        private float _elapsedActiveTime;
        private float _lastBlissActivationTime = -999f;
        private int _activeLaneIndex = -1;
        private float _currentPerfectWindowPx;
        private GameObject _sealZoneVisual;

        private readonly StringBuilder _laneBuilder = new StringBuilder();
        private string _scoreLabel = string.Empty;
        private string _comboLabel = string.Empty;
        private string _multiplierLabel = string.Empty;
        private string _blissLabel = string.Empty;
        private string _creditsLabel = string.Empty;
        private string _wasteLabel = string.Empty;
        private string _laneLabel = string.Empty;
        private string _windowLabel = string.Empty;
        private GUIStyle _hudLabelStyle;
        private GUIStyle _hudBoxStyle;

        public GameBalance Balance => balance != null ? balance : GameBalance.CreateDefault();
        public bool IsBlissActive => _blissActive;

        private void Awake()
        {
            if (balance == null)
            {
                balance = GameBalance.CreateDefault();
            }

            _multiplier = balance.combo.baseMultiplier;
            _currentPerfectWindowPx = balance.accuracy.perfectWindowPx;

            for (int i = 0; i < lanes.Count; i++)
            {
                var lane = lanes[i];
                if (lane == null)
                {
                    continue;
                }

                var config = balance.laneA;
                if (i == 1)
                {
                    config = balance.laneB;
                }
                else if (i == 2)
                {
                    config = balance.laneC;
                }

                var zonePosition = sealZone != null ? sealZone.position : Vector3.zero;
                lane.Initialize(this, balance, config, zonePosition);
                lane.OnAutoOutcome += HandleSealOutcome;
                lane.SetActive(i == 0 || (i == 1 && enableLaneBAtStart) || (i == 2 && enableLaneCAtStart));
            }

            SetActiveLane(0);
            if (sealZone != null && createSealZoneVisual)
            {
                CreateSealZoneVisual();
            }
            UpdateUI();
        }

        private void OnDestroy()
        {
            foreach (var lane in lanes)
            {
                if (lane != null)
                {
                    lane.OnAutoOutcome -= HandleSealOutcome;
                }
            }
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            var laneDelta = _blissActive ? dt * balance.bliss.timeScale : dt;

            UpdatePerfectWindow(dt);
            UpdateBliss(dt);
            UpdateLaneUnlocks();
            UpdateSealZoneVisual();

            bool anyActive = false;
            foreach (var lane in lanes)
            {
                if (lane != null && lane.IsActive)
                {
                    lane.Tick(laneDelta, _currentPerfectWindowPx, balance.accuracy.goodWindowPx);
                    anyActive = true;
                }
            }

            if (anyActive)
            {
                _elapsedActiveTime += dt;
            }

            HandleInput();
            UpdateUI();
        }

        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                TrySealActiveLane();
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SetActiveLane(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SetActiveLane(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SetActiveLane(2);
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                ActivateBliss();
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                TryQuickCalibration();
            }
        }

        private void TrySealActiveLane()
        {
            if (_activeLaneIndex < 0 || _activeLaneIndex >= lanes.Count)
            {
                return;
            }

            var lane = lanes[_activeLaneIndex];
            if (lane == null)
            {
                return;
            }

            var outcome = lane.TrySealCandy(_currentPerfectWindowPx, balance.accuracy.goodWindowPx, _blissActive);
            HandleSealOutcome(outcome);
        }

        private void CreateSealZoneVisual()
        {
            _sealZoneVisual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _sealZoneVisual.name = "SealZoneVisual";
            _sealZoneVisual.transform.SetParent(sealZone, false);
            _sealZoneVisual.transform.localPosition = Vector3.zero;
            var renderer = _sealZoneVisual.GetComponent<Renderer>();
            var material = new Material(renderer.sharedMaterial)
            {
                color = sealZoneColor
            };
            renderer.sharedMaterial = material;
            if (_sealZoneVisual.TryGetComponent<MeshCollider>(out var collider))
            {
                Destroy(collider);
            }

            UpdateSealZoneVisual();
        }

        private void UpdateSealZoneVisual()
        {
            if (_sealZoneVisual == null)
            {
                return;
            }

            float widthUnits = balance.PixelsToUnits(_currentPerfectWindowPx * 2f);
            float heightUnits = Mathf.Max(0.5f, sealZoneHeightUnits);
            _sealZoneVisual.transform.localScale = new Vector3(widthUnits, heightUnits, 1f);
        }

        private void TryQuickCalibration()
        {
            var lane = GetActiveLane();
            if (lane == null)
            {
                return;
            }

            var economy = balance.economy;
            if (_waste < economy.calibrationWasteCost)
            {
                return;
            }

            _waste -= economy.calibrationWasteCost;
            lane.ApplyCalibrationBonus();
        }

        private CandyLaneController GetActiveLane()
        {
            if (_activeLaneIndex < 0 || _activeLaneIndex >= lanes.Count)
            {
                return null;
            }

            var lane = lanes[_activeLaneIndex];
            if (lane == null || !lane.IsActive)
            {
                return null;
            }

            return lane;
        }

        private void UpdatePerfectWindow(float dt)
        {
            var accuracy = balance.accuracy;
            if (accuracy.shrinkIntervalSeconds <= 0f)
            {
                _currentPerfectWindowPx = accuracy.perfectWindowPx;
                return;
            }

            var shrinkSteps = Mathf.FloorToInt(_elapsedActiveTime / accuracy.shrinkIntervalSeconds);
            var target = Mathf.Max(accuracy.perfectWindowMinPx, accuracy.perfectWindowPx - shrinkSteps);
            _currentPerfectWindowPx = Mathf.Clamp(target, accuracy.perfectWindowMinPx, accuracy.perfectWindowMaxPx);
        }

        private void UpdateBliss(float dt)
        {
            if (_blissActive)
            {
                _blissTimer -= dt;
                if (_blissTimer <= 0f)
                {
                    _blissTimer = 0f;
                    _blissActive = false;
                    _bliss = 0f;
                    _blissCooldown = balance.bliss.cooldownSeconds;
                }
            }
            else if (_blissCooldown > 0f)
            {
                _blissCooldown -= dt;
                if (_blissCooldown < 0f)
                {
                    _blissCooldown = 0f;
                }
            }
        }

        private void ActivateBliss()
        {
            if (_blissActive)
            {
                return;
            }

            if (_bliss < balance.bliss.activationThresholdPercent)
            {
                return;
            }

            if (_blissCooldown > 0f)
            {
                return;
            }

            _blissActive = true;
            _bliss = 0f;
            _blissTimer = CalculateBlissDuration();
            _lastBlissActivationTime = Time.time;
            _blissActivationHistory.Add(Time.time);
        }

        private float CalculateBlissDuration()
        {
            var bliss = balance.bliss;
            float duration = bliss.durationSeconds;
            float now = Time.time;

            for (int i = _blissActivationHistory.Count - 1; i >= 0; i--)
            {
                if (now - _blissActivationHistory[i] > bliss.activationStreakWindowSeconds)
                {
                    _blissActivationHistory.RemoveAt(i);
                }
            }

            int streakCount = _blissActivationHistory.Count + 1;
            int penalties = streakCount / 3;
            duration -= penalties * bliss.durationPenaltyPerStreak;
            return Mathf.Clamp(duration, bliss.durationMinSeconds, bliss.durationMaxSeconds);
        }

        private void UpdateLaneUnlocks()
        {
            float time = Time.timeSinceLevelLoad;
            if (lanes.Count > 1 && lanes[1] != null && !lanes[1].IsActive && time >= balance.progression.laneBUnlockTimeSeconds)
            {
                lanes[1].SetActive(true);
            }

            if (lanes.Count > 2 && lanes[2] != null && !lanes[2].IsActive && time >= balance.progression.laneCUnlockTimeSeconds)
            {
                lanes[2].SetActive(true);
            }
        }

        private void HandleSealOutcome(SealOutcome outcome)
        {
            if (!outcome.hadCandy)
            {
                return;
            }

            switch (outcome.rating)
            {
                case SealRating.Perfect:
                    ApplyPerfectResult(outcome);
                    break;
                case SealRating.Good:
                    ApplyGoodResult();
                    break;
                case SealRating.Fail:
                    ApplyFailResult();
                    break;
            }
        }

        private void ApplyPerfectResult(SealOutcome outcome)
        {
            _combo++;
            _comboProgress++;
            int scoreDelta = Mathf.RoundToInt(balance.score.perfectScore * _multiplier);
            if (_blissActive)
            {
                scoreDelta *= 2;
            }
            _score += scoreDelta;
            _credits += balance.economy.perfectCredits;

            if (_comboProgress >= balance.combo.comboStep)
            {
                _comboProgress = 0;
                _multiplier = Mathf.Min(balance.combo.maxMultiplier, _multiplier + balance.combo.multiplierStep);
            }

            GainBliss(balance.bliss.perfectGainPercent);
        }

        private void ApplyGoodResult()
        {
            int scoreDelta = Mathf.RoundToInt(balance.score.goodScore * _multiplier);
            if (_blissActive)
            {
                scoreDelta *= 2;
            }
            _score += scoreDelta;
            _credits += balance.economy.goodCredits;
            GainBliss(balance.bliss.goodGainPercent);
        }

        private void ApplyFailResult()
        {
            _combo = 0;
            _comboProgress = 0;
            _multiplier = Mathf.Max(balance.combo.baseMultiplier, _multiplier - balance.combo.multiplierStep);
            _score += balance.score.failPenalty;
            _waste += balance.economy.failWaste;
            GainBliss(-balance.bliss.failLossPercent);
        }

        private void GainBliss(float amount)
        {
            float modifier = 1f;
            if (amount > 0f)
            {
                int activeCount = 0;
                foreach (var lane in lanes)
                {
                    if (lane != null && lane.IsActive)
                    {
                        activeCount++;
                    }
                }

                if (activeCount == 2)
                {
                    modifier -= balance.multiLane.twoLaneBlissPenalty;
                }
                else if (activeCount >= 3)
                {
                    modifier -= balance.multiLane.threeLaneBlissPenalty;
                }

                bool recentlyActivated = _lastBlissActivationTime > 0f &&
                    Time.time - _lastBlissActivationTime <= balance.bliss.antiSpamWindowSeconds;
                if (recentlyActivated)
                {
                    modifier -= balance.bliss.antiSpamPenaltyPercent / 100f;
                }
            }

            _bliss = Mathf.Clamp(_bliss + amount * modifier, 0f, 100f);
        }

        private void SetActiveLane(int index)
        {
            if (index < 0 || index >= lanes.Count)
            {
                return;
            }

            var lane = lanes[index];
            if (lane == null || !lane.IsActive)
            {
                return;
            }

            if (_activeLaneIndex == index)
            {
                return;
            }

            _activeLaneIndex = index;
            lane.NotifyLaneSwap();
        }

        private void UpdateUI()
        {
            _scoreLabel = $"Score: {_score}";
            _comboLabel = $"Combo: {_combo}";
            _multiplierLabel = $"Multiplier: x{_multiplier:F1}";

            string status = _blissActive ? "ACTIVE" : _blissCooldown > 0f ? $"CD {_blissCooldown:F1}s" : "Ready";
            _blissLabel = $"Bliss: {_bliss:F1}% ({status})";
            _creditsLabel = $"Credits: {_credits}";
            _wasteLabel = $"Waste: {_waste}";

            _laneBuilder.Clear();
            if (_activeLaneIndex >= 0 && _activeLaneIndex < lanes.Count)
            {
                var lane = lanes[_activeLaneIndex];
                if (lane != null)
                {
                    _laneBuilder.Append("Lane: ");
                    _laneBuilder.Append(lane.LaneLabel);
                }
            }

            if (_laneBuilder.Length == 0)
            {
                _laneBuilder.Append("Lane: ");
                _laneBuilder.Append(_activeLaneIndex + 1);
            }

            _laneLabel = _laneBuilder.ToString();
            _windowLabel = $"Perfect Window: {_currentPerfectWindowPx:F1}px";
        }

        private void EnsureHudStyles()
        {
            if (_hudLabelStyle == null)
            {
                _hudLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    normal = { textColor = Color.white }
                };
            }

            if (_hudBoxStyle == null)
            {
                _hudBoxStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 16
                };
            }
        }

        private void OnGUI()
        {
            EnsureHudStyles();

            GUILayout.BeginArea(new Rect(20f, 20f, 320f, 240f), _hudBoxStyle);
            GUILayout.Label(_scoreLabel, _hudLabelStyle);
            GUILayout.Label(_comboLabel, _hudLabelStyle);
            GUILayout.Label(_multiplierLabel, _hudLabelStyle);
            GUILayout.Label(_blissLabel, _hudLabelStyle);
            GUILayout.Label(_creditsLabel, _hudLabelStyle);
            GUILayout.Label(_wasteLabel, _hudLabelStyle);
            GUILayout.Label(_laneLabel, _hudLabelStyle);
            GUILayout.Label(_windowLabel, _hudLabelStyle);

            GUILayout.Space(8f);
            GUILayout.Label("Controls:", _hudLabelStyle);
            GUILayout.Label("Click - Seal", GUI.skin.label);
            GUILayout.Label("1/2/3 - Switch lanes", GUI.skin.label);
            GUILayout.Label("Space - Activate Bliss", GUI.skin.label);
            GUILayout.Label("C - Quick calibration", GUI.skin.label);
            GUILayout.EndArea();
        }
    }
}
