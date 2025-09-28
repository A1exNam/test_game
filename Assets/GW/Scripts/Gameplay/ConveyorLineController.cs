using System;
using System.Collections.Generic;
using UnityEngine;
using GW.Core;

namespace GW.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ConveyorLineController : MonoBehaviour
    {
        [Header("Belt Setup")]
        [SerializeField]
        private LineId lineId = LineId.A;

        [SerializeField]
        private CandyActor candyPrefab;

        [SerializeField]
        private Transform spawnPoint;

        [SerializeField]
        private Transform sealPoint;

        [SerializeField]
        private Transform despawnPoint;

        [SerializeField]
        private float spawnInterval = 1.2f;

        [SerializeField]
        [Range(0.25f, 3f)]
        private float beltSpeed = 1f;

        [Header("Judge Settings")]
        [SerializeField]
        private float perfectWindow = 0.05f;

        [SerializeField]
        private float goodWindow = 0.12f;

        [SerializeField]
        private int comboStep = 10;

        [SerializeField]
        private int maxMultiplierLevel = 4;

        [SerializeField]
        private float multiplierStep = 0.5f;

        [SerializeField]
        private float blissPerfect = 0.08f;

        [SerializeField]
        private float blissGood = 0.02f;

        [SerializeField]
        private float blissFailPenalty = 0.15f;

        [SerializeField]
        private int failPenalty = 5;

        [SerializeField]
        private SealZone sealZone;

        [SerializeField]
        private string activePatternId = string.Empty;

        [Header("Foil Patterns")]
        [SerializeField]
        private FoilPatternLibrary foilPatternLibrary;

        [SerializeField]
        [Tooltip("If true, a pattern will be auto-selected when the line starts up.")]
        private bool selectPatternOnStart = true;

        [SerializeField]
        [Tooltip("Resource path used when attempting to auto-load a pattern library.")]
        private string patternLibraryResourcePath = "FoilPatternLibrary";

        [SerializeField]
        [Tooltip("Rarity bias used when auto-selecting patterns and a library is available.")]
        private FoilPatternRarity defaultPatternRarity = FoilPatternRarity.Common;

        [SerializeField]
        [Tooltip("If disabled, all candies on the line share a cached runtime variant of the active pattern.")]
        private bool randomizePatternPerCandy = true;

        public event Action<int> ScoreChanged;
        public event Action<int> ComboChanged;
        public event Action<int> MultiplierChanged;
        public event Action<float> BlissChanged;
        public event Action<ConveyorLineController, SealGrade> SealResolved;
        public event Action<ConveyorLineController, FoilPatternDef> ActivePatternChanged;

        public int Score => score;
        public SealJudge Judge => judge;
        public Vector3 Forward => forwardDirection;
        public Transform SealPoint => sealPoint;
        public LineId LineId => lineId;
        public string ActivePatternId => activePatternId;
        public FoilPatternLibrary PatternLibrary => foilPatternLibrary;
        public FoilPatternDef ActivePatternDefinition => activePatternDef;
        public float BasePerfectWindow => perfectWindow;
        public float BaseGoodWindow => goodWindow;
        public int BaseComboStep => comboStep;
        public int BaseMaxMultiplierLevel => maxMultiplierLevel;
        public float BaseMultiplierStep => multiplierStep;
        public float BaseBlissPerfect => blissPerfect;
        public float BaseBlissGood => blissGood;
        public float BaseBlissFailPenalty => blissFailPenalty;
        public int BaseFailPenalty => failPenalty;
        public float BaseBeltSpeed => beltSpeed;
        public float BaseSpawnInterval => spawnInterval;
        public float CurrentBeltSpeed => GetEffectiveBeltSpeed();
        public float CurrentSpawnInterval => Mathf.Max(0.1f, spawnInterval * spawnIntervalMultiplier);

        private readonly List<CandyActor> activeCandies = new List<CandyActor>();
        private readonly Queue<CandyActor> pool = new Queue<CandyActor>();
        private SealJudge judge;
        private float spawnTimer;
        private Vector3 forwardDirection = Vector3.right;
        private float pathLength;
        private int score;
        private float beltSpeedMultiplier = 1f;
        private float spawnIntervalMultiplier = 1f;
        private FoilPatternDef activePatternDef;
        private FoilPatternRuntime? cachedPatternRuntime;
        private System.Random patternRandom;

        private void Awake()
        {
            if (spawnPoint == null || sealPoint == null || despawnPoint == null)
            {
                Debug.LogError("ConveyorLineController requires spawn, seal, and despawn points configured.", this);
            }

            forwardDirection = (despawnPoint.position - spawnPoint.position).normalized;
            pathLength = Vector3.Distance(spawnPoint.position, despawnPoint.position);

            judge = new SealJudge(perfectWindow, goodWindow, comboStep, maxMultiplierLevel, multiplierStep, blissPerfect, blissGood, blissFailPenalty, failPenalty);
            judge.OnScored += HandleScored;
            judge.OnStateChanged += HandleJudgeStateChanged;

            EnsurePatternRandom();
            EnsurePatternLibrary();

            if (selectPatternOnStart)
            {
                if (!string.IsNullOrEmpty(activePatternId))
                {
                    SetActivePattern(activePatternId);
                }
                else
                {
                    SelectRandomPattern();
                }
            }
            else if (!string.IsNullOrEmpty(activePatternId))
            {
                SetActivePattern(activePatternId);
            }
        }

        private void OnEnable()
        {
            if (sealZone != null)
            {
                sealZone.BindLine(this);
            }
        }

        private void OnDisable()
        {
            if (sealZone != null)
            {
                sealZone.UnbindLine(this);
            }
        }

        private void Update()
        {
            var delta = Time.deltaTime;
            TickSpawn(delta);
            TickCandies(delta);
        }

        private void TickSpawn(float deltaTime)
        {
            if (candyPrefab == null)
            {
                return;
            }

            spawnTimer += deltaTime;
            var interval = CurrentSpawnInterval;
            if (spawnTimer < interval)
            {
                return;
            }

            while (spawnTimer >= interval)
            {
                spawnTimer -= interval;
                SpawnCandy();
            }
        }

        private void TickCandies(float deltaTime)
        {
            if (activeCandies.Count == 0)
            {
                return;
            }

            for (var i = activeCandies.Count - 1; i >= 0; i--)
            {
                var candy = activeCandies[i];
                if (candy == null || !candy.IsActive)
                {
                    activeCandies.RemoveAt(i);
                    continue;
                }

                candy.Tick(deltaTime);

                var distanceTravelled = Vector3.Dot(candy.transform.position - spawnPoint.position, forwardDirection);
                if (distanceTravelled > pathLength + 0.1f)
                {
                    RecycleCandy(candy);
                    activeCandies.RemoveAt(i);
                }
            }
        }

        private void SpawnCandy()
        {
            var candy = GetOrCreateCandy();
            var speed = GetEffectiveBeltSpeed();
            candy.SetSpeed(speed);

            var runtime = randomizePatternPerCandy ? BuildPatternRuntime() : GetCachedPatternRuntime();
            candy.Activate(this, spawnPoint.position, forwardDirection, speed, runtime);
            activeCandies.Add(candy);
        }

        private FoilPatternRuntime? GetCachedPatternRuntime()
        {
            if (!cachedPatternRuntime.HasValue)
            {
                cachedPatternRuntime = BuildPatternRuntime();
            }

            return cachedPatternRuntime;
        }

        private FoilPatternRuntime? BuildPatternRuntime()
        {
            if (foilPatternLibrary == null || activePatternDef == null)
            {
                return null;
            }

            EnsurePatternRandom();
            return foilPatternLibrary.CreateRuntime(activePatternDef, patternRandom);
        }

        private CandyActor GetOrCreateCandy()
        {
            CandyActor candy;
            if (pool.Count > 0)
            {
                candy = pool.Dequeue();
            }
            else
            {
                candy = Instantiate(candyPrefab, spawnPoint.position, Quaternion.identity, transform);
            }

            candy.Despawned -= HandleCandyDespawned;
            candy.Despawned += HandleCandyDespawned;
            return candy;
        }

        private void HandleCandyDespawned(CandyActor candy)
        {
            if (!pool.Contains(candy))
            {
                pool.Enqueue(candy);
            }
        }

        private void RecycleCandy(CandyActor candy)
        {
            candy.Despawn();
        }

        internal void ProcessSealAttempt(CandyActor candy, float offset)
        {
            if (candy == null || !activeCandies.Contains(candy))
            {
                return;
            }

            var grade = judge.OnSeal(Mathf.Abs(offset));
            SealResolved?.Invoke(this, grade);

            activeCandies.Remove(candy);
            RecycleCandy(candy);
        }

        internal float CalculateOffsetFromSealPoint(Vector3 worldPosition)
        {
            if (sealPoint == null)
            {
                return 0f;
            }

            var delta = worldPosition - sealPoint.position;
            return Vector3.Dot(delta, forwardDirection);
        }

        private void HandleScored(int delta, SealGrade grade)
        {
            score = Mathf.Max(0, score + delta);
            ScoreChanged?.Invoke(score);
        }

        private void HandleJudgeStateChanged()
        {
            ComboChanged?.Invoke(judge.Combo);
            MultiplierChanged?.Invoke(judge.MultiplierLevel);
            BlissChanged?.Invoke(judge.Bliss);

            var save = SaveSystem.Current;
            if (judge.Combo > save.bestCombo)
            {
                save.bestCombo = judge.Combo;
                SaveSystem.Save();
            }
        }

        public void ForceReset()
        {
            score = 0;
            ScoreChanged?.Invoke(score);
            judge.Reset();
        }

        public void SetActivePattern(string patternId)
        {
            activePatternId = patternId ?? string.Empty;

            EnsurePatternLibrary();
            EnsurePatternRandom();

            if (foilPatternLibrary == null)
            {
                activePatternDef = null;
                cachedPatternRuntime = null;
                ActivePatternChanged?.Invoke(this, null);
                return;
            }

            FoilPatternDef resolved = null;
            if (!string.IsNullOrEmpty(activePatternId))
            {
                resolved = foilPatternLibrary.GetById(activePatternId);
            }

            if (resolved == null)
            {
                resolved = foilPatternLibrary.GetRandomPattern(null, patternRandom);
            }

            activePatternDef = resolved;
            cachedPatternRuntime = null;

            if (resolved != null)
            {
                activePatternId = resolved.Id;
            }

            ActivePatternChanged?.Invoke(this, activePatternDef);
        }

        public void SelectRandomPattern(FoilPatternRarity? rarityOverride = null)
        {
            EnsurePatternLibrary();
            EnsurePatternRandom();

            if (foilPatternLibrary == null)
            {
                return;
            }

            FoilPatternDef pattern;
            if (rarityOverride.HasValue)
            {
                pattern = foilPatternLibrary.GetRandomPattern(rarityOverride, patternRandom);
                pattern ??= foilPatternLibrary.GetRandomPattern(null, patternRandom);
            }
            else
            {
                pattern = foilPatternLibrary.GetRandomPattern(null, patternRandom);
                if (pattern == null)
                {
                    pattern = foilPatternLibrary.GetRandomPattern(defaultPatternRarity, patternRandom);
                }
            }

            if (pattern != null)
            {
                SetActivePattern(pattern.Id);
            }
        }

        public void ApplyJudgeOverrides(
            float perfectWindowOverride,
            float goodWindowOverride,
            int comboStepOverride,
            int maxMultiplierOverride,
            float multiplierStepOverride,
            float blissPerfectOverride,
            float blissGoodOverride,
            float blissFailPenaltyOverride,
            int failPenaltyOverride)
        {
            if (judge == null)
            {
                return;
            }

            var parameters = new SealJudge.Parameters
            {
                PerfectWindow = perfectWindowOverride,
                GoodWindow = goodWindowOverride,
                ComboStep = comboStepOverride,
                MaxMultiplierLevel = maxMultiplierOverride,
                MultiplierStep = multiplierStepOverride,
                BlissPerfect = blissPerfectOverride,
                BlissGood = blissGoodOverride,
                BlissFailPenalty = blissFailPenaltyOverride,
                FailPenalty = failPenaltyOverride,
            };

            judge.ApplyParameters(parameters);
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            beltSpeedMultiplier = Mathf.Clamp(multiplier, 0.25f, 4f);
            var newSpeed = GetEffectiveBeltSpeed();

            for (var i = 0; i < activeCandies.Count; i++)
            {
                var candy = activeCandies[i];
                if (candy == null)
                {
                    continue;
                }

                candy.SetSpeed(newSpeed);
            }
        }

        public void SetSpawnIntervalMultiplier(float multiplier)
        {
            spawnIntervalMultiplier = Mathf.Clamp(multiplier, 0.25f, 4f);
            spawnTimer = Mathf.Min(spawnTimer, CurrentSpawnInterval);
        }

        private float GetEffectiveBeltSpeed()
        {
            return Mathf.Clamp(beltSpeed * beltSpeedMultiplier, 0.05f, 6f);
        }

        private void EnsurePatternLibrary()
        {
            if (foilPatternLibrary != null || string.IsNullOrWhiteSpace(patternLibraryResourcePath))
            {
                return;
            }

            var loaded = Resources.Load<FoilPatternLibrary>(patternLibraryResourcePath);
            if (loaded != null)
            {
                foilPatternLibrary = loaded;
            }
        }

        private void EnsurePatternRandom()
        {
            patternRandom ??= new System.Random(Environment.TickCount ^ GetInstanceID());
        }
    }
}
