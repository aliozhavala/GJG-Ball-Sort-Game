using System.Collections.Generic;
using System.IO;
using BallSort.Core.Levels;
using BallSort.Core.Model;
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
    /// Test levels in <c>Assets/Data/TestLevels</c> are played by hand from Ball Sort ▸ Play Level…; these
    /// tests keep every fixture valid and solvable, and the behaviour each one is there to show. Drafts in
    /// the same folder are not checked: they may be work in progress.
    /// </summary>
    [TestFixture]
    public sealed class TestLevelFileTests
    {
        private const string StuckLock = "stuck_lock";
        private const string UnevenRows = "uneven_rows";
        private const string FullRows = "full_rows";
        private const string Veil = "veil";
        private const string LockAndKey = "lock_and_key";
        private const string ClayLargeTube = "clay_large_tube";

        /// <summary>The fixtures; any other file in the folder is a draft.</summary>
        private static readonly string[] Fixtures = { StuckLock, UnevenRows, FullRows, Veil, LockAndKey, ClayLargeTube };

        // stuck_lock's variants only permute the colours, so the same moves set the same trap in each one.
        private static readonly int[] Variants = { 1, 2, 3, 4, 5 };

        private static IEnumerable<TestCaseData> FixtureVariants()
        {
            foreach (string name in Fixtures)
            {
                foreach (int variant in Variants)
                {
                    yield return new TestCaseData(name, variant)
                        .SetName($"Fixture_IsValidAndEveryVariantSolvable({name} V{variant})");
                }
            }
        }

        private static LoadedLevel LoadFile(string name)
        {
            string path = Path.Combine(Application.dataPath, "Data", "TestLevels", name + ".json");
            return LevelFactory.Load(LevelJsonParser.Parse(File.ReadAllText(path), name), name);
        }

        private static LevelSession Play(string name, int variant, params (int From, int To)[] moves)
        {
            var session = new LevelSession(LoadFile(name), variant, new BoosterInventory(0, 0, 0), addedTubeCapacity: 4);
            foreach (var (from, to) in moves)
            {
                Assert.That(session.TryMove(from, to, out MoveRejection rejection), Is.True,
                    $"{name}: {from} -> {to} was refused ({rejection}) on {session.Board}");
            }

            return session;
        }

        [TestCaseSource(nameof(FixtureVariants))]
        public void Fixture_IsValidAndEveryVariantSolvable(string name, int variant)
        {
            SolverResult result = BoardSolver.Solve(LoadFile(name).CreateBoard(variant), SolverMode.Fast, 200000);
            Assert.That(result.Outcome, Is.EqualTo(SolverOutcome.Solved), result.ToString());

            var moves = new (int From, int To)[result.Moves.Count];
            for (int i = 0; i < moves.Length; i++)
            {
                moves[i] = (result.Moves[i].FromIndex, result.Moves[i].ToIndex);
            }

            Assert.That(Play(name, variant, moves).IsWon, Is.True, "the solver's moves must win");
        }

        [Test]
        public void StuckLock_TheTrapGetsStuckWithTheDoorClosed_AndPlayOnAddsATubeToItsRow(
            [ValueSource(nameof(Variants))] int variant)
        {
            // In V1: red from tube 1 into the empty tube 4, then yellow from tube 0 onto tube 1.
            LevelSession session = Play(StuckLock, variant, (1, 4), (0, 1));

            Assert.That(session.IsStuck, Is.True);
            Assert.That(session.Board[5].IsCovered, Is.True, "the lock is still closed");
            Assert.That(session.Board[2][0].HasKey && session.Board[2][0].IsHidden, Is.True, "its key is still buried");

            Assert.That(session.TryPlayOn(new Wallet(900), 900, out PlayOnBlockReason reason), Is.True, reason.ToString());

            Assert.That(session.IsStuck, Is.False);
            Assert.That(session.Board[5].IsCovered, Is.True, "Play on does not open the lock");
            GridPlacement added = session.Grid.GetPlacement(6);
            Assert.That((added.Row, added.Cell), Is.EqualTo((0, 6)), "the added tube joins the locked tube's row");
        }

        [Test]
        public void UnevenRows_KeepsItsTwoRows()
        {
            BoardGrid grid = LoadFile(UnevenRows).CreateGrid();

            Assert.That((grid.RowCount, grid.GetCellCount(0), grid.GetCellCount(1)), Is.EqualTo((2, 4, 3)));
        }

        [Test]
        public void FullRows_FillsTwoFullRows()
        {
            BoardGrid grid = LoadFile(FullRows).CreateGrid();

            Assert.That((grid.RowCount, grid.GetCellCount(0), grid.GetCellCount(1)), Is.EqualTo((2, 6, 6)));
        }

        [Test]
        public void Veil_OpensWhenRedCompletes_AndIsSolvable()
        {
            LevelSession session = Play(Veil, 1, (0, 2), (1, 0));
            Assert.That(session.Board[3].IsCovered, Is.False, "completing colour 0 opens the veil");

            session = Play(Veil, 1, (0, 2), (1, 0), (1, 4), (2, 1), (3, 4), (3, 1), (4, 3));
            Assert.That(session.IsWon, Is.True);
        }

        [Test]
        public void LockAndKey_OpensOnTheFirstMove_AndIsSolvable()
        {
            LevelSession session = Play(LockAndKey, 1, (0, 2));
            Assert.That(session.Board[3].IsCovered || session.Board[4].IsCovered, Is.False, "the key was found");

            session = Play(LockAndKey, 1,
                (0, 2), (1, 2), (0, 4), (1, 0), (4, 1), (3, 0), (3, 1), (3, 0), (3, 1));
            Assert.That(session.IsWon, Is.True);
        }

        [Test]
        public void ClayLargeTube_IsLaidOutAcrossTwoRows_AndSolvable()
        {
            LoadedLevel level = LoadFile(ClayLargeTube);
            BoardState board = level.CreateBoard(1);
            BoardGrid grid = level.CreateGrid();
            Assert.That(board[0].Capacity, Is.EqualTo(10));
            Assert.That(board[0].IsClay, Is.True);
            GridPlacement large = grid.GetPlacement(0);
            Assert.That((large.Row, large.Cell, large.RowSpan), Is.EqualTo((1, 0, 2)));
            Assert.That(grid.IsAligned(0) && grid.IsAligned(1), Is.True);

            LevelSession session = Play(ClayLargeTube, 1,
                (1, 0), (2, 0), (3, 0), (4, 0), (3, 2), (4, 2), (4, 1), (4, 2), (3, 1), (5, 1));
            Assert.That(session.IsWon, Is.True);
            Assert.That(session.Board.IsCompleted(0), Is.True, "the clay tube finished with all ten");
        }

        [Test]
        public void ClayLargeTube_ClayTube_IsNeverASource()
        {
            LevelSession session = Play(ClayLargeTube, 1);

            Assert.That(session.CanSelect(0), Is.False);
            Assert.That(session.TryMove(0, 5, out MoveRejection rejection), Is.False);
            Assert.That(rejection, Is.EqualTo(MoveRejection.SourceClay));
        }
    }
}
