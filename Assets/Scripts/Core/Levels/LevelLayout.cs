using System;
using System.Collections.Generic;

namespace BallSort.Core.Levels
{
    /// <summary>
    /// Row layout rules: how a level's tubes are split into rows on screen.
    /// </summary>
    /// <remarks>
    /// Lives in Core, not with the views, so the editor validator and tests apply exactly the rule
    /// the game uses.
    /// </remarks>
    public static class LevelLayout
    {
        /// <summary>Most tubes a single row may hold; beyond this tubes get too narrow on a phone.</summary>
        public const int MaxTubesPerRow = 6;

        /// <summary>Most rows a level may use.</summary>
        public const int MaxRows = 3;

        /// <summary>Largest tube count any layout can hold.</summary>
        public const int MaxTubes = MaxTubesPerRow * MaxRows;

        /// <summary>
        /// The automatic layout: the fewest rows of at most <see cref="MaxTubesPerRow"/> tubes, split
        /// as evenly as possible, with any extra tubes in the upper rows.
        /// </summary>
        /// <param name="tubeCount">Number of tubes; <c>1 .. </c><see cref="MaxTubes"/>.</param>
        /// <returns>Tube counts per row, top row first, e.g. 7 → <c>[4, 3]</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="tubeCount"/> cannot be laid out.</exception>
        public static int[] AutoRows(int tubeCount)
        {
            if (tubeCount < 1 || tubeCount > MaxTubes)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(tubeCount), tubeCount, $"Automatic layout supports 1 to {MaxTubes} tubes.");
            }

            int rowCount = (tubeCount + MaxTubesPerRow - 1) / MaxTubesPerRow;
            int baseSize = tubeCount / rowCount;
            int extra = tubeCount % rowCount;

            var rows = new int[rowCount];
            for (int i = 0; i < rowCount; i++)
            {
                rows[i] = i < extra ? baseSize + 1 : baseSize;
            }

            return rows;
        }

        /// <summary>
        /// Checks explicit rows against the layout rules.
        /// </summary>
        /// <param name="rows">Tube counts per row, top row first.</param>
        /// <param name="tubeCount">Number of tubes in the level.</param>
        /// <param name="errors">Receives one human-readable message per broken rule.</param>
        /// <returns>True when the rows are valid.</returns>
        public static bool ValidateRows(IReadOnlyList<int> rows, int tubeCount, ICollection<string> errors)
        {
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            int before = errors.Count;
            if (rows.Count < 1 || rows.Count > MaxRows)
            {
                errors.Add($"layout.rows has {rows.Count} row(s); expected 1 to {MaxRows}.");
            }

            int sum = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] < 1 || rows[i] > MaxTubesPerRow)
                {
                    errors.Add($"layout.rows[{i}] is {rows[i]}; each row holds 1 to {MaxTubesPerRow} tubes.");
                }

                sum += rows[i];
            }

            if (sum != tubeCount)
            {
                errors.Add($"layout.rows sums to {sum} but the level has {tubeCount} tube(s).");
            }

            return errors.Count == before;
        }

        /// <summary>
        /// Where an added tube goes: onto the last (bottom) row if it has room, otherwise onto a new
        /// row below it. Existing tubes never move between rows, so the board does not reshuffle
        /// under the player.
        /// </summary>
        /// <param name="rows">Current tube counts per row, top row first.</param>
        /// <param name="newRows">The rows after appending, when this returns true; null otherwise.</param>
        /// <returns>
        /// False when the tube does not fit: the last row is full and there are already
        /// <see cref="MaxRows"/> rows. For automatic layouts that is exactly <see cref="MaxTubes"/>
        /// tubes; an explicit layout with a short middle row can run out of room sooner.
        /// </returns>
        public static bool TryAppendTube(IReadOnlyList<int> rows, out int[] newRows)
        {
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            if (rows.Count == 0)
            {
                throw new ArgumentException("A layout has at least one row.", nameof(rows));
            }

            bool lastRowHasRoom = rows[rows.Count - 1] < MaxTubesPerRow;
            if (!lastRowHasRoom && rows.Count >= MaxRows)
            {
                newRows = null;
                return false;
            }

            newRows = new int[lastRowHasRoom ? rows.Count : rows.Count + 1];
            for (int i = 0; i < rows.Count; i++)
            {
                newRows[i] = rows[i];
            }

            newRows[newRows.Length - 1]++;
            return true;
        }

        /// <summary>Whether <see cref="TryAppendTube"/> would succeed for these rows.</summary>
        public static bool CanAppendTube(IReadOnlyList<int> rows)
        {
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            return rows.Count > 0 && (rows[rows.Count - 1] < MaxTubesPerRow || rows.Count < MaxRows);
        }

        /// <summary>
        /// The rows to display: the explicit rows if given, otherwise <see cref="AutoRows"/>.
        /// </summary>
        /// <remarks>Assumes the level has passed <see cref="LevelValidator"/>.</remarks>
        public static int[] Resolve(int tubeCount, IReadOnlyList<int> explicitRows)
        {
            if (explicitRows == null)
            {
                return AutoRows(tubeCount);
            }

            var rows = new int[explicitRows.Count];
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = explicitRows[i];
            }

            return rows;
        }
    }
}
