using System;
using BallSort.Core.Player;
using BallSort.SaveJson;
using UnityEngine;

namespace BallSort.Game.Services
{
    /// <summary>
    /// Stores the player's save: one versioned JSON blob (see <see cref="SaveJsonCodec"/>) under a single
    /// PlayerPrefs key.
    /// </summary>
    /// <remarks>
    /// A save that cannot be used (corrupt, missing fields, or written by a newer build) is
    /// replaced by a fresh one with a warning rather than crashing the game on launch.
    /// </remarks>
    public sealed class SaveService
    {
        /// <summary>The PlayerPrefs key holding the save.</summary>
        public const string PrefsKey = "BallSort.Save";

        private readonly EconomyConfig _economy;
        private readonly int _levelCount;
        private readonly IClock _clock;

        /// <param name="economy">Supplies a new player's starting values and the lives rules.</param>
        /// <param name="levelCount">Levels in the game; a saved level number is clamped into range.</param>
        /// <param name="clock">The current time, for the lives refill.</param>
        public SaveService(EconomyConfig economy, int levelCount, IClock clock)
        {
            _economy = economy != null ? economy : throw new ArgumentNullException(nameof(economy));
            _levelCount = Math.Max(1, levelCount);
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>Loads the saved player, or a new player when there is no usable save.</summary>
        public PlayerData Load()
        {
            PlayerRules rules = _economy.CreatePlayerRules();
            string json = PlayerPrefs.GetString(PrefsKey, string.Empty);
            SaveData save = SaveJsonCodec.Read(json, rules, _levelCount, out string warning);
            if (warning != null)
            {
                Debug.LogWarning(warning);
            }

            return new PlayerData(save, _levelCount, rules, _clock);
        }

        /// <summary>Writes <paramref name="player"/> and flushes PlayerPrefs to disk.</summary>
        public void Save(PlayerData player)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            PlayerPrefs.SetString(PrefsKey, SaveJsonCodec.Write(player.ToSaveData()));
            PlayerPrefs.Save();
        }

        /// <summary>Deletes the save. The next <see cref="Load"/> starts a new player.</summary>
        public static void Delete()
        {
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.Save();
        }
    }
}
