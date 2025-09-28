using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlissSeal
{
    public enum SealRating
    {
        None,
        Fail,
        Good,
        Perfect
    }

    public struct SealOutcome
    {
        public CandyLaneController lane;
        public SealRating rating;
        public float offsetPx;
        public bool wasAuto;
        public bool hadCandy;
    }

    [Serializable]
    public class LaneState
    {
        public float calibrationBonusPx;
        public float calibrationTimer;
        public float laneSwapTimer;
    }

    internal class CandyInstance
    {
        public Transform transform;
        public Renderer renderer;
        public float positionX;
    }

    public class CandyLaneController : MonoBehaviour
    {
        [Header("Lane settings")]
        [SerializeField] private string laneLabel = "A";
        [SerializeField] private Color laneColor = Color.white;
        [SerializeField] private bool startActive = true;

        [Header("Visual layout")]
        [SerializeField] private float laneHeightOffset = 0f;
        [SerializeField] private float laneDepth = 0f;
        [SerializeField] private float spawnLeadUnits = 6.5f;
        [SerializeField] private float despawnLeadUnits = 6.5f;
        [SerializeField] private float candyHeightUnits = 0.7f;

        private readonly List<CandyInstance> _candies = new List<CandyInstance>();
        private readonly LaneState _state = new LaneState();

        private GameController _controller;
        private GameBalance _balance;
        private LaneSpeedConfig _speedConfig;
        private Vector3 _zonePosition;

        private float _currentSpeedPx;
        private float _timeSinceSpeedIncrease;
        private float _jitterTimer;
        private float _jitterPercent;
        private float _spawnTimer;
        private bool _isActive;

        private float _candyWidthUnits;

        public string LaneLabel => laneLabel;
        public bool IsActive => _isActive;
        public LaneState State => _state;
        public float CurrentSpeedPx => _currentSpeedPx * (1f + _jitterPercent);

        public event Action<SealOutcome> OnAutoOutcome;

        public void Initialize(GameController controller, GameBalance balance, LaneSpeedConfig config, Vector3 zonePosition)
        {
            _controller = controller;
            _balance = balance;
            _speedConfig = config.Clone();
            _zonePosition = zonePosition;
            _currentSpeedPx = _speedConfig.startSpeedPxPerSecond;
            _candyWidthUnits = _balance.PixelsToUnits(220f);
            _isActive = startActive;

            ScheduleNextJitter();
        }

        public void ResetLane()
        {
            foreach (var candy in _candies)
            {
                if (candy?.transform)
                {
                    Destroy(candy.transform.gameObject);
                }
            }
            _candies.Clear();
            _state.calibrationBonusPx = 0f;
            _state.calibrationTimer = 0f;
            _state.laneSwapTimer = 0f;
            _currentSpeedPx = _speedConfig.startSpeedPxPerSecond;
            _timeSinceSpeedIncrease = 0f;
            _jitterTimer = 0f;
            _spawnTimer = 0f;
            ScheduleNextJitter();
        }

        public void SetActive(bool active)
        {
            _isActive = active;
        }

        public void NotifyLaneSwap()
        {
            _state.laneSwapTimer = Mathf.Max(_state.laneSwapTimer, _controller.Balance.multiLane.laneSwapBonusDurationSeconds);
        }

        public void ApplyCalibrationBonus()
        {
            var economy = _controller.Balance.economy;
            _state.calibrationBonusPx = Mathf.Min(_state.calibrationBonusPx + economy.calibrationWindowBonusPx, economy.calibrationWindowMaxStackPx);
            _state.calibrationTimer = Mathf.Max(_state.calibrationTimer, economy.calibrationDurationSeconds);
        }

        public float GetCurrentPerfectWindowPx(float baseWindowPx)
        {
            float bonus = _state.calibrationBonusPx;
            if (_state.laneSwapTimer > 0f)
            {
                bonus += _controller.Balance.multiLane.laneSwapPerfectBonusPx;
            }
            return baseWindowPx + bonus;
        }

        private float GetSpawnInterval(float speedPx)
        {
            return _controller.Balance.spawn.gapPx / Mathf.Max(1f, speedPx);
        }

        private void ScheduleNextJitter()
        {
            _jitterTimer = UnityEngine.Random.Range(_speedConfig.jitterIntervalMinSeconds, _speedConfig.jitterIntervalMaxSeconds);
            _jitterPercent = UnityEngine.Random.Range(-_speedConfig.jitterPercent, _speedConfig.jitterPercent);
        }

        private Vector3 GetSpawnPosition()
        {
            float y = transform.position.y + laneHeightOffset;
            float z = transform.position.z + laneDepth;
            float x = _zonePosition.x - spawnLeadUnits;
            return new Vector3(x, y, z);
        }

        private Vector3 GetDespawnPosition()
        {
            float y = transform.position.y + laneHeightOffset;
            float z = transform.position.z + laneDepth;
            float x = _zonePosition.x + despawnLeadUnits;
            return new Vector3(x, y, z);
        }

        private void SpawnCandy()
        {
            if (_candies.Count >= _controller.Balance.spawn.maxPoolSize)
            {
                return;
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = $"Candy_{laneLabel}_{_candies.Count}";
            go.transform.SetParent(transform);
            go.transform.localScale = new Vector3(_candyWidthUnits, candyHeightUnits, 1f);
            go.transform.position = GetSpawnPosition();
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = new Material(renderer.sharedMaterial)
            {
                color = laneColor
            };
            var instance = new CandyInstance
            {
                transform = go.transform,
                renderer = renderer,
                positionX = go.transform.position.x
            };
            if (go.TryGetComponent<MeshCollider>(out var collider))
            {
                Destroy(collider);
            }
            _candies.Add(instance);
        }

        private void DespawnCandy(int index)
        {
            var candy = _candies[index];
            if (candy?.transform)
            {
                Destroy(candy.transform.gameObject);
            }
            _candies.RemoveAt(index);
        }

        private void DespawnCandy(CandyInstance candy)
        {
            if (candy == null)
            {
                return;
            }

            if (candy.transform)
            {
                Destroy(candy.transform.gameObject);
            }
            _candies.Remove(candy);
        }

        private void UpdateCandies(float deltaTime, float perfectWindowPx, float goodWindowPx)
        {
            for (int i = _candies.Count - 1; i >= 0; i--)
            {
                var candy = _candies[i];
                if (candy == null)
                {
                    _candies.RemoveAt(i);
                    continue;
                }

                var targetSpeedUnits = CurrentSpeedPx / _controller.Balance.pixelsPerUnit;
                candy.positionX += targetSpeedUnits * deltaTime;
                var position = candy.transform.position;
                position.x = candy.positionX;
                candy.transform.position = position;

                var offsetPx = (candy.positionX - _zonePosition.x) * _controller.Balance.pixelsPerUnit;
                if (offsetPx >= goodWindowPx)
                {
                    DespawnCandy(i);
                    OnAutoOutcome?.Invoke(new SealOutcome
                    {
                        lane = this,
                        rating = SealRating.Fail,
                        offsetPx = offsetPx,
                        wasAuto = true,
                        hadCandy = true
                    });
                }
            }
        }

        private void UpdateSpeed(float deltaTime)
        {
            _timeSinceSpeedIncrease += deltaTime;
            if (_currentSpeedPx < _speedConfig.speedCapPxPerSecond && _timeSinceSpeedIncrease >= _speedConfig.increaseIntervalSeconds)
            {
                _currentSpeedPx = Mathf.Min(_speedConfig.speedCapPxPerSecond, _currentSpeedPx + _speedConfig.speedIncreasePerInterval);
                _timeSinceSpeedIncrease = 0f;
            }
        }

        private void UpdateBonuses(float deltaTime)
        {
            if (_state.calibrationTimer > 0f)
            {
                _state.calibrationTimer -= deltaTime;
                if (_state.calibrationTimer <= 0f)
                {
                    _state.calibrationTimer = 0f;
                    _state.calibrationBonusPx = 0f;
                }
            }

            if (_state.laneSwapTimer > 0f)
            {
                _state.laneSwapTimer -= deltaTime;
                if (_state.laneSwapTimer < 0f)
                {
                    _state.laneSwapTimer = 0f;
                }
            }
        }

        public void Tick(float deltaTime, float basePerfectWindowPx, float goodWindowPx)
        {
            if (!_isActive)
            {
                return;
            }

            UpdateSpeed(deltaTime);
            UpdateBonuses(deltaTime);

            _jitterTimer -= deltaTime;
            if (_jitterTimer <= 0f)
            {
                ScheduleNextJitter();
            }

            var spawnInterval = GetSpawnInterval(CurrentSpeedPx);
            _spawnTimer += deltaTime;
            if (_spawnTimer >= spawnInterval)
            {
                _spawnTimer -= spawnInterval;
                SpawnCandy();
            }

            UpdateCandies(deltaTime, GetCurrentPerfectWindowPx(basePerfectWindowPx), goodWindowPx);
        }

        public SealOutcome TrySealCandy(float basePerfectWindowPx, float goodWindowPx, bool blissActive)
        {
            if (!_isActive)
            {
                return new SealOutcome { lane = this, rating = SealRating.None, hadCandy = false };
            }

            if (_candies.Count == 0)
            {
                return new SealOutcome { lane = this, rating = SealRating.None, hadCandy = false };
            }

            var candy = _candies[0];
            if (candy == null || candy.transform == null)
            {
                _candies.RemoveAt(0);
                return new SealOutcome { lane = this, rating = SealRating.None, hadCandy = false };
            }

            float offsetPx = (candy.transform.position.x - _zonePosition.x) * _controller.Balance.pixelsPerUnit;
            float effectiveOffsetPx = offsetPx;

            if (blissActive)
            {
                var autoSnap = _controller.Balance.accuracy.blissAutoSnapPercent;
                float snap = Mathf.Min(Mathf.Abs(effectiveOffsetPx), GetCurrentPerfectWindowPx(basePerfectWindowPx) * autoSnap);
                effectiveOffsetPx -= Mathf.Sign(effectiveOffsetPx) * snap;
            }

            var perfectWindow = GetCurrentPerfectWindowPx(basePerfectWindowPx);
            var goodWindow = goodWindowPx;
            var speedPx = Mathf.Abs(CurrentSpeedPx);
            var coyoteDistancePx = speedPx * _controller.Balance.accuracy.coyoteTimeSeconds;

            SealRating rating;
            if (Mathf.Abs(effectiveOffsetPx) <= perfectWindow || Mathf.Abs(offsetPx) <= coyoteDistancePx)
            {
                rating = SealRating.Perfect;
            }
            else if (Mathf.Abs(effectiveOffsetPx) <= goodWindow)
            {
                rating = SealRating.Good;
            }
            else
            {
                rating = SealRating.Fail;
            }

            DespawnCandy(candy);

            return new SealOutcome
            {
                lane = this,
                rating = rating,
                offsetPx = offsetPx,
                wasAuto = false,
                hadCandy = true
            };
        }
    }
}
