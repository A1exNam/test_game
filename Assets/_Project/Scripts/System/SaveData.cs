using System;
using System.Collections.Generic;

namespace GoldenWrap.Systems
{
    [Serializable]
    public sealed class SaveData
    {
        public int credits;
        public int bestCombo;
        public List<string> unlockedPatterns;
        public SettingsData settings;

        public SaveData()
        {
            credits = 0;
            bestCombo = 0;
            unlockedPatterns = new List<string>();
            settings = null;
        }
    }

    [Serializable]
    public sealed class SettingsData
    {
    }
}
