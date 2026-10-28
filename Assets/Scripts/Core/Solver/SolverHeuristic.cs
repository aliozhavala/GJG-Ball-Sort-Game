using System;
using BallSort.Core.Model;

namespace BallSort.Core.Solver
{
    /// <summary>
    /// A lower bound on the number of moves still needed to win, for A*.
    /// </summary>
    /// <remarks>
    /// Split every tube into <em>blocks</em>: maximal runs of same-coloured balls by their true
    /// colour, hidden or not. The estimate is
    /// <list type="bullet">
    /// <item>one move for every block that is not at the bottom of its tube, plus</item>
    /// <item>for each colour, one move for every tube whose bottom block has that colour, minus one
    /// if any of those tubes has a capacity equal to the colour's ball count.</item>
    /// </list>
    /// <para>
    /// <b>Why it never overestimates.</b> In a won position every tube is empty or holds all of one
    /// colour. So a block sitting on a ball of another colour must move at least once (the ball
    /// under it cannot leave first), and of the bottom blocks of one colour at most one can stay
    /// put — all that colour's balls end in one tube — and only in a tube big enough to hold them
    /// all. That counts blocks that must each move at least once. No single move can be the first
    /// move of two of them: balls that have never moved still sit, in their original order, at the
    /// bottom of their original tube, and a move carries a contiguous run of one colour off the
    /// top, so its never-moved balls all come from one block. Hence at least that many moves remain.
    /// </para>
    /// <para>
    /// <b>Why it is consistent</b> (falls by at most one per move, so A* never has to reopen a
    /// position). A move takes balls off the top of one tube. Either a block above the bottom
    /// disappears (first term −1), or the tube's only block leaves and its colour has one bottom
    /// block fewer (second term −1, or 0 if that loses the "minus one"), or neither; never both.
    /// On the destination the balls join a block of their colour (no change) or land in an empty
    /// tube, adding a bottom block (+1) that can at most earn a "minus one" (−1), so no lower.
    /// Reveals, covers and keys do not enter the estimate. So one move lowers it by one at most.
    /// </para>
    /// <para>Covers, clay and hidden balls are ignored, which only makes the bound weaker, never wrong.</para>
    /// </remarks>
    internal sealed class SolverHeuristic
    {
        private readonly int[] _bottomBlocks;
        private readonly bool[] _canStay;

        public SolverHeuristic(int colorCount)
        {
            _bottomBlocks = new int[colorCount];
            _canStay = new bool[colorCount];
        }

        public int Estimate(BoardState board)
        {
            Array.Clear(_bottomBlocks, 0, _bottomBlocks.Length);
            Array.Clear(_canStay, 0, _canStay.Length);

            int moves = 0;
            for (int t = 0; t < board.TubeCount; t++)
            {
                Tube tube = board[t];
                if (tube.IsEmpty)
                {
                    continue;
                }

                for (int slot = 1; slot < tube.Count; slot++)
                {
                    if (tube[slot].ColorId != tube[slot - 1].ColorId)
                    {
                        moves++;
                    }
                }

                int bottomColor = tube[0].ColorId;
                _bottomBlocks[bottomColor]++;
                if (tube.Capacity == board.GetBallCount(bottomColor))
                {
                    _canStay[bottomColor] = true;
                }
            }

            for (int color = 0; color < _bottomBlocks.Length; color++)
            {
                if (_bottomBlocks[color] > 0)
                {
                    moves += _bottomBlocks[color] - (_canStay[color] ? 1 : 0);
                }
            }

            return moves;
        }
    }
}
