namespace KillerMineDoku.UI
{
    public static class PuzzleBoardValidator
    {
        public readonly struct MarkCheck
        {
            public MarkCheck(bool solved, int matchedMines, int wrongMarks, int missingMines)
            {
                this.solved = solved;
                this.matchedMines = matchedMines;
                this.wrongMarks = wrongMarks;
                this.missingMines = missingMines;
            }

            public readonly bool solved;
            public readonly int matchedMines;
            public readonly int wrongMarks;
            public readonly int missingMines;
        }

        public static MarkCheck Check(KillerMineDokuPuzzleData puzzle, BoardCellView.CellMark[,] marks)
        {
            if (puzzle == null || marks == null || !puzzle.hasUniqueSolution)
            {
                return new MarkCheck(false, 0, CountMineMarks(marks), puzzle != null ? puzzle.TotalMines : 0);
            }

            var markedMines = 0;
            var matchedMines = 0;
            for (var row = 0; row < puzzle.size; row++)
            {
                for (var column = 0; column < puzzle.size; column++)
                {
                    if (marks[row, column] != BoardCellView.CellMark.Mine) continue;

                    markedMines++;
                    if (puzzle.IsMine(row, column))
                    {
                        matchedMines++;
                    }
                }
            }

            var wrongMarks = markedMines - matchedMines;
            var missingMines = puzzle.TotalMines - matchedMines;
            return new MarkCheck(wrongMarks == 0 && missingMines == 0, matchedMines, wrongMarks, missingMines);
        }

        private static int CountMineMarks(BoardCellView.CellMark[,] marks)
        {
            if (marks == null) return 0;

            var count = 0;
            for (var row = 0; row < marks.GetLength(0); row++)
            {
                for (var column = 0; column < marks.GetLength(1); column++)
                {
                    if (marks[row, column] == BoardCellView.CellMark.Mine)
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
}
