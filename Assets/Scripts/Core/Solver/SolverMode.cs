namespace BallSort.Core.Solver
{
    /// <summary>How <see cref="BoardSolver"/> searches.</summary>
    public enum SolverMode
    {
        /// <summary>A* with an admissible heuristic: a solution it returns is a shortest one.</summary>
        Optimal,

        /// <summary>
        /// Weighted A*: leans on the heuristic to find some solution quickly. Its solutions are
        /// usually short but not guaranteed shortest. Unsolvable is still exact.
        /// </summary>
        Fast,
    }
}
