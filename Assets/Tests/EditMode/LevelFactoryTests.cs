using System.Collections.Generic;
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
        public void Load_ResolvesAutoRows()
        {
            LoadedLevel level = LevelFactory.Load(Valid(), Name);

            Assert.That(level.Rows, Is.EqualTo(new[] { 3 }));
        }

        [Test]
        public void Load_KeepsExplicitRows()
        {
            LevelDto dto = Valid();
            dto.Layout = new LayoutDto { Rows = new List<int> { 2, 1 } };

            LoadedLevel level = LevelFactory.Load(dto, Name);

            Assert.That(level.Rows, Is.EqualTo(new[] { 2, 1 }));
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
