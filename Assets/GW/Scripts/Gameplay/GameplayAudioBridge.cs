using System;
using System.Collections.Generic;
using UnityEngine;
using GW.Core;

namespace GW.Gameplay
{
    /// <summary>
    /// Bridges gameplay events (seal grades, combos, bliss state) to the audio controller.
    /// Ensures feedback stays punchy without scattering audio calls across multiple systems.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(200)]
    public sealed class GameplayAudioBridge : MonoBehaviour
    {
        private static readonly int[] ComboMilestones = { 10, 25, 50 };
        private const int PercussionComboThreshold = 10;

        [Header("Auto Binding")]
        [SerializeField]
        private bool autoDiscover = true;

        [Header("Manual References")]
        [SerializeField]
        private List<ConveyorLineController> lines = new List<ConveyorLineController>();

        [SerializeField]
        private List<BlissController> blissControllers = new List<BlissController>();

        private readonly Dictionary<ConveyorLineController, int> lineCombos = new Dictionary<ConveyorLineController, int>();
        private readonly Dictionary<ConveyorLineController, Action<int>> comboHandlers = new Dictionary<ConveyorLineController, Action<int>>();
        private readonly Dictionary<BlissController, Action<bool>> blissHandlers = new Dictionary<BlissController, Action<bool>>();
        private readonly HashSet<BlissController> activeBlissControllers = new HashSet<BlissController>();

        private void OnEnable()
        {
            _ = AudioController.Instance;
            RefreshBindings();
            SubscribeAll();
            UpdatePercussionState();
        }

        private void OnDisable()
        {
            UnsubscribeAll();
            if (AudioController.HasInstance)
            {
                AudioController.Instance.SetPercussionActive(false);

                if (activeBlissControllers.Count > 0)
                {
                    AudioController.Instance.NotifyBlissDeactivated();
                }
            }

            activeBlissControllers.Clear();
            lineCombos.Clear();
        }

        private void RefreshBindings()
        {
            if (lines == null)
            {
                lines = new List<ConveyorLineController>();
            }
            else
            {
                lines.RemoveAll(l => l == null);
            }

            if (blissControllers == null)
            {
                blissControllers = new List<BlissController>();
            }
            else
            {
                blissControllers.RemoveAll(b => b == null);
            }

            if (!autoDiscover)
            {
                return;
            }

            var discoveredLines = FindObjectsOfType<ConveyorLineController>(true);
            for (var i = 0; i < discoveredLines.Length; i++)
            {
                var line = discoveredLines[i];
                if (line == null || lines.Contains(line))
                {
                    continue;
                }

                lines.Add(line);
            }

            var discoveredBliss = FindObjectsOfType<BlissController>(true);
            for (var i = 0; i < discoveredBliss.Length; i++)
            {
                var controller = discoveredBliss[i];
                if (controller == null || blissControllers.Contains(controller))
                {
                    continue;
                }

                blissControllers.Add(controller);
            }
        }

        private void SubscribeAll()
        {
            if (lines != null)
            {
                for (var i = 0; i < lines.Count; i++)
                {
                    SubscribeLine(lines[i]);
                }
            }

            if (blissControllers != null)
            {
                for (var i = 0; i < blissControllers.Count; i++)
                {
                    SubscribeBliss(blissControllers[i]);
                }
            }
        }

        private void UnsubscribeAll()
        {
            if (lines != null)
            {
                for (var i = 0; i < lines.Count; i++)
                {
                    UnsubscribeLine(lines[i]);
                }
            }

            if (blissControllers != null)
            {
                for (var i = 0; i < blissControllers.Count; i++)
                {
                    UnsubscribeBliss(blissControllers[i]);
                }
            }
        }

        private void SubscribeLine(ConveyorLineController line)
        {
            if (line == null || comboHandlers.ContainsKey(line))
            {
                return;
            }

            line.SealResolved += HandleSealResolved;
            Action<int> comboHandler = combo => HandleLineComboChanged(line, combo);
            line.ComboChanged += comboHandler;
            comboHandlers[line] = comboHandler;
            lineCombos[line] = line.Judge?.Combo ?? 0;
        }

        private void UnsubscribeLine(ConveyorLineController line)
        {
            if (line == null)
            {
                return;
            }

            line.SealResolved -= HandleSealResolved;

            if (comboHandlers.TryGetValue(line, out var handler))
            {
                line.ComboChanged -= handler;
                comboHandlers.Remove(line);
            }

            lineCombos.Remove(line);
        }

        private void SubscribeBliss(BlissController controller)
        {
            if (controller == null || blissHandlers.ContainsKey(controller))
            {
                return;
            }

            Action<bool> handler = state => HandleBlissStateChanged(controller, state);
            controller.StateChanged += handler;
            blissHandlers[controller] = handler;
        }

        private void UnsubscribeBliss(BlissController controller)
        {
            if (controller == null)
            {
                return;
            }

            if (blissHandlers.TryGetValue(controller, out var handler))
            {
                controller.StateChanged -= handler;
                blissHandlers.Remove(controller);
            }

            activeBlissControllers.Remove(controller);
        }

        private void HandleSealResolved(ConveyorLineController line, SealGrade grade)
        {
            if (!AudioController.HasInstance)
            {
                return;
            }

            AudioController.Instance.PlaySealGrade(grade);
        }

        private void HandleLineComboChanged(ConveyorLineController line, int combo)
        {
            if (line == null)
            {
                return;
            }

            var previous = lineCombos.TryGetValue(line, out var last) ? last : 0;
            lineCombos[line] = combo;

            if (AudioController.HasInstance)
            {
                for (var i = 0; i < ComboMilestones.Length; i++)
                {
                    var milestone = ComboMilestones[i];
                    if (combo == milestone && previous < milestone)
                    {
                        AudioController.Instance.PlayComboMilestone(milestone);
                        break;
                    }
                }
            }

            UpdatePercussionState();
        }

        private void HandleBlissStateChanged(BlissController controller, bool active)
        {
            if (controller == null || !AudioController.HasInstance)
            {
                return;
            }

            if (active)
            {
                if (activeBlissControllers.Add(controller) && activeBlissControllers.Count == 1)
                {
                    AudioController.Instance.NotifyBlissActivated();
                }
            }
            else
            {
                if (activeBlissControllers.Remove(controller) && activeBlissControllers.Count == 0)
                {
                    AudioController.Instance.NotifyBlissDeactivated();
                }
            }
        }

        private void UpdatePercussionState()
        {
            if (!AudioController.HasInstance)
            {
                return;
            }

            var highest = 0;
            foreach (var kvp in lineCombos)
            {
                if (kvp.Key == null)
                {
                    continue;
                }

                if (kvp.Value > highest)
                {
                    highest = kvp.Value;
                }
            }

            AudioController.Instance.SetPercussionActive(highest >= PercussionComboThreshold);
        }
    }
}
