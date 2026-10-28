using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using BallSort.Core.Model;
using BallSort.Core.Rules;

namespace BallSort.Core.Solver
{
    /// <summary>
    /// One run of <see cref="BoardSolver.Solve"/>: A* (or weighted A*) over board positions, using
    /// the game's own <see cref="MoveRules"/> and <c>BoardState.ApplyMove</c>.
    /// </summary>
    /// <remarks>
    /// Positions are explored forward only, never by undo: an undo keeps reveals, opened covers and
    /// found keys, so it cannot restore a position. Two scratch boards are reused for the whole run
    /// — the position being expanded is unpacked into one, and each successor is copied into the
    /// other before its move is applied — so the per-move cost is a copy, not an allocation.
    /// <para>
    /// Skipped moves (each lands only on a position already reached, so no solution is lost and an
    /// exhausted search still proves the board unsolvable):
    /// <list type="bullet">
    /// <item>moves out of a completed tube — <see cref="MoveRules"/> already refuses them;</item>
    /// <item>pouring a tube that is one revealed run into an empty tube of the same capacity that is
    /// not clay or covered: the two tubes are interchangeable, so the position is unchanged;</item>
    /// <item>moving the previous move's balls straight back, unless that move revealed a ball, found
    /// a key or opened a cover: the result is the position before it.</item>
    /// </list>
    /// </para>
    /// </remarks>
    internal sealed class SolverSearch
    {
        /// <summary>Weight on the heuristic in <see cref="SolverMode.Fast"/>: priority = g + 3h.</summary>
        internal const int FastWeight = 3;

        private const int CancellationCheckInterval = 256;

        private readonly BoardState _start;
        private readonly BoardState _current;
        private readonly BoardState _next;
        private readonly SolverMode _mode;
        private readonly int _nodeLimit;
        private readonly CancellationToken _cancellationToken;
        private readonly BoardEncoder _encoder = new BoardEncoder();
        private readonly SolverHeuristic _heuristic;
        private readonly NodeHeap _frontier = new NodeHeap();
        private readonly Dictionary<BoardStateKey, SearchNode> _reached = new Dictionary<BoardStateKey, SearchNode>();
        private readonly Stopwatch _stopwatch = new Stopwatch();

        private int _sequence;
        private int _expanded;
        private int _maxFrontier;
        private int _startEstimate;

        public SolverSearch(BoardState board, SolverMode mode, int nodeLimit, CancellationToken cancellationToken)
        {
            _start = board.Clone();
            _current = board.Clone();
            _next = board.Clone();
            _mode = mode;
            _nodeLimit = nodeLimit;
            _cancellationToken = cancellationToken;
            _heuristic = new SolverHeuristic(board.ColorCount);
        }

        public SolverResult Run()
        {
            _stopwatch.Start();
            _startEstimate = _heuristic.Estimate(_start);

            _encoder.Encode(_start);
            var root = new SearchNode(
                _encoder.CopyPacked(), null, -1, -1, 0, 0, _startEstimate, Priority(0, _startEstimate), false, _sequence++);
            _reached.Add(_encoder.CreateKey(), root);
            _frontier.Push(root);
            _maxFrontier = 1;

            while (_frontier.Count > 0)
            {
                SearchNode node = _frontier.Pop();
                if (node.IsStale)
                {
                    continue;
                }

                BoardEncoder.Unpack(node.Packed, _current);

                // Tested when popped, not when generated, so A*'s answer is a shortest one.
                if (_current.IsWon)
                {
                    return Solved(node);
                }

                if (_expanded >= _nodeLimit)
                {
                    // A*'s smallest open g + h never exceeds the optimum (the heuristic is consistent).
                    int bound = _mode == SolverMode.Optimal ? Math.Max(_startEstimate, node.Depth + node.Estimate) : _startEstimate;
                    return Finish(SolverOutcome.Unknown, null, false, bound);
                }

                if (_expanded % CancellationCheckInterval == 0)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                }

                _expanded++;
                SearchNode goal = Expand(node);
                if (goal != null)
                {
                    return Solved(goal);
                }

                _maxFrontier = Math.Max(_maxFrontier, _frontier.Count);
            }

            return Finish(SolverOutcome.Unsolvable, null, false, -1);
        }

