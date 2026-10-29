using System.Collections.Generic;
using System.IO;
using BallSort.Core.Levels;
using BallSort.Core.Player;
using BallSort.Core.Rules;
using BallSort.Core.Session;
using BallSort.Core.Solver;
using BallSort.LevelJson;
using NUnit.Framework;
using UnityEngine;

namespace BallSort.Tests.EditMode
{
    /// <summary>
    /// Every level file in the project must parse and pass validation, and every variant of it must be
    /// solvable: the fast solver must find a solution, and that solution must win when played through a
    /// session. Tests that need a level with specific content use the fixtures in <c>Assets/Data/TestLevels</c>
    /// (<see cref="TestLevelFileTests"/>), so product can retune the shipped levels freely.
    /// </summary>
    [TestFixture]
    public sealed class ShippedLevelTests
    {
        /// <summary>Generous for the fast solver: the delivered levels need at most about 600 nodes.</summary>
        private const int SolverNodeLimit = 200000;

        private static string LevelsFolder => Path.Combine(Application.dataPath, "Data", "Levels");

        private static IEnumerable<string> LevelFiles() =>
            Directory.GetFiles(LevelsFolder, "*.json");

        private static IEnumerable<TestCaseData> LevelVariants()
        {
            foreach (string path in LevelFiles())
            {
                string fileName = Path.GetFileName(path);
                for (int variant = 1; variant <= VariantSelector.VariantCount; variant++)
                {
                    yield return new TestCaseData(fileName, variant)
                        .SetName($"Variant_IsSolvable_AndTheSolutionWinsWhenPlayed({Path.GetFileNameWithoutExtension(fileName)} V{variant})");
                }
            }
        }

        private static IEnumerable<TestCaseData> StartPositions()
        {
            foreach (string path in LevelFiles())
            {
                string fileName = Path.GetFileName(path);
                for (int variant = 1; variant <= VariantSelector.VariantCount; variant++)
                {
                    yield return new TestCaseData(fileName, variant)
                        .SetName($"Variant_StartIsNotStuck({Path.GetFileNameWithoutExtension(fileName)} V{variant})");
                }
            }
        }

        private static LoadedLevel LoadFile(string fileName)
        {
            string path = Path.Combine(LevelsFolder, fileName);
            string name = Path.GetFileNameWithoutExtension(path);
            return LevelFactory.Load(LevelJsonParser.Parse(File.ReadAllText(path), name), name);
        }

        private static LevelSession PlayVariant(string fileName, int variant, params (int From, int To)[] moves)
        {
            var session = new LevelSession(LoadFile(fileName), variant, new BoosterInventory(0, 0, 0), addedTubeCapacity: 4);
            foreach (var (from, to) in moves)
            {
                Assert.That(session.TryMove(from, to, out MoveRejection rejection), Is.True,
                    $"{fileName} V{variant}: {from} -> {to} was refused ({rejection}) on {session.Board}");
            }

            return session;
        }

        [TestCaseSource(nameof(LevelFiles))]
        public void LevelFile_ParsesAndValidates(string path)
        {
            Assert.DoesNotThrow(() => LoadFile(Path.GetFileName(path)));
        }

        [TestCaseSource(nameof(LevelVariants))]
        public void Variant_IsSolvable_AndTheSolutionWinsWhenPlayed(string fileName, int variant)
        {
            SolverResult result = BoardSolver.Solve(LoadFile(fileName).CreateBoard(variant), SolverMode.Fast, SolverNodeLimit);
            Assert.That(result.Outcome, Is.EqualTo(SolverOutcome.Solved), $"{fileName} V{variant}: {result}");

            var moves = new (int From, int To)[result.Moves.Count];
            for (int i = 0; i < moves.Length; i++)
            {
                moves[i] = (result.Moves[i].FromIndex, result.Moves[i].ToIndex);
            }

            Assert.That(PlayVariant(fileName, variant, moves).IsWon, Is.True, $"{fileName} V{variant}: the solver's moves must win");
        }

        [TestCaseSource(nameof(StartPositions))]
        public void Variant_StartIsNotStuck(string fileName, int variant)
        {
            StuckAnalysis analysis = StuckDetector.Analyze(LoadFile(fileName).CreateBoard(variant), StuckDetector.DefaultPositionLimit);

            Assert.That(analysis.IsStuck, Is.False, $"{fileName} V{variant}: {analysis}");
        }

        [Test]
        public void LevelFile_IdMatchesFileNumber()
        {
            foreach (string path in LevelFiles())
            {
                string name = Path.GetFileNameWithoutExtension(path);
                LoadedLevel level = LoadFile(Path.GetFileName(path));

                Assert.That(name, Is.EqualTo($"level_{level.Id:000}"), $"{name} has id {level.Id}.");
            }
        }
    }
}
