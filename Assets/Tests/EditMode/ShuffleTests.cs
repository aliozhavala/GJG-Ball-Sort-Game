using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BallSort.Core.Commands;
using BallSort.Core.Levels;
using BallSort.Core.Model;
using BallSort.Core.Player;
using BallSort.Core.Session;
using BallSort.Core.Solver;
using NUnit.Framework;
using static BallSort.Tests.EditMode.TestLevels;

namespace BallSort.Tests.EditMode
{
    /// <summary>The Shuffle booster (docs/mechanics.md, Shuffle) through <see cref="LevelSession"/>.</summary>
    [TestFixture]
    public sealed class ShuffleTests
    {
        private const int Seeds = 25;

        private static LevelSession Start(LevelDto level, BoosterInventory boosters) =>
            new LevelSession(LevelFactory.Load(level, Name), boosters, 4);

        private static LevelSession Start(LevelDto level, int shuffles = 99) =>
            Start(level, new BoosterInventory(99, shuffles, 99));

        /// <summary>
        /// Every kind of tube. Tube 1 → 0 completes colour 0 in tube 0. Tube 3 stays veiled (colour 3
        /// is not complete), tube 4 is locked, tube 5 is clay, and tube 2's bottom ball holds lock
        /// 0's key. The open tubes are 1, 2, 6 and 7.
        /// </summary>
        private static LevelDto EveryKindOfTube() =>
            Level(4,
                    T(4, 0, 0, 0), T(4, 1, 0), T(4, 1, 2, 3).WithKey(0, 0), T(4, 2, 2).WithVeil(3),
                    T(4, 3, 3), T(4, 1).AsClay(), T(4, 1, 3, 2), T(4))
                .WithLock(0, 4);

        private static readonly int[] UntouchedTubes = { 0, 3, 4, 5 };

        /// <summary>Three open tubes whose lower balls are hidden, and two empty tubes.</summary>
        private static LevelDto MostlyHidden() =>
            Level(3,
                Hidden(4, new[] { 0, 1, 2, 0 }, new[] { true, true, true, false }),
                Hidden(4, new[] { 1, 2, 0, 1 }, new[] { true, true, false, false }),
                Hidden(4, new[] { 2, 0, 1, 2 }, new[] { true, false, false, false }),
                T(4), T(4));

        /// <summary>The only open balls are four 0s, so no arrangement can differ from this one.</summary>
        private static LevelDto NothingToRearrange() =>
            Level(2, T(4, 0, 0), T(4, 0, 0), T(4, 1, 1, 1).WithVeil(0), T(4, 1).AsClay());

        private static int CountHidden(BoardState board)
        {
            int hidden = 0;
            for (int t = 0; t < board.TubeCount; t++)
            {
                for (int slot = 0; slot < board[t].Count; slot++)
                {
                    hidden += board[t][slot].IsHidden ? 1 : 0;
                }
            }

            return hidden;
        }

        private static List<int> SortedColors(BoardState board)
        {
            var colors = new List<int>();
            for (int t = 0; t < board.TubeCount; t++)
            {
                for (int slot = 0; slot < board[t].Count; slot++)
                {
                    colors.Add(board[t][slot].ColorId);
                }
            }

            colors.Sort();
            return colors;
        }

        private static void Shuffle(LevelSession session, int seed)
        {
            Assert.That(session.TryShuffle(new Random(seed), out ShuffleBlockReason reason), Is.True,
                $"seed {seed}: {reason} on {session.Board}");
        }

        // --- What a shuffle may touch ------------------------------------------------------------

        [Test]
        public void CoveredClayAndCompletedTubes_AreLeftAlone_AndKeysStayPut()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                LevelSession session = Start(EveryKindOfTube());
                session.TryMove(1, 0, out _);
                Assert.That(session.Board.IsCompleted(0), Is.True, "test setup: colour 0 completed");
                var before = new List<string>();
                foreach (int t in UntouchedTubes)
                {
                    before.Add(session.Board[t].ToString());
                }

                Shuffle(session, seed);

                for (int i = 0; i < UntouchedTubes.Length; i++)
                {
                    Assert.That(session.Board[UntouchedTubes[i]].ToString(), Is.EqualTo(before[i]), $"seed {seed}");
                }

