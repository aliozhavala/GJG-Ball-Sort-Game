using System.Collections.Generic;
using System.Threading;
using BallSort.Core.Levels;
using BallSort.Core.Solver;
using BallSort.Game.Services;
using BallSort.LevelJson;
using UnityEditor;
using UnityEngine;

namespace BallSort.Editor.Validation
{
    /// <summary>
    /// The work behind Validate Levels: for each level, the loader's validation, then the fast
    /// solver, then the optimal solver within its node limit (docs/levels.md).
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

        /// <summary>Validates and solves one level. Safe on any thread.</summary>
        /// <exception cref="System.OperationCanceledException">If <paramref name="cancellationToken"/> is cancelled.</exception>
        public static LevelReport Check(
            LevelSource source, int fastNodeLimit, int optimalNodeLimit, CancellationToken cancellationToken)
        {
            if (source.Json == null)
            {
                return new LevelReport(source, 0, 0, string.Empty, new[] { "The database slot is empty." }, null, null);
            }

            LevelDto dto = null;
            LoadedLevel level;
            try
            {
                dto = LevelJsonParser.Parse(source.Json, source.Name);
                level = LevelFactory.Load(dto, source.Name);
            }
            catch (LevelFormatException e)
            {
                return new LevelReport(
                    source, dto?.Colors ?? 0, dto?.Tubes?.Count ?? 0, string.Empty, e.Errors, null, null);
            }

            SolverResult fast = BoardSolver.Solve(level.Board, SolverMode.Fast, fastNodeLimit, cancellationToken);
            SolverResult optimal = fast.Outcome == SolverOutcome.Solved
                ? BoardSolver.Solve(level.Board, SolverMode.Optimal, optimalNodeLimit, cancellationToken)
                : null;

            return new LevelReport(source, dto.Colors, dto.Tubes.Count, DescribeMechanics(dto), null, fast, optimal);
        }

        /// <summary>The mechanics the level's data actually uses, in the <c>meta.mechanics</c> vocabulary.</summary>
        public static string DescribeMechanics(LevelDto level)
        {
            bool hidden = false, veil = false, clay = false, large = false;
            foreach (TubeDto tube in level.Tubes)
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

            if (level.Locks != null && level.Locks.Count > 0)
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
