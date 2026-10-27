using System.Collections.Generic;
using BallSort.Core.Model;
using BallSort.Core.Rules;

namespace BallSort.Core.Levels
{
    /// <summary>
    /// Checks for a parsed level: structure first, then the opening position (nothing already
    /// completed or won, at least one legal move). Solvability needs the solver and runs at editor time.
    /// </summary>
    public static class LevelValidator
    {
        /// <summary>The only <see cref="LevelDto.FormatVersion"/> this build can read.</summary>
        public const int SupportedFormatVersion = 1;

        /// <summary>
        /// Validates <paramref name="level"/> and returns every problem found.
        /// </summary>
        /// <param name="level">The parsed level; may be null (reported as an error).</param>
        /// <param name="levelName">Display name used to prefix every message, e.g. <c>level_003</c>.</param>
        /// <returns>An empty list when the level is valid.</returns>
        public static IReadOnlyList<string> Validate(LevelDto level, string levelName)
        {
            var errors = new List<string>();
            if (level == null)
            {
                errors.Add("file is empty or not a JSON object.");
                return Prefix(errors, levelName);
            }

            if (level.FormatVersion != SupportedFormatVersion)
            {
                errors.Add(level.FormatVersion == 0
                    ? $"formatVersion is missing (expected {SupportedFormatVersion})."
                    : $"formatVersion {level.FormatVersion} is not supported (expected {SupportedFormatVersion}).");
            }

            if (level.Id < 1)
            {
                errors.Add($"id must be 1 or greater (was {level.Id}).");
            }

            if (level.Colors < 1)
            {
                errors.Add($"colors must be 1 or greater (was {level.Colors}).");
            }

            if (level.Tubes == null || level.Tubes.Count == 0)
            {
                errors.Add("tubes is missing or empty.");
                return Prefix(errors, levelName);
            }

            var ballsPerColor = new int[level.Colors > 0 ? level.Colors : 0];
            for (int i = 0; i < level.Tubes.Count; i++)
            {
                ValidateTube(level.Tubes[i], i, level.Colors, ballsPerColor, errors);
            }

            if (level.Colors > 0)
            {
                ValidateBallCounts(level.Tubes, ballsPerColor, errors);
            }

            ValidateLayout(level, errors);

            // The playability checks build a board, which is only safe on structurally valid data.
            if (errors.Count == 0)
            {
                ValidateStartState(level, errors);
            }

            return Prefix(errors, levelName);
        }

        /// <summary>
        /// The opening position must be a real puzzle: nothing already finished, and something to do.
        /// </summary>
        private static void ValidateStartState(LevelDto level, List<string> errors)
        {
            BoardState board = LevelFactory.CreateBoard(level);
            if (board.IsWon)
            {
                errors.Add("is already won at start.");
                return;
            }

            for (int i = 0; i < board.TubeCount; i++)
            {
                if (board[i].IsCompleted)
                {
                    errors.Add($"tubes[{i}] is already completed at start.");
                }
            }

            if (!MoveRules.HasAnyLegalMove(board))
            {
                errors.Add("has no legal move at start.");
            }
        }

        private static void ValidateTube(
            TubeDto tube, int index, int colors, int[] ballsPerColor, List<string> errors)
        {
            string where = $"tubes[{index}]";
            if (tube == null)
            {
                errors.Add($"{where} is null.");
                return;
            }

            if (tube.Capacity < 1)
            {
                errors.Add($"{where}: capacity must be 1 or greater (was {tube.Capacity}).");
            }

            if (tube.Balls == null)
            {
                errors.Add($"{where}: balls is missing (use [] for an empty tube).");
                return;
            }

            if (tube.Capacity >= 1 && tube.Balls.Count > tube.Capacity)
            {
                errors.Add($"{where}: holds {tube.Balls.Count} balls but capacity is {tube.Capacity}.");
            }

            for (int slot = 0; slot < tube.Balls.Count; slot++)
            {
                int color = tube.Balls[slot];
                if (color < 0 || color >= colors)
                {
                    errors.Add($"{where}: balls[{slot}] has colour {color}; expected 0 to {colors - 1}.");
                }
                else
                {
                    ballsPerColor[color]++;
                }
            }

            if (tube.Hidden != null)
            {
                if (tube.Hidden.Count != tube.Balls.Count)
                {
                    errors.Add($"{where}: hidden has {tube.Hidden.Count} entries but balls has {tube.Balls.Count}.");
                }
                else if (tube.Balls.Count > 0 && tube.Hidden[tube.Balls.Count - 1])
                {
                    errors.Add($"{where}: top ball is hidden; every tube's top ball must start revealed.");
                }
            }
        }

        /// <summary>
        /// Every colour must be able to complete a tube: a colour with <c>k</c> balls needs its own
        /// tube of capacity exactly <c>k</c>, and no two colours can share one.
        /// </summary>
        private static void ValidateBallCounts(List<TubeDto> tubes, int[] ballsPerColor, List<string> errors)
        {
            var tubesPerCapacity = new Dictionary<int, int>();
            foreach (TubeDto tube in tubes)
            {
                if (tube != null && tube.Capacity >= 1)
                {
                    tubesPerCapacity.TryGetValue(tube.Capacity, out int n);
                    tubesPerCapacity[tube.Capacity] = n + 1;
                }
            }

            // Sorted so messages come out in a stable order.
            var colorsPerCount = new SortedDictionary<int, List<int>>();
            for (int color = 0; color < ballsPerColor.Length; color++)
            {
                int count = ballsPerColor[color];
                if (count == 0)
                {
                    errors.Add($"colour {color} has no balls, but colors is {ballsPerColor.Length}.");
                    continue;
                }

                if (!colorsPerCount.TryGetValue(count, out List<int> list))
                {
                    list = new List<int>();
                    colorsPerCount.Add(count, list);
                }

                list.Add(color);
            }

            foreach (KeyValuePair<int, List<int>> entry in colorsPerCount)
            {
                tubesPerCapacity.TryGetValue(entry.Key, out int available);
                if (entry.Value.Count > available)
                {
                    errors.Add(
                        $"colour(s) {string.Join(", ", entry.Value)} have {entry.Key} balls each, which needs " +
                        $"{entry.Value.Count} tube(s) of capacity {entry.Key}; the level has {available}.");
                }
            }
        }

        private static void ValidateLayout(LevelDto level, List<string> errors)
        {
            if (level.Layout == null)
            {
                if (level.Tubes.Count > LevelLayout.MaxTubes)
                {
                    errors.Add($"has {level.Tubes.Count} tubes; at most {LevelLayout.MaxTubes} fit on screen.");
                }

                return;
            }

            if (level.Layout.Rows == null)
            {
                errors.Add("layout.rows is missing (omit layout entirely for automatic rows).");
                return;
            }

            LevelLayout.ValidateRows(level.Layout.Rows, level.Tubes.Count, errors);
        }

        private static IReadOnlyList<string> Prefix(List<string> errors, string levelName)
        {
            for (int i = 0; i < errors.Count; i++)
            {
                errors[i] = $"{levelName}: {errors[i]}";
            }

            return errors;
        }
    }
}
