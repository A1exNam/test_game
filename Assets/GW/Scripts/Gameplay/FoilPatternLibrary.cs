using System;
using System.Collections.Generic;
using UnityEngine;
using GW.Core;

namespace GW.Gameplay
{
    /// <summary>
    /// Holds the catalogue of foil patterns and provides helper methods for rarity-based selection.
    /// </summary>
    [CreateAssetMenu(menuName = "GW/Foil Patterns/Foil Pattern Library", fileName = "FoilPatternLibrary")]
    public sealed class FoilPatternLibrary : ScriptableObject
    {
        [Serializable]
        private struct RarityWeight
        {
            public FoilPatternRarity Rarity;
            [Min(0)]
            public int Weight;
        }

        [SerializeField]
        [Tooltip("All foil patterns that can be used in the game.")]
        private List<FoilPatternDef> patterns = new List<FoilPatternDef>();

        [SerializeField]
        [Tooltip("Optional weight overrides that control the probability of each rarity when sampling.")]
        private List<RarityWeight> rarityWeights = new List<RarityWeight>()
        {
            new RarityWeight { Rarity = FoilPatternRarity.Common, Weight = 70 },
            new RarityWeight { Rarity = FoilPatternRarity.Rare, Weight = 20 },
            new RarityWeight { Rarity = FoilPatternRarity.Epic, Weight = 9 },
            new RarityWeight { Rarity = FoilPatternRarity.Legendary, Weight = 1 },
        };

        [SerializeField]
        [Tooltip("Base tint applied for each rarity when generating runtime data.")]
        private Gradient commonTintGradient;

        [SerializeField]
        private Gradient rareTintGradient;

        [SerializeField]
        private Gradient epicTintGradient;

        [SerializeField]
        private Gradient legendaryTintGradient;

        private readonly Dictionary<string, FoilPatternDef> patternLookup = new Dictionary<string, FoilPatternDef>();
        private static readonly List<FoilPatternDef> PatternBuffer = new List<FoilPatternDef>();
        private static readonly List<int> WeightBuffer = new List<int>();

        public IReadOnlyList<FoilPatternDef> Patterns => patterns;

        private void OnEnable()
        {
            RebuildLookup();
            EnsureGradients();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RebuildLookup();
            EnsureGradients();
        }
#endif

        public FoilPatternDef GetById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            return patternLookup.TryGetValue(id, out var def) ? def : null;
        }

        public bool TryGetById(string id, out FoilPatternDef def)
        {
            def = GetById(id);
            return def != null;
        }

        public FoilPatternDef GetRandomPattern(FoilPatternRarity? rarityOverride = null, System.Random random = null)
        {
            if (patterns == null || patterns.Count == 0)
            {
                return null;
            }

            if (rarityOverride.HasValue)
            {
                PatternBuffer.Clear();
                var rarity = rarityOverride.Value;

                for (var i = 0; i < patterns.Count; i++)
                {
                    var pattern = patterns[i];
                    if (pattern == null || pattern.Rarity != rarity)
                    {
                        continue;
                    }

                    PatternBuffer.Add(pattern);
                }

                if (PatternBuffer.Count == 0)
                {
                    return null;
                }

                var index = SampleIndex(PatternBuffer.Count, random);
                return PatternBuffer[index];
            }

            PatternBuffer.Clear();
            WeightBuffer.Clear();

            var totalWeight = 0;
            for (var i = 0; i < patterns.Count; i++)
            {
                var pattern = patterns[i];
                if (pattern == null)
                {
                    continue;
                }

                var weight = Mathf.Max(0, GetWeight(pattern.Rarity));
                if (weight <= 0)
                {
                    continue;
                }

                PatternBuffer.Add(pattern);
                WeightBuffer.Add(weight);
                totalWeight += weight;
            }

            if (totalWeight <= 0)
            {
                return null;
            }

            var roll = SampleWeighted(totalWeight, random);
            for (var i = 0; i < PatternBuffer.Count; i++)
            {
                var weight = WeightBuffer[i];
                if (roll < weight)
                {
                    return PatternBuffer[i];
                }

                roll -= weight;
            }

            return PatternBuffer.Count > 0 ? PatternBuffer[PatternBuffer.Count - 1] : null;
        }

        public FoilPatternRuntime? GetRandomRuntime(FoilPatternRarity? rarityOverride = null, System.Random random = null)
        {
            var pattern = GetRandomPattern(rarityOverride, random);
            if (pattern == null)
            {
                return null;
            }

            return CreateRuntime(pattern, random);
        }

