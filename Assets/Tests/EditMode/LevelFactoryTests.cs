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
        public void Load_BuildsBoardMatchingTheData()
        {
            LevelDto dto = Level(2,
                Hidden(4, new[] { 0, 1, 0, 1 }, new[] { true, true, false, false }),
                T(4, 1, 0, 1, 0), T(4));

            LoadedLevel level = LevelFactory.Load(dto, Name);
            BoardState board = level.Board;

            Assert.That(level.Name, Is.EqualTo(Name));
            Assert.That(level.Source, Is.SameAs(dto));
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
        public void Load_BuildsTheGrid()
        {
            LoadedLevel level = LevelFactory.Load(Valid().WithGrid(new int?[] { 0, 1 }, new int?[] { 2 }), Name);

            Assert.That(level.Grid.RowCount, Is.EqualTo(2));
            Assert.That(level.Grid.TubeCount, Is.EqualTo(3));
            Assert.That(level.Grid.GetPlacement(2).Row, Is.EqualTo(1));
            Assert.That(level.Grid.IsAligned(0) || level.Grid.IsAligned(1), Is.False);
        }

        [Test]
        public void Load_BuildsCoversClayAndKeys()
        {
            LevelDto dto = Level(3,
                    Hidden(4, new[] { 1, 2, 0, 0 }, new[] { false, true, false, false }).WithKey(1, 4),
                    T(4, 0).AsClay(),
                    T(4, 2, 1, 2, 1).WithVeil(0),
                    T(4, 0, 2, 1),
                    T(4))
                .WithLock(4, 3, 4);

            BoardState board = LevelFactory.Load(dto, Name).Board;

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
            LoadedLevel level = LevelFactory.Load(Valid(), Name);
            level.Grid.TryAddTube(out _, out _);

            BoardGrid fresh = LevelFactory.CreateGrid(level.Source);

            Assert.That(fresh, Is.Not.SameAs(level.Grid));
            Assert.That(fresh.GetCellCount(0), Is.EqualTo(3));
        }

        [Test]
        public void Load_InvalidLevel_ThrowsWithAllErrors()
        {
            LevelDto dto = Valid();
            dto.FormatVersion = 5;
            dto.Tubes[2].Capacity = 0;

            var e = Assert.Throws<LevelFormatException>(() => LevelFactory.Load(dto, Name));

            Assert.That(e.LevelName, Is.EqualTo(Name));
            Assert.That(e.Errors.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(e.Message, Does.Contain(Name));
        }

        [Test]
        public void CreateBoard_RebuildsWithBallsHiddenAgain()
        {
            LevelDto dto = Level(2,
                Hidden(4, new[] { 0, 1, 0, 1 }, new[] { true, true, true, false }),
                T(4, 1, 0, 1, 0), T(4));
            LoadedLevel level = LevelFactory.Load(dto, Name);

            // Moving the top ball reveals the one beneath it in the played board.
            Assert.That(new CommandHistory().TryExecuteMove(level.Board, 0, 2, out _), Is.True);
            Assert.That(level.Board[0].Top.IsRevealed, Is.True);

            BoardState restarted = LevelFactory.CreateBoard(level.Source);

            Assert.That(restarted[0].Count, Is.EqualTo(4));
            Assert.That(restarted[0][2].IsHidden, Is.True);
            Assert.That(restarted[2].IsEmpty, Is.True);
        }
    }
}
