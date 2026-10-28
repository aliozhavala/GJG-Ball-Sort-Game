using System.Collections.Generic;
using BallSort.Core.Commands;
using BallSort.Core.Model;
using NUnit.Framework;
using static BallSort.Tests.EditMode.TestBoards;

namespace BallSort.Tests.EditMode
{
    /// <summary>
    /// Veils, locks and keys on the board (docs/mechanics.md): when covers open, when keys are
    /// found, the chain between them, permanence under undo, and the event order.
    /// </summary>
    [TestFixture]
    public sealed class CoverTests
    {
        private CommandHistory _history;

        [SetUp]
        public void SetUp() => _history = new CommandHistory();

        private void Move(BoardState board, int from, int to) =>
            Assert.That(_history.TryExecuteMove(board, from, to, out _), Is.True, $"test setup: {from} -> {to} should be legal");

        private static List<string> RecordEvents(BoardState board)
        {
            var log = new List<string>();
            board.MoveApplied += _ => log.Add(nameof(BoardState.MoveApplied));
            board.MoveUndone += _ => log.Add(nameof(BoardState.MoveUndone));
            board.BallRevealed += _ => log.Add(nameof(BoardState.BallRevealed));
            board.TubeCompleted += _ => log.Add(nameof(BoardState.TubeCompleted));
            board.TubeUnveiled += e => log.Add($"{nameof(BoardState.TubeUnveiled)}({e.TubeIndex})");
            board.KeyFound += e => log.Add($"{nameof(BoardState.KeyFound)}({e.LockId})");
            board.LockOpened += e => log.Add($"{nameof(BoardState.LockOpened)}({e.LockId})");
            board.LevelCompleted += () => log.Add(nameof(BoardState.LevelCompleted));
            return log;
        }

        // --- Veil ------------------------------------------------------------------------------

        [Test]
        public void Veil_OpensWhenATubeCompletesInItsColour()
        {
            BoardState board = MakeBoard(
                MakeTube(4, R(0), R(0), R(0)),
                MakeTube(4, R(0)),
                MakeVeiledTube(0, 4, R(2), R(1)));
            TubeUnveiledEventArgs received = default;
            board.TubeUnveiled += e => received = e;

            Move(board, 1, 0);

            Assert.That(board[2].IsCovered, Is.False);
            Assert.That(received.TubeIndex, Is.EqualTo(2));
            Assert.That(received.ColorId, Is.EqualTo(0));
        }

        [Test]
        public void Veil_StaysClosedWhenAnotherColourCompletes()
        {
            BoardState board = MakeBoard(
                MakeTube(4, R(0), R(0), R(0)),
                MakeTube(4, R(0)),
                MakeVeiledTube(1, 4, R(2), R(1)));

            Move(board, 1, 0);

            Assert.That(board[2].Cover, Is.EqualTo(TubeCover.Veil(1)));
        }

        [Test]
        public void Veil_StaysClosedWhenATubeOnlyFillsWithPartOfItsColour()
        {
            // Four of eight colour-0 balls fill tube 0: full, but not completed, so no veil opens.
            BoardState board = MakeBoard(
                MakeTube(4, R(0), R(0), R(0)),
                MakeTube(4, R(0)),
                MakeTube(4, R(0), R(0), R(0), R(0)),
                MakeVeiledTube(0, 4, R(1)));

            Move(board, 1, 0);

            Assert.That(board[3].IsCovered, Is.True);
        }

        [Test]
        public void Veil_EveryVeilOfTheCompletedColourOpens()
        {
            BoardState board = MakeBoard(
                MakeTube(4, R(0), R(0), R(0)),
                MakeTube(4, R(0)),
                MakeVeiledTube(0, 4, R(1)),
                MakeVeiledTube(0, 4, R(2)));
            List<string> log = RecordEvents(board);

            Move(board, 1, 0);

            Assert.That(board[2].IsCovered || board[3].IsCovered, Is.False);
            Assert.That(log, Is.EqualTo(new[]
            {
                nameof(BoardState.MoveApplied), nameof(BoardState.TubeCompleted), "TubeUnveiled(2)", "TubeUnveiled(3)",
            }));
        }

        [Test]
        public void Veil_OpensOnAnUndoThatCompletesItsColour()
        {
            // Moving the top 1 off reveals a hidden 1; putting it back completes colour 1.
            BoardState board = MakeBoard(
                MakeTube(2, H(1), R(1)),
                MakeEmptyTube(2),
                MakeVeiledTube(1, 4, R(2)));
            Move(board, 0, 1);
            Assert.That(board[2].IsCovered, Is.True, "test setup: nothing completed yet");

            _history.TryUndo();

            Assert.That(board[2].IsCovered, Is.False);
        }

