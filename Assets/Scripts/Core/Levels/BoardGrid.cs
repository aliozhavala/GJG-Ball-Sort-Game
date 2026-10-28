using System;
using System.Collections.Generic;

namespace BallSort.Core.Levels
{
    /// <summary>
    /// The board's resolved grid: the cells of each row, which rows are aligned, where every tube
    /// sits, and where an added tube goes (see <c>docs/mechanics.md</c>).
    /// </summary>
    /// <remarks>
    /// <b>Aligned rows</b> hold part of a large tube and share column positions; <b>free rows</b>
    /// are centred on their own. Cells count gaps and every row a large tube spans, so the cell
    /// count is a row's width on screen.
    /// <para>
    /// Mutable only through <see cref="TryAddTube"/>: rows grow at their end, up to
    /// <see cref="LevelLayout.MaxCellsPerRowInPlay"/> cells, and rows are never added. Existing tubes
    /// therefore never change row or order within their row.
    /// </para>
    /// </remarks>
    public sealed class BoardGrid
    {
        /// <summary>Value of <see cref="GetTubeAt"/> for a gap.</summary>
        public const int Gap = -1;

        private readonly List<int>[] _rows;
        private readonly bool[] _isAligned;
        private readonly List<GridPlacement> _placements;

        /// <summary>Number of rows; fixed for the level.</summary>
        public int RowCount => _rows.Length;

        /// <summary>Number of tubes placed on the grid, which matches the board's tube count.</summary>
        public int TubeCount => _placements.Count;

        /// <summary>Cell count of the widest row; what the board scale has to fit.</summary>
        public int WidestRowCellCount
        {
            get
            {
                int widest = 0;
                foreach (List<int> row in _rows)
                {
                    widest = Math.Max(widest, row.Count);
                }

                return widest;
            }
        }

        /// <summary>True while some row has fewer than <see cref="LevelLayout.MaxCellsPerRowInPlay"/> cells.</summary>
        public bool CanAddTube => GetCellCount(ChooseRowForAddedTube()) < LevelLayout.MaxCellsPerRowInPlay;

        /// <summary>
        /// Builds the grid from a level's cells: rows top first, each cell a tube index or
        /// <c>null</c>. Assumes they passed <see cref="LevelLayout.ValidateGrid"/>, so a tube listed in
        /// several rows is a large tube spanning them in one column.
        /// </summary>
        /// <param name="cells">The level's <c>layout.grid</c>.</param>
        /// <param name="tubeCount">Number of tubes in the level; each must appear on the grid.</param>
        /// <exception cref="ArgumentException">If a tube is missing from the grid or a cell names no tube.</exception>
        public BoardGrid(IReadOnlyList<IReadOnlyList<int?>> cells, int tubeCount)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            if (cells.Count == 0)
            {
                throw new ArgumentException("A grid has at least one row.", nameof(cells));
            }

            _rows = new List<int>[cells.Count];
            _isAligned = new bool[cells.Count];
            var topRow = new int[tubeCount];
            var bottomRow = new int[tubeCount];
            var cellOf = new int[tubeCount];
            var seen = new bool[tubeCount];

            for (int r = 0; r < cells.Count; r++)
            {
                _rows[r] = new List<int>(LevelLayout.MaxCellsPerRowInPlay);
                for (int c = 0; c < cells[r].Count; c++)
                {
                    int? cell = cells[r][c];
                    if (cell == null)
                    {
                        _rows[r].Add(Gap);
                        continue;
                    }

                    int tube = cell.Value;
                    if (tube < 0 || tube >= tubeCount)
                    {
                        throw new ArgumentException($"Cell [{r}][{c}] names tube {tube}; the level has {tubeCount}.", nameof(cells));
                    }

                    if (!seen[tube])
                    {
                        seen[tube] = true;
                        topRow[tube] = r;
                        cellOf[tube] = c;
                    }

                    bottomRow[tube] = r;
                    _rows[r].Add(tube);
                }
            }

            _placements = new List<GridPlacement>(tubeCount);
            for (int t = 0; t < tubeCount; t++)
            {
                if (!seen[t])
                {
                    throw new ArgumentException($"Tube {t} is not on the grid.", nameof(cells));
                }

                int span = bottomRow[t] - topRow[t] + 1;
                _placements.Add(new GridPlacement(bottomRow[t], cellOf[t], span));
                if (span > 1)
                {
                    for (int r = topRow[t]; r <= bottomRow[t]; r++)
                    {
                        _isAligned[r] = true;
                    }
                }
            }
        }

        /// <summary>Cells in <paramref name="row"/>, counting gaps and large-tube cells.</summary>
        public int GetCellCount(int row) => _rows[row].Count;

        /// <summary>The tube in a cell, or <see cref="Gap"/>. A large tube is in every row it spans.</summary>
        public int GetTubeAt(int row, int cell) => _rows[row][cell];

        /// <summary>True for a row holding part of a large tube; aligned rows share column positions.</summary>
        public bool IsAligned(int row) => _isAligned[row];

        /// <summary>Where a tube sits.</summary>
        public GridPlacement GetPlacement(int tubeIndex) => _placements[tubeIndex];

        /// <summary>
        /// Places the next tube (index <see cref="TubeCount"/>) at the end of the row with the fewest
        /// cells; ties go to the bottom-most of the tied rows.
        /// </summary>
        /// <param name="row">The row the tube joined.</param>
        /// <param name="cell">Its cell, the row's new last cell.</param>
        /// <returns>False, changing nothing, when every row already has <see cref="LevelLayout.MaxCellsPerRowInPlay"/> cells.</returns>
        public bool TryAddTube(out int row, out int cell)
        {
            row = ChooseRowForAddedTube();
            if (GetCellCount(row) >= LevelLayout.MaxCellsPerRowInPlay)
            {
                row = -1;
                cell = -1;
                return false;
            }

            cell = _rows[row].Count;
            _rows[row].Add(_placements.Count);
            _placements.Add(new GridPlacement(row, cell, 1));
            return true;
        }

        private int ChooseRowForAddedTube()
        {
            // Scanning bottom-up and only taking strictly fewer cells makes ties go to the lowest row.
            int best = _rows.Length - 1;
            for (int r = _rows.Length - 2; r >= 0; r--)
            {
                if (_rows[r].Count < _rows[best].Count)
                {
                    best = r;
                }
            }

            return best;
        }
    }
}
