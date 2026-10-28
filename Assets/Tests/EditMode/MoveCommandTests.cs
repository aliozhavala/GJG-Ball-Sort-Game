using BallSort.Core.Commands;
using BallSort.Core.Model;
using BallSort.Core.Rules;
using NUnit.Framework;
using static BallSort.Tests.EditMode.TestBoards;

namespace BallSort.Tests.EditMode
{
    [TestFixture]
    public sealed class MoveCommandTests
    {
        private static MoveCommand Move(BoardState board, int from, int to)
        {
            Assert.That(
                MoveRules.TryGetMove(board, from, to, out MoveInfo move),
                Is.True,
                $"test setup expected tube {from} -> tube {to} to be legal");
            return new MoveCommand(board, move);
        }

        [Test]
        public void Execute_MovesTheWholeRun()
        {
            BoardState board = MakeBoard(MakeTube(4, R(2), R(1), R(1)), MakeEmptyTube(4));

            Move(board, 0, 1).Execute();

            Assert.That(board[0].Count, Is.EqualTo(1));
            Assert.That(board[0].Top.ColorId, Is.EqualTo(2));
            Assert.That(board[1].Count, Is.EqualTo(2));
            Assert.That(board[1][0].ColorId, Is.EqualTo(1));
            Assert.That(board[1][1].ColorId, Is.EqualTo(1));
        }

        [Test]
        public void Execute_PreservesStackOrderOfADifferentlyColouredTarget()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeTube(4, R(2), R(1)));

            Move(board, 0, 1).Execute();

            Assert.That(board[1][0].ColorId, Is.EqualTo(2), "the target's bottom ball is untouched");
            Assert.That(board[1][1].ColorId, Is.EqualTo(1));
            Assert.That(board[1][2].ColorId, Is.EqualTo(1));
        }

        [Test]
        public void Execute_MovesOnlyWhatFits_AndLeavesTheRestBehind()
        {
            BoardState board = MakeBoard(
                MakeTube(4, R(1), R(1), R(1)),
                MakeTube(4, R(1), R(1)));

            MoveCommand command = Move(board, 0, 1);
            command.Execute();

            Assert.That(command.Count, Is.EqualTo(2));
            Assert.That(command.IsPartial, Is.True);
            Assert.That(board[0].Count, Is.EqualTo(1), "one ball of the run stayed behind");
            Assert.That(board[1].IsFull, Is.True);
        }

        // --- Reveal ----------------------------------------------------------------------------

        [Test]
        public void Execute_RevealsTheSourcesNewTopBall()
        {
            BoardState board = MakeBoard(MakeTube(4, H(2), H(3), R(1)), MakeEmptyTube(4));

            Move(board, 0, 1).Execute();

            Assert.That(board[0].Top.IsRevealed, Is.True);
            Assert.That(board[0].Top.ColorId, Is.EqualTo(3));
            Assert.That(board[0][0].IsHidden, Is.True, "only the new top ball is revealed");
        }

        [Test]
        public void Execute_RevealsNothingWhenTheSourceBecomesEmpty()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeEmptyTube(4));
            var revealed = 0;
            board.BallRevealed += _ => revealed++;

            Move(board, 0, 1).Execute();

            Assert.That(board[0].IsEmpty, Is.True);
            Assert.That(revealed, Is.EqualTo(0));
        }

        [Test]
        public void Execute_AfterAPartialMove_LeavesTheAlreadyRevealedTopAlone()
        {
            BoardState board = MakeBoard(
                MakeTube(4, R(1), R(1), R(1)),
                MakeTube(4, R(1), R(1)));
            var revealed = 0;
            board.BallRevealed += _ => revealed++;

            Move(board, 0, 1).Execute();

            Assert.That(revealed, Is.EqualTo(0), "the remaining ball was already revealed");
            Assert.That(board[0].Top.IsRevealed, Is.True);
        }

        // --- Completion flag -------------------------------------------------------------------

        [Test]
        public void Execute_FlagsAMoveThatCompletesTheTargetTube()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeTube(2, R(1)));

            MoveCommand command = Move(board, 0, 1);
            command.Execute();

            Assert.That(command.CompletedTube, Is.True);
            Assert.That(board.IsCompleted(1), Is.True);
        }

        [Test]
        public void Execute_DoesNotFlagAMoveThatLeavesTheTargetUnfinished()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeTube(4, R(1)));

            MoveCommand command = Move(board, 0, 1);
            command.Execute();

            Assert.That(command.CompletedTube, Is.False);
        }

        // --- Undo ------------------------------------------------------------------------------

        [Test]
        public void Undo_RestoresBallPositions()
        {
            BoardState board = MakeBoard(MakeTube(4, R(2), R(1), R(1)), MakeEmptyTube(4));
            MoveCommand command = Move(board, 0, 1);

            command.Execute();
            command.Undo();

            Assert.That(board[0].Count, Is.EqualTo(3));
            Assert.That(board[0][0].ColorId, Is.EqualTo(2));
            Assert.That(board[0][1].ColorId, Is.EqualTo(1));
            Assert.That(board[0][2].ColorId, Is.EqualTo(1));
            Assert.That(board[1].IsEmpty, Is.True);
        }

        [Test]
        public void Undo_LeavesRevealedBallsRevealed()
        {
            BoardState board = MakeBoard(MakeTube(4, H(2), H(3), R(1)), MakeEmptyTube(4));
            MoveCommand command = Move(board, 0, 1);

            command.Execute();
            command.Undo();

            Assert.That(board[0].Count, Is.EqualTo(3));
            Assert.That(board[0][1].IsRevealed, Is.True, "information already gained is not taken back");
            Assert.That(board[0][1].ColorId, Is.EqualTo(3));
            Assert.That(board[0][0].IsHidden, Is.True, "the still-buried ball stays hidden");
        }

        [Test]
        public void Undo_RestoresAPartialMove()
        {
            BoardState board = MakeBoard(
                MakeTube(4, R(1), R(1), R(1)),
                MakeTube(4, R(1), R(1)));
            MoveCommand command = Move(board, 0, 1);

            command.Execute();
            command.Undo();

            Assert.That(board[0].Count, Is.EqualTo(3));
            Assert.That(board[1].Count, Is.EqualTo(2));
        }

        [Test]
        public void ExecuteThenUndo_IsRepeatable()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeEmptyTube(4));
            MoveCommand command = Move(board, 0, 1);

            command.Execute();
            command.Undo();
            command.Execute();

            Assert.That(board[1].Count, Is.EqualTo(1));
        }

        [Test]
        public void ExecutingTwiceWithoutUndo_Throws()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeEmptyTube(4));
            MoveCommand command = Move(board, 0, 1);
            command.Execute();

            Assert.That(() => command.Execute(), Throws.InvalidOperationException);
        }

        [Test]
        public void UndoingBeforeExecuting_Throws()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeEmptyTube(4));
            MoveCommand command = Move(board, 0, 1);

            Assert.That(() => command.Undo(), Throws.InvalidOperationException);
        }
    }
}
