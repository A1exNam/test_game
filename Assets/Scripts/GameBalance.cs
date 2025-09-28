using System;
using UnityEngine;

namespace BlissSeal
{
    [Serializable]
    public class AccuracyConfig
    {
        [Header("Windows (px @1080p)")]
        [Tooltip("PERFECT window width in pixels. Shrinks over time until it reaches the minimum value.")]
        public float perfectWindowPx = 12f;
        public float perfectWindowMinPx = 9f;
        public float perfectWindowMaxPx = 18f;
        [Tooltip("How often the PERFECT window shrinks by 1px.")]
        public float shrinkIntervalSeconds = 90f;

        [Tooltip("GOOD window width in pixels.")]
        public float goodWindowPx = 28f;
        public float goodWindowMinPx = 22f;
        public float goodWindowMaxPx = 34f;

        [Header("Input forgiveness")]
        [Tooltip("Coyote time for clicks in seconds.")]
        public float coyoteTimeSeconds = 0.045f;
        public float coyoteTimeMinSeconds = 0.03f;
        public float coyoteTimeMaxSeconds = 0.06f;

        [Tooltip("Additional auto snap percent applied when Bliss is active.")]
        [Range(0f, 1f)] public float blissAutoSnapPercent = 0.22f;
        public float blissAutoSnapMinPercent = 0.10f;
        public float blissAutoSnapMaxPercent = 0.35f;
    }

    [Serializable]
    public class LaneSpeedConfig
    {
        [Tooltip("Starting speed of the lane in px/s.")]
        public float startSpeedPxPerSecond = 240f;
        [Tooltip("Maximum speed in px/s.")]
        public float speedCapPxPerSecond = 360f;
        [Tooltip("Speed gain per interval in px/s.")]
        public float speedIncreasePerInterval = 16f;
        [Tooltip("Time between speed increases in seconds.")]
        public float increaseIntervalSeconds = 25f;
        [Tooltip("Jitter percent applied to the speed (0.025 = 2.5%)")]
        public float jitterPercent = 0.025f;
        public float jitterPercentMin = 0f;
        public float jitterPercentMax = 0.04f;
        [Tooltip("Minimum interval for jitter refresh.")]
        public float jitterIntervalMinSeconds = 1.2f;
        [Tooltip("Maximum interval for jitter refresh.")]
        public float jitterIntervalMaxSeconds = 1.8f;

        public LaneSpeedConfig Clone()
        {
            return (LaneSpeedConfig)MemberwiseClone();
        }
    }

    [Serializable]
    public class SpawnConfig
    {
        [Tooltip("Visual gap between candies in pixels.")]
        public float gapPx = 340f;
        public float gapMinPx = 300f;
        public float gapMaxPx = 400f;
        [Tooltip("Maximum simultaneous spawned candies per lane.")]
        public int maxPoolSize = 20;
    }

    [Serializable]
    public class ComboConfig
    {
        [Tooltip("Number of PERFECT hits required to increase the multiplier.")]
        public int comboStep = 8;
        public int comboStepMin = 6;
        public int comboStepMax = 12;
        [Tooltip("Base multiplier value.")]
        public float baseMultiplier = 1f;
        [Tooltip("How much the multiplier increases when reaching the combo step.")]
        public float multiplierStep = 0.5f;
        [Tooltip("Maximum multiplier value.")]
        public float maxMultiplier = 6f;
    }

    [Serializable]
    public class ScoreConfig
    {
        [Tooltip("Score gained for a PERFECT hit (before multiplier).")]
        public int perfectScore = 25;
        [Tooltip("Score gained for a GOOD hit (before multiplier).")]
        public int goodScore = 12;
        [Tooltip("Score penalty for FAIL.")]
        public int failPenalty = -5;
    }

    [Serializable]
    public class EconomyConfig
    {
        public int perfectCredits = 2;
        public int goodCredits = 1;
        public int failWaste = 1;
        [Tooltip("Waste cost of a quick calibration.")]
        public int calibrationWasteCost = 8;
        [Tooltip("Duration of the calibration buff in seconds.")]
        public float calibrationDurationSeconds = 30f;
        [Tooltip("Perfect window bonus (px) per calibration.")]
        public float calibrationWindowBonusPx = 2f;
        [Tooltip("Maximum stack of calibration bonus (px).")]
        public float calibrationWindowMaxStackPx = 4f;
    }

