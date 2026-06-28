using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KillerMineDoku.UI
{
    [DisallowMultipleComponent]
    public sealed class MenuSceneController : MonoBehaviour
    {
        private const string PuzzleBuilderUrl = "https://mffyyy.github.io/KillerMinePuzzleBuilder/";

        public KillerMineDokuLevelCatalog catalog;
        public Button levelSceneButton;
        public Button exitButton;
        public Button lockedButton;
        public Button unlockedButton;
        public string selectLevelScene = "SelectLevel";

        private Color unlockedButtonOriginalColor;
        private bool hasUnlockedButtonOriginalColor;

        private void Awake()
        {
            BindDefaults();
            ApplyBuilderButtonState();
            BindButtons();
        }

        private void BindDefaults()
        {
            catalog = catalog != null ? catalog : KillerMineDokuLevelCatalog.LoadDefault();
            levelSceneButton = levelSceneButton != null ? levelSceneButton : FindButton("LevelSceneButton");
            exitButton = exitButton != null ? exitButton : FindButton("ExitButton");
            lockedButton = lockedButton != null ? lockedButton : FindButton("LockedButton");
            unlockedButton = unlockedButton != null ? unlockedButton : FindButton("UnLockedButton");
            if (unlockedButton != null && unlockedButton.targetGraphic != null)
            {
                unlockedButtonOriginalColor = unlockedButton.targetGraphic.color;
                hasUnlockedButtonOriginalColor = true;
            }
        }

        private void ApplyBuilderButtonState()
        {
            var unlocked = catalog != null && catalog.CountCompletedPresets() >= catalog.PresetCount;
            if (lockedButton != null)
            {
                lockedButton.gameObject.SetActive(!unlocked);
            }

            if (unlockedButton != null)
            {
                unlockedButton.gameObject.SetActive(unlocked);
            }
        }

        private void BindButtons()
        {
            if (levelSceneButton != null)
            {
                levelSceneButton.onClick.RemoveAllListeners();
                levelSceneButton.onClick.AddListener(OpenSelectLevel);
            }

            if (exitButton != null)
            {
                exitButton.onClick.RemoveAllListeners();
                exitButton.onClick.AddListener(ExitGame);
            }

            if (unlockedButton != null)
            {
                unlockedButton.onClick.RemoveAllListeners();
                unlockedButton.onClick.AddListener(OpenPuzzleBuilder);
            }
        }

        public void OpenSelectLevel()
        {
            if (Application.CanStreamedLevelBeLoaded(selectLevelScene))
            {
                SceneManager.LoadScene(selectLevelScene);
                return;
            }

            Debug.Log($"Scene '{selectLevelScene}' is not in build settings yet.", this);
        }

        public void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void OpenPuzzleBuilder()
        {
            RestoreUnlockedButtonVisualNow(unlockedButton);
            StartCoroutine(RestoreButtonVisual(unlockedButton));
            Application.OpenURL(PuzzleBuilderUrl);
        }

        private System.Collections.IEnumerator RestoreButtonVisual(Button button)
        {
            if (button == null)
            {
                yield break;
            }

            var targetGraphic = button.targetGraphic;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            RestoreUnlockedButtonVisualNow(button);
            yield return null;
            RestoreUnlockedButtonVisualNow(button);
            yield return new WaitForEndOfFrame();
            RestoreUnlockedButtonVisualNow(button);
        }

        private void RestoreUnlockedButtonVisualNow(Button button)
        {
            if (button == null)
            {
                return;
            }

            var targetGraphic = button.targetGraphic;
            if (targetGraphic != null)
            {
                targetGraphic.color = hasUnlockedButtonOriginalColor ? unlockedButtonOriginalColor : targetGraphic.color;
            }

            button.transform.localScale = Vector3.one;
        }

        private static Button FindButton(string objectName)
        {
            var buttons = Resources.FindObjectsOfTypeAll<Button>();
            for (var i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                if (!button.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (button.name.Trim() == objectName)
                {
                    return button;
                }
            }

            return null;
        }
    }
}
