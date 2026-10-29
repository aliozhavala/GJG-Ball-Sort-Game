using System.Collections.Generic;
using BallSort.Core.Levels;
using NUnit.Framework;
using static BallSort.Tests.EditMode.TestLevels;

namespace BallSort.Tests.EditMode
{
    /// <summary>
    /// The level file as a whole: format version, id, tier, the five variants, and the rule that they
    /// share one board (docs/levels.md, Variant consistency). The per-variant rules are in
    /// <see cref="LevelValidatorTests"/> and <see cref="MechanicsValidationTests"/>.
    /// </summary>
    [TestFixture]
    public sealed class LevelFileValidationTests
    {
        private static IReadOnlyList<string> Validate(LevelDto file) => LevelValidator.Validate(file, Name);

        /// <summary>Two rows; a veil on tube 2 and a lock over the empty tubes 4 and 5, its key in tube 0.</summary>
        private static TestLevel Covered() =>
            Level(3,
                    T(4, 1, 2, 0, 0).WithKey(1, 0),
                    T(4, 0, 0, 1, 2),
                    T(4, 2, 1, 2, 1).WithVeil(0),
                    T(4),
                    T(4),
                    T(4))
                .WithGrid(new int?[] { 0, 1, 2 }, new int?[] { 3, 4, 5 })
                .WithLock(0, 4, 5);

        private static List<int> Balls(params int[] balls) => new List<int>(balls);

        [Test]
        public void ValidFiles_HaveNoErrors()
        {
            Assert.That(Validate(Valid().ToFile()), Is.Empty);
            Assert.That(Validate(Covered().ToFile()), Is.Empty);
        }

        [Test]
        public void NullFile_IsReported()
        {
            Assert.That(Validate(null), Is.EqualTo(new[] { $"{Name}: file is empty or not a JSON object." }));
        }

        // --- Header ----------------------------------------------------------------------------

