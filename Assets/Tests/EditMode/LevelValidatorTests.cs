using System.Collections.Generic;
using BallSort.Core.Levels;
using NUnit.Framework;
using static BallSort.Tests.EditMode.TestLevels;

namespace BallSort.Tests.EditMode
{
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
            LevelDto level = Level(2, T(4, 0, 1, 0, 1), T(3, 1, 0, 1, 0), T(4));

            Assert.That(Validate(level), Has.Some.Contains("tubes[1]: holds 4 balls but capacity is 3"));
        }

        [Test]
        public void ZeroCapacity_NamesTheTube()
        {
            LevelDto level = Valid();
            level.Tubes[2].Capacity = 0;

            Assert.That(Validate(level), Has.Some.Contains("tubes[2]: capacity must be"));
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
            // Colours 0 and 1 have 2 balls each but only one tube of capacity 2 exists.
            LevelDto level = Level(2, T(4, 0, 1, 0, 1), T(2), T(4));

            Assert.That(Validate(level), Has.Exactly(1).Contains("needs 2 tube(s) of capacity 2; the level has 1"));
        }

        [Test]
        public void VariableCapacity_WithMatchingTubes_IsValid()
        {
            // Colour 0 has 3 balls and colour 1 has 5; one tube of each capacity exists.
            LevelDto level = Level(2, T(5, 1, 0, 1, 0, 1), T(3, 1, 0, 1), T(5));

            Assert.That(Validate(level), Is.Empty);
        }

        [Test]
        public void ExplicitRows_AreValidated()
        {
            LevelDto level = Valid();
            level.Layout = new LayoutDto { Rows = new List<int> { 2, 2 } };

            Assert.That(Validate(level), Has.Exactly(1).Contains("sums to 4 but the level has 3"));
        }

        [Test]
        public void LayoutWithoutRows_IsReported()
        {
            LevelDto level = Valid();
            level.Layout = new LayoutDto();

            Assert.That(Validate(level), Has.Exactly(1).Contains("layout.rows is missing"));
        }

        [Test]
        public void TooManyTubesForAutoLayout_IsReported()
        {
            var tubes = new List<TubeDto> { T(4, 0, 0, 0, 0) };
            for (int i = 0; i < LevelLayout.MaxTubes; i++)
            {
                tubes.Add(T(4));
            }

            LevelDto level = Level(1, tubes.ToArray());

            Assert.That(Validate(level), Has.Exactly(1).Contains("at most 18 fit"));
        }

        [Test]
        public void MultipleProblems_AreAllReported()
        {
            LevelDto level = Level(2,
                Hidden(4, new[] { 0, 1, 0, 1 }, new[] { false, false, false, true }),
                T(3, 1, 0, 1, 0), T(4));

            IReadOnlyList<string> errors = Validate(level);

            Assert.That(errors, Has.Some.Contains("tubes[0]: top ball is hidden"));
            Assert.That(errors, Has.Some.Contains("tubes[1]: holds 4 balls"));
        }
    }
}
