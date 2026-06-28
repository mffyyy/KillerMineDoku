using System;
using System.Collections.Generic;
using UnityEngine;

namespace KillerMineDoku.UI
{
    [CreateAssetMenu(fileName = "KillerMineDokuLevelCatalog", menuName = "Killer MineDoku/Level Catalog")]
    public sealed class KillerMineDokuLevelCatalog : ScriptableObject
    {
        private const string ResourceName = "KillerMineDokuLevelCatalog";
        private const string UnlockedLevelKey = "KillerMineDoku_UnlockedLevel";
        private const string BestTimePrefix = "KillerMineDoku_BestTime_";

        [SerializeField] private List<LevelEntry> presetLevels = new();

        public IReadOnlyList<LevelEntry> PresetLevels => presetLevels;
        public int PresetCount => presetLevels.Count;

        public static KillerMineDokuLevelCatalog LoadDefault()
        {
            return Resources.Load<KillerMineDokuLevelCatalog>(ResourceName);
        }

        public LevelEntry FindByPuzzle(TextAsset puzzle)
        {
            if (puzzle == null)
            {
                return null;
            }

            for (var i = 0; i < presetLevels.Count; i++)
            {
                var entry = presetLevels[i];
                if (entry != null && entry.puzzleJson == puzzle)
                {
                    return entry;
                }
            }

            for (var i = 0; i < presetLevels.Count; i++)
            {
                var entry = presetLevels[i];
                if (entry != null && entry.puzzleJson != null && entry.puzzleJson.name == puzzle.name)
                {
                    return entry;
                }
            }

            return null;
        }

        public LevelEntry GetNext(LevelEntry current)
        {
            if (current == null)
            {
                return null;
            }

            for (var i = 0; i < presetLevels.Count; i++)
            {
                if (presetLevels[i] == current && i + 1 < presetLevels.Count)
                {
                    return presetLevels[i + 1];
                }
            }

            return null;
        }

        public int GetUnlockedLevel()
        {
            var savedLevel = Mathf.Max(1, PlayerPrefs.GetInt(UnlockedLevelKey, 1));
            if (PresetCount > 0 && CountCompletedPresets() >= PresetCount)
            {
                return Mathf.Max(savedLevel, PresetCount + 1);
            }

            return savedLevel;
        }

        public bool IsUnlocked(LevelEntry entry)
        {
            return entry != null && entry.levelNumber <= GetUnlockedLevel();
        }

        public bool IsCompleted(LevelEntry entry)
        {
            return GetBestTime(entry) > 0f;
        }

        public int CountCompletedPresets()
        {
            var count = 0;
            for (var i = 0; i < presetLevels.Count; i++)
            {
                if (IsCompleted(presetLevels[i]))
                {
                    count++;
                }
            }

            return count;
        }

        public float GetBestTime(LevelEntry entry)
        {
            return entry == null || entry.puzzleJson == null ? 0f : PlayerPrefs.GetFloat(GetBestTimeKey(entry), 0f);
        }

        public float SaveBestTime(LevelEntry entry, float seconds)
        {
            if (entry == null || entry.puzzleJson == null)
            {
                return seconds;
            }

            var key = GetBestTimeKey(entry);
            var current = PlayerPrefs.GetFloat(key, 0f);
            if (current <= 0f || seconds < current)
            {
                PlayerPrefs.SetFloat(key, seconds);
                PlayerPrefs.Save();
                return seconds;
            }

            return current;
        }

        public void UnlockNextAfter(LevelEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            var next = GetNext(entry);
            var unlockedLevel = next != null ? next.levelNumber : PresetCount + 1;
            PlayerPrefs.SetInt(UnlockedLevelKey, Mathf.Max(GetUnlockedLevel(), unlockedLevel));
            PlayerPrefs.Save();
        }

        public static string FormatTime(float seconds)
        {
            if (seconds <= 0f)
            {
                return "--:--";
            }

            var totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private static string GetBestTimeKey(LevelEntry entry)
        {
            return BestTimePrefix + entry.puzzleJson.name;
        }

        [Serializable]
        public sealed class LevelEntry
        {
            public int levelNumber;
            public string title;
            public TextAsset puzzleJson;

            [NonSerialized] private KillerMineDokuPuzzleData cachedPuzzleData;

            public KillerMineDokuPuzzleData PuzzleData
            {
                get
                {
                    if (cachedPuzzleData == null && puzzleJson != null)
                    {
                        cachedPuzzleData = KillerMineDokuPuzzleData.Parse(puzzleJson.text);
                    }

                    return cachedPuzzleData;
                }
            }

            public int GridSize => PuzzleData != null ? PuzzleData.size : 0;
            public int MineCount => PuzzleData != null ? PuzzleData.TotalMines : 0;
            public string DisplayTitle => string.IsNullOrWhiteSpace(title) ? $"关卡 {levelNumber:00}" : title;
        }
    }
}
