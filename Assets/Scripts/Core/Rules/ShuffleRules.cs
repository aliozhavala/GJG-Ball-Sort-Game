using System;
using BallSort.Core.Model;

namespace BallSort.Core.Rules
{
    /// <summary>
    /// The Shuffle booster's rules (docs/mechanics.md, Shuffle): which tubes can be shuffled, and a new
    /// order for one.
    /// </summary>
    /// <remarks>
    /// Everything is judged by what the player sees, never by the colours hidden balls conceal, so
    /// neither eligibility nor the result can leak a hidden colour. A ball's look is "?" when hidden,
    /// otherwise its colour plus its key mark (if it carries a key).
    /// </remarks>
    public static class ShuffleRules
    {
        // Random orders tried before falling back to a swap that is sure to change the look. A random
        // order repeats the look at most about half the time (two balls), so the fallback is rare.
        private const int RandomAttempts = 8;

        /// <summary>
        /// True when the tube can be shuffled: not covered, not clay, not completed, and holding at
        /// least two balls that look different.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If the index is not a valid tube index.</exception>
        public static bool IsEligible(BoardState board, int tubeIndex)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            Tube tube = board[tubeIndex];
            if (tube.IsCovered || tube.IsClay || board.IsCompleted(tubeIndex))
            {
                return false;
            }

            for (int slot = 1; slot < tube.Count; slot++)
            {
                if (!LookAlike(tube[slot], tube[0]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>True when some tube on the board can be shuffled.</summary>
        public static bool HasEligibleTube(BoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            for (int t = 0; t < board.TubeCount; t++)
            {
                if (IsEligible(board, t))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// True when the player cannot tell the two balls apart: both hidden, or both revealed with the
        /// same colour and the same key (or none). A hidden ball's key is invisible, so it does not count.
        /// </summary>
        public static bool LookAlike(Ball a, Ball b)
        {
            if (a.IsHidden || b.IsHidden)
            {
                return a.IsHidden && b.IsHidden;
            }

            return a.ColorId == b.ColorId && a.HasKey == b.HasKey && (!a.HasKey || a.KeyLockId == b.KeyLockId);
        }

        /// <summary>
        /// A new order for the balls of an eligible tube, bottom → top, as the slot each ball comes from:
        /// <c>order[slot]</c> is the slot the ball placed in <c>slot</c> held before. The same balls, each
        /// with its hidden flag and key; once the board reveals the ball on top, the tube always looks
        /// different in at least one slot.
        /// </summary>
        /// <remarks>The top ball is not revealed here: revealing it is the board's job, which announces it.</remarks>
        /// <param name="tube">An eligible tube (see <see cref="IsEligible"/>); read, not changed.</param>
        /// <param name="random">Picks the order.</param>
        /// <exception cref="ArgumentException">If no two balls in the tube look different.</exception>
        internal static int[] Arrange(Tube tube, Random random)
        {
            int count = tube.Count;
            var order = new int[count];

            for (int attempt = 0; attempt < RandomAttempts; attempt++)
            {
                for (int slot = 0; slot < count; slot++)
                {
                    order[slot] = slot;
                }

                Shuffle(order, random);
                if (LooksDifferent(order, tube))
                {
                    return order;
                }
            }

            // Swapping the top with a ball that looks different always shows: that slot now holds the
            // old top (revealed, so it keeps its look), which differs from what was there.
            int top = count - 1;
            for (int slot = 0; slot <= top; slot++)
            {
                order[slot] = slot;
            }

            for (int slot = 0; slot < top; slot++)
            {
                if (!LookAlike(tube[slot], tube[top]))
                {
                    (order[slot], order[top]) = (order[top], order[slot]);
                    return order;
                }
            }

            throw new ArgumentException("No two balls in the tube look different; it cannot be shuffled.", nameof(tube));
        }

        /// <summary>Compares as the player will see it: with the new top revealed.</summary>
        private static bool LooksDifferent(int[] order, Tube tube)
        {
            int top = order.Length - 1;
            for (int slot = 0; slot <= top; slot++)
            {
                Ball placed = slot == top ? tube[order[slot]].AsRevealed() : tube[order[slot]];
                if (!LookAlike(placed, tube[slot]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Fisher–Yates, in place.</summary>
        private static void Shuffle(int[] items, Random random)
        {
            for (int i = items.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }
    }
}
