using System.Collections.Generic;
using UnityEngine;
using GW.UI;

namespace GW.Gameplay
{
    /// <summary>
    /// Lightweight scene bootstrapper that wires together core gameplay systems and ensures
    /// focus/HUD bindings are established when the scene loads.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameStateController : MonoBehaviour
    {
        public enum GamePhase
        {
            Booting,
            Running,
            Paused,
        }

        [Header("Bindings")]
        [SerializeField]
        private bool autoDiscover = true;

        [SerializeField]
        private LineFocusController focusController;

        [SerializeField]
        private BlissController blissController;

        [SerializeField]
        private HUDRoot hud;

        [SerializeField]
        private ContractSystem contractSystem;

        [SerializeField]
        private UpgradeSystem upgradeSystem;

        [SerializeField]
        private GameplayAudioBridge audioBridge;

        [SerializeField]
        private List<ConveyorLineController> lines = new List<ConveyorLineController>();

        private bool initialised;
        private GamePhase phase = GamePhase.Booting;

        public GamePhase Phase => phase;
        public IReadOnlyList<ConveyorLineController> Lines => lines;

        private void Awake()
        {
            if (autoDiscover)
            {
                DiscoverReferences();
            }

            EnsureLineCollection();
        }

        private void OnEnable()
        {
            if (!initialised)
            {
                Initialise();
            }
            else
            {
                EnsureBindings();
            }
        }

        private void Start()
        {
            Initialise();
        }

        private void OnValidate()
        {
            EnsureLineCollection();
        }

        public void PauseGame()
        {
            if (phase == GamePhase.Paused)
            {
                return;
            }

            phase = GamePhase.Paused;
            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            if (phase == GamePhase.Running)
            {
                return;
            }

            phase = GamePhase.Running;
            Time.timeScale = 1f;
        }

        public void RegisterLine(ConveyorLineController line)
        {
            if (line == null)
            {
                return;
            }

            if (lines == null)
            {
                lines = new List<ConveyorLineController>();
            }

            if (lines.Contains(line))
            {
                return;
            }

            lines.Add(line);
            if (initialised)
            {
                EnsureBindings();
            }
        }

        public void UnregisterLine(ConveyorLineController line)
        {
            if (lines == null || line == null)
            {
                return;
            }

            if (!lines.Remove(line))
            {
                return;
            }

            if (focusController != null && focusController.CurrentLine == line)
            {
                focusController.RefreshLineCollection(false);
                focusController.TryFocusLine(0, true);
            }

            if (initialised)
            {
                EnsureBindings();
            }
        }

        private void Initialise()
        {
            if (initialised)
            {
                return;
            }

            EnsureBindings();
            phase = GamePhase.Running;
            initialised = true;
        }

        private void DiscoverReferences()
        {
            if (focusController == null)
            {
                focusController = FindObjectOfType<LineFocusController>();
            }

            if (blissController == null)
            {
                blissController = FindObjectOfType<BlissController>();
            }

            if (hud == null)
            {
                hud = FindObjectOfType<HUDRoot>(true);
            }

            if (contractSystem == null)
            {
                contractSystem = FindObjectOfType<ContractSystem>();
            }

            if (upgradeSystem == null)
            {
                upgradeSystem = FindObjectOfType<UpgradeSystem>();
            }

            if (audioBridge == null)
            {
                audioBridge = FindObjectOfType<GameplayAudioBridge>();
            }

            if (lines == null)
            {
                lines = new List<ConveyorLineController>();
            }

            if (lines.Count == 0)
            {
                var discovered = FindObjectsOfType<ConveyorLineController>(true);
                for (var i = 0; i < discovered.Length; i++)
                {
                    var line = discovered[i];
                    if (line != null && !lines.Contains(line))
                    {
                        lines.Add(line);
                    }
                }
            }
        }

        private void EnsureLineCollection()
        {
            if (lines == null)
            {
                lines = new List<ConveyorLineController>();
            }

            for (var i = lines.Count - 1; i >= 0; i--)
            {
                if (lines[i] == null)
                {
                    lines.RemoveAt(i);
                }
            }
        }

        private void EnsureBindings()
        {
            EnsureLineCollection();

            if (focusController != null)
            {
                focusController.RefreshLineCollection();
                if (lines.Count > 0)
                {
                    focusController.TryFocusLine(lines[0], true);
                }
            }

            if (hud != null)
            {
                if (focusController != null)
                {
                    hud.BindFocusController(focusController);
                }
                else if (lines.Count > 0)
                {
                    hud.BindLine(lines[0]);
                }
            }

            if (blissController != null)
            {
                if (focusController != null)
                {
                    blissController.BindFocusController(focusController);
                }

                if (lines.Count > 0)
                {
                    blissController.BindLine(lines[0]);
                }
            }

            if (contractSystem != null && lines.Count > 0)
            {
                // Ensure the contract system is aware of currently active lines by allowing it to
                // auto-populate on demand. It will discover lines in OnEnable, but we proactively
                // trigger any bindings by toggling enable state when needed.
                if (!contractSystem.enabled)
                {
                    contractSystem.enabled = true;
                }
            }

            if (upgradeSystem != null && lines.Count > 0)
            {
                if (!upgradeSystem.enabled)
                {
                    upgradeSystem.enabled = true;
                }
            }
        }
    }
}