                Assert.That(session.Board[2][0], Is.EqualTo(new Ball(1, false, 0)), $"seed {seed}: the key's ball");
            }
        }

        [Test]
        public void EveryTubeKeepsItsBallCount_AndNoBallIsGainedOrLost()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                LevelSession session = Start(EveryKindOfTube());
                session.TryMove(1, 0, out _);
                var counts = new List<int>();
                for (int t = 0; t < session.Board.TubeCount; t++)
                {
                    counts.Add(session.Board[t].Count);
                }

                List<int> colors = SortedColors(session.Board);

                Shuffle(session, seed);

                for (int t = 0; t < session.Board.TubeCount; t++)
                {
                    Assert.That(session.Board[t].Count, Is.EqualTo(counts[t]), $"seed {seed}, tube {t}");
                }

                Assert.That(SortedColors(session.Board), Is.EqualTo(colors), $"seed {seed}");
            }
        }

        [Test]
        public void EveryTopIsRevealed_AndOnlyTopsLoseTheirHiddenFlag()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                LevelSession session = Start(MostlyHidden());
                int hiddenBefore = CountHidden(session.Board);

                Shuffle(session, seed);

                for (int t = 0; t < session.Board.TubeCount; t++)
                {
                    if (!session.Board[t].IsEmpty)
                    {
                        Assert.That(session.Board[t].Top.IsRevealed, Is.True, $"seed {seed}, tube {t}");
                    }
                }

                // Three non-empty tubes, so at most three hidden balls can land on a top.
                Assert.That(CountHidden(session.Board), Is.InRange(hiddenBefore - 3, hiddenBefore), $"seed {seed}");
            }
        }

        // --- What a valid result is ------------------------------------------------------------

        [Test]
        public void TheResult_CompletesNothing_DiffersAndIsSolvable()
        {
            // Two full mixed tubes: two of the arrangements would complete both colours.
            LevelSession session = Start(Valid());
            for (int seed = 0; seed < Seeds; seed++)
            {
                string before = session.Board.ToString();

                Shuffle(session, seed);

                Assert.That(session.Board.IsCompleted(0) || session.Board.IsCompleted(1), Is.False, $"seed {seed}");
                Assert.That(session.Board.ToString(), Is.Not.EqualTo(before), $"seed {seed}");
                Assert.That(BoardSolver.Solve(session.Board, SolverMode.Fast, 20000).Outcome,
                    Is.EqualTo(SolverOutcome.Solved), $"seed {seed}");
                Assert.That(session.IsStuck, Is.False);
            }
        }

        [Test]
        public void NoKeyIsFindableAfterAShuffle()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                LevelSession session = Start(EveryKindOfTube());
                session.TryMove(1, 0, out _);
                int keysFound = 0;
                session.KeyFound += _ => keysFound++;

                Shuffle(session, seed);

                Assert.That(session.Board[2][0].HasKey, Is.True, $"seed {seed}");
                Assert.That(session.Board[4].IsCovered, Is.True, $"seed {seed}: the lock is still shut");
                Assert.That(keysFound, Is.Zero);
            }
        }

        [Test]
        public void TheSameSeed_GivesTheSameShuffle()
        {
            LevelSession a = Start(MostlyHidden());
            LevelSession b = Start(MostlyHidden());

            Shuffle(a, 7);
            Shuffle(b, 7);

            Assert.That(a.Board.ToString(), Is.EqualTo(b.Board.ToString()));
        }

        // --- Spending, history and events --------------------------------------------------------

        [Test]
        public void ASuccessfulShuffle_SpendsOneBooster_AndClearsTheUndoHistory()
        {
            var boosters = new BoosterInventory(undo: 5, shuffle: 2, addTube: 0);
            LevelSession session = Start(Valid(), boosters);
            session.TryMove(0, 2, out _);
            Assert.That(session.UndoBlockReason, Is.EqualTo(UndoBlockReason.None), "test setup: undo possible");

            Shuffle(session, 1);

            Assert.That(boosters.GetCount(BoosterType.Shuffle), Is.EqualTo(1));
            Assert.That(session.UndoBlockReason, Is.EqualTo(UndoBlockReason.NoMovesToUndo));
            Assert.That(session.MoveCount, Is.Zero);
            Assert.That(boosters.GetCount(BoosterType.Undo), Is.EqualTo(5));
        }

        [Test]
        public void AShuffle_RaisesShuffledThenChanged()
        {
            LevelSession session = Start(Valid());
            var log = new List<string>();
            session.Shuffled += () => log.Add("Shuffled");
            session.StuckChanged += stuck => log.Add($"StuckChanged({stuck})");
            session.Changed += () => log.Add("Changed");
            session.MoveApplied += _ => log.Add("MoveApplied");

            Shuffle(session, 3);

            Assert.That(log, Is.EqualTo(new[] { "Shuffled", "Changed" }));
        }

        [Test]
        public void WhenNoValidShuffleExists_NothingChangesAndNothingIsSpent()
        {
            var boosters = new BoosterInventory(undo: 0, shuffle: 3, addTube: 0);
            LevelSession session = Start(NothingToRearrange(), boosters);
            string before = session.Board.ToString();
            int events = 0;
            session.Shuffled += () => events++;
            session.Changed += () => events++;

            Assert.That(session.TryShuffle(new Random(1), out ShuffleBlockReason reason), Is.False);

            Assert.That(reason, Is.EqualTo(ShuffleBlockReason.NoValidShuffle));
            Assert.That(boosters.GetCount(BoosterType.Shuffle), Is.EqualTo(3));
            Assert.That(session.Board.ToString(), Is.EqualTo(before));
            Assert.That(events, Is.Zero);
        }

        [Test]
        public void WithoutBoosters_ShuffleIsRefused()
        {
            LevelSession session = Start(Valid(), shuffles: 0);

            Assert.That(session.ShuffleBlockReason, Is.EqualTo(ShuffleBlockReason.NoBoostersLeft));
            Assert.That(session.TryShuffle(new Random(1), out ShuffleBlockReason reason), Is.False);
            Assert.That(reason, Is.EqualTo(ShuffleBlockReason.NoBoostersLeft));
        }

        // --- Searching off the main thread ------------------------------------------------------

        [Test]
        public void AShuffleSearchedOnAnotherThread_IsAppliedLikeASynchronousOne()
        {
            LevelSession session = Start(MostlyHidden());
            LevelSession reference = Start(MostlyHidden());
            Shuffle(reference, 11);

            Assert.That(session.TryBeginShuffle(out PendingShuffle shuffle, out _), Is.True);
            bool found = Task.Run(() => shuffle.Search(new Random(11))).Result;

            Assert.That(found, Is.True);
            Assert.That(session.TryApplyShuffle(shuffle, out ShuffleBlockReason reason), Is.True, reason.ToString());
            Assert.That(session.Board.ToString(), Is.EqualTo(reference.Board.ToString()));
        }

        [Test]
        public void AShuffleIsRefused_WhenTheBoardChangedDuringTheSearch()
        {
            var boosters = new BoosterInventory(undo: 0, shuffle: 3, addTube: 0);
            LevelSession session = Start(Valid(), boosters);
            session.TryBeginShuffle(out PendingShuffle shuffle, out _);
            shuffle.Search(new Random(1));
            session.TryMove(0, 2, out _);
            string afterMove = session.Board.ToString();

            Assert.That(session.TryApplyShuffle(shuffle, out ShuffleBlockReason reason), Is.False);

            Assert.That(reason, Is.EqualTo(ShuffleBlockReason.BoardChanged));
            Assert.That(session.Board.ToString(), Is.EqualTo(afterMove));
            Assert.That(boosters.GetCount(BoosterType.Shuffle), Is.EqualTo(3));
        }

        [Test]
        public void APendingShuffle_IsSearchedOnce_AndOnlyAppliesToItsOwnSession()
        {
            LevelSession session = Start(Valid());
            session.TryBeginShuffle(out PendingShuffle shuffle, out _);
            shuffle.Search(new Random(1));

            Assert.Throws<InvalidOperationException>(() => shuffle.Search(new Random(2)));
            Assert.Throws<ArgumentException>(() => Start(Valid()).TryApplyShuffle(shuffle, out _));
        }
    }
}
