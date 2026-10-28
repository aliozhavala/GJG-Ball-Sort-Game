using System.Collections.Generic;
using BallSort.Core.Levels;
using BallSort.LevelJson;
using NUnit.Framework;
using static BallSort.Tests.EditMode.TestLevels;

namespace BallSort.Tests.EditMode
{
    /// <summary>
    /// Format, tube, colour-count, grid and start-state validation. Veils, locks, keys and clay are
    /// in <see cref="MechanicsValidationTests"/>; the grid rules in detail in <see cref="LevelLayoutTests"/>.
    /// </summary>
    [TestFixture]
    public sealed class LevelValidatorTests
    {
        private static IReadOnlyList<string> Validate(LevelDto level) => LevelValidator.Validate(level, Name);

        [Test]
        public void ValidLevel_HasNoErrors()
        {
            Assert.That(Validate(Valid()), Is.Empty);
        }

        [Test]
        public void EveryMessage_IsPrefixedWithLevelName()
        {
            LevelDto level = Valid();
            level.FormatVersion = 9;
            level.Tubes[0].Capacity = 2;

            IReadOnlyList<string> errors = Validate(level);

            Assert.That(errors.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(errors, Has.All.StartsWith(Name + ": "));
        }

        [Test]
        public void NullLevel_IsReported()
        {
            Assert.That(Validate(null), Has.Exactly(1).Contains("empty"));
        }

        [Test]
        public void MissingFormatVersion_IsReported()
        {
            LevelDto level = Valid();
            level.FormatVersion = 0;

            Assert.That(Validate(level), Has.Exactly(1).Contains("formatVersion is missing"));
        }

        [Test]
        public void UnsupportedFormatVersion_IsReported()
        {
            LevelDto level = Valid();
            level.FormatVersion = 2;

            Assert.That(Validate(level), Has.Exactly(1).Contains("formatVersion 2 is not supported"));
        }

        [Test]
        public void MissingId_IsReported()
        {
            LevelDto level = Valid();
            level.Id = 0;

            Assert.That(Validate(level), Has.Exactly(1).Contains("id must be"));
        }

        [Test]
        public void MissingTubes_IsReported()
        {
            LevelDto level = Valid();
            level.Tubes = null;

            Assert.That(Validate(level), Has.Exactly(1).Contains("tubes is missing"));
        }

        [Test]
        public void MissingBalls_NamesTheTube()
        {
            LevelDto level = Valid();
            level.Tubes[2].Balls = null;

            Assert.That(Validate(level), Has.Exactly(1).Contains("tubes[2]: balls is missing"));
        }

        [Test]
        public void TubeOverCapacity_NamesTheTube()
        {
            LevelDto level = Level(2, T(4, 0, 1, 0, 1), T(4, 1, 0, 1, 0, 1), T(4));

            Assert.That(Validate(level), Has.Some.Contains("tubes[1]: holds 5 balls but capacity is 4"));
        }

        [TestCase(0)]
        [TestCase(3)]
        [TestCase(17)]
        public void CapacityOutsideFourToSixteen_NamesTheTube(int capacity)
        {
            LevelDto level = Valid();
            level.Tubes[2].Capacity = capacity;

            Assert.That(Validate(level), Has.Some.Contains($"tubes[2]: capacity must be 4, or 5 to 16 for a large tube (was {capacity})"));
        }

        [Test]
        public void ColourOutOfRange_NamesTubeAndSlot()
        {
            LevelDto level = Level(2, T(4, 0, 1, 0, 1), T(4, 1, 0, 1, 2), T(4));

            Assert.That(Validate(level), Has.Some.Contains("tubes[1]: balls[3] has colour 2"));
        }

        [Test]
        public void HiddenTopBall_NamesTheTube()
        {
            LevelDto level = Level(2,
                Hidden(4, new[] { 0, 1, 0, 1 }, new[] { true, false, false, true }),
                T(4, 1, 0, 1, 0), T(4));

            Assert.That(Validate(level), Has.Exactly(1).Contains("tubes[0]: top ball is hidden"));
        }

        [Test]
        public void HiddenBelowRevealedTop_IsValid()
        {
            LevelDto level = Level(2,
                Hidden(4, new[] { 0, 1, 0, 1 }, new[] { true, true, true, false }),
                T(4, 1, 0, 1, 0), T(4));

            Assert.That(Validate(level), Is.Empty);
        }

        [Test]
        public void HiddenLengthMismatch_NamesTheTube()
        {
            LevelDto level = Level(2,
                Hidden(4, new[] { 0, 1, 0, 1 }, new[] { true, false }),
                T(4, 1, 0, 1, 0), T(4));

            Assert.That(Validate(level), Has.Exactly(1).Contains("tubes[0]: hidden has 2 entries but balls has 4"));
        }

        // --- Colour counts ---------------------------------------------------------------------

        [Test]
        public void ColourWithTooFewBalls_IsReported()
        {
            LevelDto level = Level(2, T(4, 0, 1, 0, 1), T(4, 1, 0, 1), T(4));

            Assert.That(Validate(level), Has.Exactly(1).Contains("colour(s) 0 have 3 balls"));
        }

        [Test]
        public void UnusedColour_IsReported()
        {
            LevelDto level = Valid();
            level.Colors = 3;

            Assert.That(Validate(level), Has.Exactly(1).Contains("colour 2 has no balls"));
        }

        [Test]
        public void MoreColoursOfACountThanTubesOfThatCapacity_IsReported()
        {
            // Colours 0 and 1 have five balls each, but only one capacity-5 tube exists.
            LevelDto level = Level(2, T(5, 0, 1, 0, 1, 0), T(4, 1, 0, 1, 0), T(4, 1))
                .WithGrid(new int?[] { 0, 1 }, new int?[] { 0, 2 });

            Assert.That(Validate(level), Has.Exactly(1).Contains("needs 2 tube(s) of capacity 5; the level has 1"));
        }

        [Test]
        public void LargeAndNormalTubes_WithMatchingColourCounts_AreValid()
        {
            // Colour 1 has six balls and a capacity-6 tube; colour 0 has four and capacity-4 tubes.
            LevelDto level = Level(2, T(6, 1, 0, 1, 0, 1, 1), T(4, 1, 0, 1, 0), T(4))
                .WithGrid(new int?[] { 0, 1 }, new int?[] { 0, 2 });

            Assert.That(Validate(level), Is.Empty);
        }

        // --- Grid ------------------------------------------------------------------------------

        [Test]
        public void MissingLayout_IsReported()
        {
            LevelDto level = Valid();
            level.Layout = null;

            Assert.That(Validate(level), Has.Exactly(1).Contains("layout is missing"));
        }

        [Test]
        public void LayoutWithoutGrid_IsReported()
        {
            LevelDto level = Valid();
            level.Layout = new LayoutDto();

            Assert.That(Validate(level), Has.Exactly(1).Contains("layout.grid is missing"));
        }

        [Test]
        public void AGridError_IsPrefixedLikeEveryOther()
        {
            LevelDto level = Valid().WithGrid(new int?[] { 0, 1 });

            Assert.That(Validate(level), Is.EqualTo(new[] { $"{Name}: tubes[2] is not on layout.grid." }));
        }

        [Test]
        public void ALevelFileWithSevenCellsInARow_IsRejected()
        {
            const string json = @"{
              ""formatVersion"": 1, ""id"": 1, ""colors"": 1,
              ""layout"": { ""grid"": [[0, 1, 2, 3, 4, 5, 6]] },
              ""tubes"": [
                { ""capacity"": 4, ""balls"": [0, 0] }, { ""capacity"": 4, ""balls"": [0, 0] },
                { ""capacity"": 4, ""balls"": [] }, { ""capacity"": 4, ""balls"": [] }, { ""capacity"": 4, ""balls"": [] },
                { ""capacity"": 4, ""balls"": [] }, { ""capacity"": 4, ""balls"": [] }
              ]
            }";
            LevelDto level = LevelJsonParser.Parse(json, Name);

            Assert.That(Validate(level), Is.EqualTo(new[] { $"{Name}: layout.grid[0] has 7 cell(s); level files allow 1 to 6." }));
        }

