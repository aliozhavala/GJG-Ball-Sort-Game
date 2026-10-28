using BallSort.Core.Commands;
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
        public void AFullSingleColourTubeStillHoldingHiddenBalls_IsNotWon()
        {
            // Follows from the completion rule; see CompletionTests.HiddenBalls_BlockCompletion.
            BoardState board = MakeBoard(MakeTube(3, H(1), H(1), R(1)), MakeEmptyTube(3));

            Assert.That(board.IsWon, Is.False);
        }

        [Test]
        public void ASingleColourFullTubeMissingPartOfItsColour_IsNotWon()
        {
            // Both tubes are full and single-coloured, but neither holds all eight colour-1 balls.
            BoardState board = MakeBoard(
                MakeTube(4, R(1), R(1), R(1), R(1)),
                MakeTube(4, R(1), R(1), R(1), R(1)));

            Assert.That(board.IsWon, Is.False);
        }

        // --- Covered and clay tubes ------------------------------------------------------------

        [TestCase(CoverKind.Veil)]
        [TestCase(CoverKind.Lock)]
        public void ACoveredTubeHoldingBalls_BlocksTheWin(CoverKind kind)
        {
            // Covered tubes are not exempt: they must be opened and sorted like any other.
            Tube covered = kind == CoverKind.Veil
                ? MakeVeiledTube(1, 2, R(1), R(2))
                : MakeLockedTube(0, 2, R(1), R(2));
            BoardState board = MakeBoard(MakeTube(2, R(1), R(2)), covered);

            Assert.That(board.IsWon, Is.False);
        }

        [TestCase(CoverKind.Veil)]
        [TestCase(CoverKind.Lock)]
        public void ACoveredEmptyTube_CountsAsEmpty(CoverKind kind)
        {
            Tube covered = kind == CoverKind.Veil ? MakeVeiledTube(1, 4) : MakeLockedTube(0, 4);
            BoardState board = MakeBoard(MakeTube(2, R(1), R(1)), covered);

            Assert.That(board.IsWon, Is.True);
        }

        [Test]
        public void ACompletedClayTube_CountsAsCompleted()
        {
            BoardState board = MakeBoard(MakeClayTube(5, R(1), R(1), R(1), R(1), R(1)), MakeEmptyTube(4));

            Assert.That(board.IsWon, Is.True);
        }

        [Test]
        public void AnUnfinishedClayTube_BlocksTheWin()
        {
            BoardState board = MakeBoard(MakeClayTube(5, R(1)), MakeTube(4, R(1), R(1), R(1), R(1)));

            Assert.That(board.IsWon, Is.False);
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

            new CommandHistory().TryExecuteMove(clone, 0, 1, out _);

            Assert.That(board[0].Count, Is.EqualTo(1), "moving on the clone leaves the original alone");
            Assert.That(clone.TubeCount, Is.EqualTo(2));
            Assert.That(clone[1].Count, Is.EqualTo(1));
        }
    }
}
