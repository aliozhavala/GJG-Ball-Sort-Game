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

        // --- Tries -----------------------------------------------------------------------------

        [Test]
        public void ANewPlayer_HasNoTriesYet()
        {
            var progress = new LevelProgress(currentLevel: 1, allLevelsComplete: false, levelCount: 100);

            Assert.That((progress.TryCount, progress.LastVariant), Is.EqualTo((0, 0)));
        }

        [Test]
        public void StartTry_CountsTries_AndPlaysV1ToV5InOrder()
        {
            var progress = new LevelProgress(currentLevel: 7, allLevelsComplete: false, levelCount: 100);
            var random = new Random(1);

            for (int expected = 1; expected <= 5; expected++)
            {
                Assert.That(progress.StartTry(random), Is.EqualTo(expected));
                Assert.That((progress.TryCount, progress.LastVariant), Is.EqualTo((expected, expected)));
            }
        }

        [Test]
        public void StartTry_ContinuesFromASavedTry()
        {
            var progress = new LevelProgress(7, false, 100, tryCount: 2, lastVariant: 2);

            Assert.That(progress.StartTry(new Random(1)), Is.EqualTo(3));
            Assert.That(progress.TryCount, Is.EqualTo(3));
        }

        [Test]
        public void StartTry_AfterTheFifth_NeverRepeatsTheLastVariant()
        {
            var progress = new LevelProgress(7, false, 100, tryCount: 5, lastVariant: 5);
            var random = new Random(12345);

            for (int i = 0; i < 200; i++)
            {
                int previous = progress.LastVariant;
                int variant = progress.StartTry(random);
                Assert.That(variant, Is.InRange(3, 5));
                Assert.That(variant, Is.Not.EqualTo(previous), $"try {progress.TryCount}");
            }
        }

        [Test]
        public void RecordWin_ResetsTheTriesForTheNextLevel()
        {
            var progress = new LevelProgress(7, false, 100, tryCount: 8, lastVariant: 4);

            progress.RecordWin(7);

            Assert.That((progress.CurrentLevel, progress.TryCount, progress.LastVariant), Is.EqualTo((8, 0, 0)));
            Assert.That(progress.StartTry(new Random(1)), Is.EqualTo(1), "the next level starts on V1");
        }

        [Test]
        public void RecordWin_OfAReplayAfterTheRun_AlsoResetsTheTries()
        {
            var progress = new LevelProgress(100, true, 100, tryCount: 3, lastVariant: 3);

            Assert.That(progress.RecordWin(100), Is.False, "no progress");
            Assert.That((progress.TryCount, progress.LastVariant), Is.EqualTo((0, 0)));
        }

        [Test]
        public void RecordWin_OfAnotherLevel_KeepsTheTries()
        {
            var progress = new LevelProgress(7, false, 100, tryCount: 2, lastVariant: 2);

            progress.RecordWin(3);

            Assert.That((progress.TryCount, progress.LastVariant), Is.EqualTo((2, 2)));
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(0, 6)]
        public void Constructor_RejectsOutOfRangeTries(int tryCount, int lastVariant)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LevelProgress(1, false, 100, tryCount, lastVariant));
        }
    }
}
