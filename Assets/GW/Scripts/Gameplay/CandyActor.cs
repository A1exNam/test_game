using System;
using UnityEngine;

namespace GW.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CandyActor : MonoBehaviour
    {
        public event Action<CandyActor> Despawned;

        public bool IsActive => isActiveAndEnabled;

        [Header("Rendering")]
        [SerializeField]
        private SpriteRenderer foilRenderer;

        [SerializeField]
        private SpriteRenderer highlightRenderer;

        [SerializeField]
        [Tooltip("How quickly the foil visuals interpolate toward the target tint each frame.")]
        private float foilLerpSpeed = 8f;

        [SerializeField]
        private Color defaultFoilColor = new Color(0.78f, 0.66f, 0.28f, 1f);

        [SerializeField]
        private Color defaultHighlightColor = new Color(0.95f, 0.83f, 0.42f, 1f);

        private ConveyorLineController owner;
        private Vector3 direction;
        private float speed;
        private FoilPatternRuntime? activePattern;
        private float patternPulseTime;
        private Vector3 highlightDefaultEuler;
        private bool highlightEulerCached;

        public void Activate(
            ConveyorLineController line,
            Vector3 spawnPosition,
            Vector3 direction,
            float speed,
            FoilPatternRuntime? pattern = null)
        {
            owner = line;
            this.direction = direction;
            this.speed = speed;

            EnsureRenderers();
            ApplyFoilPattern(pattern, true);

            transform.position = spawnPosition;
            gameObject.SetActive(true);
        }

        public void Tick(float deltaTime)
        {
            transform.position += direction * speed * deltaTime;
            UpdateFoilVisual(deltaTime);
        }

        public void SetSpeed(float value)
        {
            speed = Mathf.Max(0f, value);
        }

        public void Despawn()
        {
            gameObject.SetActive(false);
            owner = null;
            ClearPatternImmediate();
            Despawned?.Invoke(this);
        }

        public void ApplyFoilPattern(FoilPatternRuntime? pattern, bool instant = false)
        {
            EnsureRenderers();

            activePattern = pattern;
            patternPulseTime = 0f;

            if (instant)
            {
                UpdateFoilVisual(instant ? 1f : Time.deltaTime, instant);
            }
        }

        private void UpdateFoilVisual(float deltaTime, bool forceInstant = false)
        {
            if (foilRenderer == null && highlightRenderer == null)
            {
                return;
            }

            var lerpFactor = forceInstant ? 1f : Mathf.Clamp01(deltaTime * foilLerpSpeed);

            Color targetFoil = defaultFoilColor;
            Color targetHighlight = defaultHighlightColor;

            if (activePattern.HasValue)
            {
                var pattern = activePattern.Value;
                patternPulseTime += deltaTime * Mathf.Max(0.1f, pattern.PulseSpeed);
                var wave = 0.5f + 0.5f * Mathf.Sin(patternPulseTime * Mathf.PI * 2f);

                targetFoil = Color.Lerp(pattern.PrimaryTint, pattern.SecondaryTint, wave);
                targetFoil = Color.Lerp(defaultFoilColor, targetFoil, Mathf.Clamp01(pattern.Specular + pattern.EmbossDepth * 0.5f));

                targetHighlight = Color.Lerp(pattern.SecondaryTint, Color.white, 0.15f + 0.4f * wave);

                if (highlightRenderer != null)
                {
                    CacheHighlightEuler();
                    var rotation = highlightDefaultEuler;
                    var angle = Mathf.Atan2(pattern.FlowDirection.y, pattern.FlowDirection.x) * Mathf.Rad2Deg;
                    rotation.z = angle;
                    highlightRenderer.transform.localEulerAngles = rotation;
                }
            }
            else if (highlightRenderer != null)
            {
                CacheHighlightEuler();
                highlightRenderer.transform.localEulerAngles = highlightDefaultEuler;
            }

            if (foilRenderer != null)
            {
                foilRenderer.color = Color.Lerp(foilRenderer.color, targetFoil, lerpFactor);
            }

            if (highlightRenderer != null)
            {
                highlightRenderer.color = Color.Lerp(highlightRenderer.color, targetHighlight, lerpFactor);
            }
        }

        private void EnsureRenderers()
        {
            if (foilRenderer == null)
            {
                foilRenderer = GetComponentInChildren<SpriteRenderer>();
                if (foilRenderer != null)
                {
                    defaultFoilColor = foilRenderer.color;
                }
            }

            if (highlightRenderer != null && !highlightEulerCached)
            {
                highlightDefaultEuler = highlightRenderer.transform.localEulerAngles;
                highlightEulerCached = true;
                defaultHighlightColor = highlightRenderer.color;
            }
        }

        private void CacheHighlightEuler()
        {
            if (highlightRenderer == null || highlightEulerCached)
            {
                return;
            }

            highlightDefaultEuler = highlightRenderer.transform.localEulerAngles;
            highlightEulerCached = true;
            defaultHighlightColor = highlightRenderer.color;
        }

        private void ClearPatternImmediate()
        {
            activePattern = null;
            patternPulseTime = 0f;

            if (foilRenderer != null)
            {
                foilRenderer.color = defaultFoilColor;
            }

            if (highlightRenderer != null)
            {
                CacheHighlightEuler();
                highlightRenderer.transform.localEulerAngles = highlightDefaultEuler;
                highlightRenderer.color = defaultHighlightColor;
            }
        }
    }
}
