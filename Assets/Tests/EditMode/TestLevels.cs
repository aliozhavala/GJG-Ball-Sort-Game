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

        public static LevelDto Level(int colors, params TubeDto[] tubes) =>
            new LevelDto
            {
                FormatVersion = LevelValidator.SupportedFormatVersion,
                Id = 1,
                Colors = colors,
                Tubes = new List<TubeDto>(tubes),
            };

        /// <summary>Two colours, two full mixed tubes, one empty tube: valid and solvable.</summary>
        public static LevelDto Valid() =>
            Level(2, T(4, 0, 1, 0, 1), T(4, 1, 0, 1, 0), T(4));
    }
}
