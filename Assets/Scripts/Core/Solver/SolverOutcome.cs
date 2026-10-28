namespace BallSort.Core.Solver
{
    /// <summary>What a <see cref="BoardSolver"/> search concluded.</summary>
    public enum SolverOutcome
    {
        /// <summary>A sequence of moves that wins the level was found.</summary>
        Solved,

        /// <summary>Every reachable position was searched and none is won: the level cannot be won from here.</summary>
        Unsolvable,

        /// <summary>The node limit was reached first; the board may or may not be solvable.</summary>
        Unknown,
    }
}