        /// <summary>Generates <paramref name="node"/>'s successors, which is unpacked in <see cref="_current"/>.</summary>
        /// <returns>A won successor when the fast search may stop at it; null otherwise.</returns>
        private SearchNode Expand(SearchNode node)
        {
            int keysBefore = _current.KeysRemaining;
            int coversBefore = CountCovers(_current);

            // The interchangeable-empty-tube shortcut assumes nothing is waiting to be found: a key
            // already in a selectable run would be found by any move, making it a real change.
            bool settled = !_current.HasFindableKey;

            for (int from = 0; from < _current.TubeCount; from++)
            {
                if (!MoveRules.CanSelect(_current, from))
                {
                    continue;
                }

                Tube source = _current[from];
                bool sourceIsOneRun = settled && source.RunLength == source.Count;

                for (int to = 0; to < _current.TubeCount; to++)
                {
                    if (to == from || !MoveRules.TryGetMove(_current, from, to, out MoveInfo move))
                    {
                        continue;
                    }

                    if (sourceIsOneRun && move.Count == source.Count && IsInterchangeableEmpty(source, _current[to]))
                    {
                        continue;
                    }

                    if (!node.GainedInformation && node.Parent != null &&
                        from == node.ToIndex && to == node.FromIndex && move.Count == node.Count)
                    {
                        continue;
                    }

                    bool reveals = move.Count < source.Count && source[source.Count - move.Count - 1].IsHidden;
                    _next.CopyStateFrom(_current);
                    _next.ApplyMove(from, to, move.Count);

                    _encoder.Encode(_next);
                    BoardStateKey key = _encoder.CreateKey();
                    int depth = node.Depth + 1;
                    if (_reached.TryGetValue(key, out SearchNode known))
                    {
                        // The fast search never reopens a position; A* only for a strictly shorter path.
                        if (_mode == SolverMode.Fast || known.Depth <= depth)
                        {
                            continue;
                        }

                        known.IsStale = true;
                    }

                    bool gained = reveals || _next.KeysRemaining != keysBefore || CountCovers(_next) != coversBefore;
                    int estimate = _heuristic.Estimate(_next);
                    var child = new SearchNode(
                        _encoder.CopyPacked(), node, from, to, move.Count, depth, estimate, Priority(depth, estimate), gained, _sequence++);
                    _reached[key] = child;

                    // A weighted search gains nothing by waiting for the goal to surface.
                    if (_mode == SolverMode.Fast && _next.IsWon)
                    {
                        return child;
                    }

                    _frontier.Push(child);
                }
            }

            return null;
        }

        private int Priority(int depth, int estimate) =>
            _mode == SolverMode.Optimal ? depth + estimate : depth + FastWeight * estimate;

        private static bool IsInterchangeableEmpty(Tube source, Tube target) =>
            target.IsEmpty && target.Capacity == source.Capacity && !target.IsClay && !target.IsCovered;

        private static int CountCovers(BoardState board)
        {
            int covers = 0;
            for (int t = 0; t < board.TubeCount; t++)
            {
                if (board[t].IsCovered)
                {
                    covers++;
                }
            }

            return covers;
        }

        /// <summary>
        /// Rebuilds the path to <paramref name="goal"/> and replays it on a fresh copy of the start
        /// through <see cref="MoveRules"/>, so a returned solution is checked before anyone sees it.
        /// </summary>
        private SolverResult Solved(SearchNode goal)
        {
            var path = new List<SearchNode>();
            for (SearchNode node = goal; node.Parent != null; node = node.Parent)
            {
                path.Add(node);
            }

            path.Reverse();

            BoardState replay = _start.Clone();
            var moves = new List<MoveInfo>(path.Count);
            foreach (SearchNode step in path)
            {
                if (!MoveRules.TryGetMove(replay, step.FromIndex, step.ToIndex, out MoveInfo move) || move.Count != step.Count)
                {
                    throw new InvalidOperationException($"Solver path does not replay at move {moves.Count + 1}.");
                }

                replay.ApplyMove(move.FromIndex, move.ToIndex, move.Count);
                moves.Add(move);
            }

            if (!replay.IsWon)
            {
                throw new InvalidOperationException("Solver path does not win when replayed.");
            }

            bool optimal = _mode == SolverMode.Optimal;
            return Finish(SolverOutcome.Solved, moves.AsReadOnly(), optimal, optimal ? moves.Count : _startEstimate);
        }

        private SolverResult Finish(SolverOutcome outcome, IReadOnlyList<MoveInfo> moves, bool optimal, int lowerBound)
        {
            _stopwatch.Stop();
            return new SolverResult(outcome, moves, optimal, lowerBound, _expanded, _maxFrontier, _stopwatch.Elapsed);
        }
    }
}
