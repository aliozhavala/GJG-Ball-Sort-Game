using System;
using BallSort.Core.Levels;

namespace BallSort.Core.Player
{
    /// <summary>
    /// Where the player is in the linear level run: the level to play next, whether the whole run
    /// has been finished, the retries at the current level, which pick each attempt's variant, and
    /// the level whose attempt is in progress.
    /// </summary>
    /// <remarks>
    /// Only winning the current level moves progress forward. Replays (the last level after the run
    /// is finished) never skip ahead or rewind. Every attempt that ends without a win and costs a life
    /// counts one retry, and the next attempt plays the next variant (docs/levels.md, Variants and
    /// retries); winning the current level, replays included, resets the retries for the level that
    /// comes next. Lives themselves are kept by <see cref="PlayerData"/>.
    /// </remarks>
    public sealed class LevelProgress
    {
        /// <summary>Number of levels in the game.</summary>
        public int LevelCount { get; }

        /// <summary>The level to play next, 1-based. Stays on the last level once all are complete.</summary>
        public int CurrentLevel { get; private set; }

        /// <summary>True once the last level has been won.</summary>
        public bool AllLevelsComplete { get; private set; }

        /// <summary>Attempts at the current level that cost a life since it was last won.</summary>
        public int RetryCount { get; private set; }

        /// <summary>The variant the latest attempt played, 1-based; 0 before the first attempt.</summary>
        public int LastVariant { get; private set; }

        /// <summary>
        /// The level whose attempt has started and not yet ended (won, or left with a life spent); 0 for
        /// none. Still set when the app starts means the app was closed mid-level.
        /// </summary>
        public int LevelInProgress { get; private set; }

        /// <param name="currentLevel">The level to play next, in 1..<paramref name="levelCount"/>.</param>
        /// <param name="allLevelsComplete">Whether the last level has already been won.</param>
        /// <param name="levelCount">Number of levels in the game; at least one.</param>
        public LevelProgress(int currentLevel, bool allLevelsComplete, int levelCount)
            : this(currentLevel, allLevelsComplete, levelCount, 0, 0, 0)
        {
        }

        /// <param name="currentLevel">The level to play next, in 1..<paramref name="levelCount"/>.</param>
        /// <param name="allLevelsComplete">Whether the last level has already been won.</param>
        /// <param name="levelCount">Number of levels in the game; at least one.</param>
        /// <param name="retryCount">Retries already counted at the current level; 0 or more.</param>
        /// <param name="lastVariant">The variant of the latest attempt, or 0 for none.</param>
        /// <param name="levelInProgress">The level of an attempt in progress, or 0 for none.</param>
        public LevelProgress(
            int currentLevel, bool allLevelsComplete, int levelCount, int retryCount, int lastVariant, int levelInProgress)
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

            if (retryCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(retryCount), retryCount, "The retry count cannot be negative.");
            }

            if (lastVariant != 0 && !VariantSelector.IsVariant(lastVariant))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lastVariant), lastVariant, $"The last variant must be 0 (none) or 1..{VariantSelector.VariantCount}.");
            }

            if (levelInProgress < 0 || levelInProgress > levelCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(levelInProgress), levelInProgress, $"The level in progress must be 0 (none) or 1..{levelCount}.");
            }

            LevelCount = levelCount;
            CurrentLevel = currentLevel;
            AllLevelsComplete = allLevelsComplete;
            RetryCount = retryCount;
            LastVariant = lastVariant;
            LevelInProgress = levelInProgress;
        }

        /// <summary>
        /// Starts an attempt at the current level: marks it in progress and picks its variant from the
        /// retries so far (see <see cref="VariantSelector"/>). Starting counts nothing by itself.
        /// </summary>
        /// <param name="random">Picks among the rotating variants after the first five attempts.</param>
        /// <returns>The variant to play, 1-based.</returns>
        public int StartAttempt(Random random)
        {
            // A count this high is not reachable by playing; saturate rather than wrap to a "first" attempt.
            int attempt = RetryCount < int.MaxValue ? RetryCount + 1 : int.MaxValue;
            LastVariant = VariantSelector.Select(attempt, LastVariant, random);
            LevelInProgress = CurrentLevel;
            return LastVariant;
        }

        /// <summary>Counts one retry: an attempt at the current level ended without a win and cost a life.</summary>
        public void RecordRetry()
        {
            if (RetryCount < int.MaxValue)
            {
                RetryCount++;
            }
        }

        /// <summary>Clears the level-in-progress mark: the attempt ended normally.</summary>
        public void ClearLevelInProgress() => LevelInProgress = 0;

        /// <summary>
        /// Settles an attempt the app was closed in the middle of: counts its retry (if it was at the
        /// current level) and clears the mark. Call once on launch; the caller spends the life.
        /// </summary>
        /// <returns>True when an attempt was in progress.</returns>
        public bool ResolveInterruptedAttempt()
        {
            if (LevelInProgress == 0)
            {
                return false;
            }

            if (LevelInProgress == CurrentLevel)
            {
                RecordRetry();
            }

            LevelInProgress = 0;
            return true;
        }

        /// <summary>
        /// Records that <paramref name="wonLevel"/> has been won: ends the attempt, and advances to the
        /// next level or marks the run complete after the last one. Winning the current level, a replay
        /// of the last one included, also resets the retries, so the next level starts at V1.
        /// </summary>
        /// <param name="wonLevel">The level that was just won, 1-based.</param>
        /// <returns>True when progress changed; false for a replay.</returns>
        public bool RecordWin(int wonLevel)
        {
            LevelInProgress = 0;
            if (wonLevel != CurrentLevel)
            {
                return false;
            }

            RetryCount = 0;
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
