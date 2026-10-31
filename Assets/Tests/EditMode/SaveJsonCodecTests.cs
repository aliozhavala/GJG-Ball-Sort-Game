using System;
using BallSort.Core.Player;
using BallSort.SaveJson;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BallSort.Tests.EditMode
{
    [TestFixture]
    public sealed class SaveJsonCodecTests
    {
        private const int LevelCount = TestPlayers.LevelCount;

        private static readonly PlayerRules Rules = TestPlayers.Rules;

        [TestCase(null)]
        [TestCase("")]
        public void Read_WithNoSave_StartsANewPlayerWithoutAWarning(string json)
        {
            SaveData save = SaveJsonCodec.Read(json, Rules, LevelCount, out string warning);

            Assert.That(warning, Is.Null);
            AssertIsNewPlayer(save);
        }

        [Test]
        public void WriteThenRead_KeepsEveryField()
        {
            SaveData save = SaveJsonCodec.Read(SaveJsonCodec.Write(SampleSave()), Rules, LevelCount, out string warning);

            Assert.That(warning, Is.Null);
            Assert.That(save.Version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(save.CurrentLevel, Is.EqualTo(42));
            Assert.That(save.AllLevelsComplete, Is.False);
            Assert.That(save.Coins, Is.EqualTo(1234));
            Assert.That(save.Lives, Is.EqualTo(3));
            Assert.That(save.LifeTimerStartUnixMs, Is.EqualTo(1761739200000L));
            Assert.That(save.UndoBoosters, Is.EqualTo(7));
            Assert.That(save.ShuffleBoosters, Is.EqualTo(8));
            Assert.That(save.AddTubeBoosters, Is.EqualTo(9));
            Assert.That(save.RetryCount, Is.EqualTo(6));
            Assert.That(save.LastVariant, Is.EqualTo(4));
            Assert.That(save.LevelInProgress, Is.EqualTo(42));
            Assert.That(save.PlayerName, Is.EqualTo("Ada"));
            Assert.That(save.Avatar, Is.EqualTo(3));
            Assert.That(save.TutorialsSeen, Is.EqualTo(new[] { 1, 7, 20 }));
            Assert.That(save.SoundOn, Is.False);
            Assert.That(save.MusicOn, Is.True);
            Assert.That(save.HapticsOn, Is.False);
        }

        [Test]
        public void PlayerData_SurvivesAWriteAndRead()
        {
            var clock = new FakeClock(TestPlayers.Start);
            PlayerData player = TestPlayers.New(clock);
            player.Wallet.Add(50);
            player.Boosters.TrySpend(BoosterType.Undo);
            player.SetSetting(SettingType.Music, false);
            player.RecordWin(1, reward: 20);
            player.BeginAttempt(new Random(1));
            player.LeaveAttempt();
            player.TrySetPlayerName("Ada");
            player.TrySetAvatar(4);
            player.MarkTutorialSeen(7);

            SaveData save = SaveJsonCodec.Read(SaveJsonCodec.Write(player.ToSaveData()), Rules, LevelCount, out _);
            PlayerData reloaded = TestPlayers.FromSave(save, clock);

            Assert.That(reloaded.CurrentLevel, Is.EqualTo(2));
            Assert.That(reloaded.Wallet.Coins, Is.EqualTo(1070));
            Assert.That(reloaded.Boosters.GetCount(BoosterType.Undo), Is.EqualTo(98));
            Assert.That(reloaded.GetSetting(SettingType.Music), Is.False);
            Assert.That(reloaded.GetSetting(SettingType.Sound), Is.True);
            Assert.That(reloaded.LifeCount, Is.EqualTo(4));
            Assert.That(reloaded.GetTimeUntilNextLife(), Is.EqualTo(TestPlayers.RefillInterval));
            Assert.That(reloaded.RetryCount, Is.EqualTo(1));
            Assert.That(reloaded.LastVariant, Is.EqualTo(1));
            Assert.That(reloaded.LevelInProgress, Is.EqualTo(0));
            Assert.That(reloaded.PlayerName, Is.EqualTo("Ada"));
            Assert.That(reloaded.AvatarId, Is.EqualTo(4));
            Assert.That(reloaded.HasSeenTutorial(7), Is.True);
        }

        [Test]
        public void Read_Version2_TurnsTheTriesIntoRetries_AndAddsTheNewFields()
        {
            JObject json = Version2Save();

            SaveData save = SaveJsonCodec.Read(json.ToString(), Rules, LevelCount, out string warning);

            Assert.That(warning, Is.Null);
            Assert.That(save.Version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(save.RetryCount, Is.EqualTo(6), "the tries started become the retries counted");
            Assert.That(save.LastVariant, Is.EqualTo(4));
            Assert.That(save.LifeTimerStartUnixMs, Is.EqualTo(0));
            Assert.That(save.LevelInProgress, Is.EqualTo(0));
            Assert.That(save.PlayerName, Is.EqualTo(PlayerNameRules.Default));
            Assert.That(save.TutorialsSeen, Is.Empty);
            Assert.That(save.Avatar, Is.EqualTo(AvatarRules.Default));
            Assert.That(save.CurrentLevel, Is.EqualTo(42));
            Assert.That(save.Coins, Is.EqualTo(1234));
            Assert.That(save.Lives, Is.EqualTo(3));
        }

        [Test]
        public void Read_Version3_GetsTheDefaultAvatar_AndKeepsEverythingElse()
        {
            JObject json = JObject.Parse(SaveJsonCodec.Write(SampleSave()));
            json[nameof(SaveData.Version)] = 3;
            json.Remove(nameof(SaveData.Avatar));

            SaveData save = SaveJsonCodec.Read(json.ToString(), Rules, LevelCount, out string warning);

            Assert.That(warning, Is.Null);
            Assert.That(save.Version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(save.Avatar, Is.EqualTo(AvatarRules.Default));
            Assert.That(save.PlayerName, Is.EqualTo("Ada"));
            Assert.That(save.RetryCount, Is.EqualTo(6));
            Assert.That(save.LevelInProgress, Is.EqualTo(42));
            Assert.That(save.TutorialsSeen, Is.EqualTo(new[] { 1, 7, 20 }));
        }

        [Test]
        public void AVersion2PlayerBelowTheMaximum_StartsWaitingForALifeAtLoad()
        {
            SaveData save = SaveJsonCodec.Read(Version2Save().ToString(), Rules, LevelCount, out _);
            PlayerData player = TestPlayers.FromSave(save, new FakeClock(TestPlayers.Start));

            Assert.That(player.LifeCount, Is.EqualTo(3));
            Assert.That(player.GetTimeUntilNextLife(), Is.EqualTo(TestPlayers.RefillInterval));
        }

        [Test]
        public void Read_Version1_MigratesThroughVersion2()
        {
            JObject json = Version2Save();
            json[nameof(SaveData.Version)] = 1;
            json.Remove("LevelTry");
            json.Remove(nameof(SaveData.LastVariant));

            SaveData save = SaveJsonCodec.Read(json.ToString(), Rules, LevelCount, out string warning);

            Assert.That(warning, Is.Null);
            Assert.That(save.Version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(save.RetryCount, Is.EqualTo(0));
            Assert.That(save.LastVariant, Is.EqualTo(0));
            Assert.That(save.PlayerName, Is.EqualTo(PlayerNameRules.Default));
            Assert.That(save.TutorialsSeen, Is.Empty);
            Assert.That(save.Avatar, Is.EqualTo(AvatarRules.Default));
            Assert.That(save.CurrentLevel, Is.EqualTo(42));
            Assert.That(save.Coins, Is.EqualTo(1234));
        }

        [TestCase(nameof(SaveData.Coins))]
        [TestCase(nameof(SaveData.LifeTimerStartUnixMs))]
        [TestCase(nameof(SaveData.LevelInProgress))]
        [TestCase(nameof(SaveData.PlayerName))]
        [TestCase(nameof(SaveData.TutorialsSeen))]
        [TestCase(nameof(SaveData.Avatar))]
        public void Read_WithAMissingField_StartsANewPlayerWithAWarning(string field)
        {
            JObject json = JObject.Parse(SaveJsonCodec.Write(SampleSave()));
            json.Remove(field);

            SaveData save = SaveJsonCodec.Read(json.ToString(), Rules, LevelCount, out string warning);

            Assert.That(warning, Is.Not.Null);
            AssertIsNewPlayer(save);
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(SaveData.CurrentVersion + 1)]
        public void Read_UnsupportedVersion_StartsANewPlayerWithAWarning(int version)
        {
            JObject json = JObject.Parse(SaveJsonCodec.Write(SampleSave()));
            json[nameof(SaveData.Version)] = version;

            SaveData save = SaveJsonCodec.Read(json.ToString(), Rules, LevelCount, out string warning);

            Assert.That(warning, Is.Not.Null);
            AssertIsNewPlayer(save);
        }

        [TestCase("{ not json")]
        [TestCase("[1, 2]")]
        [TestCase("{ \"Coins\": 5 }")]
        public void Read_UnreadableText_StartsANewPlayerWithAWarning(string json)
        {
            SaveData save = SaveJsonCodec.Read(json, Rules, LevelCount, out string warning);

            Assert.That(warning, Is.Not.Null);
            AssertIsNewPlayer(save);
        }

        [Test]
        public void Read_OutOfRangeValues_AreClamped()
        {
            SaveData sample = SampleSave();
            sample.CurrentLevel = 500;
            sample.Coins = -5;
            sample.Lives = 9;
            sample.LifeTimerStartUnixMs = -1;
            sample.UndoBoosters = -2;
            sample.ShuffleBoosters = -3;
            sample.AddTubeBoosters = -4;
            sample.RetryCount = -6;
            sample.LastVariant = 9;
            sample.LevelInProgress = 500;
            sample.PlayerName = "   ";
            sample.Avatar = AvatarRules.Count + 1;
            sample.TutorialsSeen = new[] { 20, -1, 7, 0, 20, 1 };

            SaveData save = SaveJsonCodec.Read(SaveJsonCodec.Write(sample), Rules, LevelCount, out string warning);

            Assert.That(warning, Is.Null);
            Assert.That(save.CurrentLevel, Is.EqualTo(LevelCount));
            Assert.That(save.Coins, Is.EqualTo(0));
            Assert.That(save.Lives, Is.EqualTo(Rules.MaxLives));
            Assert.That(save.LifeTimerStartUnixMs, Is.EqualTo(0));
            Assert.That(save.UndoBoosters, Is.EqualTo(0));
            Assert.That(save.ShuffleBoosters, Is.EqualTo(0));
            Assert.That(save.AddTubeBoosters, Is.EqualTo(0));
            Assert.That(save.RetryCount, Is.EqualTo(0));
            Assert.That(save.LastVariant, Is.EqualTo(5));
            Assert.That(save.LevelInProgress, Is.EqualTo(0));
            Assert.That(save.PlayerName, Is.EqualTo(PlayerNameRules.Default));
            Assert.That(save.Avatar, Is.EqualTo(AvatarRules.Default));
            Assert.That(save.TutorialsSeen, Is.EqualTo(new[] { 1, 7, 20 }));
        }

        [Test]
        public void Read_ValuesBelowTheirRange_AreClampedUp()
        {
            SaveData sample = SampleSave();
            sample.CurrentLevel = 0;
            sample.Lives = -1;
            sample.LastVariant = -1;
            sample.LevelInProgress = -3;
            sample.PlayerName = new string('x', PlayerNameRules.MaxLength + 1);
            sample.Avatar = 0;

            SaveData save = SaveJsonCodec.Read(SaveJsonCodec.Write(sample), Rules, LevelCount, out _);

            Assert.That(save.CurrentLevel, Is.EqualTo(1));
            Assert.That(save.Lives, Is.EqualTo(0));
            Assert.That(save.LastVariant, Is.EqualTo(0));
            Assert.That(save.LevelInProgress, Is.EqualTo(0));
            Assert.That(save.PlayerName, Is.EqualTo(PlayerNameRules.Default));
            Assert.That(save.Avatar, Is.EqualTo(AvatarRules.Default));
        }

        private static SaveData SampleSave() =>
            new SaveData
            {
                Version = SaveData.CurrentVersion,
                CurrentLevel = 42,
                AllLevelsComplete = false,
                Coins = 1234,
                Lives = 3,
                LifeTimerStartUnixMs = 1761739200000L,
                UndoBoosters = 7,
                ShuffleBoosters = 8,
                AddTubeBoosters = 9,
                RetryCount = 6,
                LastVariant = 4,
                LevelInProgress = 42,
                PlayerName = "Ada",
                Avatar = 3,
                TutorialsSeen = new[] { 1, 7, 20 },
                SoundOn = false,
                MusicOn = true,
                HapticsOn = false,
            };

        /// <summary>The sample as version 2 wrote it: tries instead of retries, none of the version 3 or 4 fields.</summary>
        private static JObject Version2Save()
        {
            JObject json = JObject.Parse(SaveJsonCodec.Write(SampleSave()));
            json[nameof(SaveData.Version)] = 2;
            json["LevelTry"] = json[nameof(SaveData.RetryCount)];
            json.Remove(nameof(SaveData.RetryCount));
            json.Remove(nameof(SaveData.LifeTimerStartUnixMs));
            json.Remove(nameof(SaveData.LevelInProgress));
            json.Remove(nameof(SaveData.PlayerName));
            json.Remove(nameof(SaveData.TutorialsSeen));
            json.Remove(nameof(SaveData.Avatar));
            return json;
        }

        private static void AssertIsNewPlayer(SaveData save)
        {
            Assert.That(save.Version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(save.CurrentLevel, Is.EqualTo(1));
            Assert.That(save.AllLevelsComplete, Is.False);
            Assert.That(save.Coins, Is.EqualTo(Rules.StartCoins));
            Assert.That(save.Lives, Is.EqualTo(Rules.MaxLives));
            Assert.That(save.LifeTimerStartUnixMs, Is.EqualTo(0));
            Assert.That(save.UndoBoosters, Is.EqualTo(Rules.StartUndo));
            Assert.That(save.ShuffleBoosters, Is.EqualTo(Rules.StartShuffle));
            Assert.That(save.AddTubeBoosters, Is.EqualTo(Rules.StartAddTube));
            Assert.That(save.RetryCount, Is.EqualTo(0));
            Assert.That(save.LastVariant, Is.EqualTo(0));
            Assert.That(save.LevelInProgress, Is.EqualTo(0));
            Assert.That(save.PlayerName, Is.EqualTo(PlayerNameRules.Default));
            Assert.That(save.TutorialsSeen, Is.Empty);
            Assert.That(save.Avatar, Is.EqualTo(AvatarRules.Default));
            Assert.That(save.SoundOn && save.MusicOn && save.HapticsOn, Is.True);
        }
    }
}
