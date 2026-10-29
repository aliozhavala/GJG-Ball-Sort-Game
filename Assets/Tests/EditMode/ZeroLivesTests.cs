using System;
using BallSort.Core.Player;
using NUnit.Framework;

namespace BallSort.Tests.EditMode
{
    /// <summary>
    /// At 0 lives no level can start and Restart is refused (the game opens More Lives instead), while
    /// leaving still works: lives stay at 0 and the retry still counts.
    /// </summary>
    [TestFixture]
    public sealed class ZeroLivesTests
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
        public void AtZeroLives_NoLevelCanStart()
        {
            _player.SetLives(0);

            Assert.That(_player.CanStartLevel(), Is.False);
        }

        [Test]
        public void WithOneLife_RestartWorks_AndLeavesZero()
        {
            _player.SetLives(1);
            _player.BeginAttempt(_random);

            Assert.That(_player.TryRestartAttempt(_random, out int variant), Is.True);

            Assert.That(variant, Is.EqualTo(2));
            Assert.That(_player.LifeCount, Is.EqualTo(0));
        }

        [Test]
        public void AtZeroLives_RestartIsRefused_AndChangesNothing()
        {
            _player.SetLives(1);
            _player.BeginAttempt(_random);
            _player.TryRestartAttempt(_random, out _);
            int saves = 0;
            _player.Changed += () => saves++;

            Assert.That(_player.TryRestartAttempt(_random, out int variant), Is.False);

            Assert.That(variant, Is.EqualTo(0));
            Assert.That(_player.LifeCount, Is.EqualTo(0));
            Assert.That(_player.RetryCount, Is.EqualTo(1));
            Assert.That(_player.LastVariant, Is.EqualTo(2));
            Assert.That(_player.LevelInProgress, Is.EqualTo(1), "the attempt goes on");
            Assert.That(saves, Is.EqualTo(0));
        }

        [Test]
        public void AtZeroLives_LeavingStillWorks_KeepsZero_AndCountsTheRetry()
        {
            _player.SetLives(1);
            _player.BeginAttempt(_random);
            _player.TryRestartAttempt(_random, out _);

            _player.LeaveAttempt();

            Assert.That(_player.LifeCount, Is.EqualTo(0));
            Assert.That(_player.RetryCount, Is.EqualTo(2));
            Assert.That(_player.LevelInProgress, Is.EqualTo(0));
        }

        [Test]
        public void ALifeThatComesBack_LetsTheLevelStartAgain()
        {
            _player.SetLives(0, TimeSpan.FromMinutes(10));

            _clock.Advance(TimeSpan.FromMinutes(10));

            Assert.That(_player.CanStartLevel(), Is.True);
            Assert.That(_player.LifeCount, Is.EqualTo(1));
        }

        [Test]
        public void ALifeThatComesBackDuringTheLevel_MakesRestartWorkAgain()
        {
            _player.SetLives(1);
            _player.BeginAttempt(_random);
            _player.TryRestartAttempt(_random, out _);
            Assert.That(_player.TryRestartAttempt(_random, out _), Is.False);

            _clock.Advance(TestPlayers.RefillInterval);

            Assert.That(_player.TryRestartAttempt(_random, out int variant), Is.True);
            Assert.That(variant, Is.EqualTo(3));
            Assert.That(_player.LifeCount, Is.EqualTo(0));
        }

        [Test]
        public void AtZeroLives_TheTimeToTheNextLifeIsShown()
        {
            _player.SetLives(0, TimeSpan.FromMinutes(12));

            Assert.That(_player.GetTimeUntilNextLife(), Is.EqualTo(TimeSpan.FromMinutes(12)));
        }
    }
}
