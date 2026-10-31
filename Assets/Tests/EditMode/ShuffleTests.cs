using System;
using System.Collections.Generic;
using BallSort.Core.Commands;
using BallSort.Core.Model;
using BallSort.Core.Player;
using BallSort.Core.Rules;
using BallSort.Core.Session;
using NUnit.Framework;
using static BallSort.Tests.EditMode.TestLevels;

namespace BallSort.Tests.EditMode
{
    /// <summary>The Shuffle booster (docs/mechanics.md, Shuffle) through <see cref="LevelSession"/>.</summary>
    [TestFixture]
    public sealed class ShuffleTests
    {
        private const int Seeds = 40;

        private static LevelSession Start(TestLevel level, BoosterInventory boosters) =>
            new LevelSession(Load(level), 1, boosters, 4);

        private static LevelSession Start(TestLevel level, int shuffles = 99) =>
            Start(level, new BoosterInventory(99, shuffles, 99));

        /// <summary>
        /// Every kind of tube. Tube 1 → 0 completes colour 0 in tube 0. Tube 3 is veiled (colour 3 is not
        /// complete), tube 4 is locked, tube 5 is clay, tube 7 is empty, and tube 2's bottom ball holds
        /// lock 0's key. Tubes 2 and 6 hold balls that look different.
        /// </summary>
        private static TestLevel EveryKindOfTube() =>
            Level(4,
                    T(4, 0, 0, 0), T(4, 1, 0), T(4, 1, 2, 3).WithKey(0, 0), T(4, 2, 2).WithVeil(3),
                    T(4, 3, 3), T(4, 1).AsClay(), T(4, 1, 3, 2), T(4))
                .WithLock(0, 4);

        /// <summary>Three tubes whose lower balls are hidden, and two empty tubes.</summary>
        private static TestLevel MostlyHidden() =>
            Level(3,
                Hidden(4, new[] { 0, 1, 2, 0 }, new[] { true, true, true, false }),
                Hidden(4, new[] { 1, 2, 0, 1 }, new[] { true, true, false, false }),
                Hidden(4, new[] { 2, 0, 1, 2 }, new[] { true, false, false, false }),
                T(4), T(4));

        /// <summary>
        /// Tube 0's second ball (colour 2) holds the key of lock 0, which covers tubes 2 and 3; tube 0's
        /// top run is the two colour-0 balls above it.
        /// </summary>
        private static TestLevel KeyUnderARun() =>
            Level(3, T(4, 1, 2, 0, 0).WithKey(1, 0), T(4), T(4, 2, 1, 2, 1), T(4, 0, 0, 1, 2), T(4))
                .WithLock(0, 2, 3);

        /// <summary>All four of colour 0 in tube 0, the bottom one hidden: full, but not complete.</summary>
        private static TestLevel OneColourWithAHiddenBall() =>
            Level(2, Hidden(4, new[] { 0, 0, 0, 0 }, new[] { true, false, false, false }), T(4, 1, 1), T(4, 1, 1));

        /// <summary>
        /// Tubes 0–2 are full (tops 0, 0, 1), tube 4 holds one colour-2 ball, and tube 3 holds [1, 3, 0]:
        /// tube 0 → 3 is legal. Shuffled with colour 3 on top, tube 3 matches no other top and nothing
        /// else can move: stuck.
        /// </summary>
        private static TestLevel ShuffleCanStick() =>
            Level(4, T(4, 3, 1, 2, 0), T(4, 3, 2, 1, 0), T(4, 3, 0, 2, 1), T(4, 1, 3, 0), T(4, 2));

        /// <summary>A veil, clay, two one-colour tubes and two empty tubes: nothing can be shuffled.</summary>
        private static TestLevel NothingToShuffle() => StuckBehindCovers();

        private static void ShuffleTube(LevelSession session, int tubeIndex, int seed)
        {
            Assert.That(session.TryShuffleTube(tubeIndex, new Random(seed), out ShuffleBlockReason reason), Is.True,
                $"seed {seed}: {reason} on {session.Board}");
        }

        private static List<Ball> BallsOf(Tube tube)
        {
            var balls = new List<Ball>();
            for (int slot = 0; slot < tube.Count; slot++)
            {
                balls.Add(tube[slot]);
            }

            return balls;
        }

        private static List<string> Sorted(IEnumerable<Ball> balls)
        {
            var list = new List<string>();
            foreach (Ball ball in balls)
            {
                list.Add(ball.ToString());
            }

            list.Sort(StringComparer.Ordinal);
            return list;
        }

