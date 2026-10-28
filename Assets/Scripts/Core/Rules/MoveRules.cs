using System;
using BallSort.Core.Model;

namespace BallSort.Core.Rules
{
    /// <summary>
    /// The single source of truth for move legality. Pure and stateless: the game, the solver and
    /// the level validator all ask these same methods, so they can never disagree.
    /// </summary>
    public static class MoveRules
    {
        /// <summary>
        /// Resolves a tap-source/tap-target pair into a concrete move, or explains the refusal.
        /// </summary>
        /// <param name="board">The board to evaluate against. Not modified.</param>
        /// <param name="fromIndex">Index of the tube to take from.</param>
        /// <param name="toIndex">Index of the tube to place into.</param>
        /// <param name="move">The resolved move when this returns true; <c>default</c> otherwise.</param>
        /// <param name="rejection">Why the move was refused when this returns false; <see cref="MoveRejection.None"/> otherwise.</param>
        /// <returns>True when the move is legal.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="board"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If either index is not a valid tube index.</exception>
        public static bool TryGetMove(
            BoardState board, int fromIndex, int toIndex, out MoveInfo move, out MoveRejection rejection)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            // Out-of-range indices are a programming error, not a player action, so they throw
            // rather than becoming a rejection the UI would have to render.
            if (!board.IsValidTubeIndex(fromIndex))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fromIndex), fromIndex, $"Board has {board.TubeCount} tube(s).");
            }

            if (!board.IsValidTubeIndex(toIndex))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(toIndex), toIndex, $"Board has {board.TubeCount} tube(s).");
            }

            move = default;

            if (fromIndex == toIndex)
            {
                rejection = MoveRejection.SameTube;
                return false;
            }

            Tube source = board[fromIndex];
            Tube target = board[toIndex];

            // Covers and clay first: they are what the player sees, whatever lies underneath.
            if (source.IsCovered)
            {
                rejection = MoveRejection.SourceCovered;
                return false;
            }

            if (source.IsClay)
            {
                rejection = MoveRejection.SourceClay;
                return false;
            }

            if (source.IsEmpty)
            {
                rejection = MoveRejection.SourceEmpty;
                return false;
            }

            if (source.Top.IsHidden)
            {
                rejection = MoveRejection.SourceTopHidden;
                return false;
            }

            if (board.IsCompleted(fromIndex))
            {
                rejection = MoveRejection.SourceCompleted;
                return false;
            }

            if (target.IsCovered)
            {
                rejection = MoveRejection.TargetCovered;
                return false;
            }

            if (target.FreeSpace == 0)
            {
                rejection = MoveRejection.TargetFull;
                return false;
            }

            source.TryGetRun(out int colorId, out int runLength);

            if (!target.IsEmpty)
            {
                // Require the target's top to be visible, not merely colour-matched: the player must
                // be able to see what they are stacking onto. Equivalent in practice, since balls
                // arriving on a tube are always revealed, but it keeps corrupt data from producing a
                // move that would look wrong on screen.
                if (target.Top.IsHidden)
                {
                    rejection = MoveRejection.TargetTopHidden;
                    return false;
                }

                if (target.Top.ColorId != colorId)
                {
                    rejection = MoveRejection.ColorMismatch;
                    return false;
                }
            }

            // Water-sort style: take as much of the run as fits and leave the rest behind.
            int count = Math.Min(runLength, target.FreeSpace);
            move = new MoveInfo(fromIndex, toIndex, count, colorId, count < runLength);
            rejection = MoveRejection.None;
            return true;
        }

        /// <summary>Convenience overload for callers that do not need the reason.</summary>
        public static bool TryGetMove(BoardState board, int fromIndex, int toIndex, out MoveInfo move) =>
            TryGetMove(board, fromIndex, toIndex, out move, out _);

        /// <summary>Whether this move is legal, discarding the details.</summary>
        public static bool IsLegal(BoardState board, int fromIndex, int toIndex) =>
            TryGetMove(board, fromIndex, toIndex, out _, out _);

        /// <summary>
        /// Whether the given tube can currently start a move at all, ignoring any particular target.
        /// Use this to decide whether a tap should select a tube.
        /// </summary>
        public static bool CanSelect(BoardState board, int tubeIndex)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            Tube tube = board[tubeIndex];
            return !tube.IsEmpty && tube.Top.IsRevealed && !tube.IsCovered && !tube.IsClay &&
                   !board.IsCompleted(tubeIndex);
        }

        /// <summary>
        /// Whether any legal move exists on this board — false means a dead end. Used by the
        /// generator's difficulty scoring and by stuck detection.
        /// </summary>
        public static bool HasAnyLegalMove(BoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            for (int from = 0; from < board.TubeCount; from++)
            {
                if (!CanSelect(board, from))
                {
                    continue;
                }

                for (int to = 0; to < board.TubeCount; to++)
                {
                    if (from != to && TryGetMove(board, from, to, out _, out _))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
