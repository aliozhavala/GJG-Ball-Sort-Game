using System.Collections.Generic;
using BallSort.Core.Levels;
using NUnit.Framework;
using static BallSort.Tests.EditMode.TestLevels;

namespace BallSort.Tests.EditMode
{
    /// <summary>Level-file rules for veils, locks, keys and clay (docs/mechanics.md).</summary>
    [TestFixture]
    public sealed class MechanicsValidationTests
    {
        private static IReadOnlyList<string> Validate(LevelDto level) => LevelValidator.Validate(level, Name);

        /// <summary>Colour 0 completes from tubes 0 and 1, which opens the veil over tube 2.</summary>
        private static LevelDto VeilLevel() =>
            Level(2, T(4, 0, 0, 0), T(4, 1, 0), T(4, 1, 1, 1).WithVeil(0), T(4));

        /// <summary>Lock 0 covers tubes 2 and 3; its key sits under tube 0's top run.</summary>
        private static LevelDto LockLevel() =>
            Level(3, T(4, 1, 2, 0, 0).WithKey(1, 0), T(4), T(4, 2, 1, 2, 1), T(4, 0, 0, 1, 2), T(4))
                .WithLock(0, 2, 3);

        /// <summary>Tube 0 is clay holding one colour-0 ball; tube 1's run can join it.</summary>
        private static LevelDto ClayLevel() =>
            Level(2, T(4, 0).AsClay(), T(4, 1, 0, 0, 0), T(4, 1, 1, 1), T(4));

        [Test]
        public void TheBaseLevels_AreValid()
        {
            Assert.That(Validate(VeilLevel()), Is.Empty);
            Assert.That(Validate(LockLevel()), Is.Empty);
            Assert.That(Validate(ClayLevel()), Is.Empty);
        }

        // --- Veil ------------------------------------------------------------------------------

        [Test]
        public void VeilWithoutAColour_IsReported()
        {
            LevelDto level = VeilLevel();
            level.Tubes[2].Veil.Color = null;

            Assert.That(Validate(level), Is.EqualTo(new[] { $"{Name}: tubes[2]: veil.color is missing." }));
        }

        [TestCase(-1)]
        [TestCase(2)]
        public void VeilColourNotInTheLevel_IsReported(int color)
        {
            LevelDto level = VeilLevel();
            level.Tubes[2].Veil.Color = color;

            Assert.That(Validate(level), Is.EqualTo(new[] { $"{Name}: tubes[2]: veil.color is {color}; expected a colour 0 to 1." }));
        }

        [Test]
        public void VeilAndLockOnOneTube_IsReported()
        {
            LevelDto level = LockLevel();
            level.Tubes[2].WithVeil(1);

            Assert.That(Validate(level), Has.Exactly(1).Contains("tubes[2] has more than one cover (a veil and lock 0)"));
        }

        // --- Locks -----------------------------------------------------------------------------

        [Test]
        public void ALockSpanningSeveralAdjacentTubes_IsValid()
        {
            LevelDto level = Level(3, T(4, 1, 2, 0, 0).WithKey(1, 0), T(4), T(4, 2, 1, 2, 1), T(4, 0, 0, 1, 2), T(4))
                .WithLock(0, 2, 3, 4);

            Assert.That(Validate(level), Is.Empty);
        }

        [Test]
        public void LockTubesThatAreNotNextToEachOther_AreReported()
        {
            LevelDto level = LockLevel();
            level.Locks[0].Tubes = new List<int> { 2, 4 };

            Assert.That(Validate(level), Has.Exactly(1).Contains("locks[0]: tubes 2, 4 must sit next to each other in one grid row"));
        }

        [Test]
        public void LockTubesInDifferentRows_AreReported()
        {
            LevelDto level = LockLevel().WithGrid(new int?[] { 0, 1, 2 }, new int?[] { 3, 4 });

            Assert.That(Validate(level), Has.Exactly(1).Contains("locks[0]: tubes 2, 3 must sit next to each other in one grid row"));
        }