        // --- Keys and locks --------------------------------------------------------------------

        [Test]
        public void Key_IsFoundWhenItsBallBecomesTheTopRun_AndOpensItsLock()
        {
            BoardState board = MakeBoard(
                MakeTube(4, R(2), K(1, 0), R(3)),
                MakeEmptyTube(4),
                MakeLockedTube(0, 4, R(4)));
            KeyFoundEventArgs key = default;
            LockOpenedEventArgs opened = default;
            board.KeyFound += e => key = e;
            board.LockOpened += e => opened = e;

            Move(board, 0, 1);

            Assert.That((key.TubeIndex, key.SlotIndex, key.LockId), Is.EqualTo((0, 1, 0)));
            Assert.That(board[0][1].HasKey, Is.False, "the key mark is gone from the ball");
            Assert.That(board[0][1].ColorId, Is.EqualTo(1), "the ball itself stays");
            Assert.That(opened.LockId, Is.EqualTo(0));
            Assert.That(opened.TubeIndices, Is.EqualTo(new[] { 2 }));
            Assert.That(board[2].IsCovered, Is.False);
        }

        [Test]
        public void Key_IsFoundWhenItIsPartOfALongerRun()
        {
            // After the move the run is [1, 1-with-key]; the key does not have to be on top.
            BoardState board = MakeBoard(
                MakeTube(4, R(2), K(1, 0), R(1), R(3)),
                MakeEmptyTube(4),
                MakeLockedTube(0, 4, R(4)));

            Move(board, 0, 1);

            Assert.That(board[2].IsCovered, Is.False);
        }

        [Test]
        public void Key_BelowADifferentColour_IsNotFound()
        {
            BoardState board = MakeBoard(
                MakeTube(4, K(1, 0), R(2), R(3)),
                MakeEmptyTube(4),
                MakeLockedTube(0, 4, R(4)));

            Move(board, 0, 1);

            Assert.That(board[0][0].HasKey, Is.True);
            Assert.That(board[2].IsCovered, Is.True);
        }

        [Test]
        public void Key_OnAHiddenBall_IsFoundOnlyOnceRevealed()
        {
            BoardState board = MakeBoard(
                MakeTube(4, HK(1, 0), H(2), R(3)),
                MakeEmptyTube(4),
                MakeEmptyTube(4),
                MakeLockedTube(0, 4, R(4)));

            Move(board, 0, 1);
            Assert.That(board[3].IsCovered, Is.True, "the key's ball is still hidden");

            Move(board, 0, 2);

            Assert.That(board[0].Top.IsRevealed, Is.True);
            Assert.That(board[0].Top.HasKey, Is.False);
            Assert.That(board[3].IsCovered, Is.False);
        }

        [Test]
        public void Key_InsideACoveredTube_IsFoundWhenTheTubeIsUncovered()
        {
            BoardState board = MakeBoard(
                MakeTube(4, R(0), R(0), R(0)),
                MakeTube(4, R(0)),
                MakeVeiledTube(0, 4, R(2), K(1, 0)),
                MakeLockedTube(0, 4, R(5)));
            Assert.That(board[2].Top.HasKey, Is.True, "test setup: a key on top of a veiled tube is not found");

            Move(board, 1, 0);

            Assert.That(board[2].IsCovered, Is.False);
            Assert.That(board[2].Top.HasKey, Is.False);
            Assert.That(board[3].IsCovered, Is.False);
        }

        [Test]
        public void Lock_SpanningSeveralTubes_OpensThemAll()
        {
            BoardState board = MakeBoard(
                MakeTube(4, R(2), K(1, 7), R(3)),
                MakeEmptyTube(4),
                MakeLockedTube(7, 4, R(4)),
                MakeLockedTube(7, 4),
                MakeLockedTube(7, 4, R(5)));
            LockOpenedEventArgs opened = default;
            int lockEvents = 0;
            board.LockOpened += e =>
            {
                opened = e;
                lockEvents++;
            };

            Move(board, 0, 1);

            Assert.That(lockEvents, Is.EqualTo(1), "one event per lock, not per tube");
            Assert.That(opened.TubeIndices, Is.EqualTo(new[] { 2, 3, 4 }));
            Assert.That(board[2].IsCovered || board[3].IsCovered || board[4].IsCovered, Is.False);
        }

        [Test]
        public void Lock_OnlyItsOwnKeyOpensIt()
        {
            BoardState board = MakeBoard(
                MakeTube(4, R(2), K(1, 0), R(3)),
                MakeEmptyTube(4),
                MakeLockedTube(1, 4, R(4)));

            Move(board, 0, 1);

            Assert.That(board[2].Cover, Is.EqualTo(TubeCover.Lock(1)));
        }