        // --- Start state -----------------------------------------------------------------------

        [Test]
        public void MultipleProblems_AreAllReported()
        {
            LevelDto level = Level(2,
                Hidden(4, new[] { 0, 1, 0, 1 }, new[] { false, false, false, true }),
                T(4, 1, 0, 1, 0, 1), T(4));

            IReadOnlyList<string> errors = Validate(level);

            Assert.That(errors, Has.Some.Contains("tubes[0]: top ball is hidden"));
            Assert.That(errors, Has.Some.Contains("tubes[1]: holds 5 balls"));
        }

        [Test]
        public void ATubeAlreadyCompletedAtStart_IsReported()
        {
            LevelDto level = Level(2, T(4, 0, 0, 0, 0), T(4, 1, 1), T(4, 1, 1));

            Assert.That(Validate(level), Is.EqualTo(new[] { $"{Name}: tubes[0] is already completed at start." }));
        }

        [Test]
        public void FourOfEightInACapacityFourTube_IsNotCompletedAtStart()
        {
            LevelDto level = Level(1, T(4, 0, 0, 0, 0), T(8, 0, 0, 0, 0))
                .WithGrid(new int?[] { 0, 1 }, new int?[] { null, 1 });

            Assert.That(Validate(level), Is.Empty);
        }