        [Test]
        public void LockTubesSplitByAGap_AreReported()
        {
            // Tube 5 is a large tube so the rows are aligned and may hold a gap.
            LevelDto level = Level(4,
                    T(4, 3, 2, 0, 0).WithKey(1, 0), T(4), T(4, 2, 1, 2, 1), T(4, 0, 0, 1, 2),
                    T(4), T(6, 3, 3, 3, 3, 3, 1))
                .WithLock(0, 2, 3)
                .WithGrid(new int?[] { 0, 1, 4, 5 }, new int?[] { 2, null, 3, 5 });

            Assert.That(Validate(level), Has.Exactly(1).Contains("locks[0]: tubes 2, 3 must sit next to each other"));
        }

        [Test]
        public void LockTubeOutOfRange_IsReported()
        {
            LevelDto level = LockLevel();
            level.Locks[0].Tubes.Add(9);

            Assert.That(Validate(level), Has.Exactly(1).Contains("locks[0]: tubes[2] is 9; expected a tube index 0 to 4."));
        }

        [Test]
        public void LockWithoutTubes_IsReported()
        {
            LevelDto level = LockLevel();
            level.Locks[0].Tubes = null;

            Assert.That(Validate(level), Has.Exactly(1).Contains("locks[0]: tubes is missing or empty."));
        }

        [Test]
        public void LockWithoutAnId_IsReported()
        {
            LevelDto level = LockLevel();
            level.Locks[0].Id = null;

            Assert.That(Validate(level), Has.Some.Contains("locks[0]: id is missing."));
        }

        [Test]
        public void TwoLocksWithOneId_AreReported()
        {
            LevelDto level = LockLevel().WithLock(0, 4);

            Assert.That(Validate(level), Has.Exactly(1).Contains("locks[1]: id 0 is already used by locks[0]."));
        }

        [Test]
        public void ATubeInTwoLocks_IsReported()
        {
            LevelDto level = LockLevel().WithLock(1, 3);

            Assert.That(Validate(level), Has.Some.Contains("tubes[3] has more than one cover (locks 0 and 1)."));
        }

        [Test]
        public void ALockWithoutAKey_IsReported()
        {
            LevelDto level = LockLevel();
            level.Tubes[0].Keys = null;

            Assert.That(Validate(level), Is.EqualTo(new[] { $"{Name}: locks[0]: lock 0 has no key." }));
        }

        [Test]
        public void ALockWithTwoKeys_IsReported()
        {
            LevelDto level = LockLevel();
            level.Tubes[0].WithKey(0, 0);

            Assert.That(Validate(level), Is.EqualTo(new[] { $"{Name}: locks[0]: lock 0 has 2 keys; expected exactly one." }));
        }

        // --- Keys ------------------------------------------------------------------------------

        [Test]
        public void AKeyForAMissingLock_IsReported()
        {
            LevelDto level = LockLevel();
            level.Tubes[0].Keys[0].Lock = 3;

            IReadOnlyList<string> errors = Validate(level);

            Assert.That(errors, Has.Some.Contains("tubes[0].keys[0]: lock 3 does not exist."));
            Assert.That(errors, Has.Some.Contains("lock 0 has no key"));
        }

        [Test]
        public void AKeyWithoutASlotOrLock_IsReported()
        {
            LevelDto level = LockLevel();
            level.Tubes[0].Keys[0].Slot = null;
            level.Tubes[0].Keys[0].Lock = null;

            IReadOnlyList<string> errors = Validate(level);

            Assert.That(errors, Has.Some.Contains("tubes[0].keys[0]: slot is missing."));
            Assert.That(errors, Has.Some.Contains("tubes[0].keys[0]: lock is missing."));
        }

        [TestCase(-1)]
        [TestCase(4)]
        public void AKeyOutsideTheTubesBalls_IsReported(int slot)
        {
            LevelDto level = LockLevel();
            level.Tubes[0].Keys[0].Slot = slot;

            Assert.That(Validate(level), Has.Exactly(1).Contains($"tubes[0].keys[0]: slot {slot} is not a ball; expected 0 to 3."));
        }

