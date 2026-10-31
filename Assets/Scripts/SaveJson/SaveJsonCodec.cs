using System;
using System.Linq;
using System.Reflection;
using BallSort.Core.Levels;
using BallSort.Core.Player;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace BallSort.SaveJson
{
    /// <summary>
    /// Turns the player's <see cref="SaveData"/> into JSON and back: version check, migrations from older
    /// versions, every field required, and out-of-range values clamped.
    /// </summary>
    /// <remarks>
    /// Lives in its own engine-free assembly (Core + Newtonsoft only), like the level parser, so tests can
    /// write and read a save without the game's PlayerPrefs. Reading never throws on bad text: a save that
    /// cannot be used is replaced by a new player's, with a warning for the caller to log.
    /// </remarks>
    public static class SaveJsonCodec
    {
        // Version 2's name for the tries started at the current level; version 3 counts retries instead.
        private const string Version2TryCount = "LevelTry";

        private static readonly JsonSerializerSettings ReadSettings = new JsonSerializerSettings
        {
            ContractResolver = new RequireEveryPropertyResolver(),
        };

        /// <summary>The JSON text of <paramref name="save"/>.</summary>
        public static string Write(SaveData save)
        {
            if (save == null)
            {
                throw new ArgumentNullException(nameof(save));
            }

            return JsonConvert.SerializeObject(save);
        }

        /// <summary>Reads a save written by this build or an older one.</summary>
        /// <param name="json">The stored text; null or empty for a player who has never saved.</param>
        /// <param name="rules">A new player's values, used when there is no usable save.</param>
        /// <param name="levelCount">Levels in the game; a saved level number is clamped into range.</param>
        /// <param name="warning">
        /// Why the save was replaced by a new player's; null when it was read, or when there was none.
        /// </param>
        public static SaveData Read(string json, PlayerRules rules, int levelCount, out string warning)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            warning = null;
            if (string.IsNullOrEmpty(json))
            {
                return SaveData.CreateDefault(rules);
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
                if (version > SaveData.CurrentVersion || version < 1)
                {
                    warning = $"Save version {version} is not supported by this build and was reset.";
                    return SaveData.CreateDefault(rules);
                }

                if (version == 1)
                {
                    MigrateFromVersion1(root);
                    version = 2;
                }

                if (version == 2)
                {
                    MigrateFromVersion2(root);
                    version = 3;
                }

                if (version == 3)
                {
                    MigrateFromVersion3(root);
                }

                save = root.ToObject<SaveData>(JsonSerializer.Create(ReadSettings));
            }
            catch (JsonException e)
            {
                warning = $"Save is unreadable and was reset: {e.Message}";
                return SaveData.CreateDefault(rules);
            }

            return Sanitize(save, rules, Math.Max(1, levelCount));
        }

        /// <summary>Version 2 added the tries at the current level (level variants); a version 1 save starts with none.</summary>
        private static void MigrateFromVersion1(JObject save)
        {
            save[Version2TryCount] = 0;
            save[nameof(SaveData.LastVariant)] = 0;
            save[nameof(SaveData.Version)] = 2;
        }

        /// <summary>
        /// Version 3 added lives that refill, the level-in-progress mark, the player name and the tutorials
        /// seen, and replaced the tries started by the retries counted. The variants already played keep
        /// their order: version 2 counted a try on entering, so its count is the retries a version 3 save
        /// would hold after the same attempts. The life timer is left empty, so it starts at load if lives
        /// are below the maximum.
        /// </summary>
        private static void MigrateFromVersion2(JObject save)
        {
            if (save.TryGetValue(Version2TryCount, out JToken tries))
            {
                save[nameof(SaveData.RetryCount)] = tries;
                save.Remove(Version2TryCount);
            }

            save[nameof(SaveData.LifeTimerStartUnixMs)] = 0;
            save[nameof(SaveData.LevelInProgress)] = 0;
            save[nameof(SaveData.PlayerName)] = PlayerNameRules.Default;
            save[nameof(SaveData.TutorialsSeen)] = new JArray();
            save[nameof(SaveData.Version)] = 3;
        }

        /// <summary>Version 4 added the picked avatar; an older save gets the default one.</summary>
        private static void MigrateFromVersion3(JObject save)
        {
            save[nameof(SaveData.Avatar)] = AvatarRules.Default;
            save[nameof(SaveData.Version)] = 4;
        }

        /// <summary>Clamps values a hand-edited or stale save could have pushed out of range.</summary>
        private static SaveData Sanitize(SaveData save, PlayerRules rules, int levelCount)
        {
            save.CurrentLevel = Math.Clamp(save.CurrentLevel, 1, levelCount);
            save.Coins = Math.Max(0, save.Coins);
            save.Lives = Math.Clamp(save.Lives, 0, rules.MaxLives);
            save.LifeTimerStartUnixMs = Math.Max(0, save.LifeTimerStartUnixMs);
            save.UndoBoosters = Math.Max(0, save.UndoBoosters);
            save.ShuffleBoosters = Math.Max(0, save.ShuffleBoosters);
            save.AddTubeBoosters = Math.Max(0, save.AddTubeBoosters);
            save.RetryCount = Math.Max(0, save.RetryCount);
            save.LastVariant = Math.Clamp(save.LastVariant, 0, VariantSelector.VariantCount);
            if (save.LevelInProgress < 0 || save.LevelInProgress > levelCount)
            {
                save.LevelInProgress = 0;
            }

            save.PlayerName = PlayerNameRules.Validate(save.PlayerName) ?? PlayerNameRules.Default;
            if (!AvatarRules.IsValid(save.Avatar))
            {
                save.Avatar = AvatarRules.Default;
            }

            save.TutorialsSeen = save.TutorialsSeen.Where(id => id > 0).Distinct().OrderBy(id => id).ToArray();
            return save;
        }

        /// <summary>Every <see cref="SaveData"/> field must be present: a missing one is an error, not a silent zero.</summary>
        private sealed class RequireEveryPropertyResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                JsonProperty property = base.CreateProperty(member, memberSerialization);
                property.Required = Required.Always;
                return property;
            }
        }
    }
}
