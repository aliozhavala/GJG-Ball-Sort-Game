using System.Collections.Generic;
using System.IO;
using BallSort.Core.Levels;
using BallSort.Core.Model;
using BallSort.Core.Player;
using BallSort.Core.Rules;
using BallSort.Core.Session;
using BallSort.LevelJson;
using NUnit.Framework;
using UnityEngine;

namespace BallSort.Tests.EditMode
{
    /// <summary>
    /// Every level file in the project must parse and pass validation. Solvability is checked by
    /// the solver once it exists (roadmap 4b); until then the hand-made mechanic levels are played
    /// through here with a scripted solution.
    /// </summary>
    [TestFixture]
    public sealed class ShippedLevelTests
    {
        private static string LevelsFolder => Path.Combine(Application.dataPath, "Data", "Levels");

        private static IEnumerable<string> LevelFiles() =>
            Directory.GetFiles(LevelsFolder, "*.json");

        private static LoadedLevel LoadFile(string fileName)
        {
            string path = Path.Combine(LevelsFolder, fileName);
            string name = Path.GetFileNameWithoutExtension(path);
            return LevelFactory.Load(LevelJsonParser.Parse(File.ReadAllText(path), name), name);
        }

        private static LevelSession Play(string fileName, params (int From, int To)[] moves)
        {
            var session = new LevelSession(LoadFile(fileName), new BoosterInventory(0, 0, 0), addedTubeCapacity: 4);
            foreach (var (from, to) in moves)
            {
                Assert.That(session.TryMove(from, to, out MoveRejection rejection), Is.True,
                    $"{fileName}: {from} -> {to} was refused ({rejection}) on {session.Board}");
            }

            return session;
        }

        [TestCaseSource(nameof(LevelFiles))]
        public void LevelFile_ParsesAndValidates(string path)
        {
            Assert.DoesNotThrow(() => LoadFile(Path.GetFileName(path)));
        }

        [Test]
        public void LevelFile_IdMatchesFileNumber()
        {
            foreach (string path in LevelFiles())
            {
                string name = Path.GetFileNameWithoutExtension(path);
                LoadedLevel level = LoadFile(Path.GetFileName(path));

                Assert.That(name, Is.EqualTo($"level_{level.Source.Id:000}"), $"{name} has id {level.Source.Id}.");
            }
        }

        [Test]
        public void Level004_KeepsItsTwoRows()
        {
            BoardGrid grid = LoadFile("level_004.json").Grid;

            Assert.That((grid.RowCount, grid.GetCellCount(0), grid.GetCellCount(1)), Is.EqualTo((2, 4, 3)));
        }

        [Test]
        public void Level005_FillsTwoFullRows()
        {
            BoardGrid grid = LoadFile("level_005.json").Grid;

            Assert.That((grid.RowCount, grid.GetCellCount(0), grid.GetCellCount(1)), Is.EqualTo((2, 6, 6)));
        }

        // --- Hand-made mechanic levels ---------------------------------------------------------

        [Test]
        public void Level006_Veil_OpensWhenRedCompletes_AndIsSolvable()
        {
            LevelSession session = Play("level_006.json", (0, 2), (1, 0));
            Assert.That(session.Board[3].IsCovered, Is.False, "completing colour 0 opens the veil");

            session = Play("level_006.json", (0, 2), (1, 0), (1, 4), (2, 1), (3, 4), (3, 1), (4, 3));
            Assert.That(session.IsWon, Is.True);
        }

        [Test]
        public void Level007_LockAndKey_OpensOnTheFirstMove_AndIsSolvable()
        {
            LevelSession session = Play("level_007.json", (0, 2));
            Assert.That(session.Board[3].IsCovered || session.Board[4].IsCovered, Is.False, "the key was found");

            session = Play("level_007.json",
                (0, 2), (1, 2), (0, 4), (1, 0), (4, 1), (3, 0), (3, 1), (3, 0), (3, 1));
            Assert.That(session.IsWon, Is.True);
        }

        [Test]
        public void Level008_ClayAndALargeTube_AreLaidOutAndSolvable()
        {
            LoadedLevel level = LoadFile("level_008.json");
            Assert.That(level.Board[0].Capacity, Is.EqualTo(10));
            Assert.That(level.Board[0].IsClay, Is.True);
            GridPlacement large = level.Grid.GetPlacement(0);
            Assert.That((large.Row, large.Cell, large.RowSpan), Is.EqualTo((1, 0, 2)));
            Assert.That(level.Grid.IsAligned(0) && level.Grid.IsAligned(1), Is.True);

            LevelSession session = Play("level_008.json",
                (1, 0), (2, 0), (3, 0), (4, 0), (3, 2), (4, 2), (4, 1), (4, 2), (3, 1), (5, 1));
            Assert.That(session.IsWon, Is.True);
            Assert.That(session.Board.IsCompleted(0), Is.True, "the clay tube finished with all ten");
        }

        [Test]
        public void Level008_ClayTube_IsNeverASource()
        {
            LevelSession session = Play("level_008.json");

            Assert.That(session.CanSelect(0), Is.False);
            Assert.That(session.TryMove(0, 5, out MoveRejection rejection), Is.False);
            Assert.That(rejection, Is.EqualTo(MoveRejection.SourceClay));
        }
    }
}
