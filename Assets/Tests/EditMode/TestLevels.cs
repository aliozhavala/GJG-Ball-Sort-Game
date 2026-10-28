using System.Collections.Generic;
using BallSort.Core.Levels;

namespace BallSort.Tests.EditMode
{
    /// <summary>
    /// Builders for level DTOs. Each test starts from a valid level and breaks exactly one thing.
    /// </summary>
    internal static class TestLevels
    {
        public const string Name = "level_test";

        public static TubeDto T(int capacity, params int[] balls) =>
            new TubeDto { Capacity = capacity, Balls = new List<int>(balls) };

        public static TubeDto Hidden(int capacity, int[] balls, bool[] hidden) =>
            new TubeDto { Capacity = capacity, Balls = new List<int>(balls), Hidden = new List<bool>(hidden) };

        /// <summary>
        /// A level whose tubes fill the grid in order, at most six to a row, in the fewest rows
        /// (extra tubes on top). Use <see cref="WithGrid"/> for large tubes or a specific layout.
        /// </summary>
        public static LevelDto Level(int colors, params TubeDto[] tubes) =>
            new LevelDto
            {
                FormatVersion = LevelValidator.SupportedFormatVersion,
                Id = 1,
                Colors = colors,
                Layout = new LayoutDto { Grid = DefaultGrid(tubes.Length) },
                Tubes = new List<TubeDto>(tubes),
            };

        /// <summary>Two colours, two full mixed tubes, one empty tube: valid and solvable.</summary>
        public static LevelDto Valid() =>
            Level(2, T(4, 0, 1, 0, 1), T(4, 1, 0, 1, 0), T(4));

        /// <summary>
        /// The former placeholder level 6: valid, but tube 1 → 4 then 0 → 1 leaves it with no legal move.
        /// </summary>
        public static LevelDto StuckTrap() =>
            Level(4, T(4, 0, 1, 1, 2), T(4, 1, 2, 2, 0), T(4, 3, 3, 3, 2), T(4, 1, 0, 0, 3), T(4));

        // --- Mechanics -------------------------------------------------------------------------

        public static TubeDto WithVeil(this TubeDto tube, int? color)
        {
            tube.Veil = new VeilDto { Color = color };
            return tube;
        }

        public static TubeDto AsClay(this TubeDto tube)
        {
            tube.Clay = true;
            return tube;
        }

        public static TubeDto WithKey(this TubeDto tube, int? slot, int? lockId)
        {
            tube.Keys ??= new List<KeyDto>();
            tube.Keys.Add(new KeyDto { Slot = slot, Lock = lockId });
            return tube;
        }

        public static LevelDto WithLock(this LevelDto level, int? id, params int[] tubes)
        {
            level.Locks ??= new List<LockDto>();
            level.Locks.Add(new LockDto { Id = id, Tubes = new List<int>(tubes) });
            return level;
        }

        /// <summary>Replaces the grid: rows top first, <c>null</c> for a gap.</summary>
        public static LevelDto WithGrid(this LevelDto level, params int?[][] rows)
        {
            level.Layout = new LayoutDto { Grid = new List<List<int?>>() };
            foreach (int?[] row in rows)
            {
                level.Layout.Grid.Add(new List<int?>(row));
            }

            return level;
        }

        private static List<List<int?>> DefaultGrid(int tubeCount)
        {
            int rowCount = System.Math.Max(1, (tubeCount + LevelLayout.MaxCellsPerRow - 1) / LevelLayout.MaxCellsPerRow);
            var grid = new List<List<int?>>();
            int next = 0;
            for (int r = 0; r < rowCount; r++)
            {
                int size = tubeCount / rowCount + (r < tubeCount % rowCount ? 1 : 0);
                var row = new List<int?>();
                for (int i = 0; i < size; i++)
                {
                    row.Add(next++);
                }

                grid.Add(row);
            }

            return grid;
        }
    }
}
