using System;
using System.Threading;
using BallSort.Core.Model;

namespace BallSort.Core.Solver
{
    /// <summary>
    /// Finds a winning sequence of moves for a board, or proves there is none, under the game's own
    /// rules (<c>MoveRules</c> and the board's reveal and cover cascade).
    /// </summary>
    /// <remarks>
    /// The solver has full knowledge: it sees the true colour of hidden balls, but still plays by
    /// the hidden flags (a hidden ball is never part of a run until it is revealed).
    /// <para>
    /// Pure and self-contained, so it may run on any thread: it copies the board once on entry and
    /// never touches it again. The caller must make sure nothing changes the board during that
    /// copy — when solving off the main thread, pass a <see cref="BoardState.Clone"/> made on it.
    /// </para>
    /// </remarks>
    public static class BoardSolver
    {
        /// <summary>Searches for a solution.</summary>
        /// <param name="board">The position to solve. Not modified.</param>
        /// <param name="mode">Shortest solution (<see cref="SolverMode.Optimal"/>) or any quickly (<see cref="SolverMode.Fast"/>).</param>
        /// <param name="nodeLimit">Most positions to expand before giving up with <see cref="SolverOutcome.Unknown"/>.</param>
        /// <param name="cancellationToken">Checked every few hundred expansions.</param>
        /// <exception cref="OperationCanceledException">If <paramref name="cancellationToken"/> is cancelled during the search.</exception>
        public static SolverResult Solve(
            BoardState board, SolverMode mode, int nodeLimit, CancellationToken cancellationToken = default)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (nodeLimit < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(nodeLimit), nodeLimit, "The node limit must be at least 1.");
            }

            return new SolverSearch(board, mode, nodeLimit, cancellationToken).Run();
        }
    }
}
