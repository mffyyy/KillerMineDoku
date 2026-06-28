using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace KillerMineDoku.UI
{
    public sealed class KillerMineDokuPuzzleData
    {
        public string name;
        public int size;
        public int[] rowMineCounts;
        public int[] columnMineCounts;
        public readonly List<CageData> cages = new();
        public bool isComplete;
        public bool hasUniqueSolution;
        public int solutionCount;
        public string validationMessage = string.Empty;
        public bool[,] solutionMines;
        public readonly List<Vector2Int> mineCoordinates = new();

        private int[,] cageIndexByCell;

        public int TotalMines
        {
            get
            {
                var total = 0;
                if (rowMineCounts == null) return 0;
                for (var i = 0; i < rowMineCounts.Length; i++) total += rowMineCounts[i];
                return total;
            }
        }

        public int GetRowMineCount(int row) => rowMineCounts[row];
        public int GetColumnMineCount(int column) => columnMineCounts[column];

        public int[,] BuildCageIndexGrid()
        {
            if (cageIndexByCell != null) return cageIndexByCell;

            var grid = new int[size, size];
            for (var row = 0; row < size; row++)
            {
                for (var column = 0; column < size; column++) grid[row, column] = -1;
            }

            for (var cageIndex = 0; cageIndex < cages.Count; cageIndex++)
            {
                foreach (var cell in cages[cageIndex].cells)
                {
                    var row = cell.x - 1;
                    var column = cell.y - 1;
                    if (row >= 0 && row < size && column >= 0 && column < size)
                    {
                        grid[row, column] = cageIndex;
                    }
                }
            }

            cageIndexByCell = grid;
            return cageIndexByCell;
        }

        public int[,] BuildClueGrid()
        {
            var grid = new int[size, size];
            for (var row = 0; row < size; row++)
            {
                for (var column = 0; column < size; column++) grid[row, column] = -1;
            }

            for (var cageIndex = 0; cageIndex < cages.Count; cageIndex++)
            {
                var best = new Vector2Int(int.MaxValue, int.MaxValue);
                foreach (var cell in cages[cageIndex].cells)
                {
                    if (cell.x < best.x || cell.x == best.x && cell.y < best.y)
                    {
                        best = cell;
                    }
                }

                if (best.x != int.MaxValue)
                {
                    grid[best.x - 1, best.y - 1] = cages[cageIndex].clue;
                }
            }

            return grid;
        }

        public Dictionary<int, int> BuildColorIndexMap()
        {
            var map = new Dictionary<int, int>();
            for (var i = 0; i < cages.Count; i++)
            {
                map[i] = Mathf.Max(0, cages[i].clue);
            }

            return map;
        }

        public bool IsMine(int row, int column)
        {
            return solutionMines != null && solutionMines[row, column];
        }

        public static KillerMineDokuPuzzleData Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            var data = new KillerMineDokuPuzzleData
            {
                name = MatchString(json, "name"),
                size = MatchInt(json, "size", 6)
            };

            data.rowMineCounts = MatchIntArrayOrValue(json, "minesPerRow", data.size, 1);
            data.columnMineCounts = MatchIntArrayOrValue(json, "minesPerColumn", data.size, 1);
            data.ParseCages(json);
            data.ValidateAndSolve();
            return data;
        }

        private void ParseCages(string json)
        {
            var cagesStart = json.IndexOf("\"cages\"", StringComparison.Ordinal);
            if (cagesStart < 0) return;

            var arrayStart = json.IndexOf('[', cagesStart);
            var arrayEnd = FindMatching(json, arrayStart, '[', ']');
            if (arrayStart < 0 || arrayEnd < 0) return;

            var cagesText = json.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
            var cursor = 0;
            while (cursor < cagesText.Length)
            {
                var objectStart = cagesText.IndexOf('{', cursor);
                if (objectStart < 0) break;
                var objectEnd = FindMatching(cagesText, objectStart, '{', '}');
                if (objectEnd < 0) break;

                var objectText = cagesText.Substring(objectStart, objectEnd - objectStart + 1);
                var cage = new CageData
                {
                    id = MatchInt(objectText, "id", cages.Count + 1),
                    clue = MatchInt(objectText, "clue", 0)
                };

                foreach (Match match in Regex.Matches(objectText, @"\[\s*(\d+)\s*,\s*(\d+)\s*\]"))
                {
                    cage.cells.Add(new Vector2Int(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value)));
                }

                cages.Add(cage);
                cursor = objectEnd + 1;
            }
        }

        private void ValidateAndSolve()
        {
            isComplete = ValidateCompleteness(out validationMessage);
            if (!isComplete)
            {
                solutionCount = 0;
                hasUniqueSolution = false;
                return;
            }

            solutionCount = CountSolutions(2, out solutionMines);
            hasUniqueSolution = solutionCount == 1;
            mineCoordinates.Clear();

            if (solutionMines != null)
            {
                for (var row = 0; row < size; row++)
                {
                    for (var column = 0; column < size; column++)
                    {
                        if (solutionMines[row, column])
                        {
                            mineCoordinates.Add(new Vector2Int(row + 1, column + 1));
                        }
                    }
                }
            }

            if (!hasUniqueSolution)
            {
                validationMessage = solutionCount == 0 ? "Puzzle has no solution." : "Puzzle has multiple solutions.";
            }
        }

        private bool ValidateCompleteness(out string message)
        {
            if (size <= 0)
            {
                message = "Puzzle size is invalid.";
                return false;
            }

            if (rowMineCounts == null || rowMineCounts.Length != size || columnMineCounts == null || columnMineCounts.Length != size)
            {
                message = "Row or column mine counts do not match puzzle size.";
                return false;
            }

            var rowTotal = 0;
            var columnTotal = 0;
            for (var i = 0; i < size; i++)
            {
                if (rowMineCounts[i] < 0 || rowMineCounts[i] > size || columnMineCounts[i] < 0 || columnMineCounts[i] > size)
                {
                    message = "Row or column mine count is out of range.";
                    return false;
                }

                rowTotal += rowMineCounts[i];
                columnTotal += columnMineCounts[i];
            }

            if (rowTotal != columnTotal)
            {
                message = "Row and column mine totals do not match.";
                return false;
            }

            var cageTotal = 0;
            var covered = new bool[size, size];
            foreach (var cage in cages)
            {
                if (cage.clue < 0 || cage.clue > cage.cells.Count)
                {
                    message = $"Cage {cage.id} clue is out of range.";
                    return false;
                }

                cageTotal += cage.clue;
                foreach (var cell in cage.cells)
                {
                    var row = cell.x - 1;
                    var column = cell.y - 1;
                    if (row < 0 || row >= size || column < 0 || column >= size)
                    {
                        message = $"Cage {cage.id} has a cell outside the board.";
                        return false;
                    }

                    if (covered[row, column])
                    {
                        message = $"Cell R{row + 1}C{column + 1} is used by more than one cage.";
                        return false;
                    }

                    covered[row, column] = true;
                }
            }

            if (cageTotal != rowTotal)
            {
                message = "Cage clue total does not match total mine count.";
                return false;
            }

            for (var row = 0; row < size; row++)
            {
                for (var column = 0; column < size; column++)
                {
                    if (!covered[row, column])
                    {
                        message = $"Cell R{row + 1}C{column + 1} is not covered by any cage.";
                        return false;
                    }
                }
            }

            message = string.Empty;
            BuildCageIndexGrid();
            return true;
        }

        private int CountSolutions(int limit, out bool[,] firstSolution)
        {
            bool[,] capturedFirstSolution = null;
            var cageGrid = BuildCageIndexGrid();
            var rowMasks = BuildRowMasks();
            var columnCounts = new int[size];
            var cageCounts = new int[cages.Count];
            var solution = new bool[size, size];
            var count = 0;

            var remainingCageCellsAfterRow = new int[size, cages.Count];
            for (var row = size - 1; row >= 0; row--)
            {
                for (var cage = 0; cage < cages.Count; cage++)
                {
                    remainingCageCellsAfterRow[row, cage] = row == size - 1 ? 0 : remainingCageCellsAfterRow[row + 1, cage];
                }

                if (row < size - 1)
                {
                    for (var column = 0; column < size; column++)
                    {
                        remainingCageCellsAfterRow[row, cageGrid[row + 1, column]]++;
                    }
                }
            }

            Search(0);
            firstSolution = capturedFirstSolution;
            return count;

            void Search(int row)
            {
                if (count >= limit) return;
                if (row >= size)
                {
                    for (var column = 0; column < size; column++)
                    {
                        if (columnCounts[column] != columnMineCounts[column]) return;
                    }

                    for (var cage = 0; cage < cages.Count; cage++)
                    {
                        if (cageCounts[cage] != cages[cage].clue) return;
                    }

                    count++;
                    if (capturedFirstSolution == null)
                    {
                        capturedFirstSolution = (bool[,])solution.Clone();
                    }

                    return;
                }

                foreach (var mask in rowMasks[row])
                {
                    if (!CanApplyRow(row, mask, remainingCageCellsAfterRow)) continue;

                    ApplyRow(row, mask, true);
                    Search(row + 1);
                    ApplyRow(row, mask, false);
                }
            }

            bool CanApplyRow(int row, int mask, int[,] remainingCages)
            {
                for (var column = 0; column < size; column++)
                {
                    var hasMine = (mask & (1 << column)) != 0;
                    var nextColumnCount = columnCounts[column] + (hasMine ? 1 : 0);
                    if (nextColumnCount > columnMineCounts[column]) return false;
                    if (nextColumnCount + size - row - 1 < columnMineCounts[column]) return false;
                }

                var rowCageAdds = new int[cages.Count];
                for (var column = 0; column < size; column++)
                {
                    if ((mask & (1 << column)) != 0)
                    {
                        rowCageAdds[cageGrid[row, column]]++;
                    }
                }

                for (var cage = 0; cage < cages.Count; cage++)
                {
                    var nextCageCount = cageCounts[cage] + rowCageAdds[cage];
                    if (nextCageCount > cages[cage].clue) return false;
                    if (nextCageCount + remainingCages[row, cage] < cages[cage].clue) return false;
                }

                return true;
            }

            void ApplyRow(int row, int mask, bool add)
            {
                var delta = add ? 1 : -1;
                for (var column = 0; column < size; column++)
                {
                    var hasMine = (mask & (1 << column)) != 0;
                    solution[row, column] = add && hasMine;
                    if (!hasMine) continue;

                    columnCounts[column] += delta;
                    cageCounts[cageGrid[row, column]] += delta;
                }
            }
        }

        private List<int>[] BuildRowMasks()
        {
            var masks = new List<int>[size];
            var maxMask = 1 << size;
            for (var row = 0; row < size; row++)
            {
                masks[row] = new List<int>();
                for (var mask = 0; mask < maxMask; mask++)
                {
                    if (CountBits(mask) == rowMineCounts[row])
                    {
                        masks[row].Add(mask);
                    }
                }
            }

            return masks;
        }

        private static int CountBits(int value)
        {
            var count = 0;
            while (value != 0)
            {
                value &= value - 1;
                count++;
            }

            return count;
        }

        private static int[] MatchIntArrayOrValue(string json, string key, int size, int fallback)
        {
            var keyMatch = Regex.Match(json, "\\\"" + key + "\\\"\\s*:\\s*(\\[[^\\]]*\\]|-?\\d+)");
            var values = new int[size];
            if (!keyMatch.Success)
            {
                for (var i = 0; i < size; i++) values[i] = fallback;
                return values;
            }

            var raw = keyMatch.Groups[1].Value.Trim();
            if (!raw.StartsWith("[", StringComparison.Ordinal))
            {
                var value = int.TryParse(raw, out var parsed) ? parsed : fallback;
                for (var i = 0; i < size; i++) values[i] = value;
                return values;
            }

            var matches = Regex.Matches(raw, @"-?\d+");
            for (var i = 0; i < size; i++)
            {
                values[i] = i < matches.Count && int.TryParse(matches[i].Value, out var parsed) ? parsed : fallback;
            }

            return values;
        }

        private static string MatchString(string json, string key)
        {
            var match = Regex.Match(json, "\\\"" + key + "\\\"\\s*:\\s*\\\"([^\\\"]*)\\\"");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private static int MatchInt(string json, string key, int fallback)
        {
            var match = Regex.Match(json, "\\\"" + key + "\\\"\\s*:\\s*(-?\\d+)");
            return match.Success && int.TryParse(match.Groups[1].Value, out var value) ? value : fallback;
        }

        private static int FindMatching(string text, int start, char open, char close)
        {
            if (start < 0 || start >= text.Length || text[start] != open) return -1;
            var depth = 0;
            var inString = false;
            for (var i = start; i < text.Length; i++)
            {
                var ch = text[i];
                if (ch == '"' && (i == 0 || text[i - 1] != '\\')) inString = !inString;
                if (inString) continue;
                if (ch == open) depth++;
                if (ch == close) depth--;
                if (depth == 0) return i;
            }

            return -1;
        }

        [Serializable]
        public sealed class CageData
        {
            public int id;
            public int clue;
            public readonly List<Vector2Int> cells = new();
        }
    }
}