        [Test]
        public void AFinishedSetBehindACover_IsReported()
        {
            // It could never be played, and opening the cover would complete it out of nowhere.
            LevelDto level = Level(2, T(4, 0, 0, 0, 0).WithVeil(1), T(4, 1, 1), T(4, 1, 1));

            Assert.That(Validate(level), Is.EqualTo(new[] { $"{Name}: tubes[0] already holds a finished set behind its cover." }));
        }

        [Test]
        public void ALevelAlreadyWonAtStart_IsReported()
        {
            LevelDto level = Level(1, T(4, 0, 0, 0, 0), T(4));

            Assert.That(Validate(level), Is.EqualTo(new[] { $"{Name}: is already won at start." }));
        }

        [Test]
        public void ALevelWithNoLegalMoveAtStart_IsReported()
        {
            LevelDto level = Level(2, T(4, 0, 1, 0, 1), T(4, 1, 0, 1, 0));

            Assert.That(Validate(level), Is.EqualTo(new[] { $"{Name}: has no legal move at start." }));
        }

        [Test]
        public void ALevelWhoseOnlyMovesAreBlockedByCoversAndClay_HasNoLegalMoveAtStart()
        {
            LevelDto level = Level(2, T(4, 0, 0, 1, 1).WithVeil(0), T(4, 0, 0).AsClay(), T(4, 1, 1));

            Assert.That(Validate(level), Is.EqualTo(new[] { $"{Name}: has no legal move at start." }));
        }

        [Test]
        public void AFullSingleColourTubeWithAHiddenBall_IsNotCompletedAtStart()
        {
            LevelDto level = Level(1, Hidden(4, new[] { 0, 0, 0, 0 }, new[] { true, false, false, false }), T(4));

            Assert.That(Validate(level), Is.Empty);
        }

        [Test]
        public void StartStateChecks_AreSkippedWhileTheStructureIsBroken()
        {
            // Building a board from malformed data could throw; structural errors come alone.
            LevelDto level = Level(2, T(4, 0, 1, 0, 1), T(4, 1, 0, 1, 0));
            level.FormatVersion = 9;

            Assert.That(Validate(level), Has.None.Contains("legal move"));
        }
    }
}
