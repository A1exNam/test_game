using System;
using UnityEngine;
using GW.Core;

namespace GW.Gameplay
{
    /// <summary>
    /// Evaluates seal attempts and maintains combo, multiplier, and bliss state for a conveyor line.
    /// </summary>
    [Serializable]
    public sealed class SealJudge
    {
        public int Combo { get; private set; }
        public int MultiplierLevel { get; private set; }
        public float Bliss { get; private set; }

        public event Action<int, SealGrade> OnScored;
        public event Action OnStateChanged;

        public float PerfectWindow => perfectWindow;
        public float GoodWindow => goodWindow;
        public int ComboStep => comboStep;
        public int MaxMultiplierLevel => maxMultiplierLevel;
        public float MultiplierStep => multiplierStep;
        public float BlissPerfect => blissPerfect;
        public float BlissGood => blissGood;
        public float BlissFailPenalty => blissFailPenalty;
        public int FailPenalty => failPenalty;

        private float perfectWindow;
        private float goodWindow;
        private int comboStep;
        private int maxMultiplierLevel;
        private float multiplierStep;
        private float blissPerfect;
        private float blissGood;
        private float blissFailPenalty;
        private int failPenalty;

        public SealJudge(
            float perfectWindow,
            float goodWindow,
            int comboStep = 10,
            int maxMultiplierLevel = 4,
            float multiplierStep = 0.5f,
            float blissPerfect = 0.08f,
            float blissGood = 0.02f,
            float blissFailPenalty = 0.15f,
            int failPenalty = 5)
        {
            this.perfectWindow = Mathf.Max(0.0001f, perfectWindow);
            this.goodWindow = Mathf.Max(this.perfectWindow, goodWindow);
            this.comboStep = Mathf.Max(1, comboStep);
            this.maxMultiplierLevel = Mathf.Max(0, maxMultiplierLevel);
            this.multiplierStep = Mathf.Max(0f, multiplierStep);
            this.blissPerfect = Mathf.Max(0f, blissPerfect);
            this.blissGood = Mathf.Max(0f, blissGood);
            this.blissFailPenalty = Mathf.Max(0f, blissFailPenalty);
            this.failPenalty = Mathf.Max(0, failPenalty);
        }

        public bool TryConsumeBliss(float threshold = 1f)
        {
            if (Bliss + Mathf.Epsilon < threshold)
            {
                return false;
            }

            Bliss = 0f;
            OnStateChanged?.Invoke();
            return true;
        }

        public SealGrade OnSeal(float offsetAbs)
        {
            SealGrade grade;

            if (offsetAbs <= perfectWindow)
            {
                grade = SealGrade.Perfect;
                Combo++;
                Bliss = Mathf.Clamp01(Bliss + blissPerfect);

                if (Combo % comboStep == 0)
                {
                    MultiplierLevel = Mathf.Min(MultiplierLevel + 1, maxMultiplierLevel);
                }
            }
            else if (offsetAbs <= goodWindow)
            {
                grade = SealGrade.Good;
                Combo = Mathf.Max(1, Combo);
                Bliss = Mathf.Clamp01(Bliss + blissGood);
            }
            else
            {
                grade = SealGrade.Fail;
                Combo = 0;
                MultiplierLevel = Mathf.Max(0, MultiplierLevel - 1);
                Bliss = Mathf.Clamp01(Bliss - blissFailPenalty);
            }

            var deltaScore = CalculateScoreDelta(grade);

            if (grade == SealGrade.Fail)
            {
                deltaScore = -failPenalty;
            }

            OnScored?.Invoke(deltaScore, grade);
            OnStateChanged?.Invoke();
            return grade;
        }

        private int CalculateScoreDelta(SealGrade grade)
        {
            float baseValue = grade switch
            {
                SealGrade.Perfect => 25f,
                SealGrade.Good => 10f,
                _ => 0f,
            };

            if (baseValue <= 0f)
            {
                return 0;
            }

            var multiplier = 1f + MultiplierLevel * multiplierStep;
            return Mathf.RoundToInt(baseValue * multiplier);
        }

        public void Reset()
        {
            Combo = 0;
            MultiplierLevel = 0;
            Bliss = 0f;
            OnStateChanged?.Invoke();
        }

        public void AddBliss(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            Bliss = Mathf.Clamp01(Bliss + amount);
            OnStateChanged?.Invoke();
        }

        public void ApplyParameters(Parameters parameters)
        {
            var changed = false;

            if (parameters.PerfectWindow.HasValue)
            {
                perfectWindow = Mathf.Max(0.0001f, parameters.PerfectWindow.Value);
                changed = true;
            }

            if (parameters.GoodWindow.HasValue)
            {
                goodWindow = Mathf.Max(perfectWindow, parameters.GoodWindow.Value);
                changed = true;
            }
            else
            {
                goodWindow = Mathf.Max(goodWindow, perfectWindow);
            }

            if (parameters.ComboStep.HasValue)
            {
                comboStep = Mathf.Max(1, parameters.ComboStep.Value);
                changed = true;
            }

            if (parameters.MaxMultiplierLevel.HasValue)
            {
                maxMultiplierLevel = Mathf.Max(0, parameters.MaxMultiplierLevel.Value);
                MultiplierLevel = Mathf.Min(MultiplierLevel, maxMultiplierLevel);
                changed = true;
            }

            if (parameters.MultiplierStep.HasValue)
            {
                multiplierStep = Mathf.Max(0f, parameters.MultiplierStep.Value);
                changed = true;
            }

            if (parameters.BlissPerfect.HasValue)
            {
                blissPerfect = Mathf.Max(0f, parameters.BlissPerfect.Value);
                changed = true;
            }

            if (parameters.BlissGood.HasValue)
            {
                blissGood = Mathf.Max(0f, parameters.BlissGood.Value);
                changed = true;
            }

            if (parameters.BlissFailPenalty.HasValue)
            {
                blissFailPenalty = Mathf.Max(0f, parameters.BlissFailPenalty.Value);
                changed = true;
            }

            if (parameters.FailPenalty.HasValue)
            {
                failPenalty = Mathf.Max(0, parameters.FailPenalty.Value);
                changed = true;
            }

            if (changed)
            {
                OnStateChanged?.Invoke();
            }
        }

        [Serializable]
        public struct Parameters
        {
            public float? PerfectWindow;
            public float? GoodWindow;
            public int? ComboStep;
            public int? MaxMultiplierLevel;
            public float? MultiplierStep;
            public float? BlissPerfect;
            public float? BlissGood;
            public float? BlissFailPenalty;
            public int? FailPenalty;
        }
    }
}
