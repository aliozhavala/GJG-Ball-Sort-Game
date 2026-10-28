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
    /// Test levels in <c>Assets/Data/TestLevels</c> are played by hand from Ball Sort ▸ Play Level…;
    /// these tests keep the behaviour each one is there to show. Drafts in the same folder are not
    /// checked: they may be work in progress.
    /// </summary>
    [TestFixture]
    public sealed class TestLevelFileTests
    {
        private const string StuckLock = "stuck_lock";

        private static LoadedLevel LoadFile(string name)
        {
            string path = Path.Combine(Application.dataPath, "Data", "TestLevels", name + ".json");
            return LevelFactory.Load(LevelJsonParser.Parse(File.ReadAllText(path), name), name);
        }

        private static LevelSession Play(string name, params (int From, int To)[] moves)
        {
            var session = new LevelSession(LoadFile(name), new BoosterInventory(0, 0, 0), addedTubeCapacity: 4);
            foreach (var (from, to) in moves)
            {
                Assert.That(session.TryMove(from, to, out MoveRejection rejection), Is.True,
                    $"{name}: {from} -> {to} was refused ({rejection}) on {session.Board}");
            }

            return session;
        }

        [Test]
        public void StuckLock_IsValidAndSolvable()
        {
            SolverResult result = BoardSolver.Solve(LoadFile(StuckLock).Board, SolverMode.Fast, 200000);
            Assert.That(result.Outcome, Is.EqualTo(SolverOutcome.Solved), result.ToString());

            var moves = new (int From, int To)[result.Moves.Count];
            for (int i = 0; i < moves.Length; i++)
            {
                moves[i] = (result.Moves[i].FromIndex, result.Moves[i].ToIndex);
            }

            Assert.That(Play(StuckLock, moves).IsWon, Is.True, "the solver's moves must win");
        }

        [Test]
        public void StuckLock_TheTrapGetsStuckWithTheDoorClosed_AndPlayOnAddsATubeToItsRow()
        {
            // Red from tube 1 into the empty tube 4, then yellow from tube 0 onto tube 1.
            LevelSession session = Play(StuckLock, (1, 4), (0, 1));

            Assert.That(session.IsStuck, Is.True);
            Assert.That(session.Board[5].IsCovered, Is.True, "the lock is still closed");
            Assert.That(session.Board[2][0].HasKey && session.Board[2][0].IsHidden, Is.True, "its key is still buried");

            Assert.That(session.TryPlayOn(new Wallet(900), 900, out PlayOnBlockReason reason), Is.True, reason.ToString());

            Assert.That(session.IsStuck, Is.False);
            Assert.That(session.Board[5].IsCovered, Is.True, "Play on does not open the lock");
            GridPlacement added = session.Grid.GetPlacement(6);
            Assert.That((added.Row, added.Cell), Is.EqualTo((0, 6)), "the added tube joins the locked tube's row");
        }
    }
}
