using System.Collections.Generic;
using BallSort.Core.Levels;
using NUnit.Framework;

namespace BallSort.Tests.EditMode
{
    [TestFixture]
    public sealed class LevelLayoutTests
    {
        [TestCase(1, new[] { 1 })]
        [TestCase(5, new[] { 5 })]
        [TestCase(6, new[] { 6 })]
        [TestCase(7, new[] { 4, 3 })]
        [TestCase(8, new[] { 4, 4 })]
        [TestCase(9, new[] { 5, 4 })]
        [TestCase(11, new[] { 6, 5 })]
        [TestCase(12, new[] { 6, 6 })]
        [TestCase(13, new[] { 5, 4, 4 })]
        [TestCase(14, new[] { 5, 5, 4 })]
        [TestCase(17, new[] { 6, 6, 5 })]
        [TestCase(18, new[] { 6, 6, 6 })]
        public void AutoRows_UsesFewestEvenRows_ExtraTubesOnTop(int tubeCount, int[] expected)
        {
            Assert.That(LevelLayout.AutoRows(tubeCount), Is.EqualTo(expected));
        }

        [Test]
        public void AutoRows_ForEveryCount_RespectsRowRules()
        {
            for (int n = 1; n <= LevelLayout.MaxTubes; n++)
            {
                int[] rows = LevelLayout.AutoRows(n);
                var errors = new List<string>();

                Assert.That(LevelLayout.ValidateRows(rows, n, errors), Is.True, $"{n} tubes: {string.Join("; ", errors)}");
                Assert.That(rows.Length, Is.EqualTo((n + 5) / 6), $"{n} tubes should use the fewest rows.");
                for (int i = 1; i < rows.Length; i++)
                {
                    Assert.That(rows[i - 1] - rows[i], Is.InRange(0, 1), $"{n} tubes: rows must be even, larger on top.");
                }
            }
        }

        [TestCase(0)]
        [TestCase(19)]
        public void AutoRows_OutsideSupportedRange_Throws(int tubeCount)
        {
            Assert.That(() => LevelLayout.AutoRows(tubeCount), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void ValidateRows_AcceptsExplicitUnevenRows()
        {
            var errors = new List<string>();

            Assert.That(LevelLayout.ValidateRows(new[] { 3, 4 }, 7, errors), Is.True);
            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void ValidateRows_RejectsWrongSum()
        {
            var errors = new List<string>();

            Assert.That(LevelLayout.ValidateRows(new[] { 4, 3 }, 8, errors), Is.False);
            Assert.That(errors, Has.Exactly(1).Contains("sums to 7").And.Contains("8 tube"));
        }

        [Test]
        public void ValidateRows_RejectsRowOverSix()
        {
            var errors = new List<string>();

            Assert.That(LevelLayout.ValidateRows(new[] { 7, 1 }, 8, errors), Is.False);
            Assert.That(errors, Has.Exactly(1).Contains("layout.rows[0]"));
        }

        [Test]
        public void ValidateRows_RejectsEmptyRow()
        {
            var errors = new List<string>();

            Assert.That(LevelLayout.ValidateRows(new[] { 4, 0, 3 }, 7, errors), Is.False);
            Assert.That(errors, Has.Exactly(1).Contains("layout.rows[1]"));
        }

        [Test]
        public void ValidateRows_RejectsMoreThanThreeRows()
        {
            var errors = new List<string>();

            Assert.That(LevelLayout.ValidateRows(new[] { 1, 1, 1, 1 }, 4, errors), Is.False);
            Assert.That(errors, Has.Exactly(1).Contains("4 row(s)"));
        }

        [Test]
        public void Resolve_PrefersExplicitRows()
        {
            Assert.That(LevelLayout.Resolve(7, new[] { 3, 4 }), Is.EqualTo(new[] { 3, 4 }));
        }

        [Test]
        public void Resolve_WithoutExplicitRows_UsesAutoRows()
        {
            Assert.That(LevelLayout.Resolve(7, null), Is.EqualTo(new[] { 4, 3 }));
        }
    }
}
