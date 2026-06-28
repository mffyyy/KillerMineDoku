using TMPro;
using UnityEngine;

namespace KillerMineDoku.UI
{
    [CreateAssetMenu(menuName = "KillerMineDoku/UI Theme", fileName = "KillerMineDokuTheme")]
    public sealed class KillerMineDokuTheme : ScriptableObject
    {
        [Header("Core Colors")]
        public Color primaryYellow = FromHex("#FDC800");
        public Color secondaryPurple = FromHex("#432DD7");
        public Color successGreen = FromHex("#16A34A");
        public Color warningOrange = FromHex("#D97706");
        public Color dangerRed = FromHex("#DC2626");

        [Header("Surfaces")]
        public Color surface = FromHex("#FBFBF9");
        public Color paper = FromHex("#FFF8DF");
        public Color panel = FromHex("#FFFDF3");
        public Color ink = FromHex("#1C293C");
        public Color muted = FromHex("#5D6675");
        public Color softLine = FromHex("#D9D0B7");

        [Header("Fonts")]
        public TMP_FontAsset bodyFont;
        public TMP_FontAsset monoFont;

        [Header("Shape")]
        public int radiusSmall = 4;
        public int radiusMedium = 8;
        public float borderThin = 2f;
        public float borderNormal = 3f;
        public float borderHeavy = 6f;
        public float cageDefaultLine = 3f;
        public float cageActiveLine = 4f;

        [Header("Shadow")]
        public Vector2 shadowSmall = new(4f, -4f);
        public Vector2 shadowDefault = new(6f, -6f);
        public Vector2 shadowBoard = new(10f, -10f);
        public Vector2 shadowPressed = new(1f, -1f);

        [Header("Motion")]
        public float hoverDuration = 0.16f;
        public float pressDuration = 0.16f;
        public Vector2 hoverOffset = new(-1f, 1f);
        public Vector2 pressOffset = new(3f, -3f);

        [Header("Cage Palette")]
        public Color[] cagePalette =
        {
            FromHex("#16A34A"),
            FromHex("#432DD7"),
            FromHex("#DC2626"),
            FromHex("#F97316"),
            FromHex("#06B6D4"),
            FromHex("#2563EB"),
            FromHex("#7C3AED"),
            FromHex("#D97706"),
            FromHex("#0891B2"),
            FromHex("#F43F5E"),
            FromHex("#D9D3C8")
        };

        [Header("Icons")]
        public Sprite mineIcon;
        public Sprite safeIcon;
        public Sprite flagIcon;
        public Sprite verifiedIcon;
        public Sprite trashIcon;
        public Sprite lockIcon;
        public Sprite completeIcon;
        public Sprite backIcon;
        public Sprite editIcon;
        public Sprite clearIcon;
        public Sprite gridIcon;
        public Sprite exitIcon;
        public Sprite plusIcon;
        public Sprite playIcon;
        public Sprite clockIcon;
        public Sprite lightbulbIcon;

        [Header("UI Sprites")]
        public Sprite buttonWhite;
        public Sprite buttonPrimary;
        public Sprite buttonSecondary;
        public Sprite buttonDanger;
        public Sprite cardPanel;
        public Sprite cardPaper;
        public Sprite cardWhite;
        public Sprite boardPanel;
        public Sprite boardCell;
        public Sprite selectionOverlay;
        public Sprite solutionPreviewOverlay;
        public Sprite paperDotBackground;

        public Color GetCageColor(int index)
        {
            if (cagePalette == null || cagePalette.Length == 0)
            {
                return softLine;
            }

            if (index >= 0 && index < cagePalette.Length)
            {
                return cagePalette[index];
            }

            return cagePalette[^1];
        }

        private static Color FromHex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.magenta;
        }
    }
}
