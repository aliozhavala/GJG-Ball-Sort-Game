using System;
using System.Collections.Generic;
using System.Threading;
using BallSort.Core.Commands;
using BallSort.Core.Levels;
using BallSort.Core.Model;
using BallSort.Core.Player;
using BallSort.Core.Rules;
using BallSort.Core.Session;
using BallSort.Core.Solver;
using NUnit.Framework;
using static BallSort.Tests.EditMode.TestLevels;

namespace BallSort.Tests.EditMode
{
    /// <summary>
    /// The solver against the real rules: each mechanic, unsolvable boards, optimality against a
    /// plain breadth-first search, limits and cancellation.
    /// </summary>
    [TestFixture]
    public sealed class SolverTests
    {
        private const int Limit = 100000;

        // --- Fixtures, one per mechanic ---------------------------------------------------------

        /// <summary>Colours 0 and 1 under hidden balls; only the tops are revealed at first.</summary>
        private static TestLevel WithHiddenBalls() =>
            Level(2,
                Hidden(4, new[] { 1, 0, 1, 0 }, new[] { true, true, false, false }),
                Hidden(4, new[] { 0, 1, 0, 1 }, new[] { true, false, false, false }),
                T(4), T(4));

        /// <summary>Tube 1 → 0 completes colour 0 and opens the veil; then tube 1 → 2 wins. Two moves.</summary>
        private static TestLevel WithAVeil() =>
            Level(2, T(4, 0, 0, 0), T(4, 1, 0), T(4, 1, 1, 1).WithVeil(0), T(4));

        /// <summary>Tube 0 → 1 exposes the key for lock 0, which covers tubes 2 and 3.</summary>
        private static TestLevel WithALock() =>
            Level(3, T(4, 1, 2, 0, 0).WithKey(1, 0), T(4), T(4, 2, 1, 2, 1), T(4, 0, 0, 1, 2), T(4))
                .WithLock(0, 2, 3);

        /// <summary>The 0s must go into clay (tube 0); then tube 1's 1 joins tube 2. Two moves.</summary>
        private static TestLevel WithClay() =>
            Level(2, T(4, 0).AsClay(), T(4, 1, 0, 0, 0), T(4, 1, 1, 1), T(4));

        /// <summary>A capacity-6 tube that is not clay: colour 1 has six balls and must finish in it.</summary>
        private static TestLevel WithALargeTube() =>
            Level(2, T(6, 1, 0, 1, 0, 1, 1), T(4, 1, 0, 1, 0), T(4), T(4))
                .WithGrid(new int?[] { 0, 1, 2 }, new int?[] { 0, 3 });

        private static IEnumerable<TestCaseData> MechanicLevels()
        {
            yield return new TestCaseData((Func<TestLevel>)Valid).SetName("Plain");
            yield return new TestCaseData((Func<TestLevel>)WithHiddenBalls).SetName("HiddenBalls");
            yield return new TestCaseData((Func<TestLevel>)WithAVeil).SetName("Veil");
            yield return new TestCaseData((Func<TestLevel>)WithALock).SetName("LockAndKey");
            yield return new TestCaseData((Func<TestLevel>)WithClay).SetName("Clay");
            yield return new TestCaseData((Func<TestLevel>)WithALargeTube).SetName("LargeTube");
            yield return new TestCaseData((Func<TestLevel>)StuckTrap).SetName("StuckTrapFromTheStart");
        }


        private static SolverResult Solve(TestLevel level, SolverMode mode) =>
            BoardSolver.Solve(Board(level), mode, Limit);

        /// <summary>Plays <paramref name="moves"/> through a fresh session, failing on any refused move.</summary>
        private static LevelSession Replay(TestLevel level, IReadOnlyList<MoveInfo> moves)
        {
            var session = new LevelSession(Load(level), 1, new BoosterInventory(0, 0, 0), 4);
            foreach (MoveInfo move in moves)
            {
                Assert.That(session.TryMove(move.FromIndex, move.ToIndex, out MoveRejection rejection), Is.True,
                    $"{move} was refused ({rejection}) on {session.Board}");
            }

            return session;
        }

