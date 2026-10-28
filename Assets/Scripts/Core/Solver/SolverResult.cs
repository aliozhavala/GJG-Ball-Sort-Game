using System;
using System.Collections.Generic;
using BallSort.Core.Rules;

namespace BallSort.Core.Solver
{
    /// <summary>The outcome of one <see cref="BoardSolver.Solve"/> call, with its search statistics.</summary>
    public sealed class SolverResult
    {
        private static readonly IReadOnlyList<MoveInfo> NoMoves = Array.AsReadOnly(new MoveInfo[0]);

        public SolverOutcome Outcome { get; }

        /// <summary>
        /// The winning moves in play order, in the board's own tube indices; empty unless
        /// <see cref="Outcome"/> is <see cref="SolverOutcome.Solved"/>.
        /// </summary>
        public IReadOnlyList<MoveInfo> Moves { get; }

        /// <summary>True when <see cref="Moves"/> is proven to be a shortest solution (Optimal mode only).</summary>
        public bool IsOptimal { get; }

        /// <summary>
        /// A proven lower bound on the length of any solution: the solution length when optimal, the
        /// best bound the search reached when it stopped at the node limit, and -1 when unsolvable.
        /// </summary>
        public int LowerBound { get; }

        /// <summary>Positions whose moves were generated. The node limit counts these.</summary>
        public int NodesExpanded { get; }

        /// <summary>The most positions waiting in the search frontier at once; a proxy for memory use.</summary>
        public int MaxFrontier { get; }

        public TimeSpan Elapsed { get; }

        internal SolverResult(
            SolverOutcome outcome,
            IReadOnlyList<MoveInfo> moves,
            bool isOptimal,
            int lowerBound,
            int nodesExpanded,
            int maxFrontier,
            TimeSpan elapsed)
        {
            Outcome = outcome;
            Moves = moves ?? NoMoves;
            IsOptimal = isOptimal;
            LowerBound = lowerBound;
            NodesExpanded = nodesExpanded;
            MaxFrontier = maxFrontier;
            Elapsed = elapsed;
        }

        public override string ToString() =>
            $"{Outcome}{(Outcome == SolverOutcome.Solved ? $" in {Moves.Count}{(IsOptimal ? " (optimal)" : string.Empty)}" : string.Empty)}" +
            $", {NodesExpanded} nodes, frontier {MaxFrontier}, {Elapsed.TotalMilliseconds:0} ms";
    }
}
