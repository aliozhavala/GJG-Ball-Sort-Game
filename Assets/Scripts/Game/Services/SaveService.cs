using System;
using BallSort.Core.Levels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BallSort.Game.Services
{
    /// <summary>
    /// Reads and writes the player's save: one versioned JSON blob under a single PlayerPrefs key.
    /// </summary>
    /// <remarks>
    /// A save that cannot be used (corrupt, missing fields, or written by a newer build) is
    /// replaced by a fresh one with a warning rather than crashing the game on launch.
    /// </remarks>
    public sealed class SaveService
    {
        /// <summary>The save format this build writes. Older versions are migrated on load.</summary>
        public const int CurrentVersion = 2;

        /// <summary>The PlayerPrefs key holding the save.</summary>
        public const string PrefsKey = "BallSort.Save";

        private readonly EconomyConfig _economy;
        private readonly int _levelCount;

        /// <param name="economy">Supplies a new player's starting values.</param>
        /// <param name="levelCount">Levels in the game; a saved level number is clamped into range.</param>
        public SaveService(EconomyConfig economy, int levelCount)
        {
            _economy = economy != null ? economy : throw new ArgumentNullException(nameof(economy));
            _levelCount = Math.Max(1, levelCount);
        }

        /// <summary>Loads the saved player, or a new player when there is no usable save.</summary>
        public PlayerData Load()
        {
            string json = PlayerPrefs.GetString(PrefsKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return new PlayerData(SaveData.CreateDefault(_economy), _levelCount);
            }

            SaveData save;
            try
            {
                // Read loosely first: an older save lacks fields the current shape requires, and gets
                // them from its migration before the strict read.
                JObject root = JObject.Parse(json);
                int version = root.TryGetValue(nameof(SaveData.Version), out JToken token) && token.Type == JTokenType.Integer
                    ? token.Value<int>()
                    : 0;
                if (version > CurrentVersion || version < 1)
                {
                    Debug.LogWarning($"Save version {version} is not supported by this build and was reset.");
                    return new PlayerData(SaveData.CreateDefault(_economy), _levelCount);
                }

                if (version == 1)
                {
                    MigrateFromVersion1(root);
                }

                save = root.ToObject<SaveData>();
            }
            catch (JsonException e)
            {
                Debug.LogWarning($"Save is unreadable and was reset: {e.Message}");
                return new PlayerData(SaveData.CreateDefault(_economy), _levelCount);
            }

            return new PlayerData(Sanitize(save), _levelCount);
        }

        /// <summary>Writes <paramref name="player"/> and flushes PlayerPrefs to disk.</summary>
        public void Save(PlayerData player)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            PlayerPrefs.SetString(PrefsKey, JsonConvert.SerializeObject(player.ToSaveData()));
            PlayerPrefs.Save();
        }

        /// <summary>Deletes the save. The next <see cref="Load"/> starts a new player.</summary>
        public static void Delete()
        {
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.Save();
        }

        /// <summary>Version 2 added the tries at the current level (level variants); a version 1 save starts with none.</summary>
        private static void MigrateFromVersion1(JObject save)
        {
            save[nameof(SaveData.LevelTry)] = 0;
            save[nameof(SaveData.LastVariant)] = 0;
            save[nameof(SaveData.Version)] = 2;
        }

        /// <summary>Clamps values a hand-edited or stale save could have pushed out of range.</summary>
        private SaveData Sanitize(SaveData save)
        {
            save.CurrentLevel = Mathf.Clamp(save.CurrentLevel, 1, _levelCount);
            save.Coins = Math.Max(0, save.Coins);
            save.Lives = Math.Max(0, save.Lives);
            save.UndoBoosters = Math.Max(0, save.UndoBoosters);
            save.ShuffleBoosters = Math.Max(0, save.ShuffleBoosters);
            save.AddTubeBoosters = Math.Max(0, save.AddTubeBoosters);
            save.LevelTry = Math.Max(0, save.LevelTry);
            save.LastVariant = Mathf.Clamp(save.LastVariant, 0, VariantSelector.VariantCount);
            return save;
        }
    }
}
