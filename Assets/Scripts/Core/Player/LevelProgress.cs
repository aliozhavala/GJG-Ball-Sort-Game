using System;
using BallSort.Core.Levels;

namespace BallSort.Core.Player
{
    /// <summary>
    /// Where the player is in the linear level run: the level to play next, whether the whole run
    /// has been finished, and the tries at the current level, which pick each try's variant.
    /// </summary>
    /// <remarks>
    /// Only winning the current level moves progress forward. Replays (the last level after the run
    /// is finished) never skip ahead or rewind. A try starts on entering the level and on restart
    /// (docs/levels.md, Variants and tries); winning the current level, replays included, resets the
    /// tries for the level that comes next.
    /// </remarks>
    public sealed class LevelProgress
    {
        /// <summary>Number of levels in the game.</summary>
        public int LevelCount { get; }

        /// <summary>The level to play next, 1-based. Stays on the last level once all are complete.</summary>
        public int CurrentLevel { get; private set; }

        /// <summary>True once the last level has been won.</summary>
        public bool AllLevelsComplete { get; private set; }

        /// <summary>Tries started at the current level since it was last won; 0 before the first.</summary>
        public int TryCount { get; private set; }

        /// <summary>The variant the latest try played, 1-based; 0 before the first try.</summary>
        public int LastVariant { get; private set; }

        /// <param name="currentLevel">The level to play next, in 1..<paramref name="levelCount"/>.</param>
        /// <param name="allLevelsComplete">Whether the last level has already been won.</param>
        /// <param name="levelCount">Number of levels in the game; at least one.</param>
        public LevelProgress(int currentLevel, bool allLevelsComplete, int levelCount)
            : this(currentLevel, allLevelsComplete, levelCount, 0, 0)
        {
        }

        /// <param name="currentLevel">The level to play next, in 1..<paramref name="levelCount"/>.</param>
        /// <param name="allLevelsComplete">Whether the last level has already been won.</param>
        /// <param name="levelCount">Number of levels in the game; at least one.</param>
        /// <param name="tryCount">Tries already started at the current level; 0 or more.</param>
        /// <param name="lastVariant">The variant of the latest try, or 0 for none.</param>
        public LevelProgress(int currentLevel, bool allLevelsComplete, int levelCount, int tryCount, int lastVariant)
        {
            if (levelCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(levelCount), levelCount, "There must be at least one level.");
            }

            if (currentLevel < 1 || currentLevel > levelCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentLevel), currentLevel, $"The current level must be in 1..{levelCount}.");
            }

            if (tryCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tryCount), tryCount, "The try count cannot be negative.");
            }

            if (lastVariant != 0 && !VariantSelector.IsVariant(lastVariant))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lastVariant), lastVariant, $"The last variant must be 0 (none) or 1..{VariantSelector.VariantCount}.");
            }

            LevelCount = levelCount;
            CurrentLevel = currentLevel;
            AllLevelsComplete = allLevelsComplete;
            TryCount = tryCount;
            LastVariant = lastVariant;
        }

        /// <summary>
        /// Starts the next try at the current level and picks its variant (see <see cref="VariantSelector"/>).
        /// </summary>
        /// <param name="random">Picks among the rotating variants from the sixth try on.</param>
        /// <returns>The variant to play, 1-based.</returns>
        public int StartTry(Random random)
        {
            // A count this high is not reachable by playing; saturate rather than wrap to a "first" try.
            if (TryCount < int.MaxValue)
            {
                TryCount++;
            }

            LastVariant = VariantSelector.Select(TryCount, LastVariant, random);
            return LastVariant;
        }

        /// <summary>
        /// Records that <paramref name="wonLevel"/> has been won: advances to the next level, or
        /// marks the run complete after the last one. Winning the current level, a replay of the
        /// last one included, also resets the tries, so the next level starts at try 1.
        /// </summary>
        /// <param name="wonLevel">The level that was just won, 1-based.</param>
        /// <returns>True when progress changed; false for a replay.</returns>
        public bool RecordWin(int wonLevel)
        {
            if (wonLevel != CurrentLevel)
            {
                return false;
            }

            TryCount = 0;
            LastVariant = 0;
            if (AllLevelsComplete)
            {
                return false;
            }

            if (CurrentLevel < LevelCount)
            {
                CurrentLevel++;
            }
            else
            {
                AllLevelsComplete = true;
            }

            return true;
        }
    }
}
