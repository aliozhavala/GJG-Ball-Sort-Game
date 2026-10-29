using System;
using BallSort.Core.Player;
using NUnit.Framework;

namespace BallSort.Tests.EditMode
{
    /// <summary>
    /// Every exit from a level before a win costs a life and counts a retry; a win costs nothing.
    /// Settings → Home and stuck → Give up both end the attempt with <see cref="PlayerData.LeaveAttempt"/>;
    /// Restart with <see cref="PlayerData.TryRestartAttempt"/>.
    /// </summary>
    [TestFixture]
    public sealed class LifeCostTests
    {
        private FakeClock _clock;
        private Random _random;
        private PlayerData _player;

        [SetUp]
        public void SetUp()
        {
            _clock = new FakeClock(TestPlayers.Start);
            _random = new Random(1);
            _player = TestPlayers.New(_clock);
        }

        [Test]
        public void EnteringALevel_CostsNothing_AndMarksItInProgress()
        {
            _player.BeginAttempt(_random);

            Assert.That(_player.LifeCount, Is.EqualTo(5));
            Assert.That(_player.RetryCount, Is.EqualTo(0));
            Assert.That(_player.LevelInProgress, Is.EqualTo(1));
        }

        [Test]
        public void LeavingBeforeAWin_CostsALifeAndARetry_AndEndsTheAttempt()
        {
            _player.BeginAttempt(_random);

            _player.LeaveAttempt();

            Assert.That(_player.LifeCount, Is.EqualTo(4));
            Assert.That(_player.RetryCount, Is.EqualTo(1));
            Assert.That(_player.LevelInProgress, Is.EqualTo(0));
            Assert.That(_player.CurrentLevel, Is.EqualTo(1), "no progress");
        }

        [Test]
        public void Restart_CostsALifeAndARetry_AndKeepsTheLevelInProgress()
        {
            _player.BeginAttempt(_random);

            Assert.That(_player.TryRestartAttempt(_random, out int variant), Is.True);

            Assert.That(variant, Is.EqualTo(2));
            Assert.That(_player.LifeCount, Is.EqualTo(4));
            Assert.That(_player.RetryCount, Is.EqualTo(1));
            Assert.That(_player.LevelInProgress, Is.EqualTo(1));
        }

        [Test]
        public void AWin_CostsNoLife_ResetsTheRetries_AndEndsTheAttempt()
        {
            _player.BeginAttempt(_random);
            _player.LeaveAttempt();
            _player.BeginAttempt(_random);
            int coins = _player.Wallet.Coins;

            Assert.That(_player.RecordWin(1, reward: 20), Is.True);

            Assert.That(_player.LifeCount, Is.EqualTo(4), "only the earlier exit cost a life");
            Assert.That(_player.RetryCount, Is.EqualTo(0));
            Assert.That(_player.LevelInProgress, Is.EqualTo(0));
            Assert.That(_player.CurrentLevel, Is.EqualTo(2));
            Assert.That(_player.Wallet.Coins, Is.EqualTo(coins + 20));
        }

        [Test]
        public void EachExit_CostsExactlyOneLife()
        {
            _player.BeginAttempt(_random);
            _player.LeaveAttempt();                             // Home
            _player.BeginAttempt(_random);
            _player.TryRestartAttempt(_random, out _);          // Restart
            _player.TryRestartAttempt(_random, out _);          // Restart
            _player.LeaveAttempt();                             // Give up

            Assert.That(_player.LifeCount, Is.EqualTo(1));
            Assert.That(_player.RetryCount, Is.EqualTo(4));
        }

        [Test]
        public void AnExit_IsSavedOnce_WithEverythingItChanged()
        {
            _player.BeginAttempt(_random);
            int saves = 0;
            _player.Changed += () => saves++;

            _player.LeaveAttempt();

            Assert.That(saves, Is.EqualTo(1));
            PlayerData relaunched = TestPlayers.Launch(TestPlayers.Save(_player), _clock);
            Assert.That(relaunched.LifeCount, Is.EqualTo(4));
            Assert.That(relaunched.RetryCount, Is.EqualTo(1));
            Assert.That(relaunched.LevelInProgress, Is.EqualTo(0));
        }

        [Test]
        public void AWin_IsSavedOnce_WithTheMarkCleared()
        {
            _player.BeginAttempt(_random);
            int saves = 0;
            _player.Changed += () => saves++;

            _player.RecordWin(1, reward: 20);

            Assert.That(saves, Is.EqualTo(1));
            PlayerData relaunched = TestPlayers.Launch(TestPlayers.Save(_player), _clock);
            Assert.That(relaunched.LifeCount, Is.EqualTo(5), "a won level is not an interrupted one");
        }

        [Test]
        public void AReplayWin_AlsoCostsNoLife()
        {
            var save = SaveData.CreateDefault(TestPlayers.Rules);
            save.CurrentLevel = TestPlayers.LevelCount;
            save.AllLevelsComplete = true;
            PlayerData player = TestPlayers.FromSave(save, _clock);
            player.BeginAttempt(_random);

            Assert.That(player.RecordWin(TestPlayers.LevelCount, reward: 60), Is.False, "a replay moves no progress");

            Assert.That(player.LifeCount, Is.EqualTo(5));
            Assert.That(player.LevelInProgress, Is.EqualTo(0));
        }
    }
}
