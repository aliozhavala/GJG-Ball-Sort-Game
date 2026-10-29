using BallSort.Core.Player;
using NUnit.Framework;

namespace BallSort.Tests.EditMode
{
    [TestFixture]
    public sealed class PlayerNameTests
    {
        [Test]
        public void ANewPlayer_IsCalledPlayer()
        {
            PlayerData player = TestPlayers.New(new FakeClock(TestPlayers.Start));

            Assert.That(player.PlayerName, Is.EqualTo(PlayerNameRules.Default));
        }

        [Test]
        public void ANewName_IsTrimmed_AndSurvivesASaveAndRelaunch()
        {
            var clock = new FakeClock(TestPlayers.Start);
            PlayerData player = TestPlayers.New(clock);

            Assert.That(player.TrySetPlayerName("  Ada  "), Is.True);

            Assert.That(player.PlayerName, Is.EqualTo("Ada"));
            Assert.That(TestPlayers.Launch(TestPlayers.Save(player), clock).PlayerName, Is.EqualTo("Ada"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("ABCDEFGHIJKLMNOPQ")]
        public void AnEmptyOrTooLongName_IsRefused_AndChangesNothing(string name)
        {
            PlayerData player = TestPlayers.New(new FakeClock(TestPlayers.Start));
            int saves = 0;
            player.Changed += () => saves++;

            Assert.That(player.TrySetPlayerName(name), Is.False);

            Assert.That(player.PlayerName, Is.EqualTo(PlayerNameRules.Default));
            Assert.That(saves, Is.EqualTo(0));
        }

        [Test]
        public void ANameOfTheMaximumLength_IsAccepted()
        {
            PlayerData player = TestPlayers.New(new FakeClock(TestPlayers.Start));

            Assert.That(player.TrySetPlayerName(new string('x', PlayerNameRules.MaxLength)), Is.True);
        }
    }
}