        [Test]
        public void FormatVersion1_IsRejectedWithOnlyAConversionMessage()
        {
            LevelDto file = Valid().ToFile();
            file.FormatVersion = 1;
            file.Id = 0;
            file.Variants = null;

            IReadOnlyList<string> errors = Validate(file);

            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0], Does.StartWith($"{Name}: formatVersion 1 is no longer supported: convert the file to version 2"));
        }

        [TestCase(0, "formatVersion is missing (expected 2).")]
        [TestCase(3, "formatVersion 3 is not supported (expected 2).")]
        public void OtherFormatVersions_AreRejected(int version, string message)
        {
            LevelDto file = Valid().ToFile();
            file.FormatVersion = version;

            Assert.That(Validate(file), Is.EqualTo(new[] { $"{Name}: {message}" }));
        }

        [Test]
        public void MissingId_IsReported()
        {
            LevelDto file = Valid().ToFile();
            file.Id = 0;

            Assert.That(Validate(file), Is.EqualTo(new[] { $"{Name}: id must be 1 or greater (was 0)." }));
        }

        [Test]
        public void MissingTier_IsReported()
        {
            LevelDto file = Valid().ToFile();
            file.Tier = null;

            Assert.That(Validate(file), Is.EqualTo(new[] { $"{Name}: tier is missing (expected regular, hard or superHard)." }));
        }

        [Test]
        public void UndefinedTier_IsReported()
        {
            LevelDto file = Valid().ToFile();
            file.Tier = (LevelTier)7;

            Assert.That(Validate(file), Has.Exactly(1).Contains("tier 7 is not a tier"));
        }

        [TestCase(LevelTier.Regular)]
        [TestCase(LevelTier.Hard)]
        [TestCase(LevelTier.SuperHard)]
        public void EveryTier_IsValid(LevelTier tier)
        {
            Assert.That(Validate(Valid().ToFile(tier)), Is.Empty);
        }

        [Test]
        public void MissingLayout_IsReportedOnce()
        {
            LevelDto file = Valid().ToFile();
            file.Layout = null;

            Assert.That(Validate(file), Is.EqualTo(new[] { $"{Name}: layout is missing; every level lists its tubes in layout.grid." }));
        }

        // --- Variants --------------------------------------------------------------------------

        [Test]
        public void MissingVariants_IsReported()
        {
            LevelDto file = Valid().ToFile();
            file.Variants = null;

            Assert.That(Validate(file), Is.EqualTo(new[] { $"{Name}: variants is missing or empty; a level has exactly 5." }));
        }

        [TestCase(4)]
        [TestCase(6)]
        public void AVariantCountOtherThanFive_IsReported(int count)
        {
            LevelDto file = Valid().ToFile();
            while (file.Variants.Count > count)
            {
                file.Variants.RemoveAt(file.Variants.Count - 1);
            }

            while (file.Variants.Count < count)
            {
                file.Variants.Add(Valid().CopyVariant());
            }

            Assert.That(Validate(file), Is.EqualTo(new[] { $"{Name}: variants has {count} entries; a level has exactly 5." }));
        }

        [Test]
        public void ANullVariant_IsReportedByNumber()
        {
            LevelDto file = Valid().ToFile();
            file.Variants[1] = null;

            Assert.That(Validate(file), Is.EqualTo(new[] { $"{Name}: variant 2 is null." }));
        }

        [Test]
        public void AProblemInsideAVariant_NamesTheLevelTheVariantAndTheTube()
        {
            LevelDto file = Valid().ToFile();
            file.Variants[2].Tubes[1].Hidden = new List<bool> { false, false, false, true };

            Assert.That(Validate(file), Is.EqualTo(new[]
            {
                $"{Name}: variant 3: tubes[1]: top ball is hidden; every tube's top ball must start revealed.",
            }));
        }

        [Test]
        public void EachVariant_GetsTheStartStateChecks()
        {
            LevelDto file = Valid().ToFile();
            file.Variants[4].Tubes[0].Balls = Balls(0, 0, 0, 0);
            file.Variants[4].Tubes[1].Balls = Balls(1, 1, 1, 1);

            Assert.That(Validate(file), Is.EqualTo(new[] { $"{Name}: variant 5: is already won at start." }));
        }

        // --- Consistency -----------------------------------------------------------------------

        [Test]
        public void BallsHiddenFlagsKeysVeilColoursLockIdsAndColourCounts_MayDiffer()
        {
            LevelDto file = Covered().ToFile();

            // V2: other balls and a hidden ball.
            file.Variants[1].Tubes[0].Balls = Balls(2, 1, 0, 0);
            file.Variants[1].Tubes[1].Balls = Balls(0, 0, 2, 1);
            file.Variants[1].Tubes[1].Hidden = new List<bool> { true, false, false, false };

            // V3: the key on another ball of another tube, and another lock id (door colour).
            file.Variants[2].Tubes[0].Keys = null;
            file.Variants[2].Tubes[1].Keys = new List<KeyDto> { new KeyDto { Slot = 1, Lock = 3 } };
            file.Variants[2].Locks[0].Id = 3;

            // V4: the veil opened by another colour.
            file.Variants[3].Tubes[2].Veil.Color = 1;

            // V5: two colours instead of three, on the same tubes; the veiled tube starts empty.
            LevelVariantDto twoColours = file.Variants[4];
            twoColours.Colors = 2;
            twoColours.Tubes[0].Balls = Balls(1, 1, 0, 0);
            twoColours.Tubes[1].Balls = Balls(0, 0, 1, 1);
            twoColours.Tubes[2].Balls = Balls();

            Assert.That(Validate(file), Is.Empty);
        }

        [Test]
        public void ADifferentTubeCount_IsReportedOnceForThatVariant()
        {
            LevelDto file = Valid().ToFile();
            file.Variants[3].Tubes.Add(T(4));

            Assert.That(Validate(file), Has.Exactly(1).EqualTo($"{Name}: variant 4: has 4 tubes, but variant 1 has 3."));
        }

        [Test]
        public void ADifferentCapacity_NamesTheVariantAndTube()
        {
            LevelDto file = Level(2, T(4, 0, 1, 0, 1), T(4, 1, 0, 1, 0), T(4), T(4)).ToFile();
            file.Variants[1].Tubes[3].Capacity = 5;

            Assert.That(Validate(file), Is.EqualTo(new[] { $"{Name}: variant 2: tubes[3]: capacity is 5, but variant 1 has 4." }));
        }

        [Test]
        public void ADifferentClayFlag_NamesTheVariantAndTube()
        {
            LevelDto file = Level(2, T(4, 0, 1, 0, 1), T(4, 1, 0, 1, 0), T(4), T(4)).ToFile();
            file.Variants[2].Tubes[3].Clay = true;

            Assert.That(Validate(file), Is.EqualTo(new[] { $"{Name}: variant 3: tubes[3]: is clay, but not in variant 1." }));
        }

        [Test]
        public void AMissingVeil_NamesTheVariantAndTube()
        {
            LevelDto file = Covered().ToFile();
            file.Variants[1].Tubes[2].Veil = null;

            Assert.That(Validate(file), Is.EqualTo(new[] { $"{Name}: variant 2: tubes[2]: has no cover, but variant 1 has a veil." }));
        }

        [Test]
        public void ALockOverOtherTubes_IsReportedAsCoverDifferences()
        {
            LevelDto file = Covered().ToFile();
            file.Variants[4].Locks[0].Tubes = new List<int> { 3, 4 };

            Assert.That(Validate(file), Is.EqualTo(new[]
            {
                $"{Name}: variant 5: tubes[3]: has a lock, but variant 1 has no cover.",
                $"{Name}: variant 5: tubes[5]: has no cover, but variant 1 has a lock.",
            }));
        }

        [Test]
        public void TheSameTubesUnderTwoLocksInsteadOfOne_AreReported()
        {
            LevelDto file = Covered().ToFile();
            LevelVariantDto split = file.Variants[1];
            split.Locks[0].Tubes = new List<int> { 4 };
            split.Locks.Add(new LockDto { Id = 1, Tubes = new List<int> { 5 } });
            split.Tubes[1].Keys = new List<KeyDto> { new KeyDto { Slot = 1, Lock = 1 } };

            Assert.That(Validate(file), Is.EqualTo(new[]
            {
                $"{Name}: variant 2: locks[0] covers tubes 4, but no lock in variant 1 covers exactly those tubes.",
                $"{Name}: variant 2: locks[1] covers tubes 5, but no lock in variant 1 covers exactly those tubes.",
                $"{Name}: variant 2: no lock covers tubes 4, 5, as one lock does in variant 1.",
            }));
        }
    }
}
