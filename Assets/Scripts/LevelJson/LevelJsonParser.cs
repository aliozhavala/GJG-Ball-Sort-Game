using System;
using BallSort.Core.Levels;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
        private static readonly NamingStrategy CamelCase = new CamelCaseNamingStrategy();

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Error,
            ContractResolver = new DefaultContractResolver { NamingStrategy = CamelCase },
            // "regular", "hard", "superHard"; a number is refused, so a tier is always readable in the file.
            Converters = { new StringEnumConverter(CamelCase, allowIntegerValues: false) },
        };

        // Reads only formatVersion: the rest of a file of another version is a different schema.
        private static readonly JsonSerializerSettings VersionSettings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ContractResolver = new DefaultContractResolver { NamingStrategy = CamelCase },
        };

        /// <summary>
        /// Parses one level file. A file whose <c>formatVersion</c> is not
        /// <see cref="LevelValidator.SupportedFormatVersion"/> is refused with that one error, before
        /// anything else is read. Structural rules are checked later by <see cref="LevelValidator"/>.
        /// </summary>
        /// <param name="json">The file contents.</param>
        /// <param name="levelName">Display name used in error messages, e.g. <c>level_003</c>.</param>
        /// <exception cref="LevelFormatException">If the text is not valid JSON, has an unsupported
        /// <c>formatVersion</c> or contains unknown fields.</exception>
        public static LevelDto Parse(string json, string levelName)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new LevelFormatException(levelName, new[] { $"{levelName}: file is empty." });
            }

            try
            {
                VersionProbe probe = JsonConvert.DeserializeObject<VersionProbe>(json, VersionSettings);
                string versionError = probe == null ? null : LevelValidator.DescribeUnsupportedFormatVersion(probe.FormatVersion);
                if (versionError != null)
                {
                    throw new LevelFormatException(levelName, new[] { $"{levelName}: {versionError}" });
                }

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

        private sealed class VersionProbe
        {
            public int FormatVersion { get; set; }
        }
    }
}
