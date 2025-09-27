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
        private SealZone sealZone;

        public event Action<int> ScoreChanged;
        public event Action<int> ComboChanged;
        public event Action<int> MultiplierChanged;
        public event Action<float> BlissChanged;
        public event Action<SealGrade> SealResolved;

        public int Score => score;
        public SealJudge Judge => judge;
        public Vector3 Forward => forwardDirection;
        public Transform SealPoint => sealPoint;

        private readonly List<CandyActor> activeCandies = new();
        private readonly Queue<CandyActor> pool = new();
        private SealJudge judge;
        private float spawnTimer;
        private Vector3 forwardDirection = Vector3.right;
        private float pathLength;
        private int score;

        private void Awake()
        {
            if (spawnPoint == null || sealPoint == null || despawnPoint == null)
            {
                Debug.LogError("ConveyorLineController requires spawn, seal, and despawn points configured.", this);
            }

            forwardDirection = (despawnPoint.position - spawnPoint.position).normalized;
            pathLength = Vector3.Distance(spawnPoint.position, despawnPoint.position);

            judge = new SealJudge(perfectWindow, goodWindow, comboStep, maxMultiplierLevel);
            judge.OnScored += HandleScored;
            judge.OnStateChanged += HandleJudgeStateChanged;
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
            TickSpawn(Time.deltaTime);
            TickCandies(Time.deltaTime);
        }

        private void TickSpawn(float deltaTime)
        {
            if (candyPrefab == null)
            {
                return;
            }

            spawnTimer += deltaTime;
            if (spawnTimer < spawnInterval)
            {
                return;
            }

            spawnTimer = 0f;
            SpawnCandy();
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
            candy.SetSpeed(beltSpeed);
            candy.Activate(this, spawnPoint.position, forwardDirection, beltSpeed);
            activeCandies.Add(candy);
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
            SealResolved?.Invoke(grade);

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
        }

        public void ForceReset()
        {
            score = 0;
            ScoreChanged?.Invoke(score);
            judge.Reset();
        }
    }
}
