using System;
using System.Collections.Generic;
using UnityEngine;
using GW.Gameplay;
using GW.Core;

namespace GW.UI
{
    [DisallowMultipleComponent]
    public sealed class FoilPatternShowcase : MonoBehaviour
    {
        [SerializeField]
        private FoilPatternLibrary library;

        [SerializeField]
        private FoilPatternShowcaseItem itemPrefab;

        [SerializeField]
        private Transform contentRoot;

        [SerializeField]
        [Tooltip("Maximum number of patterns to show. Zero means show all available.")]
        private int maxItems = 4;

        [SerializeField]
        [Tooltip("Automatically refresh the showcase when the object becomes enabled.")]
        private bool refreshOnEnable = true;

        [SerializeField]
        [Tooltip("If enabled, at least one entry per rarity is attempted before filling remaining slots.")]
        private bool ensureRarityCoverage = true;

        [SerializeField]
        [Tooltip("Allow the same pattern to appear more than once when sampling.")]
        private bool allowDuplicates;

        [SerializeField]
        [Tooltip("Create a new runtime variant for each display entry.")]
        private bool generateRuntimeVariants = true;

        [SerializeField]
        [Tooltip("Optional fixed seed used to stabilise showcase ordering across sessions.")]
        private int seed;

        private readonly List<FoilPatternShowcaseItem> spawnedItems = new List<FoilPatternShowcaseItem>();
        private readonly List<FoilPatternDef> patternPool = new List<FoilPatternDef>();
        private readonly List<FoilPatternDef> selectionBuffer = new List<FoilPatternDef>();
        private readonly List<FoilPatternDef> rarityBuffer = new List<FoilPatternDef>();
        private System.Random random;

        private void Awake()
        {
            EnsureContentRoot();
        }

        private void OnEnable()
        {
            if (refreshOnEnable)
            {
                Refresh();
            }
        }

        public void SetLibrary(FoilPatternLibrary newLibrary)
        {
            if (library == newLibrary)
            {
                if (refreshOnEnable)
                {
                    Refresh();
                }

                return;
            }

            library = newLibrary;
            Refresh();
        }

        public void Refresh()
        {
            EnsureRandom();
            EnsureContentRoot();

            selectionBuffer.Clear();
            if (library == null)
            {
                ApplySelection(selectionBuffer);
                return;
            }

            BuildPatternPool();
            if (patternPool.Count == 0)
            {
                ApplySelection(selectionBuffer);
                return;
            }

            if (ensureRarityCoverage)
            {
                foreach (FoilPatternRarity rarity in Enum.GetValues(typeof(FoilPatternRarity)))
                {
                    rarityBuffer.Clear();
                    library.GetPatternsByRarity(rarity, rarityBuffer);
                    if (rarityBuffer.Count == 0)
                    {
                        continue;
                    }

                    var index = random.Next(rarityBuffer.Count);
                    var def = rarityBuffer[index];
                    selectionBuffer.Add(def);

                    if (!allowDuplicates)
                    {
                        patternPool.Remove(def);
                    }

                    if (maxItems > 0 && selectionBuffer.Count >= maxItems)
                    {
                        break;
                    }
                }
            }

            var targetCount = maxItems <= 0 ? int.MaxValue : Math.Max(0, maxItems);
            while (selectionBuffer.Count < targetCount && patternPool.Count > 0)
            {
                var index = random.Next(patternPool.Count);
                var def = patternPool[index];
                selectionBuffer.Add(def);

                if (!allowDuplicates)
                {
                    patternPool.RemoveAt(index);
                }
            }

            ApplySelection(selectionBuffer);
        }

        private void ApplySelection(List<FoilPatternDef> selection)
        {
            var count = selection?.Count ?? 0;
            if (maxItems > 0)
            {
                count = Mathf.Min(count, maxItems);
            }

            EnsureItemCapacity(count);

            for (var i = 0; i < spawnedItems.Count; i++)
            {
                var item = spawnedItems[i];
                if (item == null)
                {
                    continue;
                }

                if (i < count)
                {
                    var def = selection[i];
                    if (generateRuntimeVariants && library != null && def != null)
                    {
                        var runtime = library.CreateRuntime(def, random);
                        item.gameObject.SetActive(true);
                        item.Present(runtime);
                    }
                    else
                    {
                        item.gameObject.SetActive(true);
                        item.Present(def);
                    }
                }
                else
                {
                    item.Clear();
                    item.gameObject.SetActive(false);
                }
            }
        }

        private void EnsureItemCapacity(int desiredCount)
        {
            for (var i = spawnedItems.Count; i < desiredCount; i++)
            {
                var item = CreateItemInstance();
                if (item != null)
                {
                    spawnedItems.Add(item);
                }
            }

            for (var i = desiredCount; i < spawnedItems.Count; i++)
            {
                var item = spawnedItems[i];
                if (item == null)
                {
                    continue;
                }

                item.Clear();
                item.gameObject.SetActive(false);
            }
        }

        private FoilPatternShowcaseItem CreateItemInstance()
        {
            if (itemPrefab == null)
            {
                return null;
            }

            var parent = contentRoot != null ? contentRoot : transform;
            var instance = Instantiate(itemPrefab, parent);
            instance.gameObject.SetActive(true);
            return instance;
        }

        private void BuildPatternPool()
        {
            patternPool.Clear();
            var source = library.Patterns;
            for (var i = 0; i < source.Count; i++)
            {
                var def = source[i];
                if (def == null)
                {
                    continue;
                }

                patternPool.Add(def);
            }
        }

        private void EnsureContentRoot()
        {
            if (contentRoot != null)
            {
                return;
            }

            if (itemPrefab != null && itemPrefab.transform.parent != null)
            {
                contentRoot = itemPrefab.transform.parent;
            }
            else
            {
                contentRoot = transform;
            }
        }

        private void EnsureRandom()
        {
            if (random != null)
            {
                return;
            }

            random = seed != 0 ? new System.Random(seed) : new System.Random(Environment.TickCount ^ GetInstanceID());
        }
    }
}
