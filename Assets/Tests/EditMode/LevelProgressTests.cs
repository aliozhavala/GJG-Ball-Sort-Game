using System;
using BallSort.Core.Player;
using NUnit.Framework;

namespace BallSort.Tests.EditMode
{
    [TestFixture]
    public sealed class LevelProgressTests
    {
        [Test]
        public void RecordWin_OfTheCurrentLevel_AdvancesToTheNext()
        {
            var progress = new LevelProgress(currentLevel: 1, allLevelsComplete: false, levelCount: 100);

            Assert.That(progress.RecordWin(1), Is.True);

            Assert.That(progress.CurrentLevel, Is.EqualTo(2));
            Assert.That(progress.AllLevelsComplete, Is.False);
        }

        [Test]
        public void RecordWin_OfTheLastLevel_StaysThereAndCompletesTheRun()
        {
            var progress = new LevelProgress(currentLevel: 100, allLevelsComplete: false, levelCount: 100);

            Assert.That(progress.RecordWin(100), Is.True);

            Assert.That(progress.CurrentLevel, Is.EqualTo(100));
            Assert.That(progress.AllLevelsComplete, Is.True);
        }

        [Test]
        public void RecordWin_ReplayingTheLastLevelAfterTheRun_ChangesNothing()
        {
            var progress = new LevelProgress(currentLevel: 100, allLevelsComplete: true, levelCount: 100);

            Assert.That(progress.RecordWin(100), Is.False);

            Assert.That(progress.CurrentLevel, Is.EqualTo(100));
            Assert.That(progress.AllLevelsComplete, Is.True);
        }

        [TestCase(3)]
        [TestCase(50)]
        public void RecordWin_OfAnotherLevel_NeitherSkipsAheadNorRewinds(int wonLevel)
        {
            var progress = new LevelProgress(currentLevel: 10, allLevelsComplete: false, levelCount: 100);

            Assert.That(progress.RecordWin(wonLevel), Is.False);

            Assert.That(progress.CurrentLevel, Is.EqualTo(10));
        }

        [Test]
        public void ASingleLevelGame_IsCompleteAfterOneWin()
        {
            var progress = new LevelProgress(currentLevel: 1, allLevelsComplete: false, levelCount: 1);

            progress.RecordWin(1);

            Assert.That(progress.CurrentLevel, Is.EqualTo(1));
            Assert.That(progress.AllLevelsComplete, Is.True);
        }

        [TestCase(0, 100)]
        [TestCase(101, 100)]
        [TestCase(1, 0)]
        public void Constructor_RejectsOutOfRangeValues(int currentLevel, int levelCount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new LevelProgress(currentLevel, allLevelsComplete: false, levelCount));
        }
    }
}
