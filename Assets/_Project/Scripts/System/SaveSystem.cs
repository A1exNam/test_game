using System.Collections.Generic;
using UnityEngine;

namespace GoldenWrap.Systems
{
    public static class SaveSystem
    {
        public const string Key = "GW_Save_v1";

        public static void Save(SaveData data)
        {
            if (data == null)
            {
                data = new SaveData();
            }

            if (data.unlockedPatterns == null)
            {
                data.unlockedPatterns = new List<string>();
            }

            var json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();
        }

        public static SaveData Load()
        {
            if (!PlayerPrefs.HasKey(Key))
            {
                return new SaveData();
            }

            var json = PlayerPrefs.GetString(Key);
            if (string.IsNullOrEmpty(json))
            {
                return new SaveData();
            }

            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                data = new SaveData();
            }

            if (data.unlockedPatterns == null)
            {
                data.unlockedPatterns = new List<string>();
            }

            return data;
        }

        public static void Wipe()
        {
            if (PlayerPrefs.HasKey(Key))
            {
                PlayerPrefs.DeleteKey(Key);
                PlayerPrefs.Save();
            }
        }
    }
}
