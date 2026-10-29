using System;
using BallSort.Core.Player;
using BallSort.SaveJson;
using NUnit.Framework;

namespace BallSort.Tests.EditMode
{
    /// <summary>
    /// Players for the lives, attempt and save tests: the shipped rules (5 lives, one back every 30
    /// minutes), a 100-level run, and the save and launch steps the game performs.
    /// </summary>
    internal static class TestPlayers
    {
        public const int LevelCount = 100;

        public static readonly TimeSpan RefillInterval = TimeSpan.FromMinutes(30);

        public static readonly PlayerRules Rules = new PlayerRules(
            startCoins: 1000, maxLives: 5, lifeRefillInterval: RefillInterval, startUndo: 99, startShuffle: 99, startAddTube: 99);

        /// <summary>An arbitrary "now" the tests start from.</summary>
        public static readonly DateTime Start = new DateTime(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc);

        /// <summary>A brand-new player: full lives, level 1.</summary>
        public static PlayerData New(IClock clock) => FromSave(SaveData.CreateDefault(Rules), clock);

        public static PlayerData FromSave(SaveData save, IClock clock) => new PlayerData(save, LevelCount, Rules, clock);

        /// <summary>The save text the game would write for <paramref name="player"/> now.</summary>
        public static string Save(PlayerData player) => SaveJsonCodec.Write(player.ToSaveData());

        /// <summary>
        /// What the game does on launch (<c>AppRoot</c>): read the save, then settle an attempt the app
        /// was closed in the middle of.
        /// </summary>
        public static PlayerData Launch(string savedJson, IClock clock)
        {
            SaveData save = SaveJsonCodec.Read(savedJson, Rules, LevelCount, out string warning);
            Assert.That(warning, Is.Null, "the save should read cleanly");
            PlayerData player = FromSave(save, clock);
            player.ResolveInterruptedAttempt();
            return player;
        }
    }
}
