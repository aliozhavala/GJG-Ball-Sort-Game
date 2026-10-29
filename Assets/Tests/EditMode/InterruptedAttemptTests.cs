using System;
using BallSort.Core.Player;
using NUnit.Framework;

namespace BallSort.Tests.EditMode
{
    /// <summary>
    /// Closing the app mid-level (or the OS ending it): the level-in-progress mark is still in the
    /// save on the next launch, which then costs a life and counts a retry, once.
    /// </summary>
    [TestFixture]
    public sealed class InterruptedAttemptTests
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
        public void ClosingTheAppMidLevel_CostsALifeAndARetryOnTheNextLaunch()
        {
            _player.BeginAttempt(_random);
            string save = TestPlayers.Save(_player);

            PlayerData relaunched = TestPlayers.Launch(save, _clock);

            Assert.That(relaunched.LifeCount, Is.EqualTo(4));
            Assert.That(relaunched.RetryCount, Is.EqualTo(1));
            Assert.That(relaunched.LevelInProgress, Is.EqualTo(0));
            Assert.That(relaunched.CurrentLevel, Is.EqualTo(1));
        }

        [Test]
        public void AfterTheInterruptedAttempt_TheNextOnePlaysTheNextVariant()
        {
            _player.BeginAttempt(_random);

            PlayerData relaunched = TestPlayers.Launch(TestPlayers.Save(_player), _clock);

            Assert.That(relaunched.BeginAttempt(_random), Is.EqualTo(2));
        }

        [Test]
        public void TheLaunchCheck_ChargesOnlyOnce()
        {
            _player.BeginAttempt(_random);
            PlayerData first = TestPlayers.Launch(TestPlayers.Save(_player), _clock);

            PlayerData second = TestPlayers.Launch(TestPlayers.Save(first), _clock);

            Assert.That(second.LifeCount, Is.EqualTo(4));
            Assert.That(second.RetryCount, Is.EqualTo(1));
            Assert.That(second.ResolveInterruptedAttempt(), Is.False);
        }

        [Test]
        public void ClosingTheAppAfterAWin_CostsNothing()
        {
            _player.BeginAttempt(_random);
            _player.RecordWin(1, reward: 20);

            PlayerData relaunched = TestPlayers.Launch(TestPlayers.Save(_player), _clock);

            Assert.That(relaunched.LifeCount, Is.EqualTo(5));
            Assert.That(relaunched.RetryCount, Is.EqualTo(0));
        }

        [Test]
        public void ClosingTheAppAfterLeavingNormally_CostsNothingMore()
        {
            _player.BeginAttempt(_random);
            _player.LeaveAttempt();

            PlayerData relaunched = TestPlayers.Launch(TestPlayers.Save(_player), _clock);

            Assert.That(relaunched.LifeCount, Is.EqualTo(4));
            Assert.That(relaunched.RetryCount, Is.EqualTo(1));
        }

        [Test]
        public void ClosingTheAppAfterARestart_CostsTheNewAttemptToo()
        {
            _player.BeginAttempt(_random);
            _player.TryRestartAttempt(_random, out _);

            PlayerData relaunched = TestPlayers.Launch(TestPlayers.Save(_player), _clock);

            Assert.That(relaunched.LifeCount, Is.EqualTo(3));
            Assert.That(relaunched.RetryCount, Is.EqualTo(2));
            Assert.That(relaunched.BeginAttempt(_random), Is.EqualTo(3));
        }

        [Test]
        public void AtZeroLives_TheLaunchCheckCountsOnlyTheRetry()
        {
            _player.SetLives(1);
            _player.BeginAttempt(_random);
            _player.TryRestartAttempt(_random, out _);

            PlayerData relaunched = TestPlayers.Launch(TestPlayers.Save(_player), _clock);

            Assert.That(relaunched.LifeCount, Is.EqualTo(0));
            Assert.That(relaunched.RetryCount, Is.EqualTo(2));
            Assert.That(relaunched.LevelInProgress, Is.EqualTo(0));
        }

        [Test]
        public void AMarkForAnotherLevel_CostsALifeButCountsNoRetry()
        {
            SaveData save = SaveData.CreateDefault(TestPlayers.Rules);
            save.CurrentLevel = 3;
            save.LevelInProgress = 7;
            PlayerData player = TestPlayers.FromSave(save, _clock);

            Assert.That(player.ResolveInterruptedAttempt(), Is.True);

            Assert.That(player.LifeCount, Is.EqualTo(4));
            Assert.That(player.RetryCount, Is.EqualTo(0));
            Assert.That(player.LevelInProgress, Is.EqualTo(0));
        }

        [Test]
        public void TheLaunchCheck_IsSavedAtOnce()
        {
            SaveData save = SaveData.CreateDefault(TestPlayers.Rules);
            save.LevelInProgress = 1;
            PlayerData player = TestPlayers.FromSave(save, _clock);
            int saves = 0;
            player.Changed += () => saves++;

            player.ResolveInterruptedAttempt();

            Assert.That(saves, Is.EqualTo(1));
        }
    }
}
