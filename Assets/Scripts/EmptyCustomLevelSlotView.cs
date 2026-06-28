using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KillerMineDoku.UI
{
    [DisallowMultipleComponent]
    public sealed class EmptyCustomLevelSlotView : MonoBehaviour
    {
        public TMP_Text titleText;
        public TMP_Text detailText;
        public Image iconImage;
        public Image cardImage;
        public Button button;

        private Color buttonGraphicColor;
        private bool hasButtonGraphicColor;

        public void Apply(int slotIndex, bool unlocked, SelectLevelVisualSet visuals, Sprite plusIcon, Action<int> onImport)
        {
            BindDefaults();

            SetText(titleText, unlocked ? "\u5bfc\u5165\u5173\u5361" : "\u672a\u89e3\u9501");
            SetText(detailText, unlocked ? "\u5bfc\u5165\u6587\u4ef6\u521b\u5efa\u81ea\u5236\u5173\u5361" : "\u5b8c\u6210\u5168\u90e8\u9884\u8bbe\u5173\u5361\u540e\u89e3\u9501");

            var color = unlocked
                ? (Color)new Color32(0xFF, 0xFF, 0xFF, 0xFF)
                : (Color)new Color32(0xD5, 0xD8, 0xDE, 0xFF);
            if (cardImage != null)
            {
                cardImage.color = color;
            }

            if (iconImage != null)
            {
                iconImage.sprite = unlocked && plusIcon != null ? plusIcon : visuals.lockedIcon;
                iconImage.color = unlocked ? visuals.lockedTextColor : visuals.lockedTextColor;
                iconImage.enabled = iconImage.sprite != null;
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.interactable = unlocked;
                CacheButtonGraphicColor();
                if (unlocked)
                {
                    button.onClick.AddListener(() =>
                    {
                        RestoreButtonVisualNow();
                        onImport?.Invoke(slotIndex);
                        if (isActiveAndEnabled)
                        {
                            StartCoroutine(RestoreButtonVisualAfterClick());
                        }
                    });
                }
            }
        }

        private void BindDefaults()
        {
            button = button != null ? button : GetComponent<Button>();
            cardImage = cardImage != null ? cardImage : GetComponent<Image>();
            titleText = titleText != null ? titleText : FindText("TitleText");
            detailText = detailText != null ? detailText : FindText("Text ", "Text");
            iconImage = iconImage != null ? iconImage : FindImage("Icon ", "Icon");
        }

        private void CacheButtonGraphicColor()
        {
            if (button != null && button.targetGraphic != null)
            {
                buttonGraphicColor = button.targetGraphic.color;
                hasButtonGraphicColor = true;
            }
        }

        private IEnumerator RestoreButtonVisualAfterClick()
        {
            RestoreButtonVisualNow();
            yield return null;
            RestoreButtonVisualNow();
            yield return new WaitForEndOfFrame();
            RestoreButtonVisualNow();
        }

        private void RestoreButtonVisualNow()
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            if (button != null)
            {
                if (EventSystem.current != null)
                {
                    button.OnPointerExit(new PointerEventData(EventSystem.current));
                    button.OnDeselect(new BaseEventData(EventSystem.current));
                }

                button.transform.localScale = Vector3.one;

                if (button.targetGraphic != null && hasButtonGraphicColor)
                {
                    button.targetGraphic.color = buttonGraphicColor;
                }
            }
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

        private Image FindImage(params string[] names)
        {
            var images = GetComponentsInChildren<Image>(true);
            for (var n = 0; n < names.Length; n++)
            {
                for (var i = 0; i < images.Length; i++)
                {
                    if (images[i].name == names[n])
                    {
                        return images[i];
                    }
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
