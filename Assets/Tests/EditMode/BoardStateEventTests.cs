using System.Collections.Generic;
using BallSort.Core.Commands;
using BallSort.Core.Model;
using BallSort.Core.Rules;
using NUnit.Framework;
using static BallSort.Tests.EditMode.TestBoards;

namespace BallSort.Tests.EditMode
{
    [TestFixture]
    public sealed class BoardStateEventTests
    {
        private static MoveCommand Move(BoardState board, int from, int to)
        {
            Assert.That(
                MoveRules.TryGetMove(board, from, to, out MoveInfo move),
                Is.True,
                $"test setup expected tube {from} -> tube {to} to be legal");
            return new MoveCommand(board, move);
        }

        /// <summary>Records event names in the order they arrive, so ordering can be asserted.</summary>
        private static List<string> RecordEvents(BoardState board)
        {
            var log = new List<string>();
            board.MoveApplied += _ => log.Add(nameof(BoardState.MoveApplied));
            board.BallRevealed += _ => log.Add(nameof(BoardState.BallRevealed));
            board.TubeCompleted += _ => log.Add(nameof(BoardState.TubeCompleted));
            board.TubeUnveiled += _ => log.Add(nameof(BoardState.TubeUnveiled));
            board.KeyFound += _ => log.Add(nameof(BoardState.KeyFound));
            board.LockOpened += _ => log.Add(nameof(BoardState.LockOpened));
            board.LevelCompleted += () => log.Add(nameof(BoardState.LevelCompleted));
            board.MoveUndone += _ => log.Add(nameof(BoardState.MoveUndone));
            return log;
        }

        // --- Ordering --------------------------------------------------------------------------

        [Test]
        public void AMoveThatRevealsAndCompletes_FiresAppliedThenRevealedThenCompleted()
        {
            BoardState board = MakeBoard(
                MakeTube(3, H(5), R(1), R(1)),
                MakeTube(3, R(1)));
            List<string> log = RecordEvents(board);

            Move(board, 0, 1).Execute();

            Assert.That(log, Is.EqualTo(new[]
            {
                nameof(BoardState.MoveApplied),
                nameof(BoardState.BallRevealed),
                nameof(BoardState.TubeCompleted),
            }));
        }

        [Test]
        public void AWinningMove_FiresAppliedThenCompletedThenLevelCompleted()
        {
            // The source empties, so there is nothing to reveal. A move can never both reveal in the
            // source and leave that source completed, since removing balls makes a full tube unfull.
            BoardState board = MakeBoard(
                MakeTube(3, R(1), R(1)),
                MakeTube(4, R(1), R(1)));
            List<string> log = RecordEvents(board);

            Move(board, 0, 1).Execute();

            Assert.That(board.IsWon, Is.True);
            Assert.That(log, Is.EqualTo(new[]
            {
                nameof(BoardState.MoveApplied),
                nameof(BoardState.TubeCompleted),
                nameof(BoardState.LevelCompleted),
            }));
        }

        [Test]
        public void AnOrdinaryMove_FiresOnlyMoveApplied()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeEmptyTube(4));
            List<string> log = RecordEvents(board);

            Move(board, 0, 1).Execute();

