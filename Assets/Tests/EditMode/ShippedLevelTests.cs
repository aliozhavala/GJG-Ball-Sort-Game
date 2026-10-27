using System.Collections.Generic;
using System.IO;
using BallSort.Core.Levels;
using BallSort.Game.Services;
using NUnit.Framework;
using UnityEngine;

namespace BallSort.Tests.EditMode
{
    /// <summary>
    /// Every level file in the project must parse and pass validation. Solvability is checked by
    /// the solver once it exists (roadmap step 4).
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
        public void Level004_UsesExplicitRows()
        {
            Assert.That(LoadFile("level_004.json").Rows, Is.EqualTo(new[] { 4, 3 }));
        }

        [Test]
        public void Level005_FillsTwoFullRows()
        {
            Assert.That(LoadFile("level_005.json").Rows, Is.EqualTo(new[] { 6, 6 }));
        }
    }
}
