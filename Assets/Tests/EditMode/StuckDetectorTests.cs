using BallSort.Core.Commands;
using BallSort.Core.Model;
using BallSort.Core.Rules;
using BallSort.Core.Solver;
using NUnit.Framework;
using static BallSort.Tests.EditMode.TestBoards;

namespace BallSort.Tests.EditMode
{
    /// <summary>
    /// The stuck rule (docs/mechanics.md, Win and stuck): stuck = not won, and no legal move or only moves
    /// that go round in a loop. Raw boards, so each case is exactly the position described.
    /// </summary>
    [TestFixture]
    public sealed class StuckDetectorTests
    {
        private const int Blue = 0;
        private const int Red = 1;
        private const int Yellow = 2;
        private const int Green = 3;
        private const int Purple = 4;
        private const int Orange = 5;

        /// <summary>
        /// Blue red red red / yellow red _ _: two reds go across and back (two positions), and red can
        /// never finish because both tubes have another colour at the bottom.
        /// </summary>
        private static Tube[] RedLoop() => new[]
        {
            MakeTube(4, R(Blue), R(Red), R(Red), R(Red)),
            MakeTube(4, R(Yellow), R(Red)),
        };

        /// <summary>The same loop in other colours, so it cannot interact with <see cref="RedLoop"/>.</summary>
        private static Tube[] PurpleLoop() => new[]
        {
            MakeTube(4, R(Green), R(Purple), R(Purple), R(Purple)),
            MakeTube(4, R(Orange), R(Purple)),
        };

        /// <summary>One move only, into a dead end: the purple on top of a full tube joins the other purple.</summary>
        private static Tube[] OneMoveToADeadEnd() => new[]
        {
            MakeTube(2, R(Green), R(Purple)),
            MakeTube(3, R(Orange), R(Purple)),
        };

        private static BoardState Board(params Tube[][] groups)
        {
            var tubes = new System.Collections.Generic.List<Tube>();
            foreach (Tube[] group in groups)
            {
                tubes.AddRange(group);
            }

            return new BoardState(tubes);
        }

        private static void Move(BoardState board, int from, int to) =>
            Assert.That(new CommandHistory().TryExecuteMove(board, from, to, out _), Is.True, $"{from} -> {to} on {board}");

        private static StuckAnalysis Analyze(BoardState board) =>
            StuckDetector.Analyze(board, StuckDetector.DefaultPositionLimit);

        [Test]
        public void ALoopWithNothingElseLegal_IsStuck()
        {
            StuckAnalysis analysis = Analyze(Board(RedLoop()));

            Assert.That(analysis.IsStuck, Is.True);
            Assert.That(analysis.Verdict, Is.EqualTo(StuckVerdict.Loop));
            Assert.That(analysis.PositionsExplored, Is.EqualTo(2));
        }

        [Test]
        public void ASingleMoveIntoADeadEnd_IsNotStuck_UntilItIsTaken()
        {
            BoardState board = Board(OneMoveToADeadEnd());
            Assert.That(Analyze(board).Verdict, Is.EqualTo(StuckVerdict.OneWayMove));

            Move(board, 0, 1);

            Assert.That(Analyze(board).Verdict, Is.EqualTo(StuckVerdict.NoLegalMove));
            Assert.That(StuckDetector.IsStuck(board), Is.True);
        }

        [Test]
        public void ALoopPlusAOneWayMoveElsewhere_IsNotStuck_UntilTheOneWayMoveIsTaken()
        {
            BoardState board = Board(RedLoop(), OneMoveToADeadEnd());
            Assert.That(Analyze(board).Verdict, Is.EqualTo(StuckVerdict.OneWayMove));

            Move(board, 2, 3);

            Assert.That(Analyze(board).Verdict, Is.EqualTo(StuckVerdict.Loop));
        }

        [Test]
        public void TwoIndependentLoops_AreStuck()
        {
            StuckAnalysis analysis = Analyze(Board(RedLoop(), PurpleLoop()));

            Assert.That(analysis.Verdict, Is.EqualTo(StuckVerdict.Loop));
            Assert.That(analysis.PositionsExplored, Is.EqualTo(4), "every combination of the two loops");
        }

        [Test]
        public void AThreeTubeCycle_IsStuck()
        {
            // Eight reds over three other colours, three red slots per tube, one free slot in all: every
            // move takes one red into the gap, so the gap travels round the three tubes and back.
            BoardState board = Board(new[]
            {
                MakeTube(4, R(Blue), R(Red), R(Red), R(Red)),
                MakeTube(4, R(Yellow), R(Red), R(Red)),
                MakeTube(4, R(Green), R(Red), R(Red), R(Red)),
            });
            BoardStateKey start = BoardStateKey.Of(board);

            StuckAnalysis analysis = Analyze(board);
            Assert.That(analysis.Verdict, Is.EqualTo(StuckVerdict.Loop));
            Assert.That(analysis.PositionsExplored, Is.EqualTo(3), "the gap in A, in B or in C");

            Move(board, 0, 1);
            Move(board, 2, 0);
            Move(board, 1, 2);
            Assert.That(BoardStateKey.Of(board), Is.EqualTo(start), "A → B, C → A, B → C comes back to the start");
        }

