using System;
using BallSort.Core.Levels;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BallSort.LevelJson
{
    /// <summary>
    /// Reads level JSON into <see cref="LevelDto"/>s, strictly: unknown fields are errors so a typo
    /// in a hand-edited level fails loudly instead of being silently ignored.
    /// </summary>
    /// <remarks>
    /// Lives in its own engine-free assembly (Core + Newtonsoft only) so the game, editor tools and
    /// Core tests can all parse levels without depending on each other.
    /// </remarks>
    public static class LevelJsonParser
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Error,
            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
        };

        /// <summary>Parses one level file. Structural rules are checked later by <see cref="LevelValidator"/>.</summary>
        /// <param name="json">The file contents.</param>
        /// <param name="levelName">Display name used in error messages, e.g. <c>level_003</c>.</param>
        /// <exception cref="LevelFormatException">If the text is not valid JSON or contains unknown fields.</exception>
        public static LevelDto Parse(string json, string levelName)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new LevelFormatException(levelName, new[] { $"{levelName}: file is empty." });
            }

            try
            {
                return JsonConvert.DeserializeObject<LevelDto>(json, Settings);
            }
            catch (JsonException e)
            {
                // Newtonsoft messages already carry the JSON path and line number.
                throw new LevelFormatException(levelName, new[] { $"{levelName}: {e.Message}" });
            }
            catch (FormatException e)
            {
                throw new LevelFormatException(levelName, new[] { $"{levelName}: {e.Message}" });
            }
        }
    }
}
