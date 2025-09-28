using System;
using System.Collections.Generic;
using UnityEngine;

namespace GW.Gameplay
{
    /// <summary>
    /// Routes player focus and input between multiple conveyor lines and exposes hotkey selection.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LineFocusController : MonoBehaviour
    {
        private static readonly KeyCode[] NumberKeys =
        {
            KeyCode.Alpha1,
            KeyCode.Alpha2,
            KeyCode.Alpha3,
            KeyCode.Alpha4,
            KeyCode.Alpha5,
            KeyCode.Alpha6,
            KeyCode.Alpha7,
            KeyCode.Alpha8,
            KeyCode.Alpha9,
        };

        private static readonly KeyCode[] KeypadKeys =
        {
            KeyCode.Keypad1,
            KeyCode.Keypad2,
            KeyCode.Keypad3,
            KeyCode.Keypad4,
            KeyCode.Keypad5,
            KeyCode.Keypad6,
            KeyCode.Keypad7,
            KeyCode.Keypad8,
            KeyCode.Keypad9,
        };

        [Header("Line Binding")]
        [SerializeField]
        private List<ConveyorLineController> lines = new List<ConveyorLineController>();

        [SerializeField]
        private bool autoPopulateLines = true;

        [SerializeField]
        private int startingIndex;

        public event Action<ConveyorLineController> FocusChanged;

        public ConveyorLineController CurrentLine { get; private set; }

        private int currentIndex = -1;

        public IReadOnlyList<ConveyorLineController> Lines => lines;

        private void Awake()
        {
            RefreshLineCollection();
            startingIndex = Mathf.Clamp(startingIndex, 0, Mathf.Max(0, lines.Count - 1));
        }

        private void OnEnable()
        {
            RefreshLineCollection();
            EnsureValidFocus(true);
        }

        private void Update()
        {
            for (var i = 0; i < lines.Count; i++)
            {
                if (WasFocusKeyPressed(i) && TrySetFocusIndex(i, false))
                {
                    return;
                }
            }

            if (!IsSelectable(CurrentLine))
            {
                EnsureValidFocus(true);
            }
        }

        /// <summary>
        /// Attempts to set focus to the provided line instance.
        /// </summary>
        public bool TryFocusLine(ConveyorLineController line, bool forceNotify = false)
        {
            if (line == null)
            {
                return false;
            }

            var index = lines.IndexOf(line);
            if (index < 0)
            {
                return false;
            }

            return TrySetFocusIndex(index, forceNotify);
        }

        /// <summary>
        /// Attempts to set focus to the line with the requested identifier.
        /// </summary>
        public bool TryFocusLine(Core.LineId lineId, bool forceNotify = false)
        {
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line == null || line.LineId != lineId)
                {
                    continue;
                }

                return TrySetFocusIndex(i, forceNotify);
            }

            return false;
        }

        /// <summary>
        /// Refreshes the list of known lines (used when new lines are spawned or activated).
        /// </summary>
        public void RefreshLineCollection(bool preserveCurrentFocus = true)
        {
            if (lines == null)
            {
                lines = new List<ConveyorLineController>();
            }

            var unique = new HashSet<ConveyorLineController>();
            for (var i = lines.Count - 1; i >= 0; i--)
            {
                var line = lines[i];
                if (line == null || !unique.Add(line))
                {
                    lines.RemoveAt(i);
                }
            }

            if (autoPopulateLines)
            {
                var discovered = FindObjectsOfType<ConveyorLineController>(true);
                for (var i = 0; i < discovered.Length; i++)
                {
                    var line = discovered[i];
                    if (line == null || unique.Contains(line))
                    {
                        continue;
                    }

                    unique.Add(line);
                    lines.Add(line);
                }
            }

            if (!preserveCurrentFocus)
            {
                CurrentLine = null;
                currentIndex = -1;
            }
            else if (CurrentLine != null && !lines.Contains(CurrentLine))
            {
                CurrentLine = null;
                currentIndex = -1;
            }
        }

        /// <summary>
        /// Returns true if the supplied line is currently focused.
        /// </summary>
        public bool IsLineFocused(ConveyorLineController candidate)
        {
            return candidate != null && candidate == CurrentLine;
        }

        private void EnsureValidFocus(bool forceNotify)
        {
            if (lines.Count == 0)
            {
                if (forceNotify)
                {
                    CurrentLine = null;
                    currentIndex = -1;
                    FocusChanged?.Invoke(null);
                }

                return;
            }

            if (!TrySetFocusIndex(startingIndex, forceNotify))
            {
                if (!TrySelectFirstAvailable(forceNotify))
                {
                    if (CurrentLine != null || forceNotify)
                    {
                        CurrentLine = null;
                        currentIndex = -1;

                        if (forceNotify)
                        {
                            FocusChanged?.Invoke(null);
                        }
                    }
                }
            }
        }

        private bool TrySelectFirstAvailable(bool forceNotify)
        {
            for (var i = 0; i < lines.Count; i++)
            {
                if (TrySetFocusIndex(i, forceNotify))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TrySetFocusIndex(int index, bool forceNotify)
        {
            if (index < 0 || index >= lines.Count)
            {
                return false;
            }

            var line = lines[index];
            if (!IsSelectable(line))
            {
                return false;
            }

            var changed = CurrentLine != line || forceNotify;
            CurrentLine = line;
            currentIndex = index;

            if (changed)
            {
                FocusChanged?.Invoke(CurrentLine);
            }

            return true;
        }

        private static bool IsSelectable(ConveyorLineController line)
        {
            return line != null && line.isActiveAndEnabled && line.gameObject.activeInHierarchy;
        }

        private static bool WasFocusKeyPressed(int index)
        {
            if (index < 0 || index >= NumberKeys.Length)
            {
                return false;
            }

            var numberKey = NumberKeys[index];
            var keypadKey = KeypadKeys[index];
            return (numberKey != KeyCode.None && Input.GetKeyDown(numberKey)) ||
                   (keypadKey != KeyCode.None && Input.GetKeyDown(keypadKey));
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (lines == null)
            {
                return;
            }

            var unique = new HashSet<ConveyorLineController>();
            for (var i = lines.Count - 1; i >= 0; i--)
            {
                var line = lines[i];
                if (line == null || !unique.Add(line))
                {
                    lines.RemoveAt(i);
                }
            }

            startingIndex = Mathf.Clamp(startingIndex, 0, Mathf.Max(0, lines.Count - 1));
        }
#endif
    }
}