        /// <summary>
        /// Shortest win by brute force: breadth-first, no heuristic, no pruning, and positions told
        /// apart by <see cref="BoardState.ToString"/> rather than by the solver's own key.
        /// </summary>
        private static int BreadthFirstMinimum(BoardState start)
        {
            var seen = new HashSet<string> { start.ToString() };
            var layer = new List<BoardState> { start.Clone() };
            for (int depth = 0; layer.Count > 0; depth++)
            {
                var next = new List<BoardState>();
                foreach (BoardState board in layer)
                {
                    if (board.IsWon)
                    {
                        return depth;
                    }

                    for (int from = 0; from < board.TubeCount; from++)
                    {
                        for (int to = 0; to < board.TubeCount; to++)
                        {
                            if (from == to || !MoveRules.IsLegal(board, from, to))
                            {
                                continue;
                            }

                            BoardState child = board.Clone();
                            new CommandHistory().TryExecuteMove(child, from, to, out _);
                            if (seen.Add(child.ToString()))
                            {
                                next.Add(child);
                            }
                        }
                    }
                }

                layer = next;
            }

            return -1;
        }

        // --- Solving ---------------------------------------------------------------------------

        [TestCaseSource(nameof(MechanicLevels))]
        public void EachMechanic_IsSolvedInBothModes_AndTheMovesWinWhenReplayed(Func<TestLevel> level)
        {
            foreach (SolverMode mode in new[] { SolverMode.Fast, SolverMode.Optimal })
            {
                SolverResult result = Solve(level(), mode);

                Assert.That(result.Outcome, Is.EqualTo(SolverOutcome.Solved), $"{mode}: {result}");
                Assert.That(result.IsOptimal, Is.EqualTo(mode == SolverMode.Optimal));
                Assert.That(Replay(level(), result.Moves).IsWon, Is.True, $"{mode}: the moves must win");
            }
        }

        [TestCaseSource(nameof(MechanicLevels))]
        public void Optimal_MatchesABreadthFirstSearch(Func<TestLevel> level)
        {
            int minimum = BreadthFirstMinimum(Board(level()));
            SolverResult optimal = Solve(level(), SolverMode.Optimal);
            SolverResult fast = Solve(level(), SolverMode.Fast);

            Assert.That(optimal.Moves.Count, Is.EqualTo(minimum));
            Assert.That(optimal.LowerBound, Is.EqualTo(minimum));
            Assert.That(fast.Moves.Count, Is.GreaterThanOrEqualTo(minimum));
        }

        [Test]
        public void Optimal_FindsTheKnownMinimums()
        {
            Assert.That(Solve(Level(1, T(4, 0, 0), T(4, 0, 0)), SolverMode.Optimal).Moves.Count, Is.EqualTo(1));
            Assert.That(Solve(WithAVeil(), SolverMode.Optimal).Moves.Count, Is.EqualTo(2));
            Assert.That(Solve(WithClay(), SolverMode.Optimal).Moves.Count, Is.EqualTo(2));
        }

        [Test]
        public void AWonBoard_IsSolvedWithNoMoves()
        {
            BoardState board = TestBoards.MakeBoard(
                TestBoards.MakeTube(4, TestBoards.R(0), TestBoards.R(0), TestBoards.R(0), TestBoards.R(0)),
                TestBoards.MakeEmptyTube(4));

            SolverResult result = BoardSolver.Solve(board, SolverMode.Optimal, Limit);

            Assert.That(result.Outcome, Is.EqualTo(SolverOutcome.Solved));
            Assert.That(result.Moves, Is.Empty);
        }

        // --- Unsolvable ------------------------------------------------------------------------

        [Test]
        public void StuckBehindCovers_IsUnsolvable()
        {
            foreach (SolverMode mode in new[] { SolverMode.Fast, SolverMode.Optimal })
            {
                SolverResult result = Solve(StuckBehindCovers(), mode);

                Assert.That(result.Outcome, Is.EqualTo(SolverOutcome.Unsolvable), $"{mode}: {result}");
                Assert.That(result.Moves, Is.Empty);
                Assert.That(result.LowerBound, Is.EqualTo(-1));
            }
        }

