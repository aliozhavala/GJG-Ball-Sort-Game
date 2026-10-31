using BallSort.Core.Player;
using NUnit.Framework;

namespace BallSort.Tests.EditMode
{
    [TestFixture]
    public sealed class AvatarTests
    {
        [Test]
        public void ANewPlayer_HasTheDefaultAvatar()
        {
            PlayerData player = TestPlayers.New(new FakeClock(TestPlayers.Start));

            Assert.That(player.AvatarId, Is.EqualTo(AvatarRules.Default));
        }

        [Test]
        public void APickedAvatar_IsSaved_AndSurvivesARelaunch()
        {
            var clock = new FakeClock(TestPlayers.Start);
            PlayerData player = TestPlayers.New(clock);
            int saves = 0;
            player.Changed += () => saves++;

            Assert.That(player.TrySetAvatar(AvatarRules.Count), Is.True);

            Assert.That(player.AvatarId, Is.EqualTo(AvatarRules.Count));
            Assert.That(saves, Is.EqualTo(1));
            Assert.That(TestPlayers.Launch(TestPlayers.Save(player), clock).AvatarId, Is.EqualTo(AvatarRules.Count));
        }

        [Test]
        public void PickingTheSameAvatarAgain_ChangesNothing()
        {
            PlayerData player = TestPlayers.New(new FakeClock(TestPlayers.Start));
            int saves = 0;
            player.Changed += () => saves++;

            Assert.That(player.TrySetAvatar(AvatarRules.Default), Is.True);

            Assert.That(saves, Is.EqualTo(0));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(AvatarRules.Count + 1)]
        public void AnIdWithoutAnAvatar_IsRefused_AndChangesNothing(int avatarId)
        {
            PlayerData player = TestPlayers.New(new FakeClock(TestPlayers.Start));
            int saves = 0;
            player.Changed += () => saves++;

            Assert.That(player.TrySetAvatar(avatarId), Is.False);

            Assert.That(player.AvatarId, Is.EqualTo(AvatarRules.Default));
            Assert.That(saves, Is.EqualTo(0));
        }
    }
}
