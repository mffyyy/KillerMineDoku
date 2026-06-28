using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KillerMineDoku.UI
{
    [DisallowMultipleComponent]
    public sealed class SelectLevelController : MonoBehaviour
    {
        public KillerMineDokuLevelCatalog catalog;
        public LevelCardView levelCardPrefab;
        public EmptyCustomLevelSlotView emptyCardPrefab;
        public SelfCreatedLevelCardView selfCreatedLevelCardPrefab;
        public Transform levelGridRoot;
        public TMP_Text completedCountText;
        public TMP_Text presetTotalText;
        public TMP_Text progressText;
        public Button backButton;
        public Sprite plusIcon;
        public WrongNoticePopup wrongNoticePrefab;
        public string menuScene = "MenuScene";
        public string levelScene = "Level";
        public SelectLevelVisualSet visuals = SelectLevelVisualSet.Default;

        private readonly List<LevelCardView> spawnedCards = new();
        private readonly List<GameObject> spawnedCustomCards = new();
        private int pendingWebImportSlotIndex = -1;

        private void Awake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            gameObject.name = $"SelectLevelController_{GetInstanceID()}";
#endif
            BindDefaults();
            BuildPresetCards();
            BuildCustomSlots();
            RefreshCompletion();
            BindButtons();
        }

        private void BindDefaults()
        {
            catalog = catalog != null ? catalog : KillerMineDokuLevelCatalog.LoadDefault();
            levelGridRoot = levelGridRoot != null ? levelGridRoot : FindTransform("LevelSelectGrid");
            completedCountText = completedCountText != null ? completedCountText : FindText("value ", "value", "FinishedValue", "CompletedValue");
            presetTotalText = presetTotalText != null ? presetTotalText : FindText("total", "total (1)", "TotalValue");
            progressText = progressText != null ? progressText : FindText("ProgressText", "FinishedText");
            backButton = backButton != null ? backButton : FindButton("BackButton");
        }

        private void BuildPresetCards()
        {
            ClearSpawnedCards();
            if (catalog == null || levelCardPrefab == null || levelGridRoot == null)
            {
                Debug.LogWarning("SelectLevelController is missing catalog, level card prefab, or grid root.", this);
                return;
            }

            var anchors = CollectGridAnchors();
            var levels = catalog.PresetLevels;
            for (var i = 0; i < levels.Count; i++)
            {
                var parent = i < anchors.Count ? anchors[i] : levelGridRoot;
                var card = FindExistingCard(parent);
                if (card == null)
                {
                    card = Instantiate(levelCardPrefab, parent);
                    card.name = $"LevelCard_{levels[i].levelNumber:00}";
                    ResetRectTransform(card.transform as RectTransform);
                    spawnedCards.Add(card);
                }

                var unlocked = catalog.IsUnlocked(levels[i]);
                var completed = catalog.IsCompleted(levels[i]);
                var bestTime = catalog.GetBestTime(levels[i]);
                card.Apply(levels[i], unlocked, completed, bestTime, visuals, StartPresetLevel);
            }
        }

        private void BuildCustomSlots()
        {
            if (levelGridRoot == null || emptyCardPrefab == null || selfCreatedLevelCardPrefab == null)
            {
                return;
            }

            var customUnlocked = CustomLevelStore.AreCustomLevelsUnlocked(catalog);
            for (var slotIndex = 0; slotIndex < CustomLevelStore.SlotCount; slotIndex++)
            {
                var parent = levelGridRoot.Find($"Grid{slotIndex + 7}") ?? levelGridRoot;

                var slot = CustomLevelStore.Load(slotIndex);
                if (slot != null)
                {
                    SetCardsActive<EmptyCustomLevelSlotView>(parent, false);
                    var card = FindExistingSelfCreatedCard(parent);
                    if (card == null)
                    {
                        card = Instantiate(selfCreatedLevelCardPrefab, parent);
                        card.name = $"SelfCreatedLevelCard_{slotIndex + 1}";
                        spawnedCustomCards.Add(card.gameObject);
                    }

                    ResetRectTransform(card.transform as RectTransform);
                    card.gameObject.SetActive(true);
                    card.Apply(slot, visuals, StartCustomLevel, DeleteCustomLevel);
                }
                else
                {
                    SetCardsActive<SelfCreatedLevelCardView>(parent, false);
                    var card = FindExistingEmptyCard(parent);
                    if (card == null)
                    {
                        card = Instantiate(emptyCardPrefab, parent);
                        card.name = $"EmptyCard_{slotIndex + 1}";
                        spawnedCustomCards.Add(card.gameObject);
                    }

                    ResetRectTransform(card.transform as RectTransform);
                    card.gameObject.SetActive(true);
                    card.Apply(slotIndex, customUnlocked, visuals, plusIcon, ImportCustomLevel);
                }
            }
        }

        private void RefreshCompletion()
        {
            if (catalog == null)
            {
                return;
            }

            var completed = catalog.CountCompletedPresets();
            var total = catalog.PresetCount;
            SetText(completedCountText, completed.ToString());
            SetText(presetTotalText, total.ToString());
            SetText(progressText, $"{completed}/{total}");
        }

        private void BindButtons()
        {
            if (backButton == null)
            {
                return;
            }

            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(BackToMenu);
        }

        private void StartPresetLevel(KillerMineDokuLevelCatalog.LevelEntry entry)
        {
            if (entry == null || entry.puzzleJson == null)
            {
                return;
            }

            KillerMineDokuLevelFlow.PendingPuzzle = entry.puzzleJson;
            KillerMineDokuLevelFlow.PendingLevelNumber = entry.levelNumber;
            KillerMineDokuLevelFlow.PendingCustomSlotIndex = -1;
            LoadSceneIfAvailable(levelScene);
        }

        private void ImportCustomLevel(int slotIndex)
        {
            if (!CustomLevelStore.AreCustomLevelsUnlocked(catalog))
            {
                Debug.Log("\u5b8c\u6210\u5168\u90e8\u9884\u8bbe\u5173\u5361\u540e\u624d\u80fd\u5bfc\u5165\u81ea\u5236\u5173\u5361", this);
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            pendingWebImportSlotIndex = slotIndex;
            CustomLevelImporter.RequestWebJsonImport(
                gameObject.name,
                nameof(OnWebCustomLevelJsonImported),
                nameof(OnWebCustomLevelImportFailed));
            return;
#else
            if (!CustomLevelImporter.TryImportJson(out var json, out var importMessage))
            {
                if (!string.IsNullOrWhiteSpace(importMessage))
                {
                    ShowWrongNotice(importMessage);
                }

                return;
            }

            ImportCustomLevelJson(slotIndex, json);
#endif
        }

        private void ImportCustomLevelJson(int slotIndex, string json)
        {
            if (!CustomLevelStore.TryValidateJson(json, out _, out var validationMessage))
            {
                ShowWrongNotice(FormatValidationNotice(validationMessage));
                return;
            }

            CustomLevelStore.Save(slotIndex, json);
            BuildCustomSlots();
        }

        public void OnWebCustomLevelJsonImported(string json)
        {
            if (pendingWebImportSlotIndex < 0)
            {
                return;
            }

            var slotIndex = pendingWebImportSlotIndex;
            pendingWebImportSlotIndex = -1;
            ImportCustomLevelJson(slotIndex, json);
        }

        public void OnWebCustomLevelImportFailed(string message)
        {
            pendingWebImportSlotIndex = -1;
            if (!string.IsNullOrWhiteSpace(message))
            {
                ShowWrongNotice(message);
            }
        }

        private void StartCustomLevel(CustomLevelStore.SlotData slot)
        {
            if (slot == null)
            {
                return;
            }

            KillerMineDokuLevelFlow.PendingPuzzle = CustomLevelStore.CreateTextAsset(slot);
            KillerMineDokuLevelFlow.PendingLevelNumber = 0;
            KillerMineDokuLevelFlow.PendingCustomSlotIndex = slot.SlotIndex;
            LoadSceneIfAvailable(levelScene);
        }

        private void DeleteCustomLevel(int slotIndex)
        {
            CustomLevelStore.Delete(slotIndex);
            BuildCustomSlots();
        }

        private void BackToMenu()
        {
            LoadSceneIfAvailable(menuScene);
        }

        private void ShowWrongNotice(string message)
        {
            if (wrongNoticePrefab == null)
            {
                Debug.LogWarning(message, this);
                return;
            }

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindObjectOfType<Canvas>();
            }

            var parent = canvas != null ? canvas.transform : transform;
            var notice = Instantiate(wrongNoticePrefab, parent);
            var rect = notice.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.localRotation = Quaternion.identity;
            }

            notice.Show(message);
        }

        private static string FormatValidationNotice(string validationMessage)
        {
            return CustomLevelStore.IsKnownValidationMessage(validationMessage)
                ? $"\u6570\u636e\u9519\u8bef\n{validationMessage}"
                : "\u6570\u636e\u9519\u8bef";
        }

        private List<Transform> CollectGridAnchors()
        {
            var anchors = new List<Transform>();
            for (var i = 0; i < levelGridRoot.childCount; i++)
            {
                var child = levelGridRoot.GetChild(i);
                if (child.name.StartsWith("Grid"))
                {
                    anchors.Add(child);
                }
            }

            anchors.Sort((a, b) => ExtractNumber(a.name).CompareTo(ExtractNumber(b.name)));
            return anchors;
        }

        private static LevelCardView FindExistingCard(Transform parent)
        {
            if (parent == null)
            {
                return null;
            }

            for (var i = 0; i < parent.childCount; i++)
            {
                var card = parent.GetChild(i).GetComponent<LevelCardView>();
                if (card != null)
                {
                    return card;
                }
            }

            return null;
        }

        private static EmptyCustomLevelSlotView FindExistingEmptyCard(Transform parent)
        {
            if (parent == null)
            {
                return null;
            }

            for (var i = 0; i < parent.childCount; i++)
            {
                var card = parent.GetChild(i).GetComponent<EmptyCustomLevelSlotView>();
                if (card != null)
                {
                    return card;
                }
            }

            return null;
        }

        private static SelfCreatedLevelCardView FindExistingSelfCreatedCard(Transform parent)
        {
            if (parent == null)
            {
                return null;
            }

            for (var i = 0; i < parent.childCount; i++)
            {
                var card = parent.GetChild(i).GetComponent<SelfCreatedLevelCardView>();
                if (card != null)
                {
                    return card;
                }
            }

            return null;
        }

        private static void SetCardsActive<T>(Transform parent, bool active) where T : Component
        {
            if (parent == null)
            {
                return;
            }

            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.GetComponent<T>() != null)
                {
                    child.gameObject.SetActive(active);
                }
            }
        }

        private void ClearSpawnedCards()
        {
            for (var i = spawnedCards.Count - 1; i >= 0; i--)
            {
                if (spawnedCards[i] != null)
                {
                    DestroySpawnedObject(spawnedCards[i].gameObject);
                }
            }

            spawnedCards.Clear();
        }

        private void ClearSpawnedCustomCards()
        {
            for (var i = spawnedCustomCards.Count - 1; i >= 0; i--)
            {
                if (spawnedCustomCards[i] != null)
                {
                    DestroySpawnedObject(spawnedCustomCards[i]);
                }
            }

            spawnedCustomCards.Clear();
        }

        private static void DestroySpawnedObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private static void ResetRectTransform(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.anchoredPosition = Vector2.zero;
        }

        private static int ExtractNumber(string value)
        {
            var number = 0;
            for (var i = 0; i < value.Length; i++)
            {
                if (char.IsDigit(value[i]))
                {
                    number = number * 10 + value[i] - '0';
                }
            }

            return number;
        }

        private static Transform FindTransform(string objectName)
        {
            var objects = Resources.FindObjectsOfTypeAll<Transform>();
            for (var i = 0; i < objects.Length; i++)
            {
                if (objects[i].gameObject.scene.IsValid() && objects[i].name == objectName)
                {
                    return objects[i];
                }
            }

            return null;
        }

        private static TMP_Text FindText(params string[] names)
        {
            var texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
            for (var n = 0; n < names.Length; n++)
            {
                for (var i = 0; i < texts.Length; i++)
                {
                    if (texts[i].gameObject.scene.IsValid() && texts[i].name == names[n])
                    {
                        return texts[i];
                    }
                }
            }

            return null;
        }

        private static Button FindButton(string objectName)
        {
            var buttons = Resources.FindObjectsOfTypeAll<Button>();
            for (var i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].gameObject.scene.IsValid() && buttons[i].name == objectName)
                {
                    return buttons[i];
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
    }
}
