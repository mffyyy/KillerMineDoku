using UnityEngine;

namespace KillerMineDoku.UI
{
    public static class CustomLevelStore
    {
        private const string JsonKeyPrefix = "KillerMineDoku_CustomLevel_";
        private const string BestTimeKeyPrefix = "KillerMineDoku_CustomLevelBestTime_";
        public const string MissingPuzzleMessage = "\u6ca1\u6709\u627e\u5230puzzle";
        public const string IncompletePuzzleMessage = "puzzle\u4fe1\u606f\u4e0d\u5b8c\u6574";
        public const string NotUniquePuzzleMessage = "puzzle\u6ca1\u6709\u552f\u4e00\u89e3";
        public const int SlotCount = 3;

        public static bool AreCustomLevelsUnlocked(KillerMineDokuLevelCatalog catalog)
        {
            return catalog != null && catalog.PresetCount > 0 && catalog.CountCompletedPresets() >= catalog.PresetCount;
        }

        public static bool HasLevel(int slotIndex)
        {
            return !string.IsNullOrWhiteSpace(PlayerPrefs.GetString(GetJsonKey(slotIndex), string.Empty));
        }

        public static SlotData Load(int slotIndex)
        {
            var json = PlayerPrefs.GetString(GetJsonKey(slotIndex), string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var puzzle = KillerMineDokuPuzzleData.Parse(json);
            if (puzzle == null || !puzzle.isComplete || !puzzle.hasUniqueSolution)
            {
                return null;
            }

            return new SlotData(slotIndex, json, puzzle, GetBestTime(slotIndex));
        }

        public static void Save(int slotIndex, string json)
        {
            PlayerPrefs.SetString(GetJsonKey(slotIndex), json);
            PlayerPrefs.Save();
        }

        public static void Delete(int slotIndex)
        {
            PlayerPrefs.DeleteKey(GetJsonKey(slotIndex));
            PlayerPrefs.DeleteKey(GetBestTimeKey(slotIndex));
            PlayerPrefs.Save();
        }

        public static float GetBestTime(int slotIndex)
        {
            return PlayerPrefs.GetFloat(GetBestTimeKey(slotIndex), 0f);
        }

        public static float SaveBestTime(int slotIndex, float seconds)
        {
            var key = GetBestTimeKey(slotIndex);
            var current = PlayerPrefs.GetFloat(key, 0f);
            if (current <= 0f || seconds < current)
            {
                PlayerPrefs.SetFloat(key, seconds);
                PlayerPrefs.Save();
                return seconds;
            }

            return current;
        }

        public static TextAsset CreateTextAsset(SlotData slot)
        {
            if (slot == null)
            {
                return null;
            }

            var asset = new TextAsset(slot.Json);
            asset.name = $"custom_level_slot_{slot.SlotIndex + 1}";
            return asset;
        }

        public static bool TryValidateJson(string json, out KillerMineDokuPuzzleData puzzle, out string message)
        {
            puzzle = KillerMineDokuPuzzleData.Parse(json);
            if (puzzle == null)
            {
                message = MissingPuzzleMessage;
                return false;
            }

            if (!puzzle.isComplete)
            {
                message = IncompletePuzzleMessage;
                return false;
            }

            if (!puzzle.hasUniqueSolution)
            {
                message = NotUniquePuzzleMessage;
                return false;
            }

            message = string.Empty;
            return true;
        }

        public static bool IsKnownValidationMessage(string message)
        {
            return message == MissingPuzzleMessage
                || message == IncompletePuzzleMessage
                || message == NotUniquePuzzleMessage;
        }

        private static string GetJsonKey(int slotIndex)
        {
            return JsonKeyPrefix + slotIndex;
        }

        private static string GetBestTimeKey(int slotIndex)
        {
            return BestTimeKeyPrefix + slotIndex;
        }

        public sealed class SlotData
        {
            public SlotData(int slotIndex, string json, KillerMineDokuPuzzleData puzzle, float bestTimeSeconds)
            {
                SlotIndex = slotIndex;
                Json = json;
                Puzzle = puzzle;
                BestTimeSeconds = bestTimeSeconds;
            }

            public int SlotIndex { get; }
            public string Json { get; }
            public KillerMineDokuPuzzleData Puzzle { get; }
            public float BestTimeSeconds { get; }
        }
    }
}