        [Test]
        public void TwoKeysOnOneBall_AreReported()
        {
            LevelDto level = LockLevel().WithLock(1, 4);
            level.Tubes[0].WithKey(1, 1);

            Assert.That(Validate(level), Has.Some.Contains("tubes[0].keys[1]: balls[1] already has a key; a ball holds at most one."));
        }

        [TestCase(2)]
        [TestCase(3)]
        public void AKeyInTheTopRunAtStart_IsReported(int slot)
        {
            // Slots 2 and 3 are tube 0's top run of colour 0.
            LevelDto level = LockLevel();
            level.Tubes[0].Keys[0].Slot = slot;

            Assert.That(Validate(level), Is.EqualTo(new[]
            {
                $"{Name}: tubes[0].keys[0]: balls[{slot}] is in the tube's top run at start, so the key would be found before the first move.",
            }));
        }

        [Test]
        public void AKeyOnAHiddenBall_IsValid()
        {
            LevelDto level = LockLevel();
            level.Tubes[0].Hidden = new List<bool> { true, true, false, false };

            Assert.That(Validate(level), Is.Empty);
        }

        [Test]
        public void AKeyOnTopOfAVeiledTube_IsValid()
        {
            // Covered, so not a selectable run until the veil opens.
            LevelDto level = Level(3,
                    T(4, 0, 0, 0), T(4, 1, 0), T(4, 1, 1, 2, 2).WithVeil(0).WithKey(3, 0), T(4), T(4, 1, 2, 2))
                .WithLock(0, 4);

            Assert.That(Validate(level), Is.Empty);
        }

        [Test]
        public void AKeyInsideItsOwnLock_IsReported()
        {
            LevelDto level = LockLevel();
            level.Tubes[0].Keys = null;
            level.Tubes[2].WithKey(0, 0);

            Assert.That(Validate(level), Is.EqualTo(new[]
            {
                $"{Name}: tubes[2].keys[0]: the key for lock 0 is inside a tube that lock 0 covers, so it can never be found.",
            }));
        }

        // --- Clay ------------------------------------------------------------------------------

        [Test]
        public void AnEmptyClayTube_IsValid()
        {
            LevelDto level = Level(2, T(4).AsClay(), T(4, 1, 0, 0, 0), T(4, 1, 1, 1, 0));

            Assert.That(Validate(level), Is.Empty);
        }

        [Test]
        public void AClayTubeWithTwoColours_IsReported()
        {
            LevelDto level = ClayLevel();
            level.Tubes[0].Balls = new List<int> { 0, 1 };
            level.Tubes[2].Balls.RemoveAt(0);

            Assert.That(Validate(level), Is.EqualTo(new[]
            {
                $"{Name}: tubes[0]: a clay tube starts empty or with one colour, but holds 0 and 1.",
            }));
        }

        [Test]
        public void AClayTubeWithAHiddenBall_IsReported()
        {
            LevelDto level = Level(2, T(4, 0, 0).AsClay(), T(4, 1, 0, 0), T(4, 1, 1, 1), T(4));
            level.Tubes[0].Hidden = new List<bool> { true, false };

            Assert.That(Validate(level), Is.EqualTo(new[] { $"{Name}: tubes[0]: a clay tube cannot hold hidden balls." }));
        }

        [Test]
        public void AKeyInAClayTube_IsReported()
        {
            LevelDto level = Level(3,
                    T(4, 1, 0, 0), T(4, 2).AsClay().WithKey(0, 0), T(4, 2, 2, 2, 1), T(4, 1, 1, 0, 0), T(4))
                .WithLock(0, 3);

            Assert.That(Validate(level), Is.EqualTo(new[] { $"{Name}: tubes[1]: a clay tube cannot hold keys." }));
        }
    }
}
