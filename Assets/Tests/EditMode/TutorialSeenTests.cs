using System;
using BallSort.Core.Player;
using NUnit.Framework;

namespace BallSort.Tests.EditMode
{
    /// <summary>A tutorial popup, once dismissed, is remembered per level id, across launches.</summary>
    [TestFixture]
    public sealed class TutorialSeenTests
    {
        private FakeClock _clock;
        private PlayerData _player;

        [SetUp]
        public void SetUp()
        {
            _clock = new FakeClock(TestPlayers.Start);
            _player = TestPlayers.New(_clock);
        }

        [Test]
        public void ANewPlayer_HasSeenNoTutorial()
        {
            Assert.That(_player.HasSeenTutorial(1), Is.False);
            Assert.That(_player.ToSaveData().TutorialsSeen, Is.Empty);
        }

        [Test]
        public void ADismissedTutorial_IsRememberedForItsLevelOnly()
        {
            _player.MarkTutorialSeen(7);

            Assert.That(_player.HasSeenTutorial(7), Is.True);
            Assert.That(_player.HasSeenTutorial(20), Is.False);
        }

        [Test]
        public void TheTutorialsSeen_SurviveASaveAndRelaunch()
        {
            _player.MarkTutorialSeen(20);
            _player.MarkTutorialSeen(1);

            PlayerData relaunched = TestPlayers.Launch(TestPlayers.Save(_player), _clock);

            Assert.That(relaunched.HasSeenTutorial(1), Is.True);
            Assert.That(relaunched.HasSeenTutorial(20), Is.True);
            Assert.That(relaunched.HasSeenTutorial(7), Is.False);
            Assert.That(relaunched.ToSaveData().TutorialsSeen, Is.EqualTo(new[] { 1, 20 }));
        }

        [Test]
        public void MarkingATutorialSeen_IsSavedAtOnce_AndAgainChangesNothing()
        {
            int saves = 0;
            _player.Changed += () => saves++;

            _player.MarkTutorialSeen(30);
            _player.MarkTutorialSeen(30);

            Assert.That(saves, Is.EqualTo(1));
            Assert.That(_player.ToSaveData().TutorialsSeen, Is.EqualTo(new[] { 30 }));
        }

        [Test]
        public void ClearTutorialsSeen_ShowsEveryTutorialAgain()
        {
            _player.MarkTutorialSeen(1);
            _player.MarkTutorialSeen(7);

            _player.ClearTutorialsSeen();

            Assert.That(_player.HasSeenTutorial(1), Is.False);
            Assert.That(_player.HasSeenTutorial(7), Is.False);
        }

        [Test]
        public void MarkTutorialSeen_RejectsANonLevelId()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _player.MarkTutorialSeen(0));
        }
    }
}
