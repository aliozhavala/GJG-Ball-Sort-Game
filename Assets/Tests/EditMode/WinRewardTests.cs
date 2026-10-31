using System;
using BallSort.Core.Levels;
using BallSort.Core.Player;
using NUnit.Framework;

namespace BallSort.Tests.EditMode
{
    /// <summary>The coins a win pays by tier (docs/screens.md, Persistent player data).</summary>
    [TestFixture]
    public sealed class WinRewardTests
    {
        // The shipped rewards, as test values: product may retune EconomyConfig freely.
        private static readonly TierRewards Rewards = new TierRewards(regular: 20, hard: 60, superHard: 100);

        [TestCase(LevelTier.Regular, 20)]
        [TestCase(LevelTier.Hard, 60)]
        [TestCase(LevelTier.SuperHard, 100)]
        public void EachTier_HasItsOwnReward(LevelTier tier, int reward)
        {
            Assert.That(Rewards.For(tier), Is.EqualTo(reward));
        }

        [TestCase(LevelTier.Regular, 20)]
        [TestCase(LevelTier.Hard, 60)]
        [TestCase(LevelTier.SuperHard, 100)]
        public void AWin_PaysItsTiersReward(LevelTier tier, int reward)
        {
            PlayerData player = TestPlayers.New(new FakeClock(TestPlayers.Start));
            int coins = player.Wallet.Coins;
            player.BeginAttempt(new Random(1));

            player.RecordWin(player.CurrentLevel, Rewards.For(tier));

            Assert.That(player.Wallet.Coins, Is.EqualTo(coins + reward));
        }

        [Test]
        public void ANegativeReward_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TierRewards(-1, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Rewards.For((LevelTier)99));
        }
    }
}
