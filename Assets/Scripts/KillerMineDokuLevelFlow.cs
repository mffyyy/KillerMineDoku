using UnityEngine;

namespace KillerMineDoku.UI
{
    public static class KillerMineDokuLevelFlow
    {
        public static TextAsset PendingPuzzle { get; set; }
        public static int PendingLevelNumber { get; set; }
        public static int PendingCustomSlotIndex { get; set; } = -1;
        public static LevelResult LastResult { get; private set; }
        public static ResumeState PendingResume { get; private set; }

        public static bool HasResult => LastResult != null;

        public static void SetResult(LevelResult result)
        {
            LastResult = result;
        }

        public static void SetResume(ResumeState resume)
        {
            PendingResume = resume;
        }

        public static ResumeState ConsumeResume()
        {
            var resume = PendingResume;
            PendingResume = null;
            return resume;
        }

        public sealed class LevelResult
        {
            public bool victory;
            public Texture2D boardImage;
            public float elapsedSeconds;
            public float bestTimeSeconds;
            public int wrongMarks;
            public int missingMines;
            public int mineMarks;
            public int safeMarks;
            public int totalMines;
            public BoardCellView.CellMark[,] submittedMarks;
            public string levelSceneName;
            public string levelSelectSceneName;
            public TextAsset currentPuzzle;
            public TextAsset nextPuzzle;
            public int currentLevel;
            public int nextLevel;
            public int customSlotIndex;
            public bool allLevelsUnlocked;
        }

        public sealed class ResumeState
        {
            public TextAsset puzzle;
            public BoardCellView.CellMark[,] marks;
            public float elapsedSeconds;
            public int customSlotIndex;
        }
    }
}
