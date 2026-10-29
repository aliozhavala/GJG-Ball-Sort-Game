using System;
using System.Collections.Generic;
using BallSort.Core.Solver;

namespace BallSort.Editor.Validation
{
    /// <summary>
    /// One row of the Validate Levels table: one variant's validation errors and solver results, or,
    /// with <see cref="Variant"/> 0, problems of the level file as a whole.
    /// </summary>
    public sealed class LevelReport
    {
        private static readonly IReadOnlyList<string> NoErrors = Array.AsReadOnly(new string[0]);

        public LevelReport(
            LevelSource source,
            int variant,
            bool fileIsValid,
            int colors,
            int tubes,
            string mechanics,
            IReadOnlyList<string> errors,
            SolverResult fast,
            SolverResult optimal)
        {
            Source = source;
            Variant = variant;
            FileIsValid = fileIsValid;
            Colors = colors;
            Tubes = tubes;
            Mechanics = mechanics;
            Errors = errors ?? NoErrors;
            Fast = fast;
            Optimal = optimal;
        }

        /// <summary>A row for problems of the file as a whole (unreadable, or broken outside any variant).</summary>
        public static LevelReport ForFile(LevelSource source, IReadOnlyList<string> errors) =>
            new LevelReport(source, 0, false, 0, 0, string.Empty, errors, null, null);

        public LevelSource Source { get; }

        /// <summary>The variant, 1-based; 0 for a row about the whole file.</summary>
        public int Variant { get; }

        /// <summary>True when the whole level file passed validation, so it can be played.</summary>
        public bool FileIsValid { get; }

        public int Colors { get; }

        public int Tubes { get; }

        /// <summary>The mechanics found in the variant's data, comma-separated.</summary>
        public string Mechanics { get; }

        /// <summary>This row's parse and validation errors; empty for a valid variant.</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>The fast solve; null when the variant is invalid.</summary>
        public SolverResult Fast { get; }

        /// <summary>The optimal solve; null when the fast solve found no solution.</summary>
        public SolverResult Optimal { get; }

        public bool IsValid => Errors.Count == 0;

        public string VariantText => Variant == 0 ? "file" : $"V{Variant}";

        /// <summary>The proven shortest solution length, when the optimal solve finished.</summary>
        public int? OptimalMoves => Optimal != null && Optimal.IsOptimal ? Optimal.Moves.Count : (int?)null;

        public string SolvableText =>
            !IsValid ? "invalid"
            : Fast == null ? "–"
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
