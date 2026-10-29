using System;
using System.Collections.Generic;
using BallSort.Core.Model;
using BallSort.Core.Rules;

namespace BallSort.Core.Levels
{
    /// <summary>
    /// Checks for a parsed level file: the file itself (format, id, tier, layout, five variants), then
    /// each variant (tubes, colour counts, grid, mechanics, opening position: nothing already completed
    /// or won, at least one legal move), then that the variants share one board structure. Solvability
    /// needs the solver and runs at editor time. The rules are listed in <c>docs/mechanics.md</c> and
    /// <c>docs/levels.md</c>.
    /// </summary>
    public static class LevelValidator
    {
        /// <summary>The only <see cref="LevelDto.FormatVersion"/> this build can read.</summary>
        public const int SupportedFormatVersion = 2;

        private const string MissingLayout = "layout is missing; every level lists its tubes in layout.grid.";

        /// <summary>
        /// Why a file of <paramref name="formatVersion"/> cannot be read, or null when it can. The parser
        /// checks this before reading anything else, since the rest of the schema depends on the version.
        /// </summary>
        public static string DescribeUnsupportedFormatVersion(int formatVersion)
        {
            if (formatVersion == SupportedFormatVersion)
            {
                return null;
            }

            return formatVersion == 0 ? $"formatVersion is missing (expected {SupportedFormatVersion})."
                : formatVersion == 1 ? $"formatVersion 1 is no longer supported: convert the file to version {SupportedFormatVersion} (one grid, {VariantSelector.VariantCount} variants; see docs/level-format-guide.md)."
                : $"formatVersion {formatVersion} is not supported (expected {SupportedFormatVersion}).";
        }

        /// <summary>
        /// Validates <paramref name="level"/> and returns every problem found.
        /// </summary>
        /// <param name="level">The parsed level; may be null (reported as an error).</param>
        /// <param name="levelName">Display name used to prefix every message, e.g. <c>level_003</c>.</param>
        /// <returns>An empty list when the level is valid. Problems inside a variant read
        /// <c>level_003: variant 2: tubes[4]: …</c>, variants numbered from 1.</returns>
        public static IReadOnlyList<string> Validate(LevelDto level, string levelName)
        {
            var errors = new List<string>();
            if (level == null)
            {
                errors.Add("file is empty or not a JSON object.");
                return Prefix(errors, levelName);
            }

            // Nothing else means anything in a file of another version.
            string versionError = DescribeUnsupportedFormatVersion(level.FormatVersion);
            if (versionError != null)
            {
                errors.Add(versionError);
                return Prefix(errors, levelName);
            }

            if (level.Id < 1)
            {
                errors.Add($"id must be 1 or greater (was {level.Id}).");
            }

            if (level.Tier == null)
            {
                errors.Add("tier is missing (expected regular, hard or superHard).");
            }
            else if (!Enum.IsDefined(typeof(LevelTier), level.Tier.Value))
            {
                errors.Add($"tier {(int)level.Tier.Value} is not a tier (expected regular, hard or superHard).");
            }

            if (level.Variants == null || level.Variants.Count == 0)
            {
                errors.Add($"variants is missing or empty; a level has exactly {VariantSelector.VariantCount}.");
                if (level.Layout == null)
                {
                    errors.Add(MissingLayout);
                }

                return Prefix(errors, levelName);
            }

            if (level.Variants.Count != VariantSelector.VariantCount)
            {
                errors.Add($"variants has {level.Variants.Count} entries; a level has exactly {VariantSelector.VariantCount}.");
            }

            // The grid is checked once, against V1's tubes; the consistency check holds every other
            // variant to V1's tube count and capacities, so the grid fits them all.
            BoardGrid grid = ValidateLayout(level.Layout, level.Variants[0]?.Tubes, errors);

            for (int v = 0; v < level.Variants.Count; v++)
            {
                string where = $"variant {v + 1}";
                LevelVariantDto variant = level.Variants[v];
                if (variant == null)
                {
                    errors.Add($"{where} is null.");
                    continue;
                }

                var variantErrors = new List<string>();
                ValidateVariantContent(variant, grid, variantErrors);
                foreach (string error in variantErrors)
                {
                    errors.Add($"{where}: {error}");
                }
            }

            VariantConsistencyValidator.Validate(level.Variants, errors);
            return Prefix(errors, levelName);
        }

