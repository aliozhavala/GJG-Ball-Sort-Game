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

        // --- Retries and the attempt in progress --------------------------------------------------

        [Test]
        public void ANewPlayer_HasNoRetriesAndNoAttemptInProgress()
        {
            var progress = new LevelProgress(currentLevel: 1, allLevelsComplete: false, levelCount: 100);

            Assert.That((progress.RetryCount, progress.LastVariant, progress.LevelInProgress), Is.EqualTo((0, 0, 0)));
        }

        [Test]
        public void StartAttempt_CountsNothing_AndMarksTheCurrentLevelInProgress()
        {
            var progress = new LevelProgress(currentLevel: 7, allLevelsComplete: false, levelCount: 100);

            Assert.That(progress.StartAttempt(new Random(1)), Is.EqualTo(1));
            Assert.That(progress.StartAttempt(new Random(1)), Is.EqualTo(1));

            Assert.That((progress.RetryCount, progress.LastVariant, progress.LevelInProgress), Is.EqualTo((0, 1, 7)));
        }

        [Test]
        public void StartAttempt_AfterEachRetry_PlaysV1ToV5InOrder()
        {
            var progress = new LevelProgress(currentLevel: 7, allLevelsComplete: false, levelCount: 100);
            var random = new Random(1);

            for (int expected = 1; expected <= 5; expected++)
            {
                Assert.That(progress.StartAttempt(random), Is.EqualTo(expected));
                Assert.That(progress.LastVariant, Is.EqualTo(expected));
                progress.RecordRetry();
            }

            Assert.That(progress.RetryCount, Is.EqualTo(5));
        }

        [Test]
        public void StartAttempt_ContinuesFromSavedRetries()
        {
            var progress = new LevelProgress(7, false, 100, retryCount: 2, lastVariant: 2, levelInProgress: 0);

            Assert.That(progress.StartAttempt(new Random(1)), Is.EqualTo(3));
            Assert.That(progress.RetryCount, Is.EqualTo(2));
        }

        [Test]
        public void StartAttempt_AfterFiveRetries_NeverRepeatsTheLastVariant()
        {
            var progress = new LevelProgress(7, false, 100, retryCount: 5, lastVariant: 5, levelInProgress: 0);
            var random = new Random(12345);

            for (int i = 0; i < 200; i++)
            {
                int previous = progress.LastVariant;
                int variant = progress.StartAttempt(random);
                Assert.That(variant, Is.InRange(3, 5));
                Assert.That(variant, Is.Not.EqualTo(previous), $"retry {progress.RetryCount}");
                progress.RecordRetry();
            }
        }

        [Test]
        public void ClearLevelInProgress_EndsTheAttemptOnly()
        {
            var progress = new LevelProgress(7, false, 100, retryCount: 1, lastVariant: 2, levelInProgress: 7);

            progress.ClearLevelInProgress();

            Assert.That((progress.RetryCount, progress.LastVariant, progress.LevelInProgress), Is.EqualTo((1, 2, 0)));
        }

        [Test]
        public void ResolveInterruptedAttempt_AtTheCurrentLevel_CountsARetryAndClearsTheMark()
        {
            var progress = new LevelProgress(7, false, 100, retryCount: 1, lastVariant: 2, levelInProgress: 7);

            Assert.That(progress.ResolveInterruptedAttempt(), Is.True);

            Assert.That((progress.RetryCount, progress.LevelInProgress), Is.EqualTo((2, 0)));
            Assert.That(progress.ResolveInterruptedAttempt(), Is.False, "only once");
        }

        [Test]
        public void ResolveInterruptedAttempt_AtAnotherLevel_OnlyClearsTheMark()
        {
            var progress = new LevelProgress(7, false, 100, retryCount: 1, lastVariant: 2, levelInProgress: 3);

            Assert.That(progress.ResolveInterruptedAttempt(), Is.True);

            Assert.That((progress.RetryCount, progress.LevelInProgress), Is.EqualTo((1, 0)));
        }

        [Test]
        public void RecordWin_ResetsTheRetriesForTheNextLevel_AndEndsTheAttempt()
        {
            var progress = new LevelProgress(7, false, 100, retryCount: 8, lastVariant: 4, levelInProgress: 7);

            progress.RecordWin(7);

            Assert.That((progress.CurrentLevel, progress.RetryCount, progress.LastVariant), Is.EqualTo((8, 0, 0)));
            Assert.That(progress.LevelInProgress, Is.EqualTo(0));
            Assert.That(progress.StartAttempt(new Random(1)), Is.EqualTo(1), "the next level starts on V1");
        }

        [Test]
        public void RecordWin_OfAReplayAfterTheRun_AlsoResetsTheRetries()
        {
            var progress = new LevelProgress(100, true, 100, retryCount: 3, lastVariant: 3, levelInProgress: 100);

            Assert.That(progress.RecordWin(100), Is.False, "no progress");
            Assert.That((progress.RetryCount, progress.LastVariant, progress.LevelInProgress), Is.EqualTo((0, 0, 0)));
        }

        [Test]
        public void RecordWin_OfAnotherLevel_KeepsTheRetries()
        {
            var progress = new LevelProgress(7, false, 100, retryCount: 2, lastVariant: 2, levelInProgress: 0);

            progress.RecordWin(3);

            Assert.That((progress.RetryCount, progress.LastVariant), Is.EqualTo((2, 2)));
        }

        [Test]
        public void JumpTo_MovesProgress_AndStartsTheLevelAfresh()
        {
            var progress = new LevelProgress(100, true, 100, retryCount: 3, lastVariant: 4, levelInProgress: 100);

            progress.JumpTo(20);

            Assert.That((progress.CurrentLevel, progress.AllLevelsComplete), Is.EqualTo((20, false)));
            Assert.That((progress.RetryCount, progress.LastVariant, progress.LevelInProgress), Is.EqualTo((0, 0, 0)));
            Assert.That(progress.StartAttempt(new Random(1)), Is.EqualTo(1));
        }

        [TestCase(0)]
        [TestCase(101)]
        public void JumpTo_RejectsALevelOutsideTheRun(int level)
        {
            var progress = new LevelProgress(1, false, 100);

            Assert.Throws<ArgumentOutOfRangeException>(() => progress.JumpTo(level));
        }

        [TestCase(-1, 0, 0)]
        [TestCase(0, -1, 0)]
        [TestCase(0, 6, 0)]
        [TestCase(0, 0, -1)]
        [TestCase(0, 0, 101)]
        public void Constructor_RejectsOutOfRangeRetriesVariantsAndMarks(int retryCount, int lastVariant, int levelInProgress)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new LevelProgress(1, false, 100, retryCount, lastVariant, levelInProgress));
        }
    }
}
