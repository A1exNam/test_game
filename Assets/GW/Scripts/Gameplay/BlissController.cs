using System;
using UnityEngine;

namespace GW.Gameplay
{
    /// <summary>
    /// Controls Bliss mode activation, time dilation, and auto-snap bonuses.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BlissController : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField]
        private ConveyorLineController trackedLine;

        [SerializeField]
        private GameObject vfxRoot;

        [SerializeField]
        private LineFocusController focusController;

        [Header("Bliss Settings")]
        [SerializeField]
        private float blissThreshold = 1f;

        [SerializeField]
        private float durationSeconds = 4.5f;

        [SerializeField]
        [Range(0.1f, 1f)]
        private float slowTimeScale = 0.7f;

        [SerializeField]
        [Range(0f, 0.5f)]
        private float autoSnapPercentage = 0.2f;

        public bool IsActive => isActive;
        public float AutoSnapPercentage => autoSnapPercentage;

        public event Action<bool> StateChanged;

        private float cachedTimeScale = 1f;
        private float cachedFixedDeltaTime = 0.02f;
        private float remainingUnscaledTime;
        private bool isActive;

        private void Awake()
        {
            cachedTimeScale = Time.timeScale;
            cachedFixedDeltaTime = Time.fixedDeltaTime;

            if (vfxRoot != null)
            {
                vfxRoot.SetActive(false);
            }

            if (focusController == null)
            {
                focusController = FindObjectOfType<LineFocusController>();
            }
        }

        private void OnEnable()
        {
            ResetState(false);
        }

        private void OnDisable()
        {
            ResetState(true);
        }

        private void Update()
        {
            if (!isActive && Input.GetKeyDown(KeyCode.Space))
            {
                if (!HasInputFocus())
                {
                    return;
                }

                TryActivate();
            }

            if (!isActive)
            {
                return;
            }

            remainingUnscaledTime -= Time.unscaledDeltaTime;
            if (remainingUnscaledTime <= 0f)
            {
                ResetState(true);
            }
        }

        public void BindLine(ConveyorLineController line)
        {
            trackedLine = line;
        }

        public void BindFocusController(LineFocusController controller)
        {
            focusController = controller;
        }

        private void TryActivate()
        {
            if (trackedLine == null)
            {
                return;
            }

            var judge = trackedLine.Judge;
            if (judge == null)
            {
                return;
            }

            if (!judge.TryConsumeBliss(blissThreshold))
            {
                return;
            }

            ActivateInternal();
        }

        private void ActivateInternal()
        {
            isActive = true;
            remainingUnscaledTime = Mathf.Max(0.1f, durationSeconds);

            cachedTimeScale = Time.timeScale;
            cachedFixedDeltaTime = Time.fixedDeltaTime;

            var clampedScale = Mathf.Clamp(slowTimeScale, 0.1f, 1f);
            Time.timeScale = clampedScale;
            Time.fixedDeltaTime = cachedFixedDeltaTime * clampedScale;

            if (vfxRoot != null)
            {
                vfxRoot.SetActive(true);
            }

            StateChanged?.Invoke(true);
        }

        private void ResetState(bool notify)
        {
            var wasActive = isActive;

            isActive = false;
            remainingUnscaledTime = 0f;

            StopVfx();

            Time.timeScale = cachedTimeScale;
            Time.fixedDeltaTime = cachedFixedDeltaTime;

            if (notify)
            {
                StateChanged?.Invoke(false);
            }
        }

        private bool HasInputFocus()
        {
            if (trackedLine == null)
            {
                return false;
            }

            if (focusController == null)
            {
                return true;
            }

            return focusController.IsLineFocused(trackedLine);
        }

        private void StopVfx()
        {
            if (vfxRoot != null)
            {
                vfxRoot.SetActive(false);
            }
        }
    }
}

