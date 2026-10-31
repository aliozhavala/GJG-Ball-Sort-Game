using BallSort.Core.Levels;
using NUnit.Framework;

namespace BallSort.Tests.EditMode
{
    [TestFixture]
    public sealed class EpisodeMapTests
    {
        // The shipped split: levels 1–19, 20–39, 40–59, 60–79 and 80–100.
        private static readonly int[] FiveEpisodes = { 1, 20, 40, 60, 80 };

        [TestCase(1, 0)]
        [TestCase(19, 0)]
        [TestCase(20, 1)]
        [TestCase(39, 1)]
        [TestCase(40, 2)]
        [TestCase(59, 2)]
        [TestCase(60, 3)]
        [TestCase(79, 3)]
        [TestCase(80, 4)]
        [TestCase(100, 4)]
        public void Find_ReturnsTheLastEpisodeStartingAtOrBeforeTheLevel(int level, int episode)
        {
            Assert.That(EpisodeMap.Find(FiveEpisodes, level), Is.EqualTo(episode));
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void Find_ALevelBeforeTheRun_IsInTheFirstEpisode(int level)
        {
            // A level file outside the run is visited as level 0.
            Assert.That(EpisodeMap.Find(FiveEpisodes, level), Is.EqualTo(0));
        }

        [Test]
        public void Find_ALevelPastTheLastFirstLevel_StaysInTheLastEpisode()
        {
            Assert.That(EpisodeMap.Find(FiveEpisodes, 901), Is.EqualTo(4));
        }

        [TestCase(19, 20, 1)]
        [TestCase(39, 40, 2)]
        [TestCase(59, 60, 3)]
        [TestCase(79, 80, 4)]
        public void FindUnlocked_WinningAnEpisodesLastLevel_UnlocksTheNext(int won, int next, int episode)
        {
            Assert.That(EpisodeMap.FindUnlocked(FiveEpisodes, won, next), Is.EqualTo(episode));
        }

        [TestCase(1, 2)]
        [TestCase(18, 19)]
        [TestCase(20, 21)]
        [TestCase(40, 41)]
        [TestCase(80, 81)]
        [TestCase(99, 100)]
        public void FindUnlocked_AWinInsideAnEpisode_UnlocksNothing(int won, int next)
        {
            Assert.That(EpisodeMap.FindUnlocked(FiveEpisodes, won, next), Is.EqualTo(-1));
        }

        [TestCase(100, 100)]
        [TestCase(19, 19)]
        public void FindUnlocked_AWinThatDoesNotAdvance_UnlocksNothing(int won, int next)
        {
            // The last level, or a replay: progress stays where it is.
            Assert.That(EpisodeMap.FindUnlocked(FiveEpisodes, won, next), Is.EqualTo(-1));
        }

        [Test]
        public void Validate_AcceptsAscendingEpisodesStartingAtLevelOne()
        {
            Assert.That(EpisodeMap.Validate(FiveEpisodes), Is.Null);
            Assert.That(EpisodeMap.Validate(new[] { 1 }), Is.Null);
        }

        [Test]
        public void Validate_RejectsBrokenLists()
        {
            Assert.That(EpisodeMap.Validate(new int[0]), Does.Contain("at least one"));
            Assert.That(EpisodeMap.Validate(new[] { 2, 21 }), Does.Contain("level 1"));
            Assert.That(EpisodeMap.Validate(new[] { 1, 21, 21 }), Does.Contain("Episode 3"));
            Assert.That(EpisodeMap.Validate(new[] { 1, 41, 21 }), Does.Contain("Episode 3"));
        }
    }
}
