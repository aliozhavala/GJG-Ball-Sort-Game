using System;
using System.Collections.Generic;

namespace BallSort.Core.Levels
{
    /// <summary>
    /// Grid layout rules (see <c>docs/mechanics.md</c>): capacities, how many rows a tube spans, and
    /// validation of a level's <c>layout.grid</c>.
    /// </summary>
    /// <remarks>
    /// Lives in Core, not with the views, so the validator, the session's add-tube rule and the
    /// board view all apply exactly the same geometry.
    /// </remarks>
    public static class LevelLayout
    {
        /// <summary>Most rows a level may use. Rows are never added during play.</summary>
        public const int MaxRows = 3;

        /// <summary>Most cells a row may have in a level file.</summary>
        public const int MaxCellsPerRow = 6;

        /// <summary>Most cells a row may grow to during play, through added tubes.</summary>
        public const int MaxCellsPerRowInPlay = 8;

        /// <summary>Capacity of a normal tube; one grid row is exactly this tall.</summary>
        public const int NormalCapacity = 4;

        /// <summary>Largest capacity: a tube spanning all <see cref="MaxRows"/> rows.</summary>
        public const int MaxCapacity = 16;

        /// <summary>Ball slots between two rows; also the room a lifted run rises into.</summary>
        public const int RowGapSlots = 2;

        /// <summary>Vertical distance between the base lines of neighbouring rows, in ball slots.</summary>
        public const int RowPitchSlots = NormalCapacity + RowGapSlots;

        /// <summary>True for 4 (a normal tube) and 5–16 (a large tube spanning rows).</summary>
        public static bool IsValidCapacity(int capacity) => capacity >= NormalCapacity && capacity <= MaxCapacity;

        /// <summary>
        /// How many grid rows a tube of <paramref name="capacity"/> spans: 1 for 4, 2 for 5–10, 3 for
        /// 11–16. <c>n</c> rows are <c>n × 4</c> slots plus <c>(n − 1)</c> gaps of 2, so 10 and 16 fill
        /// their span exactly.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="capacity"/> is not valid.</exception>
        public static int GetRowSpan(int capacity)
        {
            if (!IsValidCapacity(capacity))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity), capacity, $"Capacities run from {NormalCapacity} to {MaxCapacity}.");
            }

            // The smallest n with n × pitch − gap ≥ capacity.
            return (capacity + RowGapSlots + RowPitchSlots - 1) / RowPitchSlots;
        }

        /// <summary>
        /// Checks a level's grid: 1–3 rows of 1–6 cells; every cell a tube index or <c>null</c>; every
        /// tube on the grid; a large tube once in each row it spans, in consecutive rows and the same
        /// cell; <c>null</c> only in aligned rows (rows holding part of a large tube).
        /// </summary>
        /// <param name="grid">The level's <c>layout.grid</c>; may be null (reported).</param>
        /// <param name="capacities">Capacity of every tube, by index. Invalid capacities are reported elsewhere; their span is not checked.</param>
        /// <param name="errors">Receives one message per broken rule.</param>
        /// <returns>True when the grid is valid.</returns>
        public static bool ValidateGrid(
            IReadOnlyList<IReadOnlyList<int?>> grid, IReadOnlyList<int> capacities, ICollection<string> errors)
        {
            if (capacities == null)
            {
                throw new ArgumentNullException(nameof(capacities));
            }

            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            if (grid == null)
            {
                errors.Add("layout.grid is missing; every level lists its tubes in a grid.");
                return false;
            }

            int before = errors.Count;
            if (grid.Count < 1 || grid.Count > MaxRows)
            {
                errors.Add($"layout.grid has {grid.Count} row(s); expected 1 to {MaxRows}.");
            }

            // Where each tube appears, as (row, cell) pairs.
            var occurrences = new List<(int Row, int Cell)>[capacities.Count];
            for (int t = 0; t < occurrences.Length; t++)
            {
                occurrences[t] = new List<(int, int)>();
            }

            for (int r = 0; r < grid.Count; r++)
            {
                IReadOnlyList<int?> row = grid[r];
                if (row == null || row.Count < 1 || row.Count > MaxCellsPerRow)
                {
                    errors.Add($"layout.grid[{r}] has {row?.Count ?? 0} cell(s); level files allow 1 to {MaxCellsPerRow}.");
                    if (row == null)
                    {
                        continue;
                    }
                }

                for (int c = 0; c < row.Count; c++)
                {
                    int? cell = row[c];
                    if (cell == null)
                    {
                        continue;
                    }

                    if (cell < 0 || cell >= capacities.Count)
                    {
                        errors.Add($"layout.grid[{r}][{c}] is {cell}; expected a tube index 0 to {capacities.Count - 1} or null.");
                        continue;
                    }

                    occurrences[cell.Value].Add((r, c));
                }
            }

            var alignedRows = new bool[grid.Count];
            for (int t = 0; t < occurrences.Length; t++)
            {
                ValidateTubeCells(t, capacities[t], occurrences[t], alignedRows, errors);
            }

            for (int r = 0; r < grid.Count; r++)
            {
                IReadOnlyList<int?> row = grid[r];
                for (int c = 0; row != null && c < row.Count; c++)
                {
                    if (row[c] == null && !alignedRows[r])
                    {
                        errors.Add(
                            $"layout.grid[{r}][{c}] is null; gaps are only allowed in rows holding part of a large tube.");
                    }
                }
            }

            return errors.Count == before;
        }

        /// <summary>
        /// Checks where one tube appears and marks the rows a valid large tube makes aligned.
        /// </summary>
        private static void ValidateTubeCells(
            int tubeIndex, int capacity, List<(int Row, int Cell)> cells, bool[] alignedRows, ICollection<string> errors)
        {
            string where = $"tubes[{tubeIndex}]";
            if (cells.Count == 0)
            {
                errors.Add($"{where} is not on layout.grid.");
                return;
            }

            if (!IsValidCapacity(capacity))
            {
                return;
            }

            int span = GetRowSpan(capacity);
            if (span == 1)
            {
                if (cells.Count > 1)
                {
                    errors.Add($"{where} appears {cells.Count} times in layout.grid; a capacity-{capacity} tube takes one cell.");
                }

                return;
            }

            // Listed top row first, so the rows must read r, r + 1, … in one column.
            bool valid = cells.Count == span;
            for (int i = 1; valid && i < cells.Count; i++)
            {
                valid = cells[i].Row == cells[0].Row + i && cells[i].Cell == cells[0].Cell;
            }

            if (!valid)
            {
                errors.Add(
                    $"{where} has capacity {capacity}, so it must appear in {span} consecutive rows of layout.grid, " +
                    $"in the same cell of each; found it at {Describe(cells)}.");
                return;
            }

            foreach ((int Row, int Cell) cell in cells)
            {
                alignedRows[cell.Row] = true;
            }
        }

        private static string Describe(List<(int Row, int Cell)> cells)
        {
            var parts = new string[cells.Count];
            for (int i = 0; i < cells.Count; i++)
            {
                parts[i] = $"[{cells[i].Row}][{cells[i].Cell}]";
            }

            return string.Join(", ", parts);
        }
    }
}
