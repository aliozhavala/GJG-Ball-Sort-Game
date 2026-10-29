using System;
using BallSort.Core.Commands;
using BallSort.Core.Levels;
using BallSort.Core.Model;
using NUnit.Framework;
using static BallSort.Tests.EditMode.TestLevels;

namespace BallSort.Tests.EditMode
{
    [TestFixture]
    public sealed class LevelFactoryTests
    {
        [Test]
        public void Load_KeepsTheFileAndItsHeader()
        {
            LevelDto file = Valid().ToFile(LevelTier.Hard);
            file.Tutorial = true;

            LoadedLevel level = LevelFactory.Load(file, Name);

            Assert.That(level.Name, Is.EqualTo(Name));
            Assert.That(level.Source, Is.SameAs(file));
            Assert.That(level.Id, Is.EqualTo(1));
            Assert.That(level.Tier, Is.EqualTo(LevelTier.Hard));
            Assert.That(level.IsTutorial, Is.True);
            Assert.That(level.VariantCount, Is.EqualTo(VariantSelector.VariantCount));
        }

        [Test]
        public void CreateBoard_BuildsTheBoardMatchingTheVariant()
        {
            TestLevel dto = Level(2,
                Hidden(4, new[] { 0, 1, 0, 1 }, new[] { true, true, false, false }),
                T(4, 1, 0, 1, 0), T(4));

            BoardState board = Load(dto).CreateBoard(1);

            Assert.That(board.TubeCount, Is.EqualTo(3));
            Assert.That(board[0][0], Is.EqualTo(new Ball(0, true)));
            Assert.That(board[0][1], Is.EqualTo(new Ball(1, true)));
            Assert.That(board[0][2], Is.EqualTo(new Ball(0)));
            Assert.That(board[0].Top, Is.EqualTo(new Ball(1)));
            Assert.That(board[1].Count, Is.EqualTo(4));
            Assert.That(board[2].IsEmpty, Is.True);
            Assert.That(board[2].Capacity, Is.EqualTo(4));
        }

        [Test]
        public void CreateBoard_PlaysTheRequestedVariant()
        {
            LevelDto file = Valid().ToFile();
            file.Variants[2].Tubes[0].Balls = new System.Collections.Generic.List<int> { 1, 0, 1, 0 };
            file.Variants[2].Tubes[1].Balls = new System.Collections.Generic.List<int> { 0, 1, 0, 1 };
            LoadedLevel level = LevelFactory.Load(file, Name);

            Assert.That(level.CreateBoard(1)[0][0].ColorId, Is.EqualTo(0));
            Assert.That(level.CreateBoard(3)[0][0].ColorId, Is.EqualTo(1));
            Assert.That(level.GetVariant(3), Is.SameAs(file.Variants[2]));
        }

        [TestCase(0)]
        [TestCase(6)]
        public void CreateBoard_AVariantTheLevelDoesNotHave_Throws(int variant)
        {
            LoadedLevel level = Load(Valid());

            Assert.Throws<ArgumentOutOfRangeException>(() => level.CreateBoard(variant));
        }

        [Test]
        public void CreateGrid_BuildsTheSharedGrid()
        {
            BoardGrid grid = Load(Valid().WithGrid(new int?[] { 0, 1 }, new int?[] { 2 })).CreateGrid();

            Assert.That(grid.RowCount, Is.EqualTo(2));
            Assert.That(grid.TubeCount, Is.EqualTo(3));
            Assert.That(grid.GetPlacement(2).Row, Is.EqualTo(1));
            Assert.That(grid.IsAligned(0) || grid.IsAligned(1), Is.False);
        }

        [Test]
        public void CreateBoard_BuildsCoversClayAndKeys()
        {
            TestLevel dto = Level(3,
                    Hidden(4, new[] { 1, 2, 0, 0 }, new[] { false, true, false, false }).WithKey(1, 4),
                    T(4, 0).AsClay(),
                    T(4, 2, 1, 2, 1).WithVeil(0),
                    T(4, 0, 2, 1),
                    T(4))
                .WithLock(4, 3, 4);

            BoardState board = Board(dto);

            Assert.That(board[0][1], Is.EqualTo(new Ball(2, true, 4)), "the key sits on its ball, which stays hidden");
            Assert.That(board[0][2].HasKey, Is.False);
            Assert.That(board[1].IsClay, Is.True);
            Assert.That(board[1].IsCovered, Is.False);
            Assert.That(board[2].Cover, Is.EqualTo(TubeCover.Veil(0)));
            Assert.That(board[3].Cover, Is.EqualTo(TubeCover.Lock(4)));
            Assert.That(board[4].Cover, Is.EqualTo(TubeCover.Lock(4)), "a lock covers every tube it lists");
            Assert.That(board[0].IsCovered || board[0].IsClay, Is.False);
        }

        [Test]
        public void CreateGrid_RebuildsWithoutAddedTubes()
        {
            LoadedLevel level = Load(Valid());
            BoardGrid played = level.CreateGrid();
            played.TryAddTube(out _, out _);

            BoardGrid fresh = level.CreateGrid();

            Assert.That(fresh, Is.Not.SameAs(played));
            Assert.That(fresh.GetCellCount(0), Is.EqualTo(3));
        }

        [Test]
        public void Load_InvalidLevel_ThrowsWithAllErrors()
        {
            LevelDto file = Valid().ToFile();
            file.Id = 0;
            file.Variants[1].Tubes[2].Capacity = 0;

            var e = Assert.Throws<LevelFormatException>(() => LevelFactory.Load(file, Name));

            Assert.That(e.LevelName, Is.EqualTo(Name));
            Assert.That(e.Errors.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(e.Message, Does.Contain(Name));
        }

        [Test]
        public void CreateBoard_RebuildsWithBallsHiddenAgain()
        {
            TestLevel dto = Level(2,
                Hidden(4, new[] { 0, 1, 0, 1 }, new[] { true, true, true, false }),
                T(4, 1, 0, 1, 0), T(4));
            LoadedLevel level = Load(dto);
            BoardState played = level.CreateBoard(1);

            // Moving the top ball reveals the one beneath it in the played board.
            Assert.That(new CommandHistory().TryExecuteMove(played, 0, 2, out _), Is.True);
            Assert.That(played[0].Top.IsRevealed, Is.True);

            BoardState restarted = level.CreateBoard(1);

            Assert.That(restarted[0].Count, Is.EqualTo(4));
            Assert.That(restarted[0][2].IsHidden, Is.True);
            Assert.That(restarted[2].IsEmpty, Is.True);
        }
    }
}
