using System;
using System.Collections.Generic;
using BallSort.Core.Model;
using BallSort.Core.Solver;

namespace BallSort.Core.Rules
{
    /// <summary>
    /// Decides whether the player is stuck: the level is not won and there is nothing meaningful left
    /// to do (docs/mechanics.md, Win and stuck). Stuck when no legal move exists, or when every position
    /// reachable from this one can get back to it and none of them is a win, so the reachable positions
    /// form one loop. Any one-way move still available means not stuck, even one into a dead end: the
    /// player may see it coming and use a booster first.
    /// </summary>
    /// <remarks>
    /// The search explores forward with the real rules (<see cref="MoveRules"/> and the board's own move
    /// cascade) and identifies positions by <see cref="BoardStateKey"/>.
    /// <para>
    /// Some quantities only ever move one way during play: hidden balls, covered tubes and unfound keys
    /// only go down, completed tubes only go up, and so do the places where a ball sits on a different
    /// colour or on a hidden ball (no move can create one: a ball only lands on its own colour or in an
    /// empty tube). A move that changes any of them can never be undone by playing on, so it ends the
    /// search at once (<see cref="StuckVerdict.OneWayMove"/>); reveals, completions, found keys, opened
    /// covers and wins all change one of them. What remains are partial moves and moves of whole
    /// one-colour tubes, the only moves that might be reversible.
    /// </para>
    /// <para>
    /// If those reach more than <see cref="DefaultPositionLimit"/> positions, the player is not stuck
    /// (<see cref="StuckVerdict.LimitReached"/>). Otherwise every recorded position is reachable from
    /// this one, so they form one strongly connected set exactly when each of them can get back here:
    /// one search backwards along the recorded moves decides it.
    /// </para>
    /// </remarks>
    public static class StuckDetector
    {
        /// <summary>
        /// Positions the search may record before it gives up and calls the player not stuck. Measured
        /// in the Editor (docs/mechanics.md): about 0.1 ms per position at 13 tubes, the size of the
        /// largest delivered levels, so a search that reaches this limit costs about 3.5–4.5 ms; random
        /// play on every delivered level never recorded more than 9 positions in one check.
        /// </summary>
        public const int DefaultPositionLimit = 32;

        /// <summary>True when the player is stuck on <paramref name="board"/>; see <see cref="StuckDetector"/>.</summary>
        public static bool IsStuck(BoardState board) => Analyze(board, DefaultPositionLimit).IsStuck;

        /// <summary>Decides whether the player is stuck on <paramref name="board"/>, and why.</summary>
        /// <param name="board">The position to judge. Not modified.</param>
        /// <param name="positionLimit">Most positions to record before calling it not stuck; at least one.</param>
        public static StuckAnalysis Analyze(BoardState board, int positionLimit)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (positionLimit < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(positionLimit), positionLimit, "The limit must be at least one position.");
            }

            if (board.IsWon)
            {
                return new StuckAnalysis(StuckVerdict.Won, 0);
            }

            if (!MoveRules.HasAnyLegalMove(board))
            {
                return new StuckAnalysis(StuckVerdict.NoLegalMove, 1);
            }