    [Serializable]
    public class BlissConfig
    {
        public float perfectGainPercent = 9f;
        public float goodGainPercent = 3f;
        public float failLossPercent = 15f;
        [Tooltip("Percent penalty to gain if Bliss was triggered recently.")]
        public float antiSpamPenaltyPercent = 30f;
        [Tooltip("Cooldown window for anti spam logic in seconds.")]
        public float antiSpamWindowSeconds = 20f;

        [Header("Activation")]
        public float activationThresholdPercent = 100f;
        public float durationSeconds = 4.8f;
        public float durationMinSeconds = 4f;
        public float durationMaxSeconds = 6f;
        public float cooldownSeconds = 10f;
        public float cooldownMinSeconds = 8f;
        public float cooldownMaxSeconds = 14f;
        public float timeScale = 0.72f;
        [Tooltip("Duration reduction applied on every third activation within 60 seconds.")]
        public float durationPenaltyPerStreak = 0.3f;
        [Tooltip("Window to count Bliss activations for penalty.")]
        public float activationStreakWindowSeconds = 60f;
    }

    [Serializable]
    public class MultiLaneConfig
    {
        [Tooltip("Bliss gain penalty when two lanes are active (0.15 = 15%).")]
        public float twoLaneBlissPenalty = 0.15f;
        [Tooltip("Bliss gain penalty when three lanes are active (0.25 = 25%).")]
        public float threeLaneBlissPenalty = 0.25f;
        [Tooltip("Bonus window applied on lane swap (px).")]
        public float laneSwapPerfectBonusPx = 1f;
        [Tooltip("Duration of the lane swap bonus in seconds.")]
        public float laneSwapBonusDurationSeconds = 2f;
    }

    [Serializable]
    public class SessionProgressionConfig
    {
        public float laneBUnlockTimeSeconds = 300f;
        public float laneCUnlockTimeSeconds = 480f;
    }

    [CreateAssetMenu(fileName = "GameBalance", menuName = "BlissSeal/Game Balance", order = 0)]
    public class GameBalance : ScriptableObject
    {
        public float pixelsPerUnit = 108f;
        public AccuracyConfig accuracy = new AccuracyConfig();
        public SpawnConfig spawn = new SpawnConfig();
        public ComboConfig combo = new ComboConfig();
        public ScoreConfig score = new ScoreConfig();
        public EconomyConfig economy = new EconomyConfig();
        public BlissConfig bliss = new BlissConfig();
        public MultiLaneConfig multiLane = new MultiLaneConfig();
        public SessionProgressionConfig progression = new SessionProgressionConfig();

        [Header("Lane Speeds")]
        public LaneSpeedConfig laneA = new LaneSpeedConfig();
        public LaneSpeedConfig laneB = new LaneSpeedConfig
        {
            startSpeedPxPerSecond = 270f,
            speedCapPxPerSecond = 400f,
            speedIncreasePerInterval = 16f,
            increaseIntervalSeconds = 25f,
            jitterPercent = 0.025f,
            jitterPercentMin = 0f,
            jitterPercentMax = 0.04f,
            jitterIntervalMinSeconds = 1.2f,
            jitterIntervalMaxSeconds = 1.8f
        };
        public LaneSpeedConfig laneC = new LaneSpeedConfig
        {
            startSpeedPxPerSecond = 300f,
            speedCapPxPerSecond = 440f,
            speedIncreasePerInterval = 16f,
            increaseIntervalSeconds = 25f,
            jitterPercent = 0.025f,
            jitterPercentMin = 0f,
            jitterPercentMax = 0.04f,
            jitterIntervalMinSeconds = 1.2f,
            jitterIntervalMaxSeconds = 1.8f
        };

        public float PixelsToUnits(float px)
        {
            return px / Mathf.Max(0.0001f, pixelsPerUnit);
        }

        public float UnitsToPixels(float units)
        {
            return units * pixelsPerUnit;
        }

        public static GameBalance CreateDefault()
        {
            var balance = CreateInstance<GameBalance>();
            return balance;
        }
    }
}
