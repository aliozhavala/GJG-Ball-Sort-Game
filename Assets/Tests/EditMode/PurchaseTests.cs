using System;
using BallSort.Core.Player;
using NUnit.Framework;

namespace BallSort.Tests.EditMode
{
    /// <summary>Buying with coins (docs/screens.md, Buying): boosters, the lives refill and shop bundles.</summary>
    [TestFixture]
    public sealed class PurchaseTests
    {
        // The shipped prices, as test values: product may retune EconomyConfig freely.
        private const int UndoPrice = 300;
        private const int ShufflePrice = 300;
        private const int AddTubePrice = 900;
        private const int RefillPrice = 900;

        private FakeClock _clock;

        [SetUp]
        public void SetUp() => _clock = new FakeClock(TestPlayers.Start);

        private PlayerData PlayerWith(int coins)
        {
            SaveData save = SaveData.CreateDefault(TestPlayers.Rules);
            save.Coins = coins;
            return TestPlayers.FromSave(save, _clock);
        }

        // --- Boosters ------------------------------------------------------------------------------

        [TestCase(BoosterType.Undo, UndoPrice)]
        [TestCase(BoosterType.Shuffle, ShufflePrice)]
        [TestCase(BoosterType.AddTube, AddTubePrice)]
        public void BuyingABooster_SpendsItsPrice_AndAddsOne(BoosterType type, int price)
        {
            PlayerData player = PlayerWith(1000);
            int before = player.Boosters.GetCount(type);
            int saves = 0;
            player.Changed += () => saves++;

            Assert.That(player.TryBuyBooster(type, price, 1, out PurchaseRefusal refusal), Is.True);

            Assert.That(refusal, Is.EqualTo(PurchaseRefusal.None));
            Assert.That(player.Wallet.Coins, Is.EqualTo(1000 - price));
            Assert.That(player.Boosters.GetCount(type), Is.EqualTo(before + 1));
            Assert.That(saves, Is.EqualTo(1), "coins and booster are saved together");
        }

        [Test]
        public void BuyingABooster_WithExactlyThePrice_LeavesZeroCoins()
        {
            PlayerData player = PlayerWith(UndoPrice);

            Assert.That(player.TryBuyBooster(BoosterType.Undo, UndoPrice, 1, out _), Is.True);

            Assert.That(player.Wallet.Coins, Is.Zero);
        }

        [Test]
        public void BuyingABooster_WithoutEnoughCoins_ChangesNothing()
        {
            PlayerData player = PlayerWith(AddTubePrice - 1);
            int before = player.Boosters.GetCount(BoosterType.AddTube);
            int saves = 0;
            player.Changed += () => saves++;

            Assert.That(player.TryBuyBooster(BoosterType.AddTube, AddTubePrice, 1, out PurchaseRefusal refusal), Is.False);

            Assert.That(refusal, Is.EqualTo(PurchaseRefusal.NotEnoughCoins));
            Assert.That(player.Wallet.Coins, Is.EqualTo(AddTubePrice - 1));
            Assert.That(player.Boosters.GetCount(BoosterType.AddTube), Is.EqualTo(before));
            Assert.That(saves, Is.Zero);
        }

        [Test]
        public void ABoughtBooster_SurvivesASaveAndReload()
        {
            PlayerData player = PlayerWith(1000);
            player.TryBuyBooster(BoosterType.Shuffle, ShufflePrice, 1, out _);

            PlayerData reloaded = TestPlayers.Launch(TestPlayers.Save(player), _clock);

            Assert.That(reloaded.Wallet.Coins, Is.EqualTo(1000 - ShufflePrice));
            Assert.That(reloaded.Boosters.GetCount(BoosterType.Shuffle), Is.EqualTo(player.Boosters.GetCount(BoosterType.Shuffle)));
        }