        [Test]
        public void TheStuckTrap_IsUnsolvableOnceSprung()
        {
            var session = new LevelSession(Load(StuckTrap()), 1, new BoosterInventory(0, 0, 0), 4);
            session.TryMove(1, 4, out _);
            session.TryMove(0, 1, out _);
            Assert.That(session.IsStuck, Is.True, "test setup: the trap is sprung");

            foreach (SolverMode mode in new[] { SolverMode.Fast, SolverMode.Optimal })
            {
                Assert.That(BoardSolver.Solve(session.Board, mode, Limit).Outcome, Is.EqualTo(SolverOutcome.Unsolvable));
            }
        }

        [Test]
        public void AVeilWhoseColourCanNeverComplete_IsUnsolvable()
        {
            // As WithAVeil, but the veil is colour 1 and hides 1s: completing colour 0 leaves it closed.
            TestLevel level = Level(2, T(4, 0, 0, 0), T(4, 1, 0), T(4, 1, 1, 1).WithVeil(1), T(4));

            Assert.That(Solve(level, SolverMode.Fast).Outcome, Is.EqualTo(SolverOutcome.Unsolvable));
        }

        [Test]
        public void LocksWhoseKeysAreInsideEachOther_AreUnsolvable_ThoughTheLevelIsValid()
        {
            // Lock 0's key is in tube 3 (lock 1), lock 1's key in tube 2 (lock 0): neither opens.
            TestLevel level = Level(3,
                    T(4, 0, 0, 0), T(4, 0), T(4, 1, 2).WithKey(0, 1), T(4, 2, 1).WithKey(0, 0), T(4, 1, 1, 2, 2), T(4))
                .WithLock(0, 2).WithLock(1, 3);
            Assert.That(Validate(level), Is.Empty, "test setup: the validator cannot see this");

            Assert.That(Solve(level, SolverMode.Fast).Outcome, Is.EqualTo(SolverOutcome.Unsolvable));
        }

        // --- Limits, cancellation, purity ------------------------------------------------------

        [Test]
        public void HittingTheNodeLimit_IsUnknown_WithALowerBoundThatHolds()
        {
            int minimum = Solve(StuckTrap(), SolverMode.Optimal).Moves.Count;

            SolverResult result = BoardSolver.Solve(Board(StuckTrap()), SolverMode.Optimal, 1);

            Assert.That(result.Outcome, Is.EqualTo(SolverOutcome.Unknown));
            Assert.That(result.NodesExpanded, Is.EqualTo(1));
            Assert.That(result.Moves, Is.Empty);
            Assert.That(result.LowerBound, Is.InRange(1, minimum));
        }

        [Test]
        public void ACancelledToken_StopsTheSearch()
        {
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();

                Assert.Throws<OperationCanceledException>(
                    () => BoardSolver.Solve(Board(Valid()), SolverMode.Optimal, Limit, cancellation.Token));
            }
        }

        [Test]
        public void Solving_LeavesTheBoardAndItsSubscribersAlone()
        {
            BoardState board = Board(WithALock());
            string before = board.ToString();
            int events = 0;
            board.MoveApplied += _ => events++;
            board.KeyFound += _ => events++;

            BoardSolver.Solve(board, SolverMode.Optimal, Limit);

            Assert.That(board.ToString(), Is.EqualTo(before));
            Assert.That(events, Is.Zero);
        }

        [Test]
        public void ResultsReportTheirSearch()
        {
            SolverResult result = Solve(StuckTrap(), SolverMode.Optimal);

            Assert.That(result.NodesExpanded, Is.GreaterThan(0));
            Assert.That(result.MaxFrontier, Is.GreaterThan(0));
            Assert.That(result.Elapsed, Is.GreaterThanOrEqualTo(TimeSpan.Zero));
        }
    }
}
