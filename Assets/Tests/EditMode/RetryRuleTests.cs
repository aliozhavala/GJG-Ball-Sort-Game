using System;
using BallSort.Core.Player;
using NUnit.Framework;

namespace BallSort.Tests.EditMode
{
    /// <summary>
    /// The retry rule (docs/levels.md, Variants and retries), as the game plays it through
    /// <see cref="PlayerData"/>: every attempt that costs a life moves the next one to the next variant,
    /// Restart loads it, and a win goes back to V1. The variant order itself is in
    /// <see cref="VariantSelectorTests"/> and <see cref="LevelProgressTests"/>.
    /// </summary>
    [TestFixture]
    public sealed class RetryRuleTests
    {
        private FakeClock _clock;
        private Random _random;
        private PlayerData _player;

        [SetUp]
        public void SetUp()
        {
            _clock = new FakeClock(TestPlayers.Start);
            _random = new Random(7);
            _player = TestPlayers.New(_clock);
        }

        [Test]
        public void TheFirstAttemptPlaysV1_AndEachCostedExitMovesOnToTheNextVariant()
        {
            Assert.That(_player.BeginAttempt(_random), Is.EqualTo(1));
            _player.LeaveAttempt();
            Assert.That(_player.BeginAttempt(_random), Is.EqualTo(2));
            Assert.That(Restart(), Is.EqualTo(3));
            _player.LeaveAttempt();
            Assert.That(_player.BeginAttempt(_random), Is.EqualTo(4));
            Assert.That(Restart(), Is.EqualTo(5));
        }

        [Test]
        public void AfterFiveAttempts_TheVariantRotatesAmongV3ToV5WithoutRepeats()
        {
            int previous = _player.BeginAttempt(_random);
            for (int attempt = 2; attempt <= 40; attempt++)
            {
                _player.SetLives(_player.MaxLives);
                int variant = Restart();
                if (attempt <= 5)
                {
                    Assert.That(variant, Is.EqualTo(attempt));
                }
                else
                {
                    Assert.That(variant, Is.InRange(3, 5), $"attempt {attempt}");
                    Assert.That(variant, Is.Not.EqualTo(previous), $"attempt {attempt}");
                }

                previous = variant;
            }
        }

        [Test]
        public void EnteringAgainWithoutACostedExit_CountsNothing()
        {
            Assert.That(_player.BeginAttempt(_random), Is.EqualTo(1));
            Assert.That(_player.BeginAttempt(_random), Is.EqualTo(1));
            Assert.That(_player.RetryCount, Is.EqualTo(0));
        }

        [Test]
        public void AnAttemptCutOffByClosingTheApp_CountsLikeAnExit()
        {
            _player.BeginAttempt(_random);

            PlayerData relaunched = TestPlayers.Launch(TestPlayers.Save(_player), _clock);

            Assert.That(relaunched.BeginAttempt(_random), Is.EqualTo(2));
        }

        [Test]
        public void AWin_SendsTheNextLevelBackToV1()
        {
            _player.BeginAttempt(_random);
            _player.LeaveAttempt();
            Assert.That(_player.BeginAttempt(_random), Is.EqualTo(2));

            _player.RecordWin(1, reward: 20);

            Assert.That(_player.CurrentLevel, Is.EqualTo(2));
            Assert.That(_player.BeginAttempt(_random), Is.EqualTo(1));
        }

        [Test]
        public void TheRetries_SurviveASaveAndRelaunch()
        {
            _player.BeginAttempt(_random);
            _player.LeaveAttempt();
            _player.BeginAttempt(_random);
            _player.LeaveAttempt();

            PlayerData relaunched = TestPlayers.Launch(TestPlayers.Save(_player), _clock);

            Assert.That(relaunched.RetryCount, Is.EqualTo(2));
            Assert.That(relaunched.BeginAttempt(_random), Is.EqualTo(3));
        }

        [Test]
        public void Replays_FollowTheSameRule()
        {
            SaveData save = SaveData.CreateDefault(TestPlayers.Rules);
            save.CurrentLevel = TestPlayers.LevelCount;
            save.AllLevelsComplete = true;
            PlayerData player = TestPlayers.FromSave(save, _clock);

            Assert.That(player.BeginAttempt(_random), Is.EqualTo(1));
            player.LeaveAttempt();
            Assert.That(player.BeginAttempt(_random), Is.EqualTo(2));
            player.RecordWin(TestPlayers.LevelCount, reward: 60);
            Assert.That(player.BeginAttempt(_random), Is.EqualTo(1));
        }

        private int Restart()
        {
            Assert.That(_player.TryRestartAttempt(_random, out int variant), Is.True);
            return variant;
        }
    }
}
