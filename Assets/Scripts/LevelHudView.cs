using TMPro;
using UnityEngine;

namespace KillerMineDoku.UI
{
    [DisallowMultipleComponent]
    public sealed class LevelHudView : MonoBehaviour
    {
        public TMP_Text timerText;
        public TMP_Text totalMineText;
        public TMP_Text markedMineText;
        public TMP_Text remainingMineText;
        public TMP_Text levelValueText;

        public void BindDefaults()
        {
            timerText = timerText != null ? timerText : FindText("TimerCard/value");
            totalMineText = totalMineText != null ? totalMineText : FindText("TotalMineCard/value");
            markedMineText = markedMineText != null ? markedMineText : FindText("MarkedMineCard /value");
            remainingMineText = remainingMineText != null ? remainingMineText : FindText("RemainCard/value");
            levelValueText = levelValueText != null ? levelValueText : FindText("LevelTitleCard/LevelValue");
        }

        public void SetTime(float seconds)
        {
            if (timerText == null) return;
            var whole = Mathf.FloorToInt(seconds);
            timerText.text = $"{whole / 60:00}:{whole % 60:00}";
        }

        public void SetMineCounts(int total, int marked, int remaining)
        {
            if (totalMineText != null) totalMineText.text = total.ToString();
            if (markedMineText != null) markedMineText.text = marked.ToString();
            if (remainingMineText != null) remainingMineText.text = remaining.ToString();
        }

        public void SetLevelNumber(int levelNumber, bool isCustomLevel)
        {
            if (levelValueText == null) return;
            levelValueText.text = isCustomLevel || levelNumber <= 0 ? "--" : levelNumber.ToString("00");
        }

        private TMP_Text FindText(string path)
        {
            var child = transform.Find(path);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }
    }
}
