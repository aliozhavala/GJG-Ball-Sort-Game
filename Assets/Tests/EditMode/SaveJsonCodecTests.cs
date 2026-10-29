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
        private const int LevelCount = 100;

        private static readonly PlayerRules Rules =
            new PlayerRules(startCoins: 1000, startLives: 5, startUndo: 99, startShuffle: 99, startAddTube: 99);

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
            Assert.That(save.UndoBoosters, Is.EqualTo(7));
            Assert.That(save.ShuffleBoosters, Is.EqualTo(8));
            Assert.That(save.AddTubeBoosters, Is.EqualTo(9));
            Assert.That(save.LevelTry, Is.EqualTo(6));
            Assert.That(save.LastVariant, Is.EqualTo(4));
            Assert.That(save.SoundOn, Is.False);
            Assert.That(save.MusicOn, Is.True);
            Assert.That(save.HapticsOn, Is.False);
        }

        [Test]
        public void PlayerData_SurvivesAWriteAndRead()
        {
            var player = new PlayerData(SaveData.CreateDefault(Rules), LevelCount);
            player.Wallet.Add(50);
            player.Boosters.TrySpend(BoosterType.Undo);
            player.SetSetting(SettingType.Music, false);
            player.RecordWin(1, reward: 20);
            player.StartTry(new Random(1));

            SaveData save = SaveJsonCodec.Read(SaveJsonCodec.Write(player.ToSaveData()), Rules, LevelCount, out _);
            var reloaded = new PlayerData(save, LevelCount);

            Assert.That(reloaded.CurrentLevel, Is.EqualTo(2));
            Assert.That(reloaded.Wallet.Coins, Is.EqualTo(1070));
            Assert.That(reloaded.Boosters.GetCount(BoosterType.Undo), Is.EqualTo(98));
            Assert.That(reloaded.GetSetting(SettingType.Music), Is.False);
            Assert.That(reloaded.GetSetting(SettingType.Sound), Is.True);
            Assert.That(reloaded.LevelTry, Is.EqualTo(1));
            Assert.That(reloaded.LastVariant, Is.EqualTo(1));
        }

        [Test]
        public void Read_Version1_MigratesWithNoTriesAndKeepsTheRest()
        {
            JObject json = JObject.Parse(SaveJsonCodec.Write(SampleSave()));
            json[nameof(SaveData.Version)] = 1;
            json.Remove(nameof(SaveData.LevelTry));
            json.Remove(nameof(SaveData.LastVariant));

            SaveData save = SaveJsonCodec.Read(json.ToString(), Rules, LevelCount, out string warning);

            Assert.That(warning, Is.Null);
            Assert.That(save.Version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(save.LevelTry, Is.EqualTo(0));
            Assert.That(save.LastVariant, Is.EqualTo(0));
            Assert.That(save.CurrentLevel, Is.EqualTo(42));
            Assert.That(save.Coins, Is.EqualTo(1234));
        }

        [Test]
        public void Read_WithAMissingField_StartsANewPlayerWithAWarning()
        {
            JObject json = JObject.Parse(SaveJsonCodec.Write(SampleSave()));
            json.Remove(nameof(SaveData.Coins));

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
            sample.Lives = -1;
            sample.UndoBoosters = -2;
            sample.ShuffleBoosters = -3;
            sample.AddTubeBoosters = -4;
            sample.LevelTry = -6;
            sample.LastVariant = 9;

            SaveData save = SaveJsonCodec.Read(SaveJsonCodec.Write(sample), Rules, LevelCount, out string warning);

            Assert.That(warning, Is.Null);
            Assert.That(save.CurrentLevel, Is.EqualTo(LevelCount));
            Assert.That(save.Coins, Is.EqualTo(0));
            Assert.That(save.Lives, Is.EqualTo(0));
            Assert.That(save.UndoBoosters, Is.EqualTo(0));
            Assert.That(save.ShuffleBoosters, Is.EqualTo(0));
            Assert.That(save.AddTubeBoosters, Is.EqualTo(0));
            Assert.That(save.LevelTry, Is.EqualTo(0));
            Assert.That(save.LastVariant, Is.EqualTo(5));
        }

        [Test]
        public void Read_LevelZeroAndNegativeVariant_AreClampedUp()
        {
            SaveData sample = SampleSave();
            sample.CurrentLevel = 0;
            sample.LastVariant = -1;

            SaveData save = SaveJsonCodec.Read(SaveJsonCodec.Write(sample), Rules, LevelCount, out _);

            Assert.That(save.CurrentLevel, Is.EqualTo(1));
            Assert.That(save.LastVariant, Is.EqualTo(0));
        }

        private static SaveData SampleSave() =>
            new SaveData
            {
                Version = SaveData.CurrentVersion,
                CurrentLevel = 42,
                AllLevelsComplete = false,
                Coins = 1234,
                Lives = 3,
                UndoBoosters = 7,
                ShuffleBoosters = 8,
                AddTubeBoosters = 9,
                LevelTry = 6,
                LastVariant = 4,
                SoundOn = false,
                MusicOn = true,
                HapticsOn = false,
            };

        private static void AssertIsNewPlayer(SaveData save)
        {
            Assert.That(save.Version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(save.CurrentLevel, Is.EqualTo(1));
            Assert.That(save.AllLevelsComplete, Is.False);
            Assert.That(save.Coins, Is.EqualTo(Rules.StartCoins));
            Assert.That(save.Lives, Is.EqualTo(Rules.StartLives));
            Assert.That(save.UndoBoosters, Is.EqualTo(Rules.StartUndo));
            Assert.That(save.ShuffleBoosters, Is.EqualTo(Rules.StartShuffle));
            Assert.That(save.AddTubeBoosters, Is.EqualTo(Rules.StartAddTube));
            Assert.That(save.LevelTry, Is.EqualTo(0));
            Assert.That(save.LastVariant, Is.EqualTo(0));
            Assert.That(save.SoundOn && save.MusicOn && save.HapticsOn, Is.True);
        }
    }
}