        private static bool LooksDifferent(List<Ball> before, List<Ball> after)
        {
            for (int slot = 0; slot < before.Count; slot++)
            {
                if (!ShuffleRules.LookAlike(before[slot], after[slot]))
                {
                    return true;
                }
            }

            return false;
        }

        private static string Snapshot(LevelSession session) => session.Board.ToString();

        // --- Which tubes can be shuffled --------------------------------------------------------

        [Test]
        public void OnlyOpenTubesWithBallsThatLookDifferent_AreEligible()
        {
            LevelSession session = Start(EveryKindOfTube());
            session.TryMove(1, 0, out _);
            Assert.That(session.Board.IsCompleted(0), Is.True, "test setup: colour 0 completed");

            var eligible = new List<int>();
            for (int t = 0; t < session.Board.TubeCount; t++)
            {
                if (session.CanShuffleTube(t))
                {
                    eligible.Add(t);
                }
            }

            // 0 completed, 1 a single ball, 3 veiled, 4 locked, 5 clay, 7 empty.
            Assert.That(eligible, Is.EqualTo(new[] { 2, 6 }));
        }

        [Test]
        public void ATubeOfOneColour_IsNotEligible()
        {
            LevelSession session = Start(EveryKindOfTube());

            Assert.That(session.CanShuffleTube(0), Is.False, "three revealed colour-0 balls look alike");
            Assert.That(session.CanShuffleTube(1), Is.True, "colour 1 under colour 0");
        }

        [Test]
        public void AHiddenBall_LooksDifferentFromARevealedOne_WhateverItsColour()
        {
            // Every ball in tube 0 is colour 0, so true colours alone would say "nothing to shuffle".
            LevelSession session = Start(OneColourWithAHiddenBall());

            Assert.That(session.CanShuffleTube(0), Is.True);
            Assert.That(session.CanShuffleTube(1), Is.False);
        }

        [Test]
        public void Looks_AreHiddenOrColourWithKeyMark()
        {
            Assert.That(ShuffleRules.LookAlike(new Ball(0), new Ball(0)), Is.True);
            Assert.That(ShuffleRules.LookAlike(new Ball(0), new Ball(1)), Is.False);
            Assert.That(ShuffleRules.LookAlike(new Ball(0, false, 0), new Ball(0)), Is.False, "a key mark shows");
            Assert.That(ShuffleRules.LookAlike(new Ball(0, false, 0), new Ball(0, false, 1)), Is.False, "lock colours differ");
            Assert.That(ShuffleRules.LookAlike(new Ball(0, false, 1), new Ball(0, false, 1)), Is.True);
            Assert.That(ShuffleRules.LookAlike(new Ball(0, true), new Ball(1, true)), Is.True, "both are \"?\"");
            Assert.That(ShuffleRules.LookAlike(new Ball(0, true, 0), new Ball(2, true)), Is.True, "a hidden key is invisible");
            Assert.That(ShuffleRules.LookAlike(new Ball(0, true), new Ball(0)), Is.False);
        }

        [Test]
        public void WithNoEligibleTube_ShuffleIsBlocked()
        {
            var boosters = new BoosterInventory(undo: 0, shuffle: 3, addTube: 0);
            LevelSession session = Start(NothingToShuffle(), boosters);

            Assert.That(session.ShuffleBlockReason, Is.EqualTo(ShuffleBlockReason.NoEligibleTube));
            Assert.That(session.TryShuffleTube(2, new Random(1), out ShuffleBlockReason reason), Is.False);
            Assert.That(reason, Is.EqualTo(ShuffleBlockReason.NoEligibleTube));
            Assert.That(boosters.GetCount(BoosterType.Shuffle), Is.EqualTo(3));
        }

        [Test]
        public void BlockReasons_ComeInOrder_WonThenStuckThenBoostersThenEligibility()
        {
            LevelSession won = Start(Level(1, T(4, 0, 0), T(4, 0, 0)));
            won.TryMove(0, 1, out _);
            Assert.That(won.ShuffleBlockReason, Is.EqualTo(ShuffleBlockReason.LevelWon));

            Assert.That(Start(NothingToShuffle(), shuffles: 0).ShuffleBlockReason,
                Is.EqualTo(ShuffleBlockReason.NoBoostersLeft), "out of boosters outranks no eligible tube");
            Assert.That(Start(Valid()).ShuffleBlockReason, Is.EqualTo(ShuffleBlockReason.None));
        }

