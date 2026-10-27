using System;
using BallSort.Core.Commands;
using BallSort.Core.Model;
using NUnit.Framework;
using static BallSort.Tests.EditMode.TestBoards;

namespace BallSort.Tests.EditMode
{
    [TestFixture]
    public sealed class TubeTests
    {
        [Test]
        public void EmptyTube_ReportsEmptyAndFullCapacityFree()
        {
            Tube tube = MakeEmptyTube(4);

            Assert.That(tube.Count, Is.EqualTo(0));
            Assert.That(tube.Capacity, Is.EqualTo(4));
            Assert.That(tube.FreeSpace, Is.EqualTo(4));
            Assert.That(tube.IsEmpty, Is.True);
            Assert.That(tube.IsFull, Is.False);
            Assert.That(tube.TryGetTop(out _), Is.False);
        }

        [Test]
        public void Top_OnEmptyTube_Throws()
        {
            Tube tube = MakeEmptyTube(4);

            Assert.That(() => tube.Top, Throws.InvalidOperationException);
        }

        [Test]
        public void Indexer_ReadsBottomToTop()
        {
            Tube tube = MakeTube(4, R(7), R(8), R(9));

            Assert.That(tube[0].ColorId, Is.EqualTo(7), "slot 0 is the bottom");
            Assert.That(tube[2].ColorId, Is.EqualTo(9), "the last filled slot is the top");
            Assert.That(tube.Top.ColorId, Is.EqualTo(9));
        }

        [Test]
        public void Indexer_BeyondFilledSlots_Throws()
        {
            Tube tube = MakeTube(4, R(1));

            Assert.That(() => tube[1], Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tube[-1], Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Constructor_WithMoreBallsThanCapacity_Throws()
        {
            Assert.That(() => MakeTube(2, R(0), R(0), R(0)), Throws.ArgumentException);
        }

        [Test]
        public void Constructor_WithNonPositiveCapacity_Throws()
        {
            Assert.That(() => MakeEmptyTube(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        // --- Run detection ---------------------------------------------------------------------

        [Test]
        public void RunLength_OnEmptyTube_IsZero()
        {
            Assert.That(MakeEmptyTube(4).RunLength, Is.EqualTo(0));
            Assert.That(MakeEmptyTube(4).TryGetRun(out _, out _), Is.False);
        }

        [Test]
        public void RunLength_CountsContiguousSameColourFromTop()
        {
            Tube tube = MakeTube(4, R(3), R(1), R(1), R(1));

            Assert.That(tube.RunLength, Is.EqualTo(3));
            Assert.That(tube.TryGetRun(out int colorId, out int length), Is.True);
            Assert.That(colorId, Is.EqualTo(1));
            Assert.That(length, Is.EqualTo(3));
        }

        [Test]
        public void RunLength_StopsAtDifferentColour()
        {
            Tube tube = MakeTube(4, R(1), R(1), R(2));

            Assert.That(tube.RunLength, Is.EqualTo(1));
        }

        [Test]
        public void RunLength_IsZeroWhenTopBallIsHidden()
        {
            Tube tube = MakeTube(4, R(1), H(1));

            Assert.That(tube.RunLength, Is.EqualTo(0), "a hidden top ball cannot be picked up");
            Assert.That(tube.TryGetRun(out _, out _), Is.False);
        }

        [Test]
        public void RunLength_HiddenBallDoesNotJoinRunEvenWhenColourMatches()
        {
            // The hidden ball underneath is also colour 1, but the player cannot know that, so the
            // run must stop at it.
            Tube tube = MakeTube(4, R(1), H(1), R(1), R(1));

            Assert.That(tube.RunLength, Is.EqualTo(2));
        }

        [Test]
        public void RunLength_HiddenBallBelowARunIsNeverIncluded()
        {
            Tube tube = MakeTube(5, H(2), H(2), R(2), R(2), R(2));

            Assert.That(tube.RunLength, Is.EqualTo(3));
        }

        // --- Completion ------------------------------------------------------------------------

        [Test]
        public void IsCompleted_RequiresFullAndSingleColour()
        {
            Assert.That(MakeTube(4, R(1), R(1), R(1), R(1)).IsCompleted, Is.True);
            Assert.That(MakeTube(4, R(1), R(1), R(1), R(2)).IsCompleted, Is.False, "mixed colours");
            Assert.That(MakeTube(4, R(1), R(1), R(1)).IsCompleted, Is.False, "not full");
            Assert.That(MakeEmptyTube(4).IsCompleted, Is.False, "empty is not completed");
        }

        [Test]
        public void IsCompleted_RequiresEveryBallRevealed()
        {
            // A full single-colour tube that still conceals a ball is not finished: the player cannot
            // verify it, so it must not be corked behind their back. Reachable in play, because the
            // balls beneath a revealed top may happen to share its colour.
            Assert.That(MakeTube(4, H(1), H(1), R(1), R(1)).IsCompleted, Is.False, "hidden at the bottom");
            Assert.That(MakeTube(4, R(1), H(1), R(1), R(1)).IsCompleted, Is.False, "hidden in the middle");
            Assert.That(MakeTube(4, R(1), R(1), R(1), R(1)).IsCompleted, Is.True, "all revealed");
        }

        [Test]
        public void SingleBallTube_AtCapacityOne_IsCompleted()
        {
            Assert.That(MakeTube(1, R(5)).IsCompleted, Is.True);
        }

        // --- Cloning ---------------------------------------------------------------------------

        [Test]
        public void Clone_CopiesContentsCapacityAndLockState()
        {
            Tube original = MakeLockedTube(4, R(1), H(2));
            Tube clone = original.Clone();

            Assert.That(clone.Capacity, Is.EqualTo(4));
            Assert.That(clone.Count, Is.EqualTo(2));
            Assert.That(clone.IsLocked, Is.True);
            Assert.That(clone[0], Is.EqualTo(R(1)));
            Assert.That(clone[1], Is.EqualTo(H(2)));
        }

        [Test]
        public void Clone_IsIndependentOfTheOriginal()
        {
            // Mutate the original through the only public path, a move on its board.
            Tube original = MakeTube(4, R(1));
            Tube clone = original.Clone();
            BoardState board = MakeBoard(original, MakeEmptyTube(4));

            new CommandHistory().TryExecuteMove(board, 0, 1, out _);

            Assert.That(original.IsEmpty, Is.True);
            Assert.That(clone.Count, Is.EqualTo(1));
        }
    }
}