            return Search(board, positionLimit);
        }

        private static StuckAnalysis Search(BoardState board, int positionLimit)
        {
            Progress start = Progress.Of(board);
            BoardState current = board.Clone();
            BoardState next = board.Clone();
            var encoder = new BoardEncoder();

            var ids = new Dictionary<BoardStateKey, int>();
            var positions = new List<byte[]>();
            var movesInto = new List<List<int>>();
            var open = new Queue<int>();

            encoder.Encode(board);
            ids.Add(encoder.CreateKey(), 0);
            positions.Add(encoder.CopyPacked());
            movesInto.Add(new List<int>());
            open.Enqueue(0);

            while (open.Count > 0)
            {
                int id = open.Dequeue();
                BoardEncoder.Unpack(positions[id], current);

                for (int from = 0; from < current.TubeCount; from++)
                {
                    if (!MoveRules.CanSelect(current, from))
                    {
                        continue;
                    }

                    for (int to = 0; to < current.TubeCount; to++)
                    {
                        if (to == from || !MoveRules.TryGetMove(current, from, to, out MoveInfo move))
                        {
                            continue;
                        }

                        next.CopyStateFrom(current);
                        next.ApplyMove(from, to, move.Count);
                        if (!Progress.Of(next).Equals(start))
                        {
                            return new StuckAnalysis(StuckVerdict.OneWayMove, positions.Count);
                        }

                        encoder.Encode(next);
                        BoardStateKey key = encoder.CreateKey();
                        if (!ids.TryGetValue(key, out int target))
                        {
                            if (positions.Count >= positionLimit)
                            {
                                return new StuckAnalysis(StuckVerdict.LimitReached, positions.Count);
                            }

                            target = positions.Count;
                            ids.Add(key, target);
                            positions.Add(encoder.CopyPacked());
                            movesInto.Add(new List<int>());
                            open.Enqueue(target);
                        }

                        movesInto[target].Add(id);
                    }
                }
            }

            return new StuckAnalysis(
                EveryPositionLeadsBack(movesInto) ? StuckVerdict.Loop : StuckVerdict.OneWayMove, positions.Count);
        }

        /// <summary>Walks the recorded moves backwards from the start (position 0): true when it reaches every position.</summary>
        private static bool EveryPositionLeadsBack(List<List<int>> movesInto)
        {
            var leadsBack = new bool[movesInto.Count];
            var pending = new Stack<int>();
            leadsBack[0] = true;
            pending.Push(0);
            int reached = 1;

            while (pending.Count > 0)
            {
                foreach (int source in movesInto[pending.Pop()])
                {
                    if (!leadsBack[source])
                    {
                        leadsBack[source] = true;
                        reached++;
                        pending.Push(source);
                    }
                }
            }

            return reached == movesInto.Count;
        }

        /// <summary>
        /// The quantities that only ever change one way during play. Two positions in the same loop
        /// always agree on all of them.
        /// </summary>
        private readonly struct Progress : IEquatable<Progress>
        {
            private readonly int _hiddenBalls;
            private readonly int _coveredTubes;
            private readonly int _keysLeft;
            private readonly int _completedTubes;
            private readonly int _mixedJoins;

            private Progress(int hiddenBalls, int coveredTubes, int keysLeft, int completedTubes, int mixedJoins)
            {
                _hiddenBalls = hiddenBalls;
                _coveredTubes = coveredTubes;
                _keysLeft = keysLeft;
                _completedTubes = completedTubes;
                _mixedJoins = mixedJoins;
            }

            public static Progress Of(BoardState board)
            {
                int hidden = 0, covered = 0, completed = 0, mixed = 0;
                for (int t = 0; t < board.TubeCount; t++)
                {
                    Tube tube = board[t];
                    if (tube.IsCovered)
                    {
                        covered++;
                    }

                    if (board.IsCompleted(t))
                    {
                        completed++;
                    }

                    for (int slot = 0; slot < tube.Count; slot++)
                    {
                        Ball ball = tube[slot];
                        if (ball.IsHidden)
                        {
                            hidden++;
                        }

                        // A ball on a different colour, or on a hidden ball: no move can ever put it back.
                        if (slot > 0 && (tube[slot - 1].IsHidden || tube[slot - 1].ColorId != ball.ColorId))
                        {
                            mixed++;
                        }
                    }
                }

                return new Progress(hidden, covered, board.KeysRemaining, completed, mixed);
            }

            public bool Equals(Progress other) =>
                _hiddenBalls == other._hiddenBalls && _coveredTubes == other._coveredTubes && _keysLeft == other._keysLeft &&
                _completedTubes == other._completedTubes && _mixedJoins == other._mixedJoins;

            public override bool Equals(object obj) => obj is Progress other && Equals(other);

            public override int GetHashCode() =>
                (((_hiddenBalls * 31 + _coveredTubes) * 31 + _keysLeft) * 31 + _completedTubes) * 31 + _mixedJoins;
        }
    }
}
