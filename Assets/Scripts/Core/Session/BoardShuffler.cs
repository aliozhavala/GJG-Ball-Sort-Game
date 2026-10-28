using System;
using System.Collections.Generic;
using System.Threading;
using BallSort.Core.Model;
using BallSort.Core.Solver;

namespace BallSort.Core.Session
{
    /// <summary>
    /// Finds a shuffled arrangement of a board that follows the Shuffle rules in docs/mechanics.md.
    /// </summary>
    /// <remarks>
    /// Only open tubes take part — not covered, not clay, not completed — and each keeps its ball
    /// count; balls still carrying a key keep their slot. Every ball keeps its hidden flag except
    /// that a ball landing on top of a tube is revealed. A candidate is accepted when it differs in
    /// colour from the current arrangement in at least one slot, completes no tube, leaves no key
    /// findable, and the fast solver proves it solvable within <see cref="NodeLimit"/>.
    /// </remarks>
    internal static class BoardShuffler
    {
        /// <summary>Random arrangements tried before giving up.</summary>
        public const int MaxAttempts = 20;

        /// <summary>
        /// Solver node limit per attempt; hitting it counts as unsolvable. Solvable shuffled boards
        /// have needed at most ~130 nodes, so this leaves wide headroom while keeping the worst case
        /// (every attempt failing at the limit) near one second in the Editor on the largest grid.
        /// </summary>
        public const int NodeLimit = 2000;

        /// <summary>
        /// Searches for an arrangement of <paramref name="board"/>, which is read but not changed.
        /// </summary>
        /// <returns>A copy of the board holding the new arrangement, or null when none was found.</returns>
        public static BoardState Find(BoardState board, Random random, CancellationToken cancellationToken)
        {
            var slots = new List<(int Tube, int Slot)>();
            var balls = new List<Ball>();
            for (int t = 0; t < board.TubeCount; t++)
            {
                Tube tube = board[t];
                if (tube.IsCovered || tube.IsClay || board.IsCompleted(t))
                {
                    continue;
                }

                for (int slot = 0; slot < tube.Count; slot++)
                {
                    if (!tube[slot].HasKey)
                    {
                        slots.Add((t, slot));
                        balls.Add(tube[slot]);
                    }
                }
            }

            if (balls.Count < 2)
            {
                return null;
            }

            Ball[] original = balls.ToArray();
            Ball[] order = balls.ToArray();
            BoardState candidate = board.Clone();

            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Shuffle(order, random);
                candidate.CopyStateFrom(board);

                bool differs = false;
                for (int i = 0; i < slots.Count; i++)
                {
                    (int tubeIndex, int slot) = slots[i];
                    Tube tube = candidate[tubeIndex];
                    Ball ball = slot == tube.Count - 1 ? order[i].AsRevealed() : order[i];
                    tube.SetBall(slot, ball);
                    differs |= ball.ColorId != original[i].ColorId;
                }

                if (differs && IsAcceptable(candidate, slots) &&
                    BoardSolver.Solve(candidate, SolverMode.Fast, NodeLimit, cancellationToken).Outcome == SolverOutcome.Solved)
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>No shuffled tube is completed or shows a hidden top, and no key is findable.</summary>
        private static bool IsAcceptable(BoardState candidate, List<(int Tube, int Slot)> slots)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                int tubeIndex = slots[i].Tube;
                Tube tube = candidate[tubeIndex];
                if (candidate.IsCompleted(tubeIndex) || tube.Top.IsHidden)
                {
                    return false;
                }
            }

            return !candidate.HasFindableKey;
        }

        /// <summary>Fisher–Yates, in place.</summary>
        private static void Shuffle(Ball[] balls, Random random)
        {
            for (int i = balls.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (balls[i], balls[j]) = (balls[j], balls[i]);
            }
        }
    }
}
