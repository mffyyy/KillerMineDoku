using System;
using UnityEngine;

namespace KillerMineDoku.UI
{
    [DisallowMultipleComponent]
    public sealed class PuzzleBoardRuntime : MonoBehaviour
    {
        public KillerMineDokuBoardBuilder boardBuilder;

        public event Action MarksChanged;

        private KillerMineDokuPuzzleData puzzle;
        private BoardCellView[,] cells;
        private BoardCellView.CellMark[,] marks;
        private int[,] cageIndexByCell;
        private int hoveredRow = -1;
        private int hoveredColumn = -1;
        private PuzzleBoardValidator.MarkCheck lastCheck;

        public int Size => puzzle != null ? puzzle.size : 0;
        public int TotalMines => puzzle != null ? puzzle.TotalMines : 0;
        public int MineMarks { get; private set; }
        public int SafeMarks { get; private set; }
        public int RemainingMines => Mathf.Max(0, TotalMines - MineMarks);
        public int WrongMarks => lastCheck.wrongMarks;
        public int MissingMines => lastCheck.missingMines;
        public KillerMineDokuPuzzleData Puzzle => puzzle;

        public void Initialize(KillerMineDokuBoardBuilder builder)
        {
            boardBuilder = builder;
            puzzle = KillerMineDokuPuzzleData.Parse(boardBuilder.puzzleJson.text);
            if (puzzle == null || !puzzle.isComplete || !puzzle.hasUniqueSolution)
            {
                Debug.LogWarning($"Puzzle data is not ready: {puzzle?.validationMessage}", boardBuilder.puzzleJson);
            }

            cageIndexByCell = puzzle.BuildCageIndexGrid();
            cells = new BoardCellView[puzzle.size, puzzle.size];
            marks = new BoardCellView.CellMark[puzzle.size, puzzle.size];
            BindGeneratedCells();
            RecountMarks();
            RefreshCheck();
        }

        public void HoverCell(int row, int column)
        {
            ClearHover();
            hoveredRow = row;
            hoveredColumn = column;
            SetHover(row, column, true);
        }

        public void ClearHover(int row, int column)
        {
            if (hoveredRow == row && hoveredColumn == column)
            {
                ClearHover();
            }
        }

        public void ClickCell(int row, int column, BoardCellView.CellMark requestedMark)
        {
            var current = marks[row, column];
            var next = current == requestedMark ? BoardCellView.CellMark.Unknown : requestedMark;
            marks[row, column] = next;
            cells[row, column].SetMark(next);
            RecountMarks();
            RefreshCheck();
        }

        public void ClearMarks()
        {
            for (var row = 0; row < puzzle.size; row++)
            {
                for (var column = 0; column < puzzle.size; column++)
                {
                    marks[row, column] = BoardCellView.CellMark.Unknown;
                    cells[row, column].SetMark(BoardCellView.CellMark.Unknown);
                }
            }

            RecountMarks();
            RefreshCheck();
        }

        public bool IsSolved()
        {
            RefreshCheck();
            return lastCheck.solved;
        }

        public int CountWrongMarks()
        {
            RefreshCheck();
            return lastCheck.wrongMarks;
        }

        public int CountMissingMines()
        {
            RefreshCheck();
            return lastCheck.missingMines;
        }

        public BoardCellView.CellMark[,] CreateMarkSnapshot()
        {
            return marks != null ? (BoardCellView.CellMark[,])marks.Clone() : null;
        }

        public void ApplyMarkSnapshot(BoardCellView.CellMark[,] snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            var rowCount = Mathf.Min(puzzle.size, snapshot.GetLength(0));
            var columnCount = Mathf.Min(puzzle.size, snapshot.GetLength(1));
            for (var row = 0; row < puzzle.size; row++)
            {
                for (var column = 0; column < puzzle.size; column++)
                {
                    var mark = row < rowCount && column < columnCount ? snapshot[row, column] : BoardCellView.CellMark.Unknown;
                    marks[row, column] = mark;
                    cells[row, column].SetMark(mark);
                }
            }

            RecountMarks();
            RefreshCheck();
        }

        private void BindGeneratedCells()
        {
            var root = boardBuilder.transform.Find("Generated_Board/PuzzleBoard/CellRoot");
            for (var row = 0; row < puzzle.size; row++)
            {
                for (var column = 0; column < puzzle.size; column++)
                {
                    var wrapper = root.Find($"Cell_R{row + 1}C{column + 1}");
                    var cell = wrapper.GetComponentInChildren<BoardCellView>(true);
                    cells[row, column] = cell;
                    cell.SetSelected(false);
                    cell.SetMark(BoardCellView.CellMark.Unknown);

                    var input = cell.GetComponent<BoardCellInput>();
                    if (input == null)
                    {
                        input = cell.gameObject.AddComponent<BoardCellInput>();
                    }

                    input.Initialize(this, row, column);
                }
            }
        }

        private void ClearHover()
        {
            if (hoveredRow >= 0 && hoveredColumn >= 0)
            {
                SetHover(hoveredRow, hoveredColumn, false);
            }

            hoveredRow = -1;
            hoveredColumn = -1;
        }

        private void SetHover(int row, int column, bool active)
        {
            var cage = cageIndexByCell[row, column];
            for (var r = 0; r < puzzle.size; r++)
            {
                for (var c = 0; c < puzzle.size; c++)
                {
                    var inCage = cageIndexByCell[r, c] == cage;
                    if (inCage && cells[r, c].cageBorder != null)
                    {
                        cells[r, c].cageBorder.active = active;
                        cells[r, c].cageBorder.SetVerticesDirty();
                    }
                }
            }

            cells[row, column].SetSelected(active);
        }

        private void RecountMarks()
        {
            MineMarks = 0;
            SafeMarks = 0;
            for (var row = 0; row < puzzle.size; row++)
            {
                for (var column = 0; column < puzzle.size; column++)
                {
                    if (marks[row, column] == BoardCellView.CellMark.Mine) MineMarks++;
                    if (marks[row, column] == BoardCellView.CellMark.Safe) SafeMarks++;
                }
            }

            MarksChanged?.Invoke();
        }

        private void RefreshCheck()
        {
            lastCheck = PuzzleBoardValidator.Check(puzzle, marks);
        }
    }
}
