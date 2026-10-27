using System;

namespace BallSort.Core.Player
{
    /// <summary>
    /// Where the player is in the linear level run: the level to play next, and whether the whole
    /// run has been finished.
    /// </summary>
    /// <remarks>
    /// Only winning the current level moves progress forward. Replays — the last level after the
    /// run is finished, or a level picked by an editor tool — never skip ahead or rewind.
    /// </remarks>
    public sealed class LevelProgress
    {
        /// <summary>Number of levels in the game.</summary>
        public int LevelCount { get; }

        /// <summary>The level to play next, 1-based. Stays on the last level once all are complete.</summary>
        public int CurrentLevel { get; private set; }

        /// <summary>True once the last level has been won.</summary>
        public bool AllLevelsComplete { get; private set; }

        /// <param name="currentLevel">The level to play next, in 1..<paramref name="levelCount"/>.</param>
        /// <param name="allLevelsComplete">Whether the last level has already been won.</param>
        /// <param name="levelCount">Number of levels in the game; at least one.</param>
        public LevelProgress(int currentLevel, bool allLevelsComplete, int levelCount)
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

            LevelCount = levelCount;
            CurrentLevel = currentLevel;
            AllLevelsComplete = allLevelsComplete;
        }

        /// <summary>
        /// Records that <paramref name="wonLevel"/> has been won: advances to the next level, or
        /// marks the run complete after the last one.
        /// </summary>
        /// <param name="wonLevel">The level that was just won, 1-based.</param>
        /// <returns>True when progress changed; false for a replay.</returns>
        public bool RecordWin(int wonLevel)
        {
            if (wonLevel != CurrentLevel || AllLevelsComplete)
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
