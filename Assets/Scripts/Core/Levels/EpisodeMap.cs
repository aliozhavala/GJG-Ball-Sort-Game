using System.Collections.Generic;

namespace BallSort.Core.Levels
{
    /// <summary>
    /// Maps a level to its episode. Episodes are runs of consecutive levels, each listed by its first
    /// level; the current episode is always derived from a level number, never stored.
    /// </summary>
    public static class EpisodeMap
    {
        /// <summary>The episode <paramref name="level"/> belongs to: the last one whose first level is not after it.</summary>
        /// <param name="firstLevels">Each episode's first level, ascending, as checked by <see cref="Validate"/>.</param>
        /// <param name="level">A 1-based level number. Anything before the first episode (0 for a level file
        /// outside the run) maps to episode 0.</param>
        /// <returns>A 0-based episode index; 0 when <paramref name="firstLevels"/> is empty.</returns>
        public static int Find(IReadOnlyList<int> firstLevels, int level)
        {
            int episode = 0;
            for (int i = 1; i < firstLevels.Count && firstLevels[i] <= level; i++)
            {
                episode = i;
            }

            return episode;
        }

        /// <summary>Checks an episode list: at least one episode, the first starting at level 1, first levels strictly ascending.</summary>
        /// <returns>The first problem found, or null when the list is valid.</returns>
        public static string Validate(IReadOnlyList<int> firstLevels)
        {
            if (firstLevels == null || firstLevels.Count == 0)
            {
                return "There must be at least one episode.";
            }

            if (firstLevels[0] != 1)
            {
                return $"The first episode must start at level 1 (starts at {firstLevels[0]}).";
            }

            for (int i = 1; i < firstLevels.Count; i++)
            {
                if (firstLevels[i] <= firstLevels[i - 1])
                {
                    return $"Episode {i + 1} starts at level {firstLevels[i]}, not after episode {i}'s first level {firstLevels[i - 1]}.";
                }
            }

            return null;
        }
    }
}
