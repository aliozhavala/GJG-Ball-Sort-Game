using System;
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
                }

                save = root.ToObject<SaveData>(JsonSerializer.Create(ReadSettings));
            }
            catch (JsonException e)
            {
                warning = $"Save is unreadable and was reset: {e.Message}";
                return SaveData.CreateDefault(rules);
            }

            return Sanitize(save, Math.Max(1, levelCount));
        }

        /// <summary>Version 2 added the tries at the current level (level variants); a version 1 save starts with none.</summary>
        private static void MigrateFromVersion1(JObject save)
        {
            save[nameof(SaveData.LevelTry)] = 0;
            save[nameof(SaveData.LastVariant)] = 0;
            save[nameof(SaveData.Version)] = 2;
        }

        /// <summary>Clamps values a hand-edited or stale save could have pushed out of range.</summary>
        private static SaveData Sanitize(SaveData save, int levelCount)
        {
            save.CurrentLevel = Math.Clamp(save.CurrentLevel, 1, levelCount);
            save.Coins = Math.Max(0, save.Coins);
            save.Lives = Math.Max(0, save.Lives);
            save.UndoBoosters = Math.Max(0, save.UndoBoosters);
            save.ShuffleBoosters = Math.Max(0, save.ShuffleBoosters);
            save.AddTubeBoosters = Math.Max(0, save.AddTubeBoosters);
            save.LevelTry = Math.Max(0, save.LevelTry);
            save.LastVariant = Math.Clamp(save.LastVariant, 0, VariantSelector.VariantCount);
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
