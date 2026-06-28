using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KillerMineDoku.UI
{
    [DisallowMultipleComponent]
    public sealed class LevelCardView : MonoBehaviour
    {
        public TMP_Text levelNumberText;
        public TMP_Text gridRowText;
        public TMP_Text gridColumnText;
        public TMP_Text mineNumText;
        public TMP_Text bestTimeText;
        public TMP_Text stateText;
        public Image numBackground;
        public Image stateIcon;
        public Image cardImage;
        public Button button;

        public void Apply(
            KillerMineDokuLevelCatalog.LevelEntry entry,
            bool unlocked,
            bool completed,
            float bestTimeSeconds,
            SelectLevelVisualSet visuals,
            Action<KillerMineDokuLevelCatalog.LevelEntry> onClick)
        {
            BindDefaults();

            SetText(levelNumberText, entry != null ? entry.levelNumber.ToString("00") : "--");
            SetText(gridRowText, entry != null ? entry.GridSize.ToString() : "-");
            SetText(gridColumnText, entry != null ? entry.GridSize.ToString() : "-");
            SetText(mineNumText, entry != null ? entry.MineCount.ToString() : "-");
            SetText(bestTimeText, bestTimeSeconds > 0f ? KillerMineDokuLevelCatalog.FormatTime(bestTimeSeconds) : "--:--");

            var statusText = "\u672a\u89e3\u9501";
            var statusColor = visuals.lockedTextColor;
            var numColor = visuals.lockedNumBackgroundColor;
            var icon = visuals.lockedIcon;
            var iconColor = visuals.lockedIconColor;

            if (completed)
            {
                statusText = "\u5df2\u5b8c\u6210";
                statusColor = visuals.completedTextColor;
                numColor = visuals.completedNumBackgroundColor;
                icon = visuals.completedIcon;
                iconColor = visuals.completedIconColor;
            }
            else if (unlocked)
            {
                statusText = "\u5f00\u59cb\u6e38\u620f";
                statusColor = visuals.playableTextColor;
                numColor = visuals.playableNumBackgroundColor;
                icon = visuals.playableIcon;
                iconColor = visuals.playableIconColor;
            }

            SetText(stateText, statusText);
            if (stateText != null)
            {
                stateText.color = statusColor;
            }

            if (stateIcon != null)
            {
                stateIcon.sprite = icon != null ? icon : stateIcon.sprite;
                stateIcon.color = iconColor;
                stateIcon.enabled = stateIcon.sprite != null;
            }

            if (numBackground != null)
            {
                numBackground.color = numColor;
            }

            if (cardImage != null)
            {
                cardImage.color = unlocked ? visuals.unlockedCardColor : visuals.lockedCardColor;
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.interactable = unlocked && entry != null && entry.puzzleJson != null;
                if (button.interactable)
                {
                    button.onClick.AddListener(() => onClick?.Invoke(entry));
                }
            }
        }

        private void BindDefaults()
        {
            button = button != null ? button : GetComponent<Button>();
            cardImage = cardImage != null ? cardImage : GetComponent<Image>();
            levelNumberText = levelNumberText != null ? levelNumberText : FindText("number", "Num");
            gridRowText = gridRowText != null ? gridRowText : FindText("GridRow");
            gridColumnText = gridColumnText != null ? gridColumnText : FindText("GridIColumn", "GridColumn");
            mineNumText = mineNumText != null ? mineNumText : FindText("MineNum", "MineNumText");
            bestTimeText = bestTimeText != null ? bestTimeText : FindText("BestTimeText");
            stateText = stateText != null ? stateText : FindText("StateText");
            numBackground = numBackground != null ? numBackground : FindImage("NumBackground");
            stateIcon = stateIcon != null ? stateIcon : FindImage("StateIcon");
        }

        private TMP_Text FindText(params string[] names)
        {
            var texts = GetComponentsInChildren<TMP_Text>(true);
            for (var n = 0; n < names.Length; n++)
            {
                for (var i = 0; i < texts.Length; i++)
                {
                    if (texts[i].name == names[n])
                    {
                        return texts[i];
                    }
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

    [Serializable]
    public struct SelectLevelVisualSet
    {
        public Color playableTextColor;
        public Color completedTextColor;
        public Color lockedTextColor;
        public Color playableNumBackgroundColor;
        public Color completedNumBackgroundColor;
        public Color lockedNumBackgroundColor;
        public Color playableIconColor;
        public Color completedIconColor;
        public Color lockedIconColor;
        public Color unlockedCardColor;
        public Color lockedCardColor;
        public Sprite playableIcon;
        public Sprite completedIcon;
        public Sprite lockedIcon;

        public static SelectLevelVisualSet Default => new()
        {
            playableTextColor = new Color(0.11f, 0.161f, 0.235f, 1f),
            completedTextColor = new Color(0.086f, 0.639f, 0.29f, 1f),
            lockedTextColor = new Color(0.11f, 0.161f, 0.235f, 1f),
            playableNumBackgroundColor = new Color(0.992f, 0.784f, 0f, 1f),
            completedNumBackgroundColor = new Color(0.086f, 0.639f, 0.29f, 1f),
            lockedNumBackgroundColor = new Color(0.424f, 0.439f, 0.467f, 1f),
            playableIconColor = new Color(0.11f, 0.161f, 0.235f, 1f),
            completedIconColor = Color.white,
            lockedIconColor = new Color(0.11f, 0.161f, 0.235f, 1f),
            unlockedCardColor = new Color(0.996f, 0.988f, 0.953f, 1f),
            lockedCardColor = new Color(0.996f, 0.988f, 0.953f, 1f)
        };
    }
}
