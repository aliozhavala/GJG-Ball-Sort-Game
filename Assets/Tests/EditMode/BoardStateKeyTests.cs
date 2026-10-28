using BallSort.Core.Model;
using BallSort.Core.Solver;
using NUnit.Framework;
using static BallSort.Tests.EditMode.TestBoards;

namespace BallSort.Tests.EditMode
{
    /// <summary>
    /// The solver's position key: equal exactly for the same position up to swapping
    /// interchangeable tubes (same capacity, clay flag and cover), different for anything else.
    /// </summary>
    [TestFixture]
    public sealed class BoardStateKeyTests
    {
        private static BoardStateKey Key(params Tube[] tubes) => BoardStateKey.Of(MakeBoard(tubes));

        [Test]
        public void SwappingInterchangeableTubes_GivesTheSameKey()
        {
            BoardStateKey a = Key(MakeTube(4, R(0), R(1)), MakeTube(4, R(1)), MakeEmptyTube(4));
            BoardStateKey b = Key(MakeTube(4, R(1)), MakeEmptyTube(4), MakeTube(4, R(0), R(1)));

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void SwappingCoveredTubesWithTheSameCover_GivesTheSameKey()
        {
            BoardStateKey a = Key(MakeLockedTube(0, 4, R(1)), MakeLockedTube(0, 4, R(2)), MakeTube(4, R(3)));
            BoardStateKey b = Key(MakeLockedTube(0, 4, R(2)), MakeLockedTube(0, 4, R(1)), MakeTube(4, R(3)));

            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void ACloneHasTheSameKey()
        {
            BoardState board = MakeBoard(MakeTube(4, H(2), K(1, 0), R(3)), MakeVeiledTube(1, 4, R(0)), MakeClayTube(5));

            Assert.That(BoardStateKey.Of(board.Clone()), Is.EqualTo(BoardStateKey.Of(board)));
        }

        [Test]
        public void TubesWithDifferentCovers_AreNotInterchangeable()
        {
            BoardStateKey a = Key(MakeLockedTube(0, 4, R(1)), MakeLockedTube(1, 4, R(2)));
            BoardStateKey b = Key(MakeLockedTube(0, 4, R(2)), MakeLockedTube(1, 4, R(1)));

            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test]
        public void EverythingThatCanChange_ChangesTheKey()
        {
            BoardStateKey reference = Key(MakeTube(4, R(0), R(1)), MakeEmptyTube(4));

            Assert.That(Key(MakeTube(4, R(1), R(0)), MakeEmptyTube(4)), Is.Not.EqualTo(reference), "ball order");
            Assert.That(Key(MakeTube(4, H(0), R(1)), MakeEmptyTube(4)), Is.Not.EqualTo(reference), "hidden flag");
            Assert.That(Key(MakeTube(4, K(0, 0), R(1)), MakeEmptyTube(4)), Is.Not.EqualTo(reference), "key");
            Assert.That(Key(MakeTube(4, R(0), R(1)), MakeVeiledTube(2, 4)), Is.Not.EqualTo(reference), "cover");
            Assert.That(Key(MakeTube(4, R(0), R(1)), MakeEmptyTube(4), MakeEmptyTube(4)), Is.Not.EqualTo(reference), "tube count");
            Assert.That(Key(MakeTube(4, R(0)), MakeTube(4, R(1))), Is.Not.EqualTo(reference), "a ball moved");
        }

        [Test]
        public void FixedTubePropertiesAreInTheKeyToo()
        {
            BoardStateKey reference = Key(MakeTube(4, R(0), R(1)), MakeEmptyTube(4));

            Assert.That(Key(MakeClayTube(4, R(0), R(1)), MakeEmptyTube(4)), Is.Not.EqualTo(reference), "clay");
            Assert.That(Key(MakeTube(5, R(0), R(1)), MakeEmptyTube(4)), Is.Not.EqualTo(reference), "capacity");
        }

        [Test]
        public void DefaultKeys_AreEqualToEachOther_AndToNoBoard()
        {
            Assert.That(default(BoardStateKey), Is.EqualTo(default(BoardStateKey)));
            Assert.That(Key(MakeEmptyTube(4)), Is.Not.EqualTo(default(BoardStateKey)));
        }

        [Test]
        public void LargeColourAndLockIds_GiveDistinctKeys()
        {
            // Varints: values past one byte must not collide with small ones.
            Assert.That(Key(MakeTube(4, R(200))), Is.Not.EqualTo(Key(MakeTube(4, R(72)))));
            Assert.That(Key(MakeTube(4, K(0, 300))), Is.Not.EqualTo(Key(MakeTube(4, K(0, 44)))));
        }
    }
}