        [Test]
        public void ACycleWithAnExitIntoAnotherLoop_IsNotStuck_UntilTheExitIsTaken()
        {
            // Seven reds, two free slots: A → B, B → C, C → A goes round, but B → C first drops into a set
            // of positions (one tube with a single red) that never comes back.
            BoardState board = Board(new[]
            {
                MakeTube(4, R(Blue), R(Red), R(Red), R(Red)),
                MakeTube(4, R(Yellow), R(Red), R(Red)),
                MakeTube(4, R(Green), R(Red), R(Red)),
            });
            BoardStateKey start = BoardStateKey.Of(board);
            Assert.That(Analyze(board).Verdict, Is.EqualTo(StuckVerdict.OneWayMove));

            Move(board, 0, 1);
            Move(board, 1, 2);
            Move(board, 2, 0);
            Assert.That(BoardStateKey.Of(board), Is.EqualTo(start), "A → B, B → C, C → A comes back to the start");

            Move(board, 1, 2);
            Assert.That(Analyze(board).Verdict, Is.EqualTo(StuckVerdict.Loop));
        }

        [Test]
        public void HittingThePositionLimit_IsNotStuck()
        {
            BoardState board = Board(RedLoop(), PurpleLoop());

            StuckAnalysis analysis = StuckDetector.Analyze(board, positionLimit: 3);

            Assert.That(analysis.Verdict, Is.EqualTo(StuckVerdict.LimitReached));
            Assert.That(analysis.IsStuck, Is.False);
        }

        [Test]
        public void AHiddenBallThatCanStillBeRevealed_IsNotStuck()
        {
            // The loop, plus a move that lifts a purple off a hidden ball (into a dead end).
            BoardState board = Board(RedLoop(), new[]
            {
                MakeTube(2, H(Green), R(Purple)),
                MakeTube(3, R(Orange), R(Purple)),
            });

            Assert.That(Analyze(board).Verdict, Is.EqualTo(StuckVerdict.OneWayMove));

            Move(board, 2, 3);
            Assert.That(board[2].Top.IsRevealed, Is.True);
            Assert.That(Analyze(board).Verdict, Is.EqualTo(StuckVerdict.Loop), "once revealed, only the loop is left");
        }

        [Test]
        public void ACompletionStillPossible_IsNotStuck()
        {
            // Two reds onto the two in tube 1 finish red.
            BoardState board = Board(new[]
            {
                MakeTube(4, R(Blue), R(Red), R(Red)),
                MakeTube(4, R(Red), R(Red)),
            });

            Assert.That(Analyze(board).Verdict, Is.EqualTo(StuckVerdict.OneWayMove));
        }

        [Test]
        public void AMoveWithNoWayBack_IsNotStuck_EvenWithoutARevealOrACompletion()
        {
            // Two reds on each side, one free slot each. Either move fills its target, and from there
            // the reds only swap between "A full" and "B full": nothing comes back to two and two.
            BoardState board = Board(new[]
            {
                MakeTube(4, R(Blue), R(Red), R(Red)),
                MakeTube(4, R(Yellow), R(Red), R(Red)),
            });

            StuckAnalysis analysis = Analyze(board);
            Assert.That(analysis.Verdict, Is.EqualTo(StuckVerdict.OneWayMove));
            Assert.That(analysis.PositionsExplored, Is.EqualTo(3), "decided by the backward search, not a reveal or completion");

            Move(board, 0, 1);

            Assert.That(Analyze(board).Verdict, Is.EqualTo(StuckVerdict.Loop));
        }

        [Test]
        public void AWonBoard_IsNeverStuck()
        {
            BoardState board = Board(new[] { MakeTube(2, R(Red), R(Red)), MakeEmptyTube(2) });

            Assert.That(Analyze(board).Verdict, Is.EqualTo(StuckVerdict.Won));
            Assert.That(StuckDetector.IsStuck(board), Is.False);
        }

        [Test]
        public void Analyze_DoesNotChangeTheBoard()
        {
            BoardState board = Board(RedLoop(), OneMoveToADeadEnd());
            BoardStateKey before = BoardStateKey.Of(board);

            Analyze(board);

            Assert.That(BoardStateKey.Of(board), Is.EqualTo(before));
        }
    }
}
