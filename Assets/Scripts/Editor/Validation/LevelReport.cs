using System;
using System.Collections.Generic;
using BallSort.Core.Solver;

namespace BallSort.Editor.Validation
{
    /// <summary>One row of the Validate Levels table: a level's validation errors and solver results.</summary>
    public sealed class LevelReport
    {
        private static readonly IReadOnlyList<string> NoErrors = Array.AsReadOnly(new string[0]);

        public LevelReport(
            LevelSource source,
            int colors,
            int tubes,
            string mechanics,
            IReadOnlyList<string> errors,
            SolverResult fast,
            SolverResult optimal)
        {
            Source = source;
            Colors = colors;
            Tubes = tubes;
            Mechanics = mechanics;
            Errors = errors ?? NoErrors;
            Fast = fast;
            Optimal = optimal;
        }

        public LevelSource Source { get; }

        public int Colors { get; }

        public int Tubes { get; }

        /// <summary>The mechanics found in the level data (not in <c>meta</c>), comma-separated.</summary>
        public string Mechanics { get; }

        /// <summary>Parse and validation errors; empty for a valid level.</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>The fast solve; null when the level is invalid.</summary>
        public SolverResult Fast { get; }

        /// <summary>The optimal solve; null when the fast solve found no solution.</summary>
        public SolverResult Optimal { get; }

        public bool IsValid => Errors.Count == 0;

        /// <summary>The proven shortest solution length, when the optimal solve finished.</summary>
        public int? OptimalMoves => Optimal != null && Optimal.IsOptimal ? Optimal.Moves.Count : (int?)null;

        public string SolvableText =>
            !IsValid ? "invalid"
            : Fast.Outcome == SolverOutcome.Solved ? "yes"
            : Fast.Outcome == SolverOutcome.Unsolvable ? "NO"
            : "unknown";

        /// <summary>The optimum, "≥ n" when the optimal solve hit its limit, "–" when it did not run.</summary>
        public string OptimalText =>
            OptimalMoves.HasValue ? OptimalMoves.Value.ToString()
            : Optimal != null && Optimal.Outcome == SolverOutcome.Unknown ? $"≥ {Optimal.LowerBound}"
            : "–";

        public string NodesText =>
            Fast == null ? "–" : Optimal == null ? $"{Fast.NodesExpanded:N0}" : $"{Fast.NodesExpanded:N0} / {Optimal.NodesExpanded:N0}";

        public string TimeText =>
            Fast == null ? "–"
            : Optimal == null ? $"{Fast.Elapsed.TotalMilliseconds:0} ms"
            : $"{Fast.Elapsed.TotalMilliseconds:0} / {Optimal.Elapsed.TotalMilliseconds:0} ms";
    }
}