        // --- What a shuffle does to the tube -----------------------------------------------------

        [Test]
        public void TheTube_LooksDifferent_KeepsItsBalls_AndNothingElseChanges()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                LevelSession session = Start(EveryKindOfTube());
                session.TryMove(1, 0, out _);
                var others = new List<string>();
                for (int t = 0; t < session.Board.TubeCount; t++)
                {
                    others.Add(t == 6 ? null : session.Board[t].ToString());
                }

                List<Ball> before = BallsOf(session.Board[6]);
                ShuffleTube(session, 6, seed);
                List<Ball> after = BallsOf(session.Board[6]);

                Assert.That(LooksDifferent(before, after), Is.True, $"seed {seed}: [{string.Join(", ", after)}]");
                Assert.That(Sorted(after), Is.EqualTo(Sorted(before)), $"seed {seed}");
                for (int t = 0; t < session.Board.TubeCount; t++)
                {
                    if (t != 6)
                    {
                        Assert.That(session.Board[t].ToString(), Is.EqualTo(others[t]), $"seed {seed}, tube {t}");
                    }
                }
            }
        }

        [Test]
        public void ATubeWithOneOddBall_AlwaysChangesLook()
        {
            // Three alike and one odd: a random order often looks the same, so the fallback swap is used.
            for (int seed = 0; seed < Seeds; seed++)
            {
                LevelSession session = Start(Level(2, T(4, 1, 0, 0, 0), T(4, 0, 1), T(4, 1, 1)));
                List<Ball> before = BallsOf(session.Board[0]);

                ShuffleTube(session, 0, seed);

                Assert.That(LooksDifferent(before, BallsOf(session.Board[0])), Is.True, $"seed {seed}");
            }
        }

        [Test]
        public void HiddenBalls_KeepTheirFlag_UnlessTheyLandOnTop()
        {
            bool sawTopRevealed = false;
            for (int seed = 0; seed < Seeds; seed++)
            {
                LevelSession session = Start(MostlyHidden());
                List<Ball> before = BallsOf(session.Board[0]);
                var revealed = new List<BallRevealedEventArgs>();
                session.BallRevealed += revealed.Add;

                ShuffleTube(session, 0, seed);

                Tube tube = session.Board[0];
                Assert.That(tube.Top.IsRevealed, Is.True, $"seed {seed}");

                // Hide the top again if the shuffle revealed it: then the balls are exactly the old ones.
                List<Ball> after = BallsOf(tube);
                if (revealed.Count == 1)
                {
                    sawTopRevealed = true;
                    Assert.That(revealed[0].TubeIndex, Is.Zero);
                    Assert.That(revealed[0].SlotIndex, Is.EqualTo(tube.Count - 1));
                    Assert.That(revealed[0].ColorId, Is.EqualTo(tube.Top.ColorId));
                    after[after.Count - 1] = after[after.Count - 1].AsHidden();
                }
                else
                {
                    Assert.That(revealed, Is.Empty, $"seed {seed}");
                }

                Assert.That(Sorted(after), Is.EqualTo(Sorted(before)), $"seed {seed}: no other ball changed its flag");
            }

            Assert.That(sawTopRevealed, Is.True, "some seed should put a hidden ball on top");
        }

        [Test]
        public void Keys_MoveWithTheirBalls_AndAKeyInTheTopRunIsFound()
        {
            bool sawFound = false;
            bool sawKept = false;
            for (int seed = 0; seed < Seeds; seed++)
            {
                LevelSession session = Start(KeyUnderARun());
                var log = new List<string>();
                session.Shuffled += e => log.Add("Shuffled");
                session.KeyFound += e => log.Add($"KeyFound({e.TubeIndex},{e.SlotIndex},{e.LockId})");
                session.LockOpened += e => log.Add($"LockOpened({e.LockId})");
                session.Changed += () => log.Add("Changed");
                Ball keyBall = default;
                session.Shuffled += e =>
                {
                    foreach (Ball ball in e.Balls)
                    {
                        if (ball.HasKey)
                        {
                            keyBall = ball;
                        }
                    }
                };

                ShuffleTube(session, 0, seed);

                Assert.That(keyBall, Is.EqualTo(new Ball(2, false, 0)), $"seed {seed}: the key travels on its colour-2 ball");
                Tube tube = session.Board[0];
                int runStart = tube.Count - tube.RunLength;
                int keySlot = -1;
                for (int slot = 0; slot < tube.Count; slot++)
                {
                    if (tube[slot].ColorId == 2)
                    {
                        keySlot = slot;
                    }
                }

                if (keySlot >= runStart)
                {
                    sawFound = true;
                    Assert.That(tube[keySlot].HasKey, Is.False, $"seed {seed}: a found key leaves its ball");
                    Assert.That(session.Board[2].IsCovered || session.Board[3].IsCovered, Is.False, $"seed {seed}");
                    Assert.That(log, Is.EqualTo(new[] { "Shuffled", $"KeyFound(0,{keySlot},0)", "LockOpened(0)", "Changed" }));
                }
                else
                {
                    sawKept = true;
                    Assert.That(tube[keySlot].HasKey, Is.True, $"seed {seed}");
                    Assert.That(session.Board[2].IsCovered && session.Board[3].IsCovered, Is.True, $"seed {seed}");
                    Assert.That(log, Is.EqualTo(new[] { "Shuffled", "Changed" }));
                }
            }

            Assert.That(sawFound && sawKept, Is.True, "both outcomes should occur over the seeds");
        }

        [Test]
        public void RevealingTheTop_CanCompleteTheTube()
        {
            bool sawCompleted = false;
            for (int seed = 0; seed < Seeds; seed++)
            {
                LevelSession session = Start(OneColourWithAHiddenBall());
                var log = new List<string>();
                session.Shuffled += _ => log.Add("Shuffled");
                session.BallRevealed += _ => log.Add("BallRevealed");
                session.TubeCompleted += e => log.Add($"TubeCompleted({e.TubeIndex},{e.ColorId})");
                session.Changed += () => log.Add("Changed");

                ShuffleTube(session, 0, seed);

                if (session.Board.IsCompleted(0))
                {
                    sawCompleted = true;
                    Assert.That(log, Is.EqualTo(new[] { "Shuffled", "BallRevealed", "TubeCompleted(0,0)", "Changed" }));
                }
                else
                {
                    Assert.That(log, Is.EqualTo(new[] { "Shuffled", "Changed" }), $"seed {seed}");
                }
            }

            Assert.That(sawCompleted, Is.True, "some seed should put the hidden ball on top");
        }

        [Test]
        public void TheSameSeed_GivesTheSameShuffle()
        {
            LevelSession a = Start(MostlyHidden());
            LevelSession b = Start(MostlyHidden());

            ShuffleTube(a, 1, 7);
            ShuffleTube(b, 1, 7);

            Assert.That(Snapshot(a), Is.EqualTo(Snapshot(b)));
        }

        // --- Spending, history, stuck and events ------------------------------------------------

        [Test]
        public void AShuffle_SpendsOneBooster_AndClearsTheUndoHistory()
        {
            var boosters = new BoosterInventory(undo: 5, shuffle: 2, addTube: 0);
            LevelSession session = Start(Valid(), boosters);
            session.TryMove(0, 2, out _);
            Assert.That(session.UndoBlockReason, Is.EqualTo(UndoBlockReason.None), "test setup: undo possible");

            ShuffleTube(session, 1, 1);

            Assert.That(boosters.GetCount(BoosterType.Shuffle), Is.EqualTo(1));
            Assert.That(session.UndoBlockReason, Is.EqualTo(UndoBlockReason.NoMovesToUndo));
            Assert.That(session.MoveCount, Is.Zero);
            Assert.That(boosters.GetCount(BoosterType.Undo), Is.EqualTo(5));
        }

        [Test]
        public void Shuffled_CarriesTheTubeAndItsNewOrder_ThenChanged()
        {
            LevelSession session = Start(Valid());
            var log = new List<string>();
            TubeShuffledEventArgs shuffled = default;
            session.Shuffled += e =>
            {
                shuffled = e;
                log.Add("Shuffled");
            };
            session.StuckChanged += stuck => log.Add($"StuckChanged({stuck})");
            session.Changed += () => log.Add("Changed");
            session.MoveApplied += _ => log.Add("MoveApplied");

            ShuffleTube(session, 1, 3);

            Assert.That(log, Is.EqualTo(new[] { "Shuffled", "Changed" }));
            Assert.That(shuffled.TubeIndex, Is.EqualTo(1));
            Assert.That(shuffled.Balls, Is.EqualTo(BallsOf(session.Board[1])));
        }

        [Test]
        public void AShuffle_MayLeaveTheBoardStuck_AndStuckIsReevaluated()
        {
            bool sawStuck = false;
            for (int seed = 0; seed < Seeds && !sawStuck; seed++)
            {
                LevelSession session = Start(ShuffleCanStick());
                Assert.That(session.IsStuck, Is.False, "test setup: tube 0 → 3 is legal");
                var log = new List<string>();
                session.StuckChanged += stuck => log.Add($"StuckChanged({stuck})");
                session.Changed += () => log.Add("Changed");

                ShuffleTube(session, 3, seed);

                if (session.Board[3].Top.ColorId == 3)
                {
                    sawStuck = true;
                    Assert.That(session.IsStuck, Is.True, $"seed {seed}: {session.Board}");
                    Assert.That(log, Is.EqualTo(new[] { "StuckChanged(True)", "Changed" }));
                    Assert.That(session.ShuffleBlockReason, Is.EqualTo(ShuffleBlockReason.Stuck));
                }
                else
                {
                    Assert.That(session.IsStuck, Is.False, $"seed {seed}: {session.Board}");
                }
            }

            Assert.That(sawStuck, Is.True, "some seed should put colour 3 on top");
        }

        // --- Refusals and cancelling spend nothing --------------------------------------------------

        [Test]
        public void ATubeThatIsNotEligible_IsRefused_AndNothingChanges()
        {
            var boosters = new BoosterInventory(undo: 5, shuffle: 2, addTube: 0);
            LevelSession session = Start(EveryKindOfTube(), boosters);
            session.TryMove(1, 0, out _);
            string before = Snapshot(session);
            int events = 0;
            session.Shuffled += _ => events++;
            session.Changed += () => events++;

            foreach (int tube in new[] { 0, 1, 3, 4, 5, 7 })
            {
                Assert.That(session.TryShuffleTube(tube, new Random(1), out ShuffleBlockReason reason), Is.False, $"tube {tube}");
                Assert.That(reason, Is.EqualTo(ShuffleBlockReason.TubeNotEligible), $"tube {tube}");
            }

            Assert.That(Snapshot(session), Is.EqualTo(before));
            Assert.That(events, Is.Zero);
            Assert.That(boosters.GetCount(BoosterType.Shuffle), Is.EqualTo(2));
            Assert.That(session.UndoBlockReason, Is.EqualTo(UndoBlockReason.SealedByTubeCompletion),
                "the history is kept: only a shuffle that happens clears it");
        }

        [Test]
        public void WithoutBoosters_NothingIsShuffled()
        {
            LevelSession session = Start(Valid(), shuffles: 0);
            string before = Snapshot(session);

            Assert.That(session.TryShuffleTube(1, new Random(1), out ShuffleBlockReason reason), Is.False);

            Assert.That(reason, Is.EqualTo(ShuffleBlockReason.NoBoostersLeft));
            Assert.That(Snapshot(session), Is.EqualTo(before));
        }

        [Test]
        public void AskingWhatCanBeShuffled_ChangesAndSpendsNothing()
        {
            // The flow's pick asks these, then cancels by never calling TryShuffleTube.
            var boosters = new BoosterInventory(undo: 1, shuffle: 1, addTube: 1);
            LevelSession session = Start(Valid(), boosters);
            session.TryMove(0, 2, out _);
            string before = Snapshot(session);
            int events = 0;
            session.Shuffled += _ => events++;
            session.Changed += () => events++;

            Assert.That(session.ShuffleBlockReason, Is.EqualTo(ShuffleBlockReason.None));
            for (int t = 0; t < session.Board.TubeCount; t++)
            {
                session.CanShuffleTube(t);
            }

            Assert.That(Snapshot(session), Is.EqualTo(before));
            Assert.That(events, Is.Zero);
            Assert.That(boosters.GetCount(BoosterType.Shuffle), Is.EqualTo(1));
            Assert.That(session.MoveCount, Is.EqualTo(1), "the undo history is kept");
        }

        [Test]
        public void AnInvalidTubeIndex_Throws()
        {
            LevelSession session = Start(Valid());

            Assert.Throws<ArgumentOutOfRangeException>(() => session.CanShuffleTube(9));
            Assert.Throws<ArgumentOutOfRangeException>(() => session.TryShuffleTube(-1, new Random(1), out _));
        }
    }
}
