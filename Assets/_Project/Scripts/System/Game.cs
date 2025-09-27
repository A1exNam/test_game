using System.Collections;
using UnityEngine;

namespace GoldenWrap.Systems
{
    public sealed class Game : MonoBehaviour
    {
        private const float AutoSaveIntervalSeconds = 60f;

        private static readonly WaitForSeconds AutoSaveDelay = new WaitForSeconds(AutoSaveIntervalSeconds);
        private static readonly Rect AddCreditsButtonRect = new Rect(10f, 10f, 160f, 32f);
        private static readonly Rect WipeButtonRect = new Rect(10f, 52f, 160f, 32f);

        private SaveData _saveData;
        private Coroutine _autoSaveCoroutine;

        private void Awake()
        {
            LoadSaveData();
            EnsureSaveKeyExists();
        }

        private void OnEnable()
        {
            StartAutoSave();
        }

        private void StartAutoSave()
        {
            if (_autoSaveCoroutine == null)
            {
                _autoSaveCoroutine = StartCoroutine(AutoSaveRoutine());
            }
        }

        private void StopAutoSave()
        {
            if (_autoSaveCoroutine != null)
            {
                StopCoroutine(_autoSaveCoroutine);
                _autoSaveCoroutine = null;
            }
        }

        private IEnumerator AutoSaveRoutine()
        {
            while (true)
            {
                yield return AutoSaveDelay;
                SaveCurrentState();
            }
        }

        private void LoadSaveData()
        {
            _saveData = SaveSystem.Load();
            if (_saveData == null)
            {
                _saveData = new SaveData();
            }
        }

        private void EnsureSaveKeyExists()
        {
            if (!PlayerPrefs.HasKey(SaveSystem.Key))
            {
                SaveCurrentState();
            }
        }

        private void SaveCurrentState()
        {
            if (_saveData == null)
            {
                _saveData = new SaveData();
            }

            SaveSystem.Save(_saveData);
        }

        private void OnDisable()
        {
            StopAutoSave();
            SaveCurrentState();
        }

        private void OnApplicationQuit()
        {
            SaveCurrentState();
        }

        private void OnGUI()
        {
            if (GUI.Button(AddCreditsButtonRect, "+100 credits"))
            {
                AddCredits(100);
                SaveCurrentState();
            }

            if (GUI.Button(WipeButtonRect, "Wipe Save"))
            {
                WipeSave();
            }
        }

        private void AddCredits(int amount)
        {
            if (_saveData == null)
            {
                _saveData = new SaveData();
            }

            _saveData.credits += amount;
        }

        private void WipeSave()
        {
            StopAutoSave();
            SaveSystem.Wipe();
            _saveData = new SaveData();
            SaveCurrentState();
            StartAutoSave();
        }
    }
}
