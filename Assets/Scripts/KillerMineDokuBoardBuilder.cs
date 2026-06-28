using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KillerMineDoku.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class KillerMineDokuBoardBuilder : MonoBehaviour
    {
        [Header("Data")]
        public KillerMineDokuTheme theme;
        public TextAsset puzzleJson;

        [Header("Prefabs")]
        public RectTransform puzzleBoardPrefab;
        public BoardCellView boardCellPrefab;
        public RectTransform axisHintPrefab;

        [Header("Preview")]
        public bool showSelectionPreview = true;
        public Vector2Int selectedCell = new(3, 4);
        public bool showActiveCagePreview = true;
        public int activeCageId = -1;

        private const string GeneratedRootName = "Generated_Board";
        private static readonly Vector2 BoardPosition = new(32f, -20f);
        private static readonly Vector2 ColumnHintOffset = new(0f, 32f);
        private static readonly Vector2 RowHintOffset = new(-50f, -8f);
        private static readonly Vector2 PuzzleBoardPadding = new(12f, 12f);

        [ContextMenu("Rebuild Board From JSON")]
        public void RebuildBoardFromJson()
        {
            if (theme == null || puzzleJson == null)
            {
                Debug.LogWarning("KillerMineDokuBoardBuilder needs a theme and puzzle JSON.", this);
                return;
            }

            var puzzle = KillerMineDokuPuzzleData.Parse(puzzleJson.text);
            if (puzzle == null)
            {
                Debug.LogWarning("Puzzle JSON could not be parsed.", puzzleJson);
                return;
            }

            if (!puzzle.isComplete || !puzzle.hasUniqueSolution)
            {
                Debug.LogWarning($"Puzzle JSON is not complete or unique: {puzzle.validationMessage}", puzzleJson);
            }

            CachePrefabDefaults();
            if (puzzleBoardPrefab == null || boardCellPrefab == null || axisHintPrefab == null)
            {
                Debug.LogWarning("KillerMineDokuBoardBuilder needs PuzzleBoard, BoardCell, and BoardAxisHint prefabs.", this);
                return;
            }

            ClearGeneratedBoard();
            BuildBoard(puzzle);
        }

        private void Reset()
        {
#if UNITY_EDITOR
            theme = UnityEditor.AssetDatabase.LoadAssetAtPath<KillerMineDokuTheme>("Assets/UI/Theme/KillerMineDokuTheme.asset");
            CachePrefabDefaults();
#endif
        }

        private void CachePrefabDefaults()
        {
#if UNITY_EDITOR
            if (puzzleBoardPrefab == null)
            {
                var boardObject = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PuzzleBoard.prefab");
                if (boardObject != null)
                {
                    puzzleBoardPrefab = boardObject.GetComponent<RectTransform>();
                }
            }

            if (boardCellPrefab == null)
            {
                var cellObject = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BoardCell.prefab");
                if (cellObject != null)
                {
                    boardCellPrefab = cellObject.GetComponent<BoardCellView>();
                }
            }

            if (axisHintPrefab == null)
            {
                var axisObject = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BoardAxisHint.prefab");
                if (axisObject != null)
                {
                    axisHintPrefab = axisObject.GetComponent<RectTransform>();
                }
            }
#endif
        }

        private void ClearGeneratedBoard()
        {
            var old = transform.Find(GeneratedRootName);
            if (old != null)
            {
                DestroyObject(old.gameObject);
            }
        }

        private void BuildBoard(KillerMineDokuPuzzleData puzzle)
        {
            var generatedRoot = CreateRect(GeneratedRootName, transform, Vector2.zero);
            Stretch(generatedRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var size = puzzle.size;
            var cellSize = GetCellSize(size);
            var gridSize = new Vector2(cellSize.x * size, cellSize.y * size);
            var firstColumnX = BoardPosition.x - gridSize.x * 0.5f + cellSize.x * 0.5f;
            var firstRowY = BoardPosition.y + gridSize.y * 0.5f - cellSize.y * 0.5f;
            var topEdgeY = BoardPosition.y + gridSize.y * 0.5f;
            var leftEdgeX = BoardPosition.x - gridSize.x * 0.5f;

            var puzzleBoard = CreatePuzzleBoard(generatedRoot, gridSize);
            var cellsRoot = GetPuzzleBoardCellRoot(puzzleBoard, gridSize);
            BuildCells(cellsRoot, puzzle, gridSize, cellSize);

            var axisRoot = CreateRect("AxisHints", generatedRoot, Vector2.zero);
            Stretch(axisRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            BuildAxisLabels(axisRoot, puzzle, firstColumnX, firstRowY, topEdgeY, leftEdgeX);
        }

        private RectTransform CreatePuzzleBoard(RectTransform parent, Vector2 gridSize)
        {
#if UNITY_EDITOR
            var rect = !Application.isPlaying
                ? (RectTransform)UnityEditor.PrefabUtility.InstantiatePrefab(puzzleBoardPrefab, parent)
                : Instantiate(puzzleBoardPrefab, parent);
#else
            var rect = Instantiate(puzzleBoardPrefab, parent);
#endif
            rect.name = "PuzzleBoard";
            SetCenter(rect, BoardPosition);
            rect.sizeDelta = gridSize + PuzzleBoardPadding;
            return rect;
        }

        private RectTransform GetPuzzleBoardCellRoot(RectTransform puzzleBoard, Vector2 gridSize)
        {
            var cellRoot = puzzleBoard.Find("CellRoot") as RectTransform;
            SetCenter(cellRoot, Vector2.zero);
            cellRoot.sizeDelta = gridSize;
            return cellRoot;
        }

        private void BuildCells(RectTransform cellsRoot, KillerMineDokuPuzzleData puzzle, Vector2 gridSize, Vector2 cellSize)
        {
            var cageIndexByCell = puzzle.BuildCageIndexGrid();
            var clueByCell = puzzle.BuildClueGrid();
            var colorByCage = puzzle.BuildColorIndexMap();
            var startX = -gridSize.x * 0.5f + cellSize.x * 0.5f;
            var startY = gridSize.y * 0.5f - cellSize.y * 0.5f;

            for (var row = 0; row < puzzle.size; row++)
            {
                for (var column = 0; column < puzzle.size; column++)
                {
                    var cellRect = CreateRect($"Cell_R{row + 1}C{column + 1}", cellsRoot, cellSize);
                    SetCenter(cellRect, new Vector2(startX + column * cellSize.x, startY - row * cellSize.y));

                    var cageIndex = cageIndexByCell[row, column];
                    var cage = puzzle.cages[cageIndex];
                    var colorIndex = colorByCage[cageIndex];
                    var color = theme.GetCageColor(colorIndex);
                    var cell = CreateBoardCell(cellRect, cellSize);
                    cell.SetTheme(theme);
                    cell.SetSelected(showSelectionPreview && selectedCell.x == row + 1 && selectedCell.y == column + 1);
                    cell.SetMark(BoardCellView.CellMark.Unknown);

                    var active = showActiveCagePreview && cage.id == activeCageId;
                    cell.SetCage(colorIndex, cage.clue, active);
                    if (cell.cageBorder != null)
                    {
                        cell.cageBorder.edges = (CageBorderRenderer.Edges)GetCageEdges(cageIndexByCell, row, column);
                        SetCageBorderContinuity(cell.cageBorder, cageIndexByCell, row, column);
                        cell.cageBorder.raycastTarget = false;
                        cell.cageBorder.SetVerticesDirty();
                    }

                    if (cell.cageLabel != null)
                    {
                        var hasClue = clueByCell[row, column] >= 0;
                        cell.cageLabel.gameObject.SetActive(hasClue);
                        cell.cageLabel.text = hasClue ? cage.clue.ToString() : string.Empty;
                        cell.cageLabel.color = color;
                    }
                }
            }
        }

        private static void SetCageBorderContinuity(CageBorderRenderer border, int[,] cageIndexByCell, int row, int column)
        {
            var size = cageIndexByCell.GetLength(0);
            var cage = cageIndexByCell[row, column];

            border.extendTopLeft = column > 0 && cageIndexByCell[row, column - 1] == cage && (row == 0 || cageIndexByCell[row - 1, column - 1] != cage);
            border.extendTopRight = column < size - 1 && cageIndexByCell[row, column + 1] == cage && (row == 0 || cageIndexByCell[row - 1, column + 1] != cage);
            border.extendBottomLeft = column > 0 && cageIndexByCell[row, column - 1] == cage && (row == size - 1 || cageIndexByCell[row + 1, column - 1] != cage);
            border.extendBottomRight = column < size - 1 && cageIndexByCell[row, column + 1] == cage && (row == size - 1 || cageIndexByCell[row + 1, column + 1] != cage);
            border.extendLeftTop = row > 0 && cageIndexByCell[row - 1, column] == cage && (column == 0 || cageIndexByCell[row - 1, column - 1] != cage);
            border.extendLeftBottom = row < size - 1 && cageIndexByCell[row + 1, column] == cage && (column == 0 || cageIndexByCell[row + 1, column - 1] != cage);
            border.extendRightTop = row > 0 && cageIndexByCell[row - 1, column] == cage && (column == size - 1 || cageIndexByCell[row - 1, column + 1] != cage);
            border.extendRightBottom = row < size - 1 && cageIndexByCell[row + 1, column] == cage && (column == size - 1 || cageIndexByCell[row + 1, column + 1] != cage);
        }

        private void BuildAxisLabels(RectTransform parent, KillerMineDokuPuzzleData puzzle, float firstColumnX, float firstRowY, float topEdgeY, float leftEdgeX)
        {
            for (var i = 0; i < puzzle.size; i++)
            {
                var cellSize = GetCellSize(puzzle.size);
                var columnX = firstColumnX + i * cellSize.x;
                var rowY = firstRowY - i * cellSize.y;

                var columnHint = CreateAxisHint($"ColumnHint_C{i + 1}", parent, puzzle.GetColumnMineCount(i).ToString(), $"C{i + 1}");
                SetCenter(columnHint, new Vector2(columnX + ColumnHintOffset.x, topEdgeY + ColumnHintOffset.y));

                var rowHint = CreateAxisHint($"RowHint_R{i + 1}", parent, puzzle.GetRowMineCount(i).ToString(), $"R{i + 1}");
                SetCenter(rowHint, new Vector2(leftEdgeX + RowHintOffset.x, rowY + RowHintOffset.y));
            }
        }

        private BoardCellView CreateBoardCell(RectTransform wrapper, Vector2 cellSize)
        {
#if UNITY_EDITOR
            var cell = !Application.isPlaying
                ? (BoardCellView)UnityEditor.PrefabUtility.InstantiatePrefab(boardCellPrefab, wrapper)
                : Instantiate(boardCellPrefab, wrapper);
#else
            var cell = Instantiate(boardCellPrefab, wrapper);
#endif
            cell.name = "BoardCellView";
            var prefabRect = cell.GetComponent<RectTransform>();
            var prefabSize = GetPrefabCellSize(cellSize);
            var scale = Mathf.Min(cellSize.x / prefabSize.x, cellSize.y / prefabSize.y);
            prefabRect.anchorMin = new Vector2(0.5f, 0.5f);
            prefabRect.anchorMax = new Vector2(0.5f, 0.5f);
            prefabRect.pivot = new Vector2(0.5f, 0.5f);
            prefabRect.sizeDelta = prefabSize;
            prefabRect.localScale = new Vector3(scale, scale, 1f);
            prefabRect.localRotation = Quaternion.identity;
            prefabRect.anchoredPosition = Vector2.zero;
            return cell;
        }

        private Vector2 GetPrefabCellSize(Vector2 cellSize)
        {
            var prefabRect = boardCellPrefab.GetComponent<RectTransform>();
            var size = prefabRect != null ? prefabRect.sizeDelta : cellSize;
            if (size.x <= 0f) size.x = cellSize.x;
            if (size.y <= 0f) size.y = cellSize.y;
            return size;
        }

        private static Vector2 GetCellSize(int puzzleSize)
        {
            var size = puzzleSize >= 9 ? 88f : 128f;
            return new Vector2(size, size);
        }

        private RectTransform CreateAxisHint(string name, Transform parent, string text, string title)
        {
#if UNITY_EDITOR
            var rect = !Application.isPlaying
                ? (RectTransform)UnityEditor.PrefabUtility.InstantiatePrefab(axisHintPrefab, parent)
                : Instantiate(axisHintPrefab, parent);
#else
            var rect = Instantiate(axisHintPrefab, parent);
#endif
            rect.name = name;

            var tmpLabel = FindAxisTmpText(rect, "Label");
            if (tmpLabel != null)
            {
                tmpLabel.text = text;
                tmpLabel.raycastTarget = false;
            }

            var tmpTitle = FindAxisTmpText(rect, "title") ?? FindAxisTmpText(rect, "Title");
            if (tmpTitle != null)
            {
                tmpTitle.text = title;
                tmpTitle.raycastTarget = false;
            }

            var imageComponent = rect.GetComponent<Image>();
            if (imageComponent != null)
            {
                imageComponent.color = Color.clear;
                imageComponent.raycastTarget = false;
            }

            SetAxisBorder(rect, true);
            return rect;
        }

        private static TMP_Text FindAxisTmpText(RectTransform root, string childName)
        {
            var child = root.Find(childName);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }

        private void SetAxisBorder(RectTransform rect, bool visible)
        {
            var borderNames = new[] { "BorderTop", "BorderRight", "BorderBottom", "BorderLeft" };
            for (var i = 0; i < borderNames.Length; i++)
            {
                var child = rect.Find(borderNames[i]);
                if (child != null)
                {
                    child.gameObject.SetActive(visible);
                    var line = child.GetComponent<Image>();
                    if (line != null)
                    {
                        line.color = WithAlpha(theme.ink, 0.72f);
                        line.raycastTarget = false;
                    }
                }
            }

            var border = rect.Find("Border");
            if (border != null)
            {
                border.gameObject.SetActive(visible);
                var image = border.GetComponent<Image>();
                if (image != null)
                {
                    image.raycastTarget = false;
                }
            }
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var rect = (RectTransform)go.transform;
            rect.sizeDelta = size;
            return rect;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetCenter(RectTransform rect, Vector2 position)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            DestroyImmediate(target);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static int GetCageEdges(int[,] cageIndexByCell, int row, int column)
        {
            var size = cageIndexByCell.GetLength(0);
            var cage = cageIndexByCell[row, column];
            var edges = 0;
            if (row == 0 || cageIndexByCell[row - 1, column] != cage) edges |= (int)CageBorderRenderer.Edges.Top;
            if (column == size - 1 || cageIndexByCell[row, column + 1] != cage) edges |= (int)CageBorderRenderer.Edges.Right;
            if (row == size - 1 || cageIndexByCell[row + 1, column] != cage) edges |= (int)CageBorderRenderer.Edges.Bottom;
            if (column == 0 || cageIndexByCell[row, column - 1] != cage) edges |= (int)CageBorderRenderer.Edges.Left;
            return edges;
        }

    }
}
