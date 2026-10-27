using System;
using System.Collections.Generic;
using BallSort.Core.Player;
using NUnit.Framework;

namespace BallSort.Tests.EditMode
{
    [TestFixture]
    public sealed class WalletTests
    {
        [Test]
        public void TrySpend_DeductsAndAnnouncesTheNewBalance()
        {
            var wallet = new Wallet(1000);
            var changes = new List<int>();
            wallet.Changed += changes.Add;

            Assert.That(wallet.TrySpend(900), Is.True);

            Assert.That(wallet.Coins, Is.EqualTo(100));
            Assert.That(changes, Is.EqualTo(new[] { 100 }));
        }

        [Test]
        public void TrySpend_MoreThanTheBalance_RefusesSilently()
        {
            var wallet = new Wallet(899);
            var changes = 0;
            wallet.Changed += _ => changes++;

            Assert.That(wallet.CanAfford(900), Is.False);
            Assert.That(wallet.TrySpend(900), Is.False);

            Assert.That(wallet.Coins, Is.EqualTo(899));
            Assert.That(changes, Is.EqualTo(0));
        }

        [Test]
        public void TrySpend_TheExactBalance_LeavesZero()
        {
            var wallet = new Wallet(900);

            Assert.That(wallet.CanAfford(900), Is.True);
            Assert.That(wallet.TrySpend(900), Is.True);
            Assert.That(wallet.Coins, Is.EqualTo(0));
        }

        [Test]
        public void Add_IncreasesTheBalanceAndAnnouncesIt()
        {
            var wallet = new Wallet(1000);
            var changes = new List<int>();
            wallet.Changed += changes.Add;

            wallet.Add(20);
            wallet.Add(0);

            Assert.That(wallet.Coins, Is.EqualTo(1020));
            Assert.That(changes, Is.EqualTo(new[] { 1020 }), "adding zero changes nothing and says nothing");
        }

        [Test]
        public void NegativeAmounts_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Wallet(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Wallet(10).Add(-5));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Wallet(10).TrySpend(-5));
        }
    }
}
