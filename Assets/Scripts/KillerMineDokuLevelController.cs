using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KillerMineDoku.UI
{
    [DisallowMultipleComponent]
    public sealed class KillerMineDokuLevelController : MonoBehaviour
    {
        public KillerMineDokuBoardBuilder boardBuilder;
        public PuzzleBoardRuntime boardRuntime;
        public LevelHudView hudView;
        public LevelActionPanel actionPanel;
        public RectTransform boardPanel;
        public KillerMineDokuLevelCatalog catalog;
        public string levelSelectScene = "SelectLevel";
        public string victoryScene = "VictoryScene";
        public string failScene = "FailScene";

        private float elapsedSeconds;
        private bool running;
        private bool submitting;
        private int customSlotIndex = -1;

        private void Awake()
        {
            BindDefaults();
            var resume = KillerMineDokuLevelFlow.ConsumeResume();
            if (resume != null && resume.puzzle != null)
            {
                boardBuilder.puzzleJson = resume.puzzle;
                customSlotIndex = resume.customSlotIndex;
                KillerMineDokuLevelFlow.PendingPuzzle = null;
                KillerMineDokuLevelFlow.PendingCustomSlotIndex = -1;
            }
            else if (KillerMineDokuLevelFlow.PendingPuzzle != null)
            {
                boardBuilder.puzzleJson = KillerMineDokuLevelFlow.PendingPuzzle;
                customSlotIndex = KillerMineDokuLevelFlow.PendingCustomSlotIndex;
                KillerMineDokuLevelFlow.PendingPuzzle = null;
                KillerMineDokuLevelFlow.PendingCustomSlotIndex = -1;
            }

            boardBuilder.RebuildBoardFromJson();
            boardRuntime.Initialize(boardBuilder);
            boardRuntime.MarksChanged += RefreshHud;
            actionPanel.Initialize(this);
            hudView.BindDefaults();
            elapsedSeconds = 0f;
            if (resume != null)
            {
                boardRuntime.ApplyMarkSnapshot(resume.marks);
                elapsedSeconds = resume.elapsedSeconds;
            }

            running = true;
            RefreshHud();
            hudView.SetTime(elapsedSeconds);
        }

        private void Update()
        {
            if (!running) return;
            elapsedSeconds += Time.deltaTime;
            hudView.SetTime(elapsedSeconds);
        }

        public void ClearMarks()
        {
            boardRuntime.ClearMarks();
        }

        public void Submit()
        {
            if (submitting)
            {
                return;
            }

            submitting = true;
            running = false;
            StartCoroutine(SubmitRoutine());
        }

        public void BackToLevelSelect()
        {
            LoadSceneIfAvailable(levelSelectScene);
        }

        private void RefreshHud()
        {
            hudView.SetMineCounts(boardRuntime.TotalMines, boardRuntime.MineMarks, boardRuntime.RemainingMines);
        }

        private void BindDefaults()
        {
            boardBuilder = boardBuilder != null ? boardBuilder : FindObjectOfType<KillerMineDokuBoardBuilder>();
            boardRuntime = boardRuntime != null ? boardRuntime : boardBuilder.GetComponent<PuzzleBoardRuntime>();
            if (boardRuntime == null) boardRuntime = boardBuilder.gameObject.AddComponent<PuzzleBoardRuntime>();
            boardPanel = boardPanel != null ? boardPanel : GameObject.Find("BoardPanel").GetComponent<RectTransform>();

            var leftColumn = GameObject.Find("LeftStatusColumn");
            hudView = hudView != null ? hudView : leftColumn.GetComponent<LevelHudView>();
            if (hudView == null) hudView = leftColumn.AddComponent<LevelHudView>();

            var actionPanelObject = GameObject.Find("OperationPanel");
            actionPanel = actionPanel != null ? actionPanel : actionPanelObject.GetComponent<LevelActionPanel>();
            if (actionPanel == null) actionPanel = actionPanelObject.AddComponent<LevelActionPanel>();

            catalog = catalog != null ? catalog : KillerMineDokuLevelCatalog.LoadDefault();
        }

        private IEnumerator SubmitRoutine()
        {
            var victory = boardRuntime.IsSolved();
            yield return new WaitForEndOfFrame();

            var currentEntry = catalog != null ? catalog.FindByPuzzle(boardBuilder.puzzleJson) : null;
            var nextEntry = victory && catalog != null ? catalog.GetNext(currentEntry) : null;
            var nextPuzzle = nextEntry != null ? nextEntry.puzzleJson : null;
            var currentLevel = currentEntry != null ? currentEntry.levelNumber : KillerMineDokuLevelFlow.PendingLevelNumber;
            var nextLevel = nextEntry != null ? nextEntry.levelNumber : 0;
            var bestTime = victory ? SaveBestTime(currentEntry, elapsedSeconds) : 0f;
            if (victory && catalog != null && currentEntry != null)
            {
                catalog.UnlockNextAfter(currentEntry);
            }

            KillerMineDokuLevelFlow.SetResult(new KillerMineDokuLevelFlow.LevelResult
            {
                victory = victory,
                boardImage = CaptureBoardPanel(),
                elapsedSeconds = elapsedSeconds,
                bestTimeSeconds = bestTime,
                wrongMarks = boardRuntime.CountWrongMarks(),
                missingMines = boardRuntime.CountMissingMines(),
                mineMarks = boardRuntime.MineMarks,
                safeMarks = boardRuntime.SafeMarks,
                totalMines = boardRuntime.TotalMines,
                submittedMarks = boardRuntime.CreateMarkSnapshot(),
                levelSceneName = SceneManager.GetActiveScene().name,
                levelSelectSceneName = levelSelectScene,
                currentPuzzle = boardBuilder.puzzleJson,
                nextPuzzle = nextPuzzle,
                currentLevel = currentLevel,
                nextLevel = nextLevel,
                customSlotIndex = customSlotIndex,
                allLevelsUnlocked = victory && catalog != null && catalog.GetUnlockedLevel() > catalog.PresetCount
            });

            LoadSceneIfAvailable(victory ? victoryScene : failScene);
        }

        private Texture2D CaptureBoardPanel()
        {
            if (boardPanel == null)
            {
                return null;
            }

            var rect = GetScreenRect(boardPanel);
            rect = FitAspect(rect, 26f / 22f);
            rect.xMin = Mathf.Clamp(rect.xMin, 0f, Screen.width - 1f);
            rect.xMax = Mathf.Clamp(rect.xMax, rect.xMin + 1f, Screen.width);
            rect.yMin = Mathf.Clamp(rect.yMin, 0f, Screen.height - 1f);
            rect.yMax = Mathf.Clamp(rect.yMax, rect.yMin + 1f, Screen.height);

            var texture = new Texture2D(Mathf.RoundToInt(rect.width), Mathf.RoundToInt(rect.height), TextureFormat.RGBA32, false);
            texture.ReadPixels(rect, 0, 0);
            texture.Apply();
            return texture;
        }

        private static Rect GetScreenRect(RectTransform target)
        {
            var canvas = target.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            for (var i = 0; i < corners.Length; i++)
            {
                var point = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static Rect FitAspect(Rect rect, float aspect)
        {
            var current = rect.width / rect.height;
            if (current > aspect)
            {
                var width = rect.height * aspect;
                rect.x += (rect.width - width) * 0.5f;
                rect.width = width;
            }
            else
            {
                var height = rect.width / aspect;
                rect.y += (rect.height - height) * 0.5f;
                rect.height = height;
            }

            return rect;
        }

        private void LoadSceneIfAvailable(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
                return;
            }

            Debug.Log($"Scene '{sceneName}' is not in build settings yet.", this);
        }

        private float SaveBestTime(KillerMineDokuLevelCatalog.LevelEntry currentEntry, float seconds)
        {
            if (customSlotIndex >= 0)
            {
                return CustomLevelStore.SaveBestTime(customSlotIndex, seconds);
            }

            return catalog != null ? catalog.SaveBestTime(currentEntry, seconds) : seconds;
        }
    }
}
