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

        /// <summary>
        /// The episode a win unlocked: the one the player's next level belongs to, when it is a later
        /// episode than the won level's. With first levels 1, 20, 40, …, winning 19 unlocks episode 1
        /// (0-based); winning 20, or the last level, unlocks nothing.
        /// </summary>
        /// <param name="firstLevels">Each episode's first level, as for <see cref="Find"/>.</param>
        /// <param name="wonLevel">The level just won.</param>
        /// <param name="nextLevel">The player's current level after the win; not after <paramref name="wonLevel"/> for a replay.</param>
        /// <returns>The unlocked episode's 0-based index, or -1 when the win unlocked none.</returns>
        public static int FindUnlocked(IReadOnlyList<int> firstLevels, int wonLevel, int nextLevel)
        {
            if (nextLevel <= wonLevel)
            {
                return -1;
            }

            int next = Find(firstLevels, nextLevel);
            return next > Find(firstLevels, wonLevel) ? next : -1;
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