        public FoilPatternRuntime CreateRuntime(FoilPatternDef pattern, System.Random random = null)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            var rng = random ?? new System.Random(pattern.Seed ^ (int)DateTime.UtcNow.Ticks);
            var jitter = (float)(rng.NextDouble() * 30d - 15d);
            var angle = Mathf.Deg2Rad * (pattern.LineAngle + jitter);
            var flow = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var frequency = pattern.LineFrequency * Mathf.Lerp(0.85f, 1.25f, (float)rng.NextDouble());
            var primary = EvaluateTint(pattern.Rarity, rng, true);
            var secondary = EvaluateTint(pattern.Rarity, rng, false);
            var pulse = Mathf.Lerp(0.5f, 1.75f, (float)rng.NextDouble());
            return new FoilPatternRuntime(pattern, flow, frequency, pattern.Specular, pattern.EmbossDepth, primary, secondary, pulse);
        }

        public List<FoilPatternDef> GetPatternsByRarity(FoilPatternRarity rarity, List<FoilPatternDef> buffer = null)
        {
            var results = buffer ?? new List<FoilPatternDef>();
            results.Clear();

            if (patterns == null)
            {
                return results;
            }

            for (var i = 0; i < patterns.Count; i++)
            {
                var pattern = patterns[i];
                if (pattern == null || pattern.Rarity != rarity)
                {
                    continue;
                }

                results.Add(pattern);
            }

            return results;
        }

        private int GetWeight(FoilPatternRarity rarity)
        {
            if (rarityWeights != null)
            {
                for (var i = 0; i < rarityWeights.Count; i++)
                {
                    var entry = rarityWeights[i];
                    if (entry.Rarity == rarity)
                    {
                        return entry.Weight;
                    }
                }
            }

            return rarity switch
            {
                FoilPatternRarity.Common => 70,
                FoilPatternRarity.Rare => 20,
                FoilPatternRarity.Epic => 9,
                FoilPatternRarity.Legendary => 1,
                _ => 1,
            };
        }

        private static int SampleIndex(int count, System.Random random)
        {
            if (count <= 0)
            {
                return -1;
            }

            return random != null ? random.Next(count) : UnityEngine.Random.Range(0, count);
        }

        private static int SampleWeighted(int totalWeight, System.Random random)
        {
            if (totalWeight <= 0)
            {
                return 0;
            }

            return random != null ? random.Next(totalWeight) : Mathf.FloorToInt(UnityEngine.Random.value * totalWeight);
        }

        private Color EvaluateTint(FoilPatternRarity rarity, System.Random random, bool primary)
        {
            var gradient = rarity switch
            {
                FoilPatternRarity.Common => commonTintGradient,
                FoilPatternRarity.Rare => rareTintGradient,
                FoilPatternRarity.Epic => epicTintGradient,
                FoilPatternRarity.Legendary => legendaryTintGradient,
                _ => commonTintGradient,
            };

            if (gradient == null)
            {
                EnsureGradients();
                gradient = rarity switch
                {
                    FoilPatternRarity.Common => commonTintGradient,
                    FoilPatternRarity.Rare => rareTintGradient,
                    FoilPatternRarity.Epic => epicTintGradient,
                    FoilPatternRarity.Legendary => legendaryTintGradient,
                    _ => commonTintGradient,
                };
            }

            var t = primary ? (float)random.NextDouble() * 0.6f : 0.4f + (float)random.NextDouble() * 0.6f;
            return gradient.Evaluate(Mathf.Clamp01(t));
        }

        private void RebuildLookup()
        {
            patternLookup.Clear();

            if (patterns == null)
            {
                return;
            }

            for (var i = patterns.Count - 1; i >= 0; i--)
            {
                var pattern = patterns[i];
                if (pattern == null)
                {
                    patterns.RemoveAt(i);
                    continue;
                }

                var key = pattern.Id;
                if (string.IsNullOrEmpty(key))
                {
                    Debug.LogWarning($"Foil pattern '{pattern.name}' has no ID assigned and will be skipped.", pattern);
                    continue;
                }

                if (patternLookup.ContainsKey(key))
                {
                    Debug.LogWarning($"Duplicate foil pattern id '{key}' detected. Only the first instance will be used.", pattern);
                    continue;
                }

                patternLookup[key] = pattern;
            }
        }

        private void EnsureGradients()
        {
            if (commonTintGradient == null)
            {
                commonTintGradient = CreateDefaultGradient(new Color32(201, 166, 70, 255), new Color32(143, 122, 47, 255));
            }

            if (rareTintGradient == null)
            {
                rareTintGradient = CreateDefaultGradient(new Color32(255, 211, 110, 255), new Color32(201, 166, 70, 255));
            }

            if (epicTintGradient == null)
            {
                epicTintGradient = CreateDefaultGradient(new Color32(255, 238, 163, 255), new Color32(201, 166, 70, 255));
            }

            if (legendaryTintGradient == null)
            {
                legendaryTintGradient = CreateDefaultGradient(new Color32(255, 249, 220, 255), new Color32(255, 211, 110, 255));
            }
        }

        private static Gradient CreateDefaultGradient(Color a, Color b)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(a, 0f),
                    new GradientColorKey(Color.Lerp(a, b, 0.5f), 0.5f),
                    new GradientColorKey(b, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f),
                });
            return gradient;
        }
    }
}