        [Test]
        public void OpeningALock_CanRevealTheKeyToTheNextLock()
        {
            BoardState board = MakeBoard(
                MakeTube(4, R(2), K(1, 0), R(3)),
                MakeEmptyTube(4),
                MakeLockedTube(0, 4, R(5), K(6, 1)),
                MakeLockedTube(1, 4, R(7)));
            List<string> log = RecordEvents(board);

            Move(board, 0, 1);

            Assert.That(board[3].IsCovered, Is.False);
            Assert.That(log, Is.EqualTo(new[]
            {
                nameof(BoardState.MoveApplied), "KeyFound(0)", "LockOpened(0)", "KeyFound(1)", "LockOpened(1)",
            }));
        }

        [Test]
        public void Key_InAClayTube_IsNeverFound()
        {
            // Clay is never a move source, so its balls are never a selectable run.
            BoardState board = MakeBoard(
                MakeClayTube(5, K(1, 0)),
                MakeTube(4, R(2)),
                MakeEmptyTube(4),
                MakeLockedTube(0, 4, R(4)));

            Move(board, 1, 2);

            Assert.That(board[3].IsCovered, Is.True);
        }

        [Test]
        public void Undo_AfterALockOpened_LeavesItOpenAndTheKeyGone()
        {
            BoardState board = MakeBoard(
                MakeTube(4, R(2), K(1, 0), R(3)),
                MakeEmptyTube(4),
                MakeLockedTube(0, 4, R(4)));
            Move(board, 0, 1);
            List<string> log = RecordEvents(board);

            _history.TryUndo();

            Assert.That(board[0].Top, Is.EqualTo(R(3)), "the ball is back on top of the key's ball");
            Assert.That(board[0][1].HasKey, Is.False);
            Assert.That(board[2].IsCovered, Is.False, "opened covers are never put back");
            Assert.That(log, Is.EqualTo(new[] { nameof(BoardState.MoveUndone) }), "and nothing reopens");
        }

        [Test]
        public void Clone_CarriesCoversAndKeys_AndSettlesThemSilently()
        {
            BoardState board = MakeBoard(
                MakeTube(4, R(2), K(1, 0), R(3)),
                MakeEmptyTube(4),
                MakeLockedTube(0, 4, R(4)));
            List<string> log = RecordEvents(board);
            BoardState clone = board.Clone();

            Assert.That(clone[2].Cover, Is.EqualTo(TubeCover.Lock(0)));
            new CommandHistory().TryExecuteMove(clone, 0, 1, out _);

            Assert.That(clone[2].IsCovered, Is.False, "the clone plays by the same rules");
            Assert.That(board[2].IsCovered, Is.True, "and never touches the original");
            Assert.That(log, Is.Empty);
        }

        // --- Event order -----------------------------------------------------------------------

        [Test]
        public void AMoveThatTriggersEverything_FiresInTheDocumentedOrder()
        {
            BoardState board = MakeBoard(
                MakeTube(4, R(3), H(4), R(0)),              // 0: moving the 0 reveals the 4
                MakeTube(4, R(0), R(0), R(0)),              // 1: completes colour 0
                MakeVeiledTube(0, 4, R(5), K(6, 0)),        // 2: opens; its top run holds lock 0's key
                MakeLockedTube(0, 4, R(7)));                // 3: opens
            List<string> log = RecordEvents(board);

            Move(board, 0, 1);

            Assert.That(log, Is.EqualTo(new[]
            {
                nameof(BoardState.MoveApplied),
                nameof(BoardState.BallRevealed),
                nameof(BoardState.TubeCompleted),
                "TubeUnveiled(2)",
                "KeyFound(0)",
                "LockOpened(0)",
            }));
        }

        [Test]
        public void AWinThatOpensACover_FiresLevelCompletedLast()
        {
            BoardState board = MakeBoard(
                MakeTube(4, R(0), R(0), R(0)),
                MakeTube(4, R(0)),
                MakeVeiledTube(0, 4));
            List<string> log = RecordEvents(board);

            Move(board, 1, 0);

            Assert.That(log, Is.EqualTo(new[]
            {
                nameof(BoardState.MoveApplied), nameof(BoardState.TubeCompleted), "TubeUnveiled(2)",
                nameof(BoardState.LevelCompleted),
            }));
        }

        [Test]
        public void SubscribersSeeTheSettledBoard_OnMoveApplied()
        {
            BoardState board = MakeBoard(
                MakeTube(4, R(2), K(1, 0), R(3)),
                MakeEmptyTube(4),
                MakeLockedTube(0, 4, R(4)));
            bool coveredAtEvent = true;
            board.MoveApplied += _ => coveredAtEvent = board[2].IsCovered;

            Move(board, 0, 1);

            Assert.That(coveredAtEvent, Is.False);
        }
    }
}
