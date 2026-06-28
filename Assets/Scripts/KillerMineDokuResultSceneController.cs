using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KillerMineDoku.UI
{
    [DisallowMultipleComponent]
    public sealed class KillerMineDokuResultSceneController : MonoBehaviour
    {
        public Image gameImage;
        public RawImage rawGameImage;
        public TMP_Text timeValue;
        public TMP_Text bestTimeValue;
        public TMP_Text wrongNum;
        public TMP_Text missingNum;
        public TMP_Text unlockLevel;
        public Button retryButton;
        public Button reCheckButton;
        public Button nextLevelButton;
        public Button backButton;
        public KillerMineDokuLevelCatalog catalog;

        private KillerMineDokuLevelFlow.LevelResult result;

        private void Awake()
        {
            BindDefaults();
            result = KillerMineDokuLevelFlow.LastResult;
            ApplyResult();
            BindButtons();
        }

        private void BindDefaults()
        {
            gameImage = gameImage != null ? gameImage : FindComponentByName<Image>("GameImage");
            rawGameImage = rawGameImage != null ? rawGameImage : FindComponentByName<RawImage>("GameImage");
            timeValue = timeValue != null ? timeValue : FindComponentByName<TMP_Text>("TimeValue");
            bestTimeValue = bestTimeValue != null ? bestTimeValue : FindComponentByName<TMP_Text>("BestTimeValue");
            wrongNum = wrongNum != null ? wrongNum : FindComponentByName<TMP_Text>("WrongNum");
            missingNum = missingNum != null ? missingNum : FindFirstText("MissingNum", "UnmarkedNum", "RemainNum");
            unlockLevel = unlockLevel != null ? unlockLevel : FindComponentByName<TMP_Text>("UnlockLevel");
            retryButton = retryButton != null ? retryButton : FindComponentByName<Button>("RetryButton");
            reCheckButton = reCheckButton != null ? reCheckButton : FindComponentByName<Button>("ReCheckButton");
            nextLevelButton = nextLevelButton != null ? nextLevelButton : FindComponentByName<Button>("NextLevelButton");
            backButton = backButton != null ? backButton : FindComponentByName<Button>("BackButton");
            catalog = catalog != null ? catalog : KillerMineDokuLevelCatalog.LoadDefault();
        }

        private void ApplyResult()
        {
            if (result == null)
            {
                SetText(timeValue, "00:00");
                SetText(bestTimeValue, "00:00");
                SetText(wrongNum, "0");
                SetText(missingNum, "0");
                SetText(unlockLevel, string.Empty);
                return;
            }

            ApplyBoardImage(result.boardImage);
            SetText(timeValue, FormatTime(result.elapsedSeconds));
            SetText(bestTimeValue, FormatTime(result.bestTimeSeconds > 0f ? result.bestTimeSeconds : result.elapsedSeconds));
            SetText(wrongNum, result.wrongMarks.ToString());
            SetText(missingNum, result.missingMines.ToString());

            if (unlockLevel == null)
            {
                return;
            }

            if (!result.victory)
            {
                unlockLevel.text = string.Empty;
            }
            else if (catalog != null)
            {
                var unlockedLevel = catalog.GetUnlockedLevel();
                unlockLevel.text = unlockedLevel > catalog.PresetCount ? "\u5df2\u5168\u90e8\u89e3\u9501" : $"\u5173\u5361 {unlockedLevel:00}";
            }
            else
            {
                unlockLevel.text = result.allLevelsUnlocked || result.nextPuzzle == null ? "\u5df2\u5168\u90e8\u89e3\u9501" : $"\u5173\u5361 {result.nextLevel:00}";
            }
        }

        private void ApplyBoardImage(Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            if (rawGameImage != null)
            {
                rawGameImage.texture = texture;
            }

            if (gameImage != null)
            {
                gameImage.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                gameImage.preserveAspect = true;
            }
        }

        private void BindButtons()
        {
            retryButton?.onClick.AddListener(Retry);
            reCheckButton?.onClick.AddListener(ReCheck);
            nextLevelButton?.onClick.AddListener(NextLevel);
            backButton?.onClick.AddListener(BackToLevelSelect);
        }

        public void Retry()
        {
            if (result == null)
            {
                return;
            }

            KillerMineDokuLevelFlow.PendingPuzzle = result.currentPuzzle;
            KillerMineDokuLevelFlow.PendingCustomSlotIndex = result.customSlotIndex;
            LoadScene(result.levelSceneName);
        }

        public void ReCheck()
        {
            if (result == null)
            {
                return;
            }

            KillerMineDokuLevelFlow.SetResume(new KillerMineDokuLevelFlow.ResumeState
            {
                puzzle = result.currentPuzzle,
                marks = result.submittedMarks != null ? (BoardCellView.CellMark[,])result.submittedMarks.Clone() : null,
                elapsedSeconds = result.elapsedSeconds,
                customSlotIndex = result.customSlotIndex
            });
            LoadScene(result.levelSceneName);
        }

        public void NextLevel()
        {
            if (result == null || result.nextPuzzle == null)
            {
                BackToLevelSelect();
                return;
            }

            KillerMineDokuLevelFlow.PendingPuzzle = result.nextPuzzle;
            KillerMineDokuLevelFlow.PendingCustomSlotIndex = -1;
            LoadScene(result.levelSceneName);
        }

        public void BackToLevelSelect()
        {
            LoadScene(result != null ? result.levelSelectSceneName : "SelectLevel");
        }

        private static void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.Log($"Scene '{sceneName}' is not in build settings yet.");
            }
        }

        private static T FindComponentByName<T>(string objectName) where T : Component
        {
            var objects = Resources.FindObjectsOfTypeAll<T>();
            foreach (var component in objects)
            {
                if (component.gameObject.scene.IsValid() && component.name == objectName)
                {
                    return component;
                }
            }

            return null;
        }

        private static TMP_Text FindFirstText(params string[] names)
        {
            for (var i = 0; i < names.Length; i++)
            {
                var text = FindComponentByName<TMP_Text>(names[i]);
                if (text != null)
                {
                    return text;
                }
            }

            return null;
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private static string FormatTime(float seconds)
        {
            var totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }
    }
}
