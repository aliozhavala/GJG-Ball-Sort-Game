using System;
using System.Collections.Generic;

namespace BallSort.Core.Levels
{
    /// <summary>
    /// A level file could not be parsed or broke one or more format rules.
    /// </summary>
    /// <remarks>
    /// Carries every problem found, not just the first, so a designer fixing a hand-edited level
    /// sees the whole list at once.
    /// </remarks>
    public sealed class LevelFormatException : Exception
    {
        /// <summary>The level's display name, usually its file name.</summary>
        public string LevelName { get; }

        /// <summary>One message per problem, each already prefixed with <see cref="LevelName"/>.</summary>
        public IReadOnlyList<string> Errors { get; }

        public LevelFormatException(string levelName, IReadOnlyList<string> errors)
            : base(BuildMessage(levelName, errors))
        {
            LevelName = levelName;
            Errors = errors;
        }

        private static string BuildMessage(string levelName, IReadOnlyList<string> errors)
        {
            return $"Level '{levelName}' is invalid ({errors.Count} problem(s)):\n" + string.Join("\n", errors);
        }
    }
}