        [Test]
        public void NegativePricesAndAmounts_AreRejected()
        {
            PlayerData player = PlayerWith(1000);

            Assert.Throws<ArgumentOutOfRangeException>(() => player.TryBuyBooster(BoosterType.Undo, -1, 1, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => player.TryBuyBooster(BoosterType.Undo, 1, -1, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => player.TryRefillLives(-1, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => player.AddCoins(-1));
        }

        // --- Hearts -----------------------------------------------------------------------------

        [Test]
        public void BuyingHearts_RefillsLivesToTheMaximum_AndStopsTheTimer()
        {
            PlayerData player = PlayerWith(1000);
            player.SetLives(2);
            int saves = 0;
            player.Changed += () => saves++;

            Assert.That(player.CanRefillLives(), Is.True);
            Assert.That(player.TryRefillLives(RefillPrice, out PurchaseRefusal refusal), Is.True);

            Assert.That(refusal, Is.EqualTo(PurchaseRefusal.None));
            Assert.That(player.LifeCount, Is.EqualTo(player.MaxLives));
            Assert.That(player.Wallet.Coins, Is.EqualTo(1000 - RefillPrice));
            Assert.That(player.GetTimeUntilNextLife(), Is.Null, "full: no wait for a next life");
            Assert.That(saves, Is.EqualTo(1), "coins and lives are saved together");
        }

        [Test]
        public void BuyingHearts_AtZeroLives_FillsThemToo()
        {
            PlayerData player = PlayerWith(RefillPrice);
            player.SetLives(0);

            Assert.That(player.TryRefillLives(RefillPrice, out _), Is.True);

            Assert.That(player.LifeCount, Is.EqualTo(player.MaxLives));
            Assert.That(player.CanStartLevel(), Is.True);
        }

        [Test]
        public void BuyingHearts_WhenFull_IsRefused_AndSpendsNothing()
        {
            PlayerData player = PlayerWith(1000);

            Assert.That(player.CanRefillLives(), Is.False);
            Assert.That(player.TryRefillLives(RefillPrice, out PurchaseRefusal refusal), Is.False);

            Assert.That(refusal, Is.EqualTo(PurchaseRefusal.LivesFull));
            Assert.That(player.Wallet.Coins, Is.EqualTo(1000));
        }

        [Test]
        public void BuyingHearts_AfterTheLastLifeCameBack_IsRefusedAsFull()
        {
            PlayerData player = PlayerWith(1000);
            player.SetLives(4, TimeSpan.FromMinutes(5));
            _clock.Advance(TimeSpan.FromMinutes(6));

            Assert.That(player.TryRefillLives(RefillPrice, out PurchaseRefusal refusal), Is.False);

            Assert.That(refusal, Is.EqualTo(PurchaseRefusal.LivesFull));
            Assert.That(player.Wallet.Coins, Is.EqualTo(1000));
            Assert.That(player.LifeCount, Is.EqualTo(player.MaxLives));
        }

        [Test]
        public void BuyingHearts_WithoutEnoughCoins_ChangesNothing()
        {
            PlayerData player = PlayerWith(RefillPrice - 1);
            player.SetLives(1);
            int saves = 0;
            player.Changed += () => saves++;

            Assert.That(player.TryRefillLives(RefillPrice, out PurchaseRefusal refusal), Is.False);

            Assert.That(refusal, Is.EqualTo(PurchaseRefusal.NotEnoughCoins));
            Assert.That(player.LifeCount, Is.EqualTo(1));
            Assert.That(player.Wallet.Coins, Is.EqualTo(RefillPrice - 1));
            Assert.That(saves, Is.Zero);
        }

        [Test]
        public void BoughtHearts_SurviveASaveAndReload()
        {
            PlayerData player = PlayerWith(1000);
            player.SetLives(1);
            player.TryRefillLives(RefillPrice, out _);

            PlayerData reloaded = TestPlayers.Launch(TestPlayers.Save(player), _clock);

            Assert.That(reloaded.LifeCount, Is.EqualTo(reloaded.MaxLives));
            Assert.That(reloaded.GetTimeUntilNextLife(), Is.Null);
            Assert.That(reloaded.Wallet.Coins, Is.EqualTo(1000 - RefillPrice));
        }

        // --- Coin bundles ----------------------------------------------------------------------------

        [TestCase(1000)]
        [TestCase(100000)]
        public void ABundle_AddsItsCoins(int coins)
        {
            PlayerData player = PlayerWith(250);
            int saves = 0;
            player.Changed += () => saves++;

            player.AddCoins(coins);

            Assert.That(player.Wallet.Coins, Is.EqualTo(250 + coins));
            Assert.That(saves, Is.EqualTo(1));
        }
    }
}
