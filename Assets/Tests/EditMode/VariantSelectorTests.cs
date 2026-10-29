using System;
using System.Collections.Generic;
using BallSort.Core.Levels;
using NUnit.Framework;

namespace BallSort.Tests.EditMode
{
    /// <summary>The try rules of docs/levels.md: V1–V5 in order, then V3–V5 at random, never twice in a row.</summary>
    [TestFixture]
    public sealed class VariantSelectorTests
    {
        /// <summary>A random source that returns the given values, in turn, from <see cref="Random.Next(int)"/>.</summary>
        private sealed class ScriptedRandom : Random
        {
            private readonly Queue<int> _values;

            public ScriptedRandom(params int[] values) => _values = new Queue<int>(values);

            public List<int> Bounds { get; } = new List<int>();

            public override int Next(int maxValue)
            {
                Bounds.Add(maxValue);
                return _values.Dequeue();
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void TriesOneToFive_PlayTheirOwnVariant_WithoutRandomness(int tryNumber)
        {
            Assert.That(VariantSelector.Select(tryNumber, tryNumber - 1, null), Is.EqualTo(tryNumber));
        }

        [Test]
        public void TriesOneToFive_IgnoreThePreviousVariant()
        {
            // Even a previous variant out of order (e.g. a hand-edited save) does not change the sequence.
            Assert.That(VariantSelector.Select(3, 3, null), Is.EqualTo(3));
            Assert.That(VariantSelector.Select(1, 4, null), Is.EqualTo(1));
        }

        [TestCase(5, 0, 3)]
        [TestCase(5, 1, 4)]
        [TestCase(3, 0, 4)]
        [TestCase(3, 1, 5)]
        [TestCase(4, 0, 3)]
        [TestCase(4, 1, 5)]
        public void FromTrySix_ItDrawsAmongTheOtherTwoRotatingVariants(int previous, int draw, int expected)
        {
            var random = new ScriptedRandom(draw);

            Assert.That(VariantSelector.Select(6, previous, random), Is.EqualTo(expected));
            Assert.That(random.Bounds, Is.EqualTo(new[] { 2 }), "two candidates: the rotation minus the previous variant");
        }

        [TestCase(0, 3)]
        [TestCase(1, 4)]
        [TestCase(2, 5)]
        public void FromTrySix_AfterAVariantOutsideTheRotation_AnyOfTheThreeCanCome(int draw, int expected)
        {
            var random = new ScriptedRandom(draw);

            Assert.That(VariantSelector.Select(9, 1, random), Is.EqualTo(expected));
            Assert.That(random.Bounds, Is.EqualTo(new[] { 3 }));
        }

        [Test]
        public void ALongRunOfTries_FollowsTheRulesExactly()
        {
            var random = new Random(2026);
            int previous = 0;
            var counts = new int[VariantSelector.VariantCount + 1];

            for (int tryNumber = 1; tryNumber <= 3000; tryNumber++)
            {
                int variant = VariantSelector.Select(tryNumber, previous, random);
                if (tryNumber <= 5)
                {
                    Assert.That(variant, Is.EqualTo(tryNumber));
                }
                else
                {
                    Assert.That(variant, Is.InRange(3, 5), $"try {tryNumber}");
                    Assert.That(variant, Is.Not.EqualTo(previous), $"try {tryNumber} repeats V{previous}");
                    counts[variant]++;
                }

                previous = variant;
            }

            // Each rotating variant comes up about a third of the time.
            for (int v = 3; v <= 5; v++)
            {
                Assert.That(counts[v], Is.InRange(800, 1200), $"V{v}");
            }
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ATryNumberBelowOne_Throws(int tryNumber)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => VariantSelector.Select(tryNumber, 0, new Random(1)));
        }

        [Test]
        public void FromTrySix_ARandomSourceIsRequired()
        {
            Assert.Throws<ArgumentNullException>(() => VariantSelector.Select(6, 5, null));
        }
    }
}
