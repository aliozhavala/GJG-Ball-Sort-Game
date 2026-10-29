using System.Collections.Generic;
using System.Threading;
using BallSort.Core.Levels;
using BallSort.Core.Model;
using BallSort.Core.Solver;
using BallSort.Game.Services;
using BallSort.LevelJson;
using UnityEditor;
using UnityEngine;

namespace BallSort.Editor.Validation
{
    /// <summary>
    /// The work behind Validate Levels: for each level, the loader's validation, then for each
    /// variant the fast solver, then the optimal solver within its node limit (docs/levels.md).
    /// </summary>
    /// <remarks>
    /// <see cref="ReadSources"/> touches Unity assets and must run on the main thread;
    /// <see cref="Check"/> is pure and may run on a worker thread.
    /// </remarks>
    public static class LevelValidationRunner
    {
        public const int DefaultFastNodeLimit = 200000;

        /// <summary>Roughly a few hundred MB of search state at most on the largest boards.</summary>
        public const int DefaultOptimalNodeLimit = 500000;

        /// <summary>Reads every level of <paramref name="database"/>, in order. Main thread only.</summary>
        public static List<LevelSource> ReadSources(LevelDatabase database)
        {
            var sources = new List<LevelSource>(database.Count);
            for (int number = 1; number <= database.Count; number++)
            {
                TextAsset asset = database.GetLevel(number);
                sources.Add(asset == null
                    ? new LevelSource(number, $"level {number}", null, null)
                    : new LevelSource(number, asset.name, AssetDatabase.GetAssetPath(asset), asset.text));
            }

            return sources;
        }

        /// <summary>
        /// Validates one level file and solves each of its variants: one report per variant, plus one
        /// for the file when it has problems outside any variant. A variant with no problems of its own
        /// is solved even when another part of the file is broken. Safe on any thread.
        /// </summary>
        /// <exception cref="System.OperationCanceledException">If <paramref name="cancellationToken"/> is cancelled.</exception>
        public static List<LevelReport> Check(
            LevelSource source, int fastNodeLimit, int optimalNodeLimit, CancellationToken cancellationToken)
        {
            var reports = new List<LevelReport>();
            if (source.Json == null)
            {
                reports.Add(LevelReport.ForFile(source, new[] { "The database slot is empty." }));
                return reports;
            }

            LevelDto level;
            try
            {
                level = LevelJsonParser.Parse(source.Json, source.Name);
            }
            catch (LevelFormatException e)
            {
                reports.Add(LevelReport.ForFile(source, e.Errors));
                return reports;
            }

            IReadOnlyList<string> errors = LevelValidator.Validate(level, source.Name);
            List<string>[] variantErrors = SplitByVariant(errors, source.Name, level?.Variants?.Count ?? 0, out List<string> fileErrors);
            if (fileErrors.Count > 0)
            {
                reports.Add(LevelReport.ForFile(source, fileErrors));
            }

            bool fileIsValid = errors.Count == 0;
            for (int v = 1; v <= variantErrors.Length; v++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                LevelVariantDto variant = level.Variants[v - 1];
                int colors = variant?.Colors ?? 0;
                int tubes = variant?.Tubes?.Count ?? 0;
                if (variant == null || variantErrors[v - 1].Count > 0)
                {
                    reports.Add(new LevelReport(source, v, fileIsValid, colors, tubes, string.Empty, variantErrors[v - 1], null, null));
                    continue;
                }

                BoardState board = LevelFactory.CreateBoard(variant);
                SolverResult fast = BoardSolver.Solve(board, SolverMode.Fast, fastNodeLimit, cancellationToken);
                SolverResult optimal = fast.Outcome == SolverOutcome.Solved
                    ? BoardSolver.Solve(board, SolverMode.Optimal, optimalNodeLimit, cancellationToken)
                    : null;

                reports.Add(new LevelReport(source, v, fileIsValid, colors, tubes, DescribeMechanics(variant), null, fast, optimal));
            }

            return reports;
        }

        /// <summary>Sorts validation messages into those of each variant (by their "variant k:" prefix) and the rest.</summary>
        private static List<string>[] SplitByVariant(
            IReadOnlyList<string> errors, string levelName, int variantCount, out List<string> fileErrors)
        {
            var byVariant = new List<string>[variantCount];
            for (int v = 0; v < variantCount; v++)
            {
                byVariant[v] = new List<string>();
            }

            fileErrors = new List<string>();
            foreach (string error in errors)
            {
                int variant = 0;
                for (int v = 1; v <= variantCount && variant == 0; v++)
                {
                    if (error.StartsWith($"{levelName}: variant {v}:", System.StringComparison.Ordinal) ||
                        error == $"{levelName}: variant {v} is null.")
                    {
                        variant = v;
                    }
                }

                (variant == 0 ? fileErrors : byVariant[variant - 1]).Add(error);
            }

            return byVariant;
        }

        /// <summary>The mechanics a variant's data actually uses: hidden, veil, lock, clay, large.</summary>
        public static string DescribeMechanics(LevelVariantDto variant)
        {
            bool hidden = false, veil = false, clay = false, large = false;
            foreach (TubeDto tube in variant.Tubes)
            {
                hidden |= tube.Hidden != null && tube.Hidden.Contains(true);
                veil |= tube.Veil != null;
                clay |= tube.Clay;
                large |= tube.Capacity > LevelLayout.NormalCapacity;
            }

            var names = new List<string>();
            if (hidden)
            {
                names.Add("hidden");
            }

            if (veil)
            {
                names.Add("veil");
            }

            if (variant.Locks != null && variant.Locks.Count > 0)
            {
                names.Add("lock");
            }

            if (clay)
            {
                names.Add("clay");
            }

            if (large)
            {
                names.Add("large");
            }

            return names.Count == 0 ? "–" : string.Join(", ", names);
        }
    }
}
