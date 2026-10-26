using BallSort.Core.Model;
using NUnit.Framework;
using static BallSort.Tests.EditMode.TestBoards;

namespace BallSort.Tests.EditMode
{
    [TestFixture]
    public sealed class WinConditionTests
    {
        [Test]
        public void AllTubesCompleted_IsWon()
        {
            BoardState board = MakeBoard(
                MakeTube(2, R(1), R(1)),
                MakeTube(3, R(2), R(2), R(2)));

            Assert.That(board.IsWon, Is.True);
        }

        [Test]
        public void AMixOfCompletedAndEmptyTubes_IsWon()
        {
            BoardState board = MakeBoard(
                MakeTube(2, R(1), R(1)),
                MakeEmptyTube(4),
                MakeTube(2, R(2), R(2)));

            Assert.That(board.IsWon, Is.True);
        }

        [Test]
        public void AllTubesEmpty_IsWon()
        {
            // Degenerate but consistent with the rule. No such level will be generated.
            BoardState board = MakeBoard(MakeEmptyTube(4), MakeEmptyTube(4));

            Assert.That(board.IsWon, Is.True);
        }

        [Test]
        public void AMixedTube_IsNotWon()
        {
            BoardState board = MakeBoard(
                MakeTube(2, R(1), R(2)),
                MakeTube(2, R(2), R(1)));

            Assert.That(board.IsWon, Is.False);
        }

        [Test]
        public void ASingleColourTubeThatIsNotFull_IsNotWon()
        {
            // Sorted but not finished: the brief requires completed tubes, not merely tidy ones.
            BoardState board = MakeBoard(
                MakeTube(4, R(1), R(1)),
                MakeTube(4, R(1), R(1)));

            Assert.That(board.IsWon, Is.False);
        }

        [Test]
        public void ACompletedTubeStillHoldingHiddenBalls_CountsAsWon()
        {
            // Follows from the literal completion rule; see TubeTests.IsCompleted_IgnoresHiddenFlags.
            BoardState board = MakeBoard(MakeTube(3, H(1), H(1), R(1)), MakeEmptyTube(3));

            Assert.That(board.IsWon, Is.True);
        }

        // --- Locked tubes ----------------------------------------------------------------------

        [Test]
        public void ALockedMixedTube_BlocksTheWin()
        {
            // Locked tubes are not exempt from the win check. A level that locks a mixed tube must
            // therefore also provide a way to unlock it, or it is unwinnable by construction.
            BoardState board = MakeBoard(
                MakeTube(2, R(1), R(1)),
                MakeLockedTube(2, R(1), R(2)));

            Assert.That(board.IsWon, Is.False);
        }

        [Test]
        public void ALockedButCompletedTube_DoesNotBlockTheWin()
        {
            BoardState board = MakeBoard(
                MakeTube(2, R(1), R(1)),
                MakeLockedTube(2, R(2), R(2)));

            Assert.That(board.IsWon, Is.True);
        }

        [Test]
        public void ALockedEmptyTube_DoesNotBlockTheWin()
        {
            BoardState board = MakeBoard(MakeTube(2, R(1), R(1)), MakeLockedTube(4));

            Assert.That(board.IsWon, Is.True);
        }

        // --- Board construction ----------------------------------------------------------------

        [Test]
        public void ColorCount_IsDerivedFromTheHighestColourIdPresent()
        {
            BoardState board = MakeBoard(MakeTube(4, R(0), H(5)), MakeTube(4, R(2)));

            Assert.That(board.ColorCount, Is.EqualTo(6), "ids 0..5 are in play");
        }

        [Test]
        public void ColorCount_IsZeroForABoardWithNoBalls()
        {
            Assert.That(MakeBoard(MakeEmptyTube(4)).ColorCount, Is.EqualTo(0));
        }

        [Test]
        public void ABoardWithNoTubes_Throws()
        {
            Assert.That(() => MakeBoard(), Throws.ArgumentException);
        }

        [Test]
        public void Clone_ProducesAnIndependentBoard()
        {
            BoardState board = MakeBoard(MakeTube(4, R(1)), MakeEmptyTube(4));
            BoardState clone = board.Clone();

            clone[0].IsLocked = true;

            Assert.That(board[0].IsLocked, Is.False);
            Assert.That(clone.TubeCount, Is.EqualTo(2));
            Assert.That(clone[0].Count, Is.EqualTo(1));
        }
    }
}
