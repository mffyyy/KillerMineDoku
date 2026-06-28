using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KillerMineDoku.UI
{
    [DisallowMultipleComponent]
    public sealed class SelfCreatedLevelCardView : MonoBehaviour
    {
        public TMP_Text gridRowText;
        public TMP_Text gridColumnText;
        public TMP_Text mineNumText;
        public TMP_Text bestTimeText;
        public TMP_Text stateText;
        public Image stateIcon;
        public Image cardImage;
        public Button playButton;
        public Button deleteButton;

        public void Apply(
            CustomLevelStore.SlotData slot,
            SelectLevelVisualSet visuals,
            Action<CustomLevelStore.SlotData> onPlay,
            Action<int> onDelete)
        {
            BindDefaults();
            if (slot == null)
            {
                return;
            }

            SetText(gridRowText, slot.Puzzle.size.ToString());
            SetText(gridColumnText, slot.Puzzle.size.ToString());
            SetText(mineNumText, slot.Puzzle.TotalMines.ToString());
            SetText(bestTimeText, slot.BestTimeSeconds > 0f ? KillerMineDokuLevelCatalog.FormatTime(slot.BestTimeSeconds) : "--:--");

            var completed = slot.BestTimeSeconds > 0f;
            SetText(stateText, completed ? "\u5df2\u5b8c\u6210" : "\u5f00\u59cb\u6e38\u620f");

            if (cardImage != null)
            {
                cardImage.color = visuals.unlockedCardColor;
            }

            if (stateText != null)
            {
                stateText.color = completed ? visuals.completedTextColor : visuals.playableTextColor;
            }

            if (stateIcon != null)
            {
                stateIcon.sprite = completed ? visuals.completedIcon : visuals.playableIcon;
                stateIcon.color = completed ? visuals.completedIconColor : visuals.playableIconColor;
                stateIcon.enabled = stateIcon.sprite != null;
            }

            if (playButton != null)
            {
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(() => onPlay?.Invoke(slot));
            }

            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveAllListeners();
                deleteButton.onClick.AddListener(() => onDelete?.Invoke(slot.SlotIndex));
            }
        }

        private void BindDefaults()
        {
            cardImage = cardImage != null ? cardImage : GetComponent<Image>();
            playButton = playButton != null ? playButton : GetComponent<Button>();
            playButton = playButton != null ? playButton : FindPrimaryPlayButton();
            deleteButton = deleteButton != null ? deleteButton : FindButton("DeleteButton");
            gridRowText = gridRowText != null ? gridRowText : FindText("GridRow");
            gridColumnText = gridColumnText != null ? gridColumnText : FindText("GridColumn");
            mineNumText = mineNumText != null ? mineNumText : FindText("MineNumText");
            bestTimeText = bestTimeText != null ? bestTimeText : FindText("BestTimeText");
            stateText = stateText != null ? stateText : FindText("StateText");
            stateIcon = stateIcon != null ? stateIcon : FindImage("StateIcon");
        }

        private Button FindPrimaryPlayButton()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            for (var i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].name != "DeleteButton")
                {
                    return buttons[i];
                }
            }

            return null;
        }

        private Button FindButton(string objectName)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            for (var i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].name == objectName)
                {
                    return buttons[i];
                }
            }

            return null;
        }

        private TMP_Text FindText(string objectName)
        {
            var texts = GetComponentsInChildren<TMP_Text>(true);
            for (var i = 0; i < texts.Length; i++)
            {
                if (texts[i].name == objectName)
                {
                    return texts[i];
                }
            }

            return null;
        }

        private Image FindImage(string objectName)
        {
            var images = GetComponentsInChildren<Image>(true);
            for (var i = 0; i < images.Length; i++)
            {
                if (images[i].name == objectName)
                {
                    return images[i];
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
    }
}