            Assert.That(log, Is.EqualTo(new[] { nameof(BoardState.MoveApplied) }));
        }

        [Test]
        public void EventsFireAfterAllMutation_SoSubscribersSeeAConsistentBoard()
        {
            BoardState board = MakeBoard(MakeTube(3, H(5), R(1), R(1)), MakeTube(3, R(1)));
            var sourceCountAtEvent = -1;
            var targetCompletedAtEvent = false;
            board.MoveApplied += _ =>
            {
                sourceCountAtEvent = board[0].Count;
                targetCompletedAtEvent = board.IsCompleted(1);
            };

            Move(board, 0, 1).Execute();

            Assert.That(sourceCountAtEvent, Is.EqualTo(1));
            Assert.That(targetCompletedAtEvent, Is.True, "the completion is already visible on MoveApplied");
        }

        // --- Payloads --------------------------------------------------------------------------

        [Test]
        public void MoveApplied_CarriesIndicesCountAndColour()
        {
            BoardState board = MakeBoard(MakeEmptyTube(4), MakeTube(4, R(2), R(7), R(7)));
            MoveEventArgs received = default;
            board.MoveApplied += args => received = args;

            Move(board, 1, 0).Execute();

            Assert.That(received.FromIndex, Is.EqualTo(1));
            Assert.That(received.ToIndex, Is.EqualTo(0));
            Assert.That(received.Count, Is.EqualTo(2));
            Assert.That(received.ColorId, Is.EqualTo(7));
        }

        [Test]
        public void MoveApplied_ReportsThePartialCountNotTheRunLength()
        {
            BoardState board = MakeBoard(
                MakeTube(4, R(1), R(1), R(1)),
                MakeTube(4, R(1), R(1)));
            MoveEventArgs received = default;
            board.MoveApplied += args => received = args;

            Move(board, 0, 1).Execute();

            Assert.That(received.Count, Is.EqualTo(2), "run length was 3, but only 2 fit");
        }

        [Test]
        public void BallRevealed_CarriesTubeSlotAndTheNewlyVisibleColour()
        {
            BoardState board = MakeBoard(MakeTube(3, H(5), H(6), R(1)), MakeEmptyTube(4));
            BallRevealedEventArgs received = default;
            board.BallRevealed += args => received = args;

            Move(board, 0, 1).Execute();

            Assert.That(received.TubeIndex, Is.EqualTo(0));
            Assert.That(received.SlotIndex, Is.EqualTo(1), "the source's new top slot");
            Assert.That(received.ColorId, Is.EqualTo(6));
        }

        [Test]
        public void TubeCompleted_CarriesTheTubeAndItsColour()
        {
            BoardState board = MakeBoard(MakeTube(4, R(3)), MakeTube(2, R(3)));
            TubeCompletedEventArgs received = default;
            board.TubeCompleted += args => received = args;

            Move(board, 0, 1).Execute();

            Assert.That(received.TubeIndex, Is.EqualTo(1));
            Assert.That(received.ColorId, Is.EqualTo(3));
        }

        // --- Undo ------------------------------------------------------------------------------

        [Test]
        public void Undo_FiresMoveUndoneWithTheOriginalMovesIndices()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1), R(1)), MakeEmptyTube(4));
            MoveCommand command = Move(board, 0, 1);
            command.Execute();

            MoveEventArgs received = default;
            var count = 0;
            board.MoveUndone += args =>
            {
                received = args;
                count++;
            };

            command.Undo();

            Assert.That(count, Is.EqualTo(1));
            Assert.That(received.FromIndex, Is.EqualTo(0), "the indices still describe the original move");
            Assert.That(received.ToIndex, Is.EqualTo(1));
            Assert.That(received.Count, Is.EqualTo(2));
            Assert.That(received.ColorId, Is.EqualTo(1));
        }

        [Test]
        public void Undo_DoesNotFireBallRevealedAgain()
        {
            BoardState board = MakeBoard(MakeTube(3, H(5), R(1)), MakeEmptyTube(4));
            MoveCommand command = Move(board, 0, 1);
            command.Execute();

            List<string> log = RecordEvents(board);
            command.Undo();

            Assert.That(log, Is.EqualTo(new[] { nameof(BoardState.MoveUndone) }));
        }

        // --- Clones are silent -----------------------------------------------------------------

        [Test]
        public void Clone_DoesNotInheritSubscribers()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeEmptyTube(4));
            List<string> log = RecordEvents(board);

            BoardState clone = board.Clone();
            Move(clone, 0, 1).Execute();

            Assert.That(log, Is.Empty, "solver copies must never notify the view layer");
            Assert.That(board[0].Count, Is.EqualTo(1), "and must never touch the real board");
        }

        // --- Undo that completes a tube --------------------------------------------------------

        [Test]
        public void AnUndoThatCompletesTheSource_FiresUndoneThenCompleted()
        {
            BoardState board = MakeBoard(MakeTube(2, H(1), R(1)), MakeEmptyTube(2), MakeTube(2, R(4)));
            MoveCommand command = Move(board, 0, 1);
            command.Execute();                               // reveals tube 0's bottom ball
            List<string> log = RecordEvents(board);
            TubeCompletedEventArgs completed = default;
            board.TubeCompleted += e => completed = e;

            command.Undo();

            Assert.That(log, Is.EqualTo(new[] { nameof(BoardState.MoveUndone), nameof(BoardState.TubeCompleted) }));
            Assert.That(completed.TubeIndex, Is.EqualTo(0));
            Assert.That(completed.ColorId, Is.EqualTo(1));
            Assert.That(command.UndoCompletedTube, Is.True);
        }

        [Test]
        public void AnUndoThatCompletesTheLastTube_AlsoFiresLevelCompleted()
        {
            BoardState board = MakeBoard(MakeTube(2, H(1), R(1)), MakeEmptyTube(2));
            MoveCommand command = Move(board, 0, 1);
            command.Execute();
            List<string> log = RecordEvents(board);

            command.Undo();

            Assert.That(log, Is.EqualTo(new[]
            {
                nameof(BoardState.MoveUndone), nameof(BoardState.TubeCompleted), nameof(BoardState.LevelCompleted),
            }));
        }
    }
}