        /// <summary>
        /// Checks one variant on a layout against every rule <see cref="Validate"/> applies to each
        /// variant, the grid included: one puzzle on its own. The file-level rules (version, id, tier,
        /// variant count) and the consistency between variants are not part of it.
        /// </summary>
        /// <param name="layout">The grid the variant is laid out on.</param>
        /// <param name="variant">The variant; may be null (reported as an error).</param>
        /// <param name="levelName">Display name used to prefix every message.</param>
        /// <returns>An empty list when the variant is valid on <paramref name="layout"/>.</returns>
        public static IReadOnlyList<string> ValidateVariant(LayoutDto layout, LevelVariantDto variant, string levelName)
        {
            var errors = new List<string>();
            if (variant == null)
            {
                errors.Add("variant is null.");
                return Prefix(errors, levelName);
            }

            BoardGrid grid = ValidateLayout(layout, variant.Tubes, errors);
            ValidateVariantContent(variant, grid, errors);
            return Prefix(errors, levelName);
        }

        /// <param name="grid">The resolved grid, or null when it is invalid (lock adjacency is then not checked).</param>
        /// <param name="errors">Receives this variant's problems, unprefixed.</param>
        private static void ValidateVariantContent(LevelVariantDto variant, BoardGrid grid, List<string> errors)
        {
            int errorsBefore = errors.Count;
            if (variant.Colors < 1)
            {
                errors.Add($"colors must be 1 or greater (was {variant.Colors}).");
            }

            if (variant.Tubes == null || variant.Tubes.Count == 0)
            {
                errors.Add("tubes is missing or empty.");
                return;
            }

            var ballsPerColor = new int[variant.Colors > 0 ? variant.Colors : 0];
            for (int i = 0; i < variant.Tubes.Count; i++)
            {
                ValidateTube(variant.Tubes[i], i, variant.Colors, ballsPerColor, errors);
            }

            if (variant.Colors > 0)
            {
                ValidateBallCounts(variant.Tubes, ballsPerColor, errors);
            }

            MechanicsValidator.Validate(variant, grid, errors);

            // The playability checks build a board, which is only safe on structurally valid data. The
            // board does not depend on the grid, so only this variant's own problems hold them back.
            if (errors.Count == errorsBefore)
            {
                ValidateStartState(variant, errors);
            }
        }

        /// <summary>
        /// The opening position must be a real puzzle: nothing already finished, and something to do.
        /// </summary>
        private static void ValidateStartState(LevelVariantDto variant, List<string> errors)
        {
            BoardState board = LevelFactory.CreateBoard(variant);
            if (board.IsWon)
            {
                errors.Add("is already won at start.");
                return;
            }

            // Covered tubes too: their contents never change while covered, so a finished set behind a
            // cover would complete the moment it opened, which removing a cover must never do.
            for (int i = 0; i < board.TubeCount; i++)
            {
                if (board.IsCompleted(i))
                {
                    errors.Add(board[i].IsCovered
                        ? $"tubes[{i}] already holds a finished set behind its cover."
                        : $"tubes[{i}] is already completed at start.");
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

            if (!LevelLayout.IsValidCapacity(tube.Capacity))
            {
                errors.Add(
                    $"{where}: capacity must be {LevelLayout.NormalCapacity}, or {LevelLayout.NormalCapacity + 1} to " +
                    $"{LevelLayout.MaxCapacity} for a large tube (was {tube.Capacity}).");
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
        /// tube of capacity exactly <c>k</c>, and no two colours can share one. Per capacity value:
        /// colours with that many balls ≤ tubes with that capacity.
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

        /// <param name="tubes">The tubes the grid lays out; when null or empty the grid is not checked (the tubes are reported instead).</param>
        /// <returns>The resolved grid, or null when the grid is invalid or could not be checked.</returns>
        private static BoardGrid ValidateLayout(LayoutDto layout, List<TubeDto> tubes, List<string> errors)
        {
            if (layout == null)
            {
                errors.Add(MissingLayout);
                return null;
            }

            if (tubes == null || tubes.Count == 0)
            {
                return null;
            }

            var capacities = new int[tubes.Count];
            for (int i = 0; i < capacities.Length; i++)
            {
                capacities[i] = tubes[i]?.Capacity ?? 0;
            }

            return LevelLayout.ValidateGrid(layout.Grid, capacities, errors)
                ? new BoardGrid(layout.Grid, tubes.Count)
                : null;
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
