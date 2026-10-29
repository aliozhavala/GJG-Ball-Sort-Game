using BallSort.Core.Levels;
using NUnit.Framework;

namespace BallSort.Tests.EditMode
{
    [TestFixture]
    public sealed class EpisodeMapTests
    {
        private static readonly int[] FiveEpisodes = { 1, 21, 41, 61, 81 };

        [TestCase(1, 0)]
        [TestCase(20, 0)]
        [TestCase(21, 1)]
        [TestCase(40, 1)]
        [TestCase(41, 2)]
        [TestCase(80, 3)]
        [TestCase(81, 4)]
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
