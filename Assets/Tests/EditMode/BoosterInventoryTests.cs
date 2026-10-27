using System;
using System.Collections.Generic;
using BallSort.Core.Player;
using NUnit.Framework;

namespace BallSort.Tests.EditMode
{
    [TestFixture]
    public sealed class BoosterInventoryTests
    {
        [Test]
        public void StartsWithTheGivenCounts()
        {
            var boosters = new BoosterInventory(undo: 1, shuffle: 2, addTube: 3);

            Assert.That(boosters.GetCount(BoosterType.Undo), Is.EqualTo(1));
            Assert.That(boosters.GetCount(BoosterType.Shuffle), Is.EqualTo(2));
            Assert.That(boosters.GetCount(BoosterType.AddTube), Is.EqualTo(3));
        }

        [Test]
        public void TrySpend_TakesOneAndAnnouncesTheNewCount()
        {
            var boosters = new BoosterInventory(undo: 2, shuffle: 0, addTube: 0);
            var changes = new List<(BoosterType, int)>();
            boosters.Changed += (type, count) => changes.Add((type, count));

            Assert.That(boosters.TrySpend(BoosterType.Undo), Is.True);

            Assert.That(boosters.GetCount(BoosterType.Undo), Is.EqualTo(1));
            Assert.That(changes, Is.EqualTo(new[] { (BoosterType.Undo, 1) }));
        }

        [Test]
        public void TrySpend_AtZero_RefusesSilently()
        {
            var boosters = new BoosterInventory(undo: 0, shuffle: 0, addTube: 0);
            var changes = 0;
            boosters.Changed += (_, _) => changes++;

            Assert.That(boosters.TrySpend(BoosterType.AddTube), Is.False);

            Assert.That(boosters.GetCount(BoosterType.AddTube), Is.EqualTo(0));
            Assert.That(changes, Is.EqualTo(0));
        }

        [Test]
        public void TypesAreIndependent()
        {
            var boosters = new BoosterInventory(undo: 1, shuffle: 1, addTube: 1);

            boosters.TrySpend(BoosterType.Shuffle);

            Assert.That(boosters.GetCount(BoosterType.Undo), Is.EqualTo(1));
            Assert.That(boosters.GetCount(BoosterType.Shuffle), Is.EqualTo(0));
            Assert.That(boosters.GetCount(BoosterType.AddTube), Is.EqualTo(1));
        }

        [Test]
        public void Add_IncreasesTheCountAndAnnouncesIt()
        {
            var boosters = new BoosterInventory(undo: 0, shuffle: 0, addTube: 2);
            var changes = new List<int>();
            boosters.Changed += (_, count) => changes.Add(count);

            boosters.Add(BoosterType.AddTube, 3);
            boosters.Add(BoosterType.AddTube, 0);

            Assert.That(boosters.GetCount(BoosterType.AddTube), Is.EqualTo(5));
            Assert.That(changes, Is.EqualTo(new[] { 5 }), "adding zero changes nothing and says nothing");
        }

        [Test]
        public void NegativeAmounts_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BoosterInventory(-1, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BoosterInventory(0, 0, 0).Add(BoosterType.Undo, -1));
        }

        [Test]
        public void AnUnknownType_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BoosterInventory(0, 0, 0).GetCount((BoosterType)42));
        }
    }
}
