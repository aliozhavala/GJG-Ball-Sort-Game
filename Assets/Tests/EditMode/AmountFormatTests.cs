using System;
using BallSort.Core.Player;
using NUnit.Framework;

namespace BallSort.Tests.EditMode
{
    [TestFixture]
    public sealed class AmountFormatTests
    {
        [TestCase(0, "0")]
        [TestCase(7, "7")]
        [TestCase(999, "999")]
        public void Abbreviate_BelowAThousand_ShowsTheWholeNumber(long amount, string expected) =>
            Assert.That(AmountFormat.Abbreviate(amount), Is.EqualTo(expected));

        [TestCase(1000, "1K")]
        [TestCase(1250, "1.2K")]
        [TestCase(12500, "12.5K")]
        [TestCase(99999, "99.9K")]
        [TestCase(125000, "125K")]
        [TestCase(999999, "999K")]
        public void Abbreviate_Thousands_UsesK(long amount, string expected) =>
            Assert.That(AmountFormat.Abbreviate(amount), Is.EqualTo(expected));

        [TestCase(1000000, "1M")]
        [TestCase(1234567, "1.2M")]
        [TestCase(250000000, "250M")]
        [TestCase(2147483647, "2.1B")]
        [TestCase(5000000000000, "5T")]
        [TestCase(1250000000000000, "1250T")]
        public void Abbreviate_LargerAmounts_UseTheLargestFittingUnit(long amount, string expected) =>
            Assert.That(AmountFormat.Abbreviate(amount), Is.EqualTo(expected));

        [Test]
        public void Abbreviate_NeverRoundsUp()
        {
            Assert.That(AmountFormat.Abbreviate(1999), Is.EqualTo("1.9K"));
            Assert.That(AmountFormat.Abbreviate(999999999), Is.EqualTo("999M"));
        }

        [Test]
        public void Abbreviate_NegativeAmount_Throws() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => AmountFormat.Abbreviate(-1));
    }
}
