using System;
using System.Collections.Generic;
using BallSort.Core.Levels;
using NUnit.Framework;

namespace BallSort.Tests.EditMode
{
    /// <summary>Capacities, row spans and <c>layout.grid</c> validation (docs/mechanics.md).</summary>
    [TestFixture]
    public sealed class LevelLayoutTests
    {
        private static List<string> Errors(int[] capacities, params int?[][] grid)
        {
            var errors = new List<string>();
            bool valid = LevelLayout.ValidateGrid(grid, capacities, errors);
            Assert.That(valid, Is.EqualTo(errors.Count == 0), "the result matches the error list");
            return errors;
        }

        private static int[] Capacities(params int[] capacities) => capacities;

        // --- Capacities and spans --------------------------------------------------------------

        [TestCase(4, 1)]
        [TestCase(5, 2)]
        [TestCase(10, 2)]
        [TestCase(11, 3)]
        [TestCase(16, 3)]
        public void GetRowSpan_FollowsTheRowGeometry(int capacity, int span)
        {
            Assert.That(LevelLayout.GetRowSpan(capacity), Is.EqualTo(span));
        }

        [TestCase(0)]
        [TestCase(3)]
        [TestCase(17)]
        public void InvalidCapacities_AreRejected(int capacity)
        {
            Assert.That(LevelLayout.IsValidCapacity(capacity), Is.False);
            Assert.That(() => LevelLayout.GetRowSpan(capacity), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void SpanningTubes_FillTheirRowsExactlyAtTenAndSixteen()
        {
            // n rows are n × 4 slots plus (n − 1) gaps of 2.
            Assert.That(2 * LevelLayout.RowPitchSlots - LevelLayout.RowGapSlots, Is.EqualTo(10));
            Assert.That(3 * LevelLayout.RowPitchSlots - LevelLayout.RowGapSlots, Is.EqualTo(LevelLayout.MaxCapacity));
        }

        // --- Valid grids -----------------------------------------------------------------------

        [Test]
        public void OneFreeRow_IsValid()
        {
            Assert.That(Errors(Capacities(4, 4, 4), new int?[] { 0, 1, 2 }), Is.Empty);
        }

        [Test]
        public void ThreeRows_WithAFreeTopRowAndALargeTubeAcrossTheOthers_AreValid()
        {
            // The docs example: row 0 free, rows 1–2 aligned, with a gap in the shorter aligned row.
            List<string> errors = Errors(Capacities(4, 4, 4, 8, 4, 4),
                new int?[] { 0, 1 },
                new int?[] { 3, 2, null },
                new int?[] { 3, 4, 5 });

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void ACapacitySixteenTube_SpanningAllThreeRows_IsValid()
        {
            Assert.That(Errors(Capacities(16, 4, 4, 4),
                new int?[] { 0, 1 },
                new int?[] { 0, 2 },
                new int?[] { 0, 3 }), Is.Empty);
        }

        // --- Rows and cells --------------------------------------------------------------------

        [Test]
        public void NoRows_IsReported()
        {
            Assert.That(Errors(Capacities(4)), Has.Exactly(1).Contains("layout.grid has 0 row(s)"));
        }

        [Test]
        public void FourRows_IsReported()
        {
            Assert.That(Errors(Capacities(4, 4, 4, 4),
                    new int?[] { 0 }, new int?[] { 1 }, new int?[] { 2 }, new int?[] { 3 }),
                Has.Exactly(1).Contains("layout.grid has 4 row(s); expected 1 to 3."));
        }

        [Test]
        public void SevenCellsInARow_IsReported()
        {
            Assert.That(Errors(Capacities(4, 4, 4, 4, 4, 4, 4), new int?[] { 0, 1, 2, 3, 4, 5, 6 }),
                Is.EqualTo(new[] { "layout.grid[0] has 7 cell(s); level files allow 1 to 6." }));
        }

        [Test]
        public void AnEmptyOrNullRow_IsReported()
        {
            Assert.That(Errors(Capacities(4), new int?[] { 0 }, new int?[0]), Has.Some.Contains("layout.grid[1] has 0 cell(s)"));
            Assert.That(Errors(Capacities(4), new int?[] { 0 }, null), Has.Some.Contains("layout.grid[1] has 0 cell(s)"));
        }

        [Test]
        public void ACellNamingNoTube_IsReported()
        {
            Assert.That(Errors(Capacities(4, 4), new int?[] { 0, 1, 2 }),
                Has.Exactly(1).Contains("layout.grid[0][2] is 2; expected a tube index 0 to 1 or null."));
        }

        [Test]
        public void ATubeMissingFromTheGrid_IsReported()
        {
            Assert.That(Errors(Capacities(4, 4, 4), new int?[] { 0, 2 }),
                Is.EqualTo(new[] { "tubes[1] is not on layout.grid." }));
        }

        [Test]
        public void ANormalTubeListedTwice_IsReported()
        {
            Assert.That(Errors(Capacities(4, 4), new int?[] { 0, 1 }, new int?[] { 0 }),
                Has.Exactly(1).Contains("tubes[0] appears 2 times in layout.grid; a capacity-4 tube takes one cell."));
        }

        [Test]
        public void AGapInAFreeRow_IsReported()
        {
            Assert.That(Errors(Capacities(4, 4), new int?[] { 0, null, 1 }),
                Is.EqualTo(new[] { "layout.grid[0][1] is null; gaps are only allowed in rows holding part of a large tube." }));
        }

        [Test]
        public void AGapInTheFreeRowOfAMixedGrid_IsReported()
        {
            List<string> errors = Errors(Capacities(6, 4, 4),
                new int?[] { 1, null },
                new int?[] { 0, 2 },
                new int?[] { 0 });

            Assert.That(errors, Has.Exactly(1).Contains("layout.grid[0][1] is null"));
        }

        // --- Large tubes -----------------------------------------------------------------------

        [Test]
        public void ALargeTubeListedOnce_IsReported()
        {
            Assert.That(Errors(Capacities(6, 4), new int?[] { 0, 1 }),
                Has.Exactly(1).Contains("tubes[0] has capacity 6, so it must appear in 2 consecutive rows"));
        }

        [Test]
        public void ALargeTubeInDifferentColumns_IsReported()
        {
            Assert.That(Errors(Capacities(6, 4), new int?[] { 0, 1 }, new int?[] { 1, 0 }),
                Has.Some.Contains("tubes[0] has capacity 6").And.Contains("found it at [0][0], [1][1]"));
        }

        [Test]
        public void ALargeTubeInRowsThatAreNotConsecutive_IsReported()
        {
            List<string> errors = Errors(Capacities(6, 4, 4),
                new int?[] { 0, 1 },
                new int?[] { 2 },
                new int?[] { 0 });

            Assert.That(errors, Has.Some.Contains("tubes[0] has capacity 6, so it must appear in 2 consecutive rows"));
        }

        [Test]
        public void ALargeTubeTwiceInOneRow_IsReported()
        {
            Assert.That(Errors(Capacities(6, 4), new int?[] { 0, 0, 1 }),
                Has.Exactly(1).Contains("tubes[0] has capacity 6"));
        }

        [Test]
        public void ACapacitySixteenTube_InATwoRowGrid_IsReported()
        {
            Assert.That(Errors(Capacities(16, 4), new int?[] { 0, 1 }, new int?[] { 0 }),
                Has.Exactly(1).Contains("tubes[0] has capacity 16, so it must appear in 3 consecutive rows"));
        }

        [Test]
        public void ALargeTubeWithTheWrongSpan_DoesNotMakeItsRowsAligned()
        {
            // The broken tube is reported once; its rows stay free, so the gap is reported too.
            List<string> errors = Errors(Capacities(12, 4), new int?[] { 0, null }, new int?[] { 0, 1 });

            Assert.That(errors, Has.Some.Contains("tubes[0] has capacity 12"));
            Assert.That(errors, Has.Some.Contains("layout.grid[0][1] is null"));
        }

        [Test]
        public void AnInvalidCapacity_SkipsTheSpanCheck()
        {
            // The capacity itself is reported by the level validator; no second error here.
            Assert.That(Errors(Capacities(3, 4), new int?[] { 0, 1 }), Is.Empty);
        }

        [Test]
        public void ANullGrid_IsReported()
        {
            var errors = new List<string>();

            Assert.That(LevelLayout.ValidateGrid(null, Capacities(4), errors), Is.False);
            Assert.That(errors, Has.Exactly(1).Contains("layout.grid is missing"));
        }
    }
}
