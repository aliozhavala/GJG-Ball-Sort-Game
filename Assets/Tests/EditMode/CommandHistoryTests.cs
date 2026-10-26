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
            Assert.That(history.UndoChargesRemaining, Is.EqualTo(CommandHistory.DefaultUndoCharges));
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
        public void TryUndo_RevertsTheMostRecentMoveAndSpendsACharge()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeEmptyTube(4));
            var history = new CommandHistory(undoCharges: 5);
            history.TryExecuteMove(board, 0, 1, out _);

            Assert.That(history.TryUndo(), Is.True);

            Assert.That(board[0].Count, Is.EqualTo(1));
            Assert.That(board[1].IsEmpty, Is.True);
            Assert.That(history.MoveCount, Is.EqualTo(0));
            Assert.That(history.UndoChargesRemaining, Is.EqualTo(4));
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
            Assert.That(board[1].IsCompleted, Is.True, "the tube stays corked");
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

        // --- Charges ---------------------------------------------------------------------------

        [Test]
        public void UndoIsBlocked_OnceTheChargesRunOut()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeTube(4, R(2)), MakeEmptyTube(4));
            var history = new CommandHistory(undoCharges: 1);
            history.TryExecuteMove(board, 0, 2, out _);
            history.TryExecuteMove(board, 1, 0, out _);

            Assert.That(history.TryUndo(), Is.True);
            Assert.That(history.UndoChargesRemaining, Is.EqualTo(0));

            Assert.That(history.UndoBlockReason, Is.EqualTo(UndoBlockReason.NoChargesLeft));
            Assert.That(history.TryUndo(), Is.False);
            Assert.That(history.MoveCount, Is.EqualTo(1), "the remaining move is still applied");
        }

        [Test]
        public void AHistoryWithZeroCharges_NeverAllowsUndo()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeEmptyTube(4));
            var history = new CommandHistory(undoCharges: 0);
            history.TryExecuteMove(board, 0, 1, out _);

            Assert.That(history.UndoBlockReason, Is.EqualTo(UndoBlockReason.NoChargesLeft));
            Assert.That(history.TryUndo(), Is.False);
        }

        [Test]
        public void TheCompletionSeal_TakesPrecedenceOverTheChargeCount()
        {
            // Reporting "no charges left" when the real reason is a corked tube would mislead the
            // player into spending a booster that cannot help.
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeTube(2, R(1)));
            var history = new CommandHistory(undoCharges: 0);
            history.TryExecuteMove(board, 0, 1, out _);

            Assert.That(history.UndoBlockReason, Is.EqualTo(UndoBlockReason.SealedByTubeCompletion));
        }

        [Test]
        public void GrantUndoCharges_AddsToTheRemainingCount()
        {
            var history = new CommandHistory(undoCharges: 2);

            history.GrantUndoCharges(3);

            Assert.That(history.UndoChargesRemaining, Is.EqualTo(5));
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
        public void Clear_KeepsTheRemainingChargesUnlessGivenNewOnes()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeTube(4, R(2)), MakeEmptyTube(4));
            var history = new CommandHistory(undoCharges: 3);
            history.TryExecuteMove(board, 0, 2, out _);
            history.TryUndo();

            history.Clear();
            Assert.That(history.UndoChargesRemaining, Is.EqualTo(2));

            history.Clear(undoCharges: 10);
            Assert.That(history.UndoChargesRemaining, Is.EqualTo(10));
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
