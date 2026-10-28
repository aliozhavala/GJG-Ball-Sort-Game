using BallSort.Core.Commands;
using BallSort.Core.Model;
using BallSort.Core.Rules;
using NUnit.Framework;
using static BallSort.Tests.EditMode.TestBoards;

namespace BallSort.Tests.EditMode
{
    [TestFixture]
    public sealed class CommandHistoryTests
    {
        [Test]
        public void NewHistory_HasNothingToUndo()
        {
            var history = new CommandHistory();

            Assert.That(history.MoveCount, Is.EqualTo(0));
            Assert.That(history.CanUndo, Is.False);
            Assert.That(history.UndoBlockReason, Is.EqualTo(UndoBlockReason.NoMovesToUndo));
            Assert.That(history.LastMove, Is.Null);
        }

        [Test]
        public void TryExecuteMove_AppliesAndRecordsALegalMove()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeEmptyTube(4));
            var history = new CommandHistory();

            Assert.That(history.TryExecuteMove(board, 0, 1, out MoveCommand command), Is.True);

            Assert.That(command, Is.Not.Null);
            Assert.That(history.MoveCount, Is.EqualTo(1));
            Assert.That(history.LastMove, Is.SameAs(command));
            Assert.That(board[1].Count, Is.EqualTo(1));
        }

        [Test]
        public void TryExecuteMove_RecordsNothingWhenTheMoveIsIllegal()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeTube(4, R(2)));
            var history = new CommandHistory();

            Assert.That(
                history.TryExecuteMove(board, 0, 1, out MoveCommand command, out MoveRejection rejection),
                Is.False);

            Assert.That(command, Is.Null);
            Assert.That(rejection, Is.EqualTo(MoveRejection.ColorMismatch));
            Assert.That(history.MoveCount, Is.EqualTo(0));
            Assert.That(board[0].Count, Is.EqualTo(1), "the board is untouched");
        }

        [Test]
        public void TryUndo_RevertsTheMostRecentMove()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeEmptyTube(4));
            var history = new CommandHistory();
            history.TryExecuteMove(board, 0, 1, out _);

            Assert.That(history.TryUndo(), Is.True);

            Assert.That(board[0].Count, Is.EqualTo(1));
            Assert.That(board[1].IsEmpty, Is.True);
            Assert.That(history.MoveCount, Is.EqualTo(0));
        }

        [Test]
        public void TryUndo_UnwindsMovesInReverseOrder()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeTube(4, R(2)), MakeEmptyTube(4));
            var history = new CommandHistory();
            history.TryExecuteMove(board, 0, 2, out _);   // colour 1 -> empty tube
            history.TryExecuteMove(board, 1, 0, out _);   // colour 2 -> now-empty tube 0

            history.TryUndo();

            Assert.That(board[1].Count, Is.EqualTo(1), "the second move was undone first");
            Assert.That(board[2].Count, Is.EqualTo(1), "the first move is still applied");
            Assert.That(history.MoveCount, Is.EqualTo(1));
        }

        // --- The completion seal ---------------------------------------------------------------

        [Test]
        public void AMoveThatCompletesATube_CannotBeUndone()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeTube(2, R(1)));
            var history = new CommandHistory();

            history.TryExecuteMove(board, 0, 1, out MoveCommand command);

            Assert.That(command.CompletedTube, Is.True);
            Assert.That(history.CanUndo, Is.False);
            Assert.That(history.UndoBlockReason, Is.EqualTo(UndoBlockReason.SealedByTubeCompletion));
            Assert.That(history.TryUndo(), Is.False);
            Assert.That(board.IsCompleted(1), Is.True, "the tube stays corked");
        }

        [Test]
        public void TheCompletionSeal_AlsoLocksAwayEveryEarlierMove()
        {
            // Move 1 is ordinary, move 2 completes a tube. Because undo is LIFO, move 1 is now
            // unreachable too — it is behind the seal for good.
            BoardState board = MakeBoard(
                MakeTube(4, R(5)),        // 0: filler, gives us a harmless first move
                MakeEmptyTube(4),         // 1
                MakeTube(4, R(1)),        // 2
                MakeTube(2, R(1)));       // 3: completes when it receives one colour-1 ball
            var history = new CommandHistory();

            history.TryExecuteMove(board, 0, 1, out _);   // ordinary move
            history.TryExecuteMove(board, 2, 3, out _);   // completes tube 3

            Assert.That(history.MoveCount, Is.EqualTo(2));
            Assert.That(history.UndoBlockReason, Is.EqualTo(UndoBlockReason.SealedByTubeCompletion));
            Assert.That(history.TryUndo(), Is.False);
            Assert.That(board[1].Count, Is.EqualTo(1), "the first move stays applied as well");
        }

        [Test]
        public void MovesMadeAfterACompletion_AreUndoableDownToTheSeal()
        {
            BoardState board = MakeBoard(
                MakeTube(4, R(1)),        // 0
                MakeTube(2, R(1)),        // 1: completes on the first move
                MakeTube(4, R(7)),        // 2
                MakeEmptyTube(4));        // 3
            var history = new CommandHistory();

            history.TryExecuteMove(board, 0, 1, out _);   // completes tube 1, seals the history
            history.TryExecuteMove(board, 2, 3, out _);   // ordinary move after the seal

            Assert.That(history.CanUndo, Is.True, "the post-seal move is undoable");
            Assert.That(history.TryUndo(), Is.True);
            Assert.That(board[2].Count, Is.EqualTo(1));

            Assert.That(history.UndoBlockReason, Is.EqualTo(UndoBlockReason.SealedByTubeCompletion),
                "and now we are back at the seal");
            Assert.That(history.TryUndo(), Is.False);
        }

        [Test]
        public void MoveCount_KeepsCountingSealedMoves()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeTube(2, R(1)));
            var history = new CommandHistory();
            history.TryExecuteMove(board, 0, 1, out _);

            history.TryUndo();

            Assert.That(history.MoveCount, Is.EqualTo(1), "a refused undo does not shrink the move counter");
        }

        // --- Undo that completes a tube ---------------------------------------------------------

        [Test]
        public void AnUndoThatCompletesATube_SealsTheHistory()
        {
            // Moving the run off reveals a same-coloured ball; putting it back fills a single-colour
            // tube with nothing hidden, which corks it just as a move would.
            BoardState board = MakeBoard(
                MakeTube(4, R(5)),                    // 0: filler, gives us an earlier move
                MakeEmptyTube(4),                     // 1
                MakeTube(4, H(1), R(1), R(1), R(1)),  // 2: full, one colour, but not complete
                MakeEmptyTube(4));                    // 3
            var history = new CommandHistory();
            history.TryExecuteMove(board, 0, 1, out _);
            history.TryExecuteMove(board, 2, 3, out MoveCommand reveal);   // reveals tube 2's bottom ball

            Assert.That(history.TryUndo(), Is.True);

            Assert.That(reveal.UndoCompletedTube, Is.True);
            Assert.That(board.IsCompleted(2), Is.True);
            Assert.That(history.UndoBlockReason, Is.EqualTo(UndoBlockReason.SealedByTubeCompletion));
            Assert.That(history.TryUndo(), Is.False);
            Assert.That(board[1].Count, Is.EqualTo(1), "the earlier move is behind the seal");
        }

        [Test]
        public void AfterAnUndoCorksATube_NoFurtherUndoCanTakeBallsOutOfIt()
        {
            // Move 1 fills tube 0, but it hides a ball, so it is not complete. Move 2 lifts the run
            // off and reveals that ball. Undoing move 2 corks tube 0. Undoing move 1 would then pull
            // a ball back out of the corked tube, so it must be refused.
            BoardState board = MakeBoard(
                MakeTube(4, H(1), R(1), R(1)),        // 0: becomes full with a hidden ball
                MakeTube(4, R(1)),                    // 1: supplies the fourth ball
                MakeEmptyTube(4));                    // 2
            var history = new CommandHistory();
            history.TryExecuteMove(board, 1, 0, out MoveCommand fill);
            Assert.That(fill.CompletedTube, Is.False, "test setup: the hidden ball keeps tube 0 open");
            history.TryExecuteMove(board, 0, 2, out _);   // reveals tube 0's bottom ball

            Assert.That(history.TryUndo(), Is.True);
            Assert.That(board.IsCompleted(0), Is.True, "test setup: the undo corked tube 0");

            Assert.That(history.UndoBlockReason, Is.EqualTo(UndoBlockReason.SealedByTubeCompletion));
            Assert.That(history.TryUndo(), Is.False);
            Assert.That(board[0].Count, Is.EqualTo(4), "the corked tube keeps every ball");
            Assert.That(board.IsCompleted(0), Is.True);
            Assert.That(board[1].IsEmpty, Is.True, "the move that filled it stays applied");
        }

        [Test]
        public void AnOrdinaryUndo_DoesNotSeal()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeTube(4, R(2)), MakeEmptyTube(4));
            var history = new CommandHistory();
            history.TryExecuteMove(board, 0, 2, out _);
            history.TryExecuteMove(board, 1, 0, out MoveCommand second);

            history.TryUndo();

            Assert.That(second.UndoCompletedTube, Is.False);
            Assert.That(history.CanUndo, Is.True);
        }

        // --- Clearing for a new level ----------------------------------------------------------

        [Test]
        public void Clear_DropsTheLogAndLiftsTheSeal()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeTube(2, R(1)));
            var history = new CommandHistory();
            history.TryExecuteMove(board, 0, 1, out _);   // seals the history

            history.Clear();

            Assert.That(history.MoveCount, Is.EqualTo(0));
            Assert.That(history.UndoBlockReason, Is.EqualTo(UndoBlockReason.NoMovesToUndo));
            Assert.That(history.LastMove, Is.Null);
        }

        [Test]
        public void Clear_DoesNotTouchTheBoard()
        {
            // Restarting a level reloads it; clearing the history is only the bookkeeping half.
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeEmptyTube(4));
            var history = new CommandHistory();
            history.TryExecuteMove(board, 0, 1, out _);

            history.Clear();

            Assert.That(board[1].Count, Is.EqualTo(1));
        }
    }
}
