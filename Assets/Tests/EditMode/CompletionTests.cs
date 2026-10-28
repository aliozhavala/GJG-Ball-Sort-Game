using BallSort.Core.Commands;
using BallSort.Core.Model;
using NUnit.Framework;
using static BallSort.Tests.EditMode.TestBoards;

namespace BallSort.Tests.EditMode
{
    /// <summary>
    /// Completion (docs/mechanics.md): full, every ball revealed, and holding every ball of its colour.
    /// </summary>
    [TestFixture]
    public sealed class CompletionTests
    {
        [Test]
        public void FullSingleColourTube_HoldingTheWholeColour_IsCompleted()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1), R(1), R(1), R(1)), MakeEmptyTube(4));

            Assert.That(board.IsCompleted(0), Is.True);
        }

        [Test]
        public void FourOfTenPurples_InACapacityFourTube_IsNotCompleted()
        {
            const int purple = 3;
            BoardState board = MakeBoard(
                MakeTube(4, R(purple), R(purple), R(purple), R(purple)),
                MakeTube(10, R(purple), R(purple), R(purple), R(purple), R(purple), R(purple)));

            Assert.That(board.GetBallCount(purple), Is.EqualTo(10));
            Assert.That(board.IsCompleted(0), Is.False, "the other six purples can still join them");
        }

        [Test]
        public void LargeTube_IsCompletedOnlyWithEveryBallOfItsColour()
        {
            BoardState nine = MakeBoard(MakeTube(10, Repeat(R(2), 9)), MakeTube(4, R(2)));
            BoardState ten = MakeBoard(MakeTube(10, Repeat(R(2), 10)), MakeEmptyTube(4));

            Assert.That(nine.IsCompleted(0), Is.False, "not full");
            Assert.That(ten.IsCompleted(0), Is.True);
        }

        [Test]
        public void MixedNotFullOrEmptyTubes_AreNotCompleted()
        {
            BoardState board = MakeBoard(
                MakeTube(4, R(1), R(1), R(1), R(2)),
                MakeTube(4, R(1), R(2), R(2), R(2)),
                MakeEmptyTube(4));

            Assert.That(board.IsCompleted(0), Is.False, "mixed colours");
            Assert.That(board.IsCompleted(2), Is.False, "empty is not completed");
            Assert.That(MakeBoard(MakeTube(4, R(1), R(1), R(1))).IsCompleted(0), Is.False, "not full");
        }

        [Test]
        public void HiddenBalls_BlockCompletion()
        {
            // A full single-colour tube that still conceals a ball is not finished: the player cannot
            // verify it, so it must not be corked behind their back.
            Assert.That(MakeBoard(MakeTube(4, H(1), H(1), R(1), R(1))).IsCompleted(0), Is.False, "hidden at the bottom");
            Assert.That(MakeBoard(MakeTube(4, R(1), H(1), R(1), R(1))).IsCompleted(0), Is.False, "hidden in the middle");
        }

        [Test]
        public void IsCompleted_WithAnInvalidIndex_Throws()
        {
            Assert.That(() => MakeBoard(MakeEmptyTube(4)).IsCompleted(1), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void FillingATubeWithPartOfAColour_DoesNotCompleteOrSeal()
        {
            // Eight colour-1 balls: tube 1 fills with four of them, which are still free to move.
            BoardState board = MakeBoard(
                MakeTube(4, R(1)),
                MakeTube(4, R(1), R(1), R(1)),
                MakeTube(4, R(1), R(1), R(1), R(1)));
            var history = new CommandHistory();
            bool completed = false;
            board.TubeCompleted += _ => completed = true;

            history.TryExecuteMove(board, 0, 1, out MoveCommand command);

            Assert.That(board[1].IsFull, Is.True);
            Assert.That(completed, Is.False);
            Assert.That(command.CompletedTube, Is.False);
            Assert.That(history.CanUndo, Is.True);
        }

        [Test]
        public void GatheringTheLastBall_CompletesAndSeals()
        {
            BoardState board = MakeBoard(MakeTube(4, R(2), R(1)), MakeTube(4, R(1), R(1), R(1)), MakeEmptyTube(4));
            var history = new CommandHistory();

            history.TryExecuteMove(board, 0, 1, out MoveCommand command);

            Assert.That(command.CompletedTube, Is.True);
            Assert.That(history.UndoBlockReason, Is.EqualTo(UndoBlockReason.SealedByTubeCompletion));
        }

        [Test]
        public void ColourTotals_AreCountedAtCreationAndCarriedByClones()
        {
            BoardState board = MakeBoard(MakeTube(4, R(0), H(1), R(1)), MakeTube(4, R(1)), MakeEmptyTube(4));
            BoardState clone = board.Clone();

            Assert.That(board.GetBallCount(0), Is.EqualTo(1));
            Assert.That(board.GetBallCount(1), Is.EqualTo(3));
            Assert.That(clone.GetBallCount(1), Is.EqualTo(3));
            Assert.That(clone.GetBallCount(7), Is.EqualTo(0), "an unused colour has no balls");
        }

        [Test]
        public void ColourTotals_DoNotChangeWhenBallsMove()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1), R(1)), MakeTube(4, R(1), R(1)));

            new CommandHistory().TryExecuteMove(board, 0, 1, out _);

            Assert.That(board.GetBallCount(1), Is.EqualTo(4));
            Assert.That(board.IsCompleted(1), Is.True);
        }

        private static Ball[] Repeat(Ball ball, int count)
        {
            var balls = new Ball[count];
            for (int i = 0; i < count; i++)
            {
                balls[i] = ball;
            }

            return balls;
        }
    }
}
