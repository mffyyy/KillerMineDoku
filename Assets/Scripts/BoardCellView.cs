using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KillerMineDoku.UI
{
    [DisallowMultipleComponent]
    public sealed class BoardCellView : MonoBehaviour
    {
        public enum CellMark
        {
            Unknown,
            Safe,
            Mine,
            SolutionPreview
        }

        public KillerMineDokuTheme theme;
        public Image background;
        public Image selectionOverlay;
        public Image icon;
        public TMP_Text cageLabel;
        public CageBorderRenderer cageBorder;

        [SerializeField] private CellMark mark;
        [SerializeField] private bool selected;


        private void OnValidate()
        {
            Refresh();
        }

        public void SetTheme(KillerMineDokuTheme newTheme)
        {
            theme = newTheme;
            if (cageBorder != null)
            {
                cageBorder.theme = theme;
            }

            Refresh();
        }

        public void SetSelected(bool isSelected)
        {
            selected = isSelected;
            Refresh();
        }

        public void SetMark(CellMark nextMark)
        {
            mark = nextMark;
            Refresh();
        }

        public void SetCage(int cageIndex, int mineCount, bool active)
        {
            if (cageLabel != null)
            {
                cageLabel.text = mineCount.ToString();
                cageLabel.color = theme != null ? theme.GetCageColor(cageIndex) : Color.black;
            }

            if (cageBorder != null)
            {
                cageBorder.color = theme != null ? theme.GetCageColor(cageIndex) : Color.black;
                cageBorder.active = active;
                cageBorder.SetVerticesDirty();
            }
        }

        private void Refresh()
        {

            if (selectionOverlay != null)
            {
                selectionOverlay.gameObject.SetActive(selected);
                selectionOverlay.enabled = selected;
                for (var i = 0; i < selectionOverlay.transform.childCount; i++)
                {
                    selectionOverlay.transform.GetChild(i).gameObject.SetActive(selected);
                }
            }

            if (icon == null)
            {
                return;
            }

            icon.enabled = mark != CellMark.Unknown;
            icon.color = Color.white;
            var nextSprite = mark switch
            {
                CellMark.Safe => theme != null ? theme.safeIcon : null,
                CellMark.Mine => theme != null ? theme.mineIcon : null,
                CellMark.SolutionPreview => null,
                _ => null
            };
            if (nextSprite != null)
            {
                icon.sprite = nextSprite;
            }

            if (mark == CellMark.SolutionPreview)
            {
                icon.enabled = false;
            }
        }
    }
}
